using System.Collections.Generic;
using UnityEngine;

class LevelManager : Singleton<LevelManager>
{
    public Transform goalPoint;
    public int currentLevel = 1;
    public List<Portal> portals;

    void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Portal Traveller"), LayerMask.NameToLayer("Portal"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Clone Traveller"), LayerMask.NameToLayer("Portal"), true);

        if (goalPoint == null)
        {
            Debug.LogError("Goal Point is not assigned in LevelManager!");
        }

        if (goalPoint.GetComponent<Collider>() == null)
        {
            Debug.LogError("Goal Point does not have a Collider component!");
        }

        portals = new List<Portal>(FindObjectsOfType<Portal>());
    }

    public void AddPortal(Portal portal)
    {
        portals.Add(portal);
    }

    public void RemovePortal(Portal portal)
    {
        portals.Remove(portal);
    }

    public void OnPlayerArriveAtGoal()
    {
        Debug.Log("Player has reached the goal!");
        // Add your level completion logic here
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        // Add your level loading logic here
        Debug.Log("Loading Level: " + level);
    }
}