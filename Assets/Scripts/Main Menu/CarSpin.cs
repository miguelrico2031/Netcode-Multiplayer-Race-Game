using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarSpin : MonoBehaviour
{
    [SerializeField] private float _turnSpeed;

    void Update()
    {
        transform.Rotate(Vector3.up, Time.deltaTime * _turnSpeed);
    }
}
