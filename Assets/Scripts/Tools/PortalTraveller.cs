using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public Vector3 prevOffsetFromPortal { get; set; }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        // This method can be overridden by derived classes to implement teleportation logic
        transform.position = pos;
        transform.rotation = rot;
    }
}

public class PortalTravellerSingleton<T> : PortalTraveller where T : PortalTraveller
{
    private static T _instance;

    public static T instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<T>();
                if (_instance == null)
                {
                    Debug.LogError("Cannot find " + typeof(T) + "!");
                }
            }
            return _instance;
        }
    }
}