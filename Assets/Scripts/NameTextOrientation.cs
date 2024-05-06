using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

public class NameTextOrientation : MonoBehaviour
{
    public Transform Cam;


    private void LateUpdate()
    {
        if (!Cam) return;
        transform.rotation = Quaternion.LookRotation(Cam.forward, Cam.up);
    }
}
