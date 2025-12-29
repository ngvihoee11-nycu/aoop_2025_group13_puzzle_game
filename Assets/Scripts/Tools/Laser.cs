using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public Vector3 startPosition;
    public Collider exitingCollider;

    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Laser hit: " + other.name);
        if (other.CompareTag("Player"))
        {
            LevelManager.instance.ResetPlayerPosition(other.transform);
        }
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

    void OnTriggerExit(Collider other)
    {
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

}
