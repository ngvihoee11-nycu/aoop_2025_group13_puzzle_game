using System.Collections.Generic;
using UnityEngine;

public class PortalTraveller : MonoBehaviour
{
    public GameObject graphicsObject;
    public GameObject graphicsClone { get; set; }
    public Vector3 prevOffsetFromPortal { get; set; }

    public Material[] originalMaterials { get; set; }
    public Material[] cloneMaterials { get; set; }

    public virtual Collider GetCollider()
    {
        return GetComponent<Collider>();
    }

    public virtual void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        transform.position = pos;
        transform.rotation = rot;
    }

    public virtual void AdjustClone(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        graphicsClone.transform.position = pos;
        graphicsClone.transform.rotation = rot;
    }

    public virtual void EnterPortalTrigger()
    {
        if (graphicsClone == null)
        {
            graphicsClone = Instantiate(graphicsObject);
            graphicsClone.transform.parent = graphicsObject.transform.parent;
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
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
        // Disable slicing
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            originalMaterials[i].SetVector("_sliceNormal", Vector3.zero);
        }
    }

    public virtual void IgnoreCollision(Collider other, bool ignore)
    {
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
        {
            Physics.IgnoreCollision(col, other, ignore);
        }
    }

    public void SetOffsetDst(float dst, bool isClone)
    {
        for (int i = 0; i < originalMaterials.Length; i++)
        {
            if (isClone)
            {
                cloneMaterials[i].SetFloat("_sliceOffsetDst", dst);
            }
            else
            {
                originalMaterials[i].SetFloat("_sliceOffsetDst", dst);
            }
        }
    }

    protected Material[] GetMaterials(GameObject obj)
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
        return materials.ToArray();
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