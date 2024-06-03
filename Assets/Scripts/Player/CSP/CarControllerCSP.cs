using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

public class CarControllerCSP : NetworkBehaviour, ICarController
{
    #region Variables

    [Header("Movement")] public List<AxleInfo> axleInfos;
    [SerializeField] private float forwardMotorTorque = 100000;
    [SerializeField] private float backwardMotorTorque = 50000;
    [SerializeField] private float maxSteeringAngle = 15;
    [SerializeField] private float engineBrake = 1e+12f;
    [SerializeField] private float footBrake = 1e+24f;
    [SerializeField] private float topSpeed = 200f;
    [SerializeField] private float downForce = 100f;
    [SerializeField] private float slipLimit = 0.2f;


    [SerializeField] private float _reconciliationDistanceThreshold;
    [SerializeField] private float _reconcileLerpSpeed;

    
    [SerializeField] private GameObject _serverClientPosPrefab;
    
    
    public float InputAcceleration { get; set; }
    public float InputSteering { get; set; }
    public float InputBrake { get; set; }
    public Transform GoalCheck { get; private set; }

    
    private float _currentRotation;
    private Rigidbody _rigidbody;
    private float _steerHelper = 0.8f;
    private bool _moveByInput = true;

    private bool _hasToReconcile;
    private Vector3 _reconcilePos;
    private Quaternion _reconcileRot;
    // private float _reconciliationT = 0f;
    private HashSet<int> _clientTicksWhileReconciling = new();

    private GameObject _serverClientPos;
    
    
    private const float EPS_SQ = float.Epsilon * float.Epsilon;
    private float _currentSpeed = 0;

    private float Speed
    {
        get => _currentSpeed;
        set
        {
            // if (Math.Abs(_currentSpeed - value) < float.Epsilon) return;
            if (Math.Pow(_currentSpeed - value, 2) < EPS_SQ) return;
            _currentSpeed = value;
            OnSpeedChangeEvent?.Invoke(_currentSpeed);
        }
    }

    public delegate void OnSpeedChangeDelegate(float newVal);

    public event OnSpeedChangeDelegate OnSpeedChangeEvent;
    
    
    
    //CSP tal

    private NetworkTimer _timer;
    private const float SERVER_TICK_RATE = 50f;
    private const int BUFFER_SIZE = 1024;
    
    //Cliente
    private CircularBuffer<StatePayload> _clientStateBuffer;
    private CircularBuffer<InputPayload> _clientInputBuffer;
    private StatePayload _lastServerState;
    private StatePayload _lastProcessedState;
    
    //Server
    private CircularBuffer<StatePayload> _serverStateBuffer;
    private Queue<InputPayload> _serverInputQueue;
    
    
    
    
    
    

    #endregion Variables

    #region Unity Callbacks

    public void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        GoalCheck = transform.Find("Goal Check");

        _timer = new(SERVER_TICK_RATE);
        _clientStateBuffer = new(BUFFER_SIZE);
        _clientInputBuffer = new(BUFFER_SIZE);
        _serverStateBuffer = new(BUFFER_SIZE);
        _serverInputQueue = new();

    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (!IsHost && !IsOwner)
        {
            _rigidbody.isKinematic = true;
        }

        if (IsHost && !IsOwner)
        {
            _serverClientPos = Instantiate(_serverClientPosPrefab);
            _serverClientPos.GetComponent<NetworkObject>().Spawn();
        }
    }

    public override void OnNetworkDespawn()
    {
    
        if (IsHost && !IsOwner)
        {
            Destroy(_serverClientPos);
        }
        base.OnNetworkDespawn();
    }

    public void Update()
    {
        Speed = _rigidbody.velocity.magnitude;
        
        _timer.Update(Time.deltaTime);
    }

    public void FixedUpdate()
    {
        if (!IsOwner && !IsHost) return;
        while (_timer.CanTick())
        {
            HandleClientTick();
            HandleServerTick();
        }
    }

    #endregion

    #region Methods


    private void HandleServerTick()
    {
        if (!IsHost) return;
        if (IsOwner) return;
        int bufferIndex = -1;

        //Debug.Log($"servidor inputs de cliente a procesar este frame: {_serverInputQueue.Count}");
        while (_serverInputQueue.Count > 0)
        {
            var inputPayload = _serverInputQueue.Dequeue();
            bufferIndex = inputPayload.Tick % BUFFER_SIZE;
            //var statePayload = SimulateMovement(inputPayload);
            var statePayload = ProcessMovement(inputPayload);
            _serverStateBuffer.Add(statePayload, bufferIndex);
            _serverClientPos.transform.position = statePayload.Position;
            _serverClientPos.transform.rotation = statePayload.Rotation;
        }

        if (bufferIndex == -1) return;
        SendToClientRpc(_serverStateBuffer.Get(bufferIndex));
    }

    [ClientRpc]
    private void SendToClientRpc(StatePayload statePayload)
    {
        if (!IsOwner) return;
        _lastServerState = statePayload;
    }

    private void HandleClientTick()
    {
        if (!IsOwner) return;
        int bufferIndex = _timer.Tick % BUFFER_SIZE;

        InputPayload inputPayload = new()
        {
            Tick = _timer.Tick,
            Acceleration = InputAcceleration,
            Steering = InputSteering,
            Brake = InputBrake
        };
        
        _clientInputBuffer.Add(inputPayload, bufferIndex);
        
        SendToServerRpc(inputPayload);

        if (_hasToReconcile)
        {
            _clientTicksWhileReconciling.Add(inputPayload.Tick);
        }
        StatePayload statePayload = ProcessMovement(inputPayload, _hasToReconcile);
        

        _clientStateBuffer.Add(statePayload, bufferIndex);
        
        if(!IsHost) HandleServerReconciliation();
    }
    private void HandleServerReconciliation()
    {
        //comprobacion de que sea un estado valido y no un default
        var isLastServerStateValid = !_lastServerState.IsDefault(); 
        //comprobacion de que el estado ha cambiado
        var isDifferentFromLastProcessed = _lastProcessedState.IsDefault() || !_lastProcessedState.Equals(_lastServerState);
  
        if (!isLastServerStateValid || !isDifferentFromLastProcessed) return;
        
        
        int bufferIndex = _lastServerState.Tick % BUFFER_SIZE;
        if(bufferIndex - 1 < 0) return; //no hay suficiente info
        
        var clientStateToCheck = _clientStateBuffer.Get(bufferIndex);
        if (clientStateToCheck.IsDefault()) return;

        if (_hasToReconcile || _clientTicksWhileReconciling.Contains(_lastServerState.Tick))
        {
            _reconcilePos = _lastServerState.Position;
            _reconcileRot = _lastServerState.Rotation;
            _lastProcessedState = _lastServerState;
            return;
        }
        
        float posError = Vector3.Distance(_lastServerState.Position, clientStateToCheck.Position);
        if (posError > _reconciliationDistanceThreshold)
        {
            // Debug.Log($"disparidad: dif de pos: {posError}");
            // Debug.Log($"TICKS: {clientStateToCheck.Tick} en cliente;   {_lastServerState.Tick} en server.");
            ReconcileState(_lastServerState);
        }

        _lastProcessedState = _lastServerState;
    }

    private void ReconcileState(StatePayload rewindState)
    {
        // Debug.Log("Reconciliando");
        // _rigidbody.position = rewindState.Position;
        // _rigidbody.rotation = rewindState.Rotation;
        _rigidbody.velocity = rewindState.Velocity;
        _rigidbody.angularVelocity = rewindState.AngularVelocity;
        _hasToReconcile = true;
        _reconcilePos = rewindState.Position;
        _reconcileRot = rewindState.Rotation;

        
        //_clientStateBuffer.Add(rewindState, rewindState.Tick);
        return;
        //if (!rewindState.Equals(_lastServerState))
        //{
        //    Debug.LogError("No coincide el estado restaurado con el ultimo del servidor. " +
        //                   "Puede ser porque se este ejecutando ReconcileState en el Host.");
        //    return;
        //}
        
        ////reprocesar todos los inputs que han pasado desde el estado que hemos corregido

        //int tickToReplay = _lastServerState.Tick;
        //while (tickToReplay < _timer.Tick) //hasta llegar al tick actual
        //{
        //    int bufferIndex = tickToReplay % BUFFER_SIZE;
        //    var statePayload = ProcessMovement(_clientInputBuffer.Get(bufferIndex));
        //    _clientStateBuffer.Add(statePayload, bufferIndex);
        //    tickToReplay++;
        //}
    }

    private void MoveToReconcile()
    {
        //Debug.Log("mover para reconciliar");
        _rigidbody.position = Vector3.Lerp(_rigidbody.position, _reconcilePos, Time.fixedDeltaTime * _reconcileLerpSpeed);
        _rigidbody.rotation = Quaternion.Lerp(_rigidbody.rotation, _reconcileRot, Time.fixedDeltaTime * _reconcileLerpSpeed);

        if (Vector3.Distance(_rigidbody.position, _reconcilePos) > .1f) return;
        _hasToReconcile = false;
        //Debug.Log("Reconciliacion completa");
    }
    
    

    [ServerRpc]
    private void SendToServerRpc(InputPayload inputPayload)
    {
        _serverInputQueue.Enqueue(inputPayload);
    }
    
    
    private StatePayload ProcessMovement(InputPayload inputPayload, bool reconcile = false)
    {
        Move(inputPayload.Acceleration, inputPayload.Steering, inputPayload.Brake);
        if(reconcile) MoveToReconcile();
        return new ()
        {
            Tick = inputPayload.Tick,
            Position = _rigidbody.position,
            Rotation = _rigidbody.rotation,
            Velocity = _rigidbody.velocity,
            AngularVelocity = _rigidbody.angularVelocity
        };
    }

    private void Move(float inputAcceleration, float inputSteering, float inputBrake)   
    {
        if (_moveByInput)
        {
            InputSteering = Mathf.Clamp(inputSteering, -1, 1);
            InputAcceleration = Mathf.Clamp(inputAcceleration, -1, 1);
            InputBrake = Mathf.Clamp(inputBrake, 0, 1);
        }
        else InputSteering = InputAcceleration = InputBrake = 0f;

        // var targetSpeed = s_MaxSpeed * InputAcceleration;
        // var forward = transform.forward;
        // forward.y = 0f;
        //
        // var t = _timer.MinTimeBetweenTicks / (1f / Time.deltaTime);
        // var ft = Time.fixedDeltaTime;
        // Debug.Log($"fixed: {ft} , tick: {t}");
        //
        // // _rigidbody.velocity = Vector3.Lerp(_rigidbody.velocity, targetSpeed * forward.normalized, t);
        // _rigidbody.angularVelocity = s_TurnSpeed * ft * InputSteering * transform.up;
        //
        // if (InputBrake > 0f) InputAcceleration = 0f;
        // _rigidbody.velocity = s_MaxSpeed * ft * InputAcceleration * forward.normalized;
        //
        // return;
        float steering = maxSteeringAngle * InputSteering;

        foreach (AxleInfo axleInfo in axleInfos)
        {
            if (axleInfo.steering)
            {
                axleInfo.leftWheel.steerAngle = steering;
                axleInfo.rightWheel.steerAngle = steering;
            }

            if (axleInfo.motor)
            {
                if (InputAcceleration > float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = forwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = forwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (InputAcceleration < -float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.leftWheel.brakeTorque = 0;
                    axleInfo.rightWheel.motorTorque = -backwardMotorTorque;
                    axleInfo.rightWheel.brakeTorque = 0;
                }

                if (Math.Abs(InputAcceleration) < float.Epsilon)
                {
                    axleInfo.leftWheel.motorTorque = 0;
                    axleInfo.leftWheel.brakeTorque = engineBrake;
                    axleInfo.rightWheel.motorTorque = 0;
                    axleInfo.rightWheel.brakeTorque = engineBrake;
                }

                if (InputBrake > 0)
                {
                    axleInfo.leftWheel.brakeTorque = footBrake;
                    axleInfo.rightWheel.brakeTorque = footBrake;
                }
            }

            ApplyLocalPositionToVisuals(axleInfo.leftWheel);
            ApplyLocalPositionToVisuals(axleInfo.rightWheel);
        }

        SteerHelper();
        SpeedLimiter();
        AddDownForce();
        TractionControl();

        if (IsHost)
        {
            ServerSetGuestCarTransform(_rigidbody.position, _rigidbody.rotation);
        }
    }
    

    // crude traction control that reduces the power to wheel if the car is wheel spinning too much
    private void TractionControl()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit wheelHitLeft;
            WheelHit wheelHitRight;
            axleInfo.leftWheel.GetGroundHit(out wheelHitLeft);
            axleInfo.rightWheel.GetGroundHit(out wheelHitRight);

            if (wheelHitLeft.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitLeft.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.leftWheel.motorTorque -= axleInfo.leftWheel.motorTorque * howMuchSlip * slipLimit;
            }

            if (wheelHitRight.forwardSlip >= slipLimit)
            {
                var howMuchSlip = (wheelHitRight.forwardSlip - slipLimit) / (1 - slipLimit);
                axleInfo.rightWheel.motorTorque -= axleInfo.rightWheel.motorTorque * howMuchSlip * slipLimit;
            }
        }
    }

// this is used to add more grip in relation to speed
    private void AddDownForce()
    {
        foreach (var axleInfo in axleInfos)
        {
            axleInfo.leftWheel.attachedRigidbody.AddForce(
                -transform.up * (downForce * axleInfo.leftWheel.attachedRigidbody.velocity.magnitude));
        }
    }

    private void SpeedLimiter()
    {
        float speed = _rigidbody.velocity.magnitude;
        if (speed > topSpeed)
            _rigidbody.velocity = topSpeed * _rigidbody.velocity.normalized;
    }

// finds the corresponding visual wheel
// correctly applies the transform
    private void ApplyLocalPositionToVisuals(WheelCollider col)
    {
        if (col.transform.childCount == 0)
        {
            return;
        }

        Transform visualWheel = col.transform.GetChild(0);

        col.GetWorldPose(out var position, out var rotation);
        var myTransform = visualWheel.transform;
        myTransform.position = position;
        myTransform.rotation = rotation;
    }

    private void SteerHelper()
    {
        foreach (var axleInfo in axleInfos)
        {
            WheelHit[] wheelHit = new WheelHit[2];
            axleInfo.leftWheel.GetGroundHit(out wheelHit[0]);
            axleInfo.rightWheel.GetGroundHit(out wheelHit[1]);
            foreach (var wh in wheelHit)
            {
                if (wh.normal == Vector3.zero)
                    return; // wheels arent on the ground so dont realign the rigidbody velocity
            }
        }

// this if is needed to avoid gimbal lock problems that will make the car suddenly shift direction
        if (Mathf.Abs(_currentRotation - transform.eulerAngles.y) < 10f)
        {
            var turnAdjust = (transform.eulerAngles.y - _currentRotation) * _steerHelper;
            Quaternion velRotation = Quaternion.AngleAxis(turnAdjust, Vector3.up);
            _rigidbody.velocity = velRotation * _rigidbody.velocity;    
        }

        _currentRotation = transform.eulerAngles.y;
    }

    #endregion


    public void RepositionCar(Action onRepositionedCallback)
    {
        StartCoroutine(DisableCarAndReposition(onRepositionedCallback));
    }

    private IEnumerator DisableCarAndReposition(Action onRepositionedCallback)
    {
        _moveByInput = false;
        var circuitController = GameManager.Instance.RaceController.CircuitController;
        circuitController.ComputeClosestPointArcLength(transform.position, out var segIdx, out _, out _);
        var newPosition = circuitController.GetPoint(segIdx) + Vector3.up * 1.5f;
        var newDirection = circuitController.GetSegment(segIdx);
        var newRotation = Quaternion.LookRotation(newDirection);
        _rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
        SetOwnerCarRigidbodyInClientRpc(_rigidbody.position, _rigidbody.rotation, _rigidbody.velocity, _rigidbody.angularVelocity);
        ServerSetGuestCarTransform(_rigidbody.position, _rigidbody.rotation);
        yield return new WaitForSeconds(1.5f);

        _moveByInput = true;
        onRepositionedCallback();
    }

    public void ServerSetCarTransform(Vector3 pos, Quaternion rot) //solo se deberia llamar en el server/host
    {
        if (!IsHost) return;
        _rigidbody.position = pos;
        _rigidbody.rotation = rot;
        SetCarTransformInClientRpc(pos, rot);
    }

    public void ShowOverturnText()
    {
        ShowOverturnTextClientRpc();
    }
    
    public void HideOverturnText()
    {
        HideOverturnTextClientRpc();
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetCarTransformInClientRpc(Vector3 pos, Quaternion rot)
    {
        _rigidbody.position = pos;
        _rigidbody.rotation = rot;
    }

    private void ServerSetGuestCarTransform(Vector3 pos, Quaternion rot)
    {
        if (!IsHost) return;
        SetGuestCarTransformInClientRpc(pos, rot);
    }

    [ClientRpc(RequireOwnership = false)]
    private void SetGuestCarTransformInClientRpc(Vector3 pos, Quaternion rot)
    {
        if (IsOwner) return;
        _rigidbody.position = pos;
        _rigidbody.rotation = rot;
    }
    
    [ClientRpc(RequireOwnership = false)]
    private void SetOwnerCarRigidbodyInClientRpc(Vector3 pos, Quaternion rot, Vector3 vel, Vector3 angularVel)
    {
        if (!IsOwner) return;
        _rigidbody.position = pos;
        _rigidbody.rotation = rot;
        _rigidbody.velocity = vel;
        _rigidbody.angularVelocity = angularVel;
    }

    [ClientRpc]
    private void ShowOverturnTextClientRpc()
    {
        if (IsOwner)
            GameManager.Instance.HUD.ShowResetText();
    }
    
    [ClientRpc]
    private void HideOverturnTextClientRpc()
    {
        if (IsOwner)
            GameManager.Instance.HUD.HideResetText();
    }
}