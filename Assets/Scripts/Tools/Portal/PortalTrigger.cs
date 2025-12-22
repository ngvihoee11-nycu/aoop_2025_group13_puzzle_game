using UnityEngine;

public class PortalTrigger : MonoBehaviour
{
    Portal parentPortal;

    void Awake()
    {
        if (parentPortal == null)
        {
            parentPortal = GetComponentInParent<Portal>();
            if (parentPortal == null)
            {
                Debug.LogError("PortalTrigger must be a child of a Portal!");
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        parentPortal.ChildTriggerEnter(other);
    }

    void OnTriggerExit(Collider other)
    {
        parentPortal.ChildTriggerExit(other);
    }
}