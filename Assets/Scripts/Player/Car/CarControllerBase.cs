using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CarControllerBase : NetworkBehaviour, ICarController
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

    public float InputAcceleration { get; set; }
    public float InputSteering { get; set; }
    public float InputBrake { get; set; }
    public Transform GoalCheck { get; private set; }

    
    private float _currentRotation;
    private Rigidbody _rigidbody;
    private float _steerHelper = 0.8f;
    private bool _moveByInput = true;


    
    
    
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

    #endregion Variables

    #region Unity Callbacks

    public void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
        GoalCheck = transform.Find("Goal Check");
    }

    public void Update()
    {
        Speed = _rigidbody.velocity.magnitude;
    }

    public void FixedUpdate()
    {
        if (!IsSpawned || !IsServer) return;

        if (_moveByInput)
        {
            InputSteering = Mathf.Clamp(InputSteering, -1, 1);
            InputAcceleration = Mathf.Clamp(InputAcceleration, -1, 1);
            InputBrake = Mathf.Clamp(InputBrake, 0, 1);
        }
        else InputSteering = InputAcceleration = InputBrake = 0f;
        


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
        
        // if(steering != 0) Debug.Break();
    }

    #endregion

    #region Methods

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
        // transform.SetPositionAndRotation(newPosition, newRotation);
        _rigidbody.velocity = _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.MovePosition(newPosition);
        _rigidbody.MoveRotation(newRotation);
        yield return new WaitForSeconds(1.5f);

        _moveByInput = true;
        onRepositionedCallback();
    }

    public void ServerSetCarTransform(Vector3 pos, Quaternion rot)
    {
        _rigidbody.position = pos;
        _rigidbody.rotation = rot;
    }

}