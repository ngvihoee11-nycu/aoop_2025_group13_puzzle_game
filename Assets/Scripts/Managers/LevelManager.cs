using UnityEngine;

class LevelManager : Singleton<LevelManager>
{
    public Transform goalPoint;
    public int currentLevel = 1;

    void Start()
    {
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Player"), LayerMask.NameToLayer("Portal"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Clone Player"), LayerMask.NameToLayer("Portal"), true);
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