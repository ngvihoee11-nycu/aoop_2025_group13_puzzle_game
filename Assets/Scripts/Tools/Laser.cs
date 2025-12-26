using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

    void OnTriggerExit(Collider other)
    {
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }
}
