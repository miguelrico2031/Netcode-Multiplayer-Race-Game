using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OverturnCheck : MonoBehaviour
{
    [SerializeField] private string[] _environmentTags;
    [SerializeField] private Vector3 _overlapBoxSize;
    [SerializeField] private float _checkPeriod;

    private Vector3 _overlapBoxHalfSize;
    private NetworkManager _networkManager;
    private float _timer = 0f;
    private Collider[] _overlaps = new Collider[10];
    private HashSet<string> _checkTags;
    private bool _doChecking = true;
    private ICarController _carController;
    private Vector3 _lastPos;
    private int _checksSincePosChanged = 0;
    
    private void Start()
    {
        _overlapBoxHalfSize = _overlapBoxSize / 2f;
        _networkManager = NetworkManager.Singleton;
        _checkTags = new(_environmentTags);
        _carController = GetComponentInParent<ICarController>();
    }

    private void Update()
    {
        if (!_networkManager.IsHost || !_doChecking) return;

        _timer += Time.deltaTime;
        if (_timer < _checkPeriod) return;
        _timer = 0f;
        CheckOverturn();

    }
    

    private void CheckOverturn() //codigo de servidor
    {
        int length = Physics.OverlapBoxNonAlloc(transform.position, _overlapBoxHalfSize, _overlaps, transform.rotation);
        for (int i = 0; i < length; i++)
        {
            if (!_checkTags.Contains(_overlaps[i].tag)) continue;
            
            HandleOverturn();
            return;
        }

        if (transform.position != _lastPos)
        {
            _lastPos = transform.position;
            _checksSincePosChanged = 0;
        }

        // else if (++_checksSincePosChanged >= 6)
        // {
        //     _checksSincePosChanged = 0;
        //     HandleOverturn();
        // }
    }

    private void HandleOverturn()
    {
        _doChecking = false;
        _checksSincePosChanged = 0;
        _carController.RepositionCar(() => _doChecking = true);
    }
}
