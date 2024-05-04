using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class NameTextOrientation : MonoBehaviour
{
    private Transform _cam;

    private void Awake()
    {
        _cam = FindObjectOfType<CinemachineVirtualCamera>().transform;
    }

    private void LateUpdate()
    {
        transform.rotation = Quaternion.LookRotation(_cam.forward, _cam.up);
    }
}
