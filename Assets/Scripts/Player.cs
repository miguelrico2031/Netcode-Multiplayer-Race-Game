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



    [SerializeField] private TextMeshProUGUI _nameText;
    
    public override string ToString() => Name;

    private void Awake()
    {
        CurrentLap = new();
    }

    private void Start()
    {
        var goal = FindObjectOfType<GoalController>();
        goal.OnPlayerFinish += OnPlayerFinish;
    }

    private void OnDisable()
    {
        FindObjectOfType<GoalController>().OnPlayerFinish -= OnPlayerFinish;
    }


    public override void OnNetworkSpawn()
    {
        if (IsHost)
        {
            GameManager.Instance.RaceController.AddPlayer(this);
            CurrentLap.Value = 1;
        }
        if (IsOwner)
        {
            InputSystem.Instance.SetPlayer(this);
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();
            vcam.Follow = car.transform;
            vcam.LookAt = car.transform;

            FindObjectOfType<SpeedometerUI>().SetPlayerRb(car.GetComponent<Rigidbody>());

        }
        
        var id = GetComponent<NetworkObject>().OwnerClientId;
        foreach (var pi in GameManager.Instance.PlayerInfos)
        {
            if (pi.ID != id) continue;
            PlayerInfo = pi;
            break;
        }

        SetColorAndName();
        
        base.OnNetworkSpawn();
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

        _nameText.text = PlayerInfo.Name.Value;
    }

    private void OnPlayerFinish(Player p)
    {
        if (p == this)
        {
            GetComponent<InputController>().InputEnabled = false;
        }
    }
}