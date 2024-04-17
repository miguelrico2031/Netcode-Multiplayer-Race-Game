using Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : NetworkBehaviour
{
    // Player Info
    public string Name { get; set; }
    public int ID { get; set; }

    // Race Info
    public GameObject car;
    public int CurrentPosition { get; set; }
    public int CurrentLap { get; set; }

    public override string ToString()
    {
        return Name;
    }

    public override void OnNetworkSpawn()
    {
        if(IsServer) GameManager.Instance.CurrentRace.AddPlayer(this);
        if (IsOwner)
        {
            InputSystem.Instance.SetPlayer(this);
            var vcam = FindObjectOfType<CinemachineVirtualCamera>();
            vcam.Follow = car.transform;
            vcam.LookAt = car.transform;
        }
        
        base.OnNetworkSpawn();
    }
}