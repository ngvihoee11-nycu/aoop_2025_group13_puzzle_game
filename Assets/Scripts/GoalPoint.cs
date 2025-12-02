using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoalPoint : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("GoalPoint triggered by: " + other.gameObject.name);
        if (other.gameObject.CompareTag("Player"))
        {
            LevelManager.instance.OnPlayerArriveAtGoal();
        }
    }
}
