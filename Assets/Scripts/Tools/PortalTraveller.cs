using System.Collections.Generic;
using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }
    public Vector3 prevOffsetFromPortal { get; set; }

    public Material[] originalMaterials { get; set; }
    public Material[] cloneMaterials { get; set; }
    
    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        // This method can be overridden by derived classes to implement teleportation logic
        transform.position = pos;
        transform.rotation = rot;
    }

    public virtual void EnterPortalTrigger()
    {
        if (graphicsClone == null)
        {
            graphicsClone = Instantiate(graphicsObject, graphicsObject.transform.position, graphicsObject.transform.rotation, graphicsObject.transform.parent);
            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        }
        else
        {
            graphicsClone.SetActive(true);
        }
    }

    public virtual void ExitPortalTrigger()
    {
        if (graphicsClone != null)
        {
            graphicsClone.SetActive(false);
        }
    }

    Material[] GetMaterials(GameObject obj)
    {
        MeshRenderer[] renderers = obj.GetComponentsInChildren<MeshRenderer>();
        List<Material> materials = new List<Material>();
        foreach (var renderer in renderers)
        {
            foreach (var mat in renderer.materials)
            {
                materials.Add(mat);
            }
        }
        return  materials.ToArray();
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