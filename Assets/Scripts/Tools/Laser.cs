using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Laser triggered: " + other.gameObject.name);
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

    void OnTriggerExit(Collider other)
    {
        Debug.Log("Laser exited: " + other.gameObject.name);
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }
}
