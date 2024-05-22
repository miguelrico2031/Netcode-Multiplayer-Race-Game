using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using System;
using TMPro;

public class Player : NetworkBehaviour
{
    // Player Info
    public string Name { get; set; }
    public ulong ID { get; set; }
    public int IndexInRaceController { get; set; }

    // Race Info
    public GameObject car;
    public int CurrentPosition { get; set; }
    public NetworkVariable<int> CurrentLap;
    
    public int StartOrder { get; set; }
    [HideInInspector] public PlayerInfo PlayerInfo { get; private set; }
    
    public enum PlayerColor { Red, Blue, Green }
    [Serializable] public class CarColor
    {
        public PlayerColor Color;
        public Material Material;
    }

    [field:SerializeField] public CarColor[] CarColors { get; private set; }

    public NetworkVariable<float> ArcLength; //esta variable se actualiza cada segundo
    private int _secondsGoingBackwards = 0;
    private bool _backwardsTextEnabled = false;


    [SerializeField] private TextMeshProUGUI _nameText;
    
    public override string ToString() => Name;

    private void Awake()
    {
        CurrentLap = new();
        ArcLength = new ();
    }


    private void OnDisable()
    {
        FindObjectOfType<GoalController>().OnPlayerFinish -= OnPlayerFinish;
    }


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsHost)
        {
            GameManager.Instance.RaceController.AddPlayer(this);
            CurrentLap.Value = 0;
            ArcLength.Value = 0f;
            var goal = FindObjectOfType<GoalController>(true);
            goal.OnPlayerFinish += OnPlayerFinish;

            foreach (var checkpoint in FindObjectsOfType<Checkpoint>())
            {
                checkpoint.CheckPlayer(this);
            }
        }

        if (IsOwner)
        {
            ArcLength.OnValueChanged += OnArcLengthChanged;
        }


        var vcam = FindObjectOfType<CinemachineVirtualCamera>();
        GetComponentInChildren<NameTextOrientation>().Cam = vcam.transform;
        
        
        if (IsOwner)
        {
            InputSystem.Instance.SetPlayer(this);
            vcam.Follow = car.transform;
            vcam.LookAt = car.transform;

            var speedo = FindObjectOfType<SpeedometerUI>();
            var rb = car.GetComponent<Rigidbody>();
            speedo.SetPlayerRb(rb);
        }



        var id = GetComponent<NetworkObject>().OwnerClientId;
        foreach (var pi in GameManager.Instance.PlayerInfos)
        {
            if (pi.ID != id) continue;
            PlayerInfo = pi;
            break;
        }

        SetColorAndName();
        
    }

    private void SetColorAndName()
    {
        foreach (var cc in CarColors)
        {
            if (cc.Color != PlayerInfo.Color) continue;
            var carRend = car.transform.Find("body").GetComponent<MeshRenderer>();
            var mats = carRend.materials;
            mats[1] = cc.Material;
            carRend.materials = mats;
            break;
        }

        Name = _nameText.text = PlayerInfo.Name.Value;
    }

    private void OnPlayerFinish(Player p)
    {
        if (p == this)
        {
            GetComponent<IInputController>().InputEnabled = false;
        }
    }
    
    private void OnArcLengthChanged(float previous, float newArc)
    {
        Debug.Log("arcotal");
        if (previous > newArc)
        {
            Debug.Log("arco patras");
            if (++_secondsGoingBackwards > 3 && !_backwardsTextEnabled)
            {
                GameManager.Instance.HUD.ShowBackwardsText();
                _backwardsTextEnabled = true;
            }
        }
        else if(_backwardsTextEnabled)
        {
            Debug.Log("arco bien");
            _secondsGoingBackwards = 0;
            _backwardsTextEnabled = false;
            GameManager.Instance.HUD.HideBackwardsText();
        }
    }
}