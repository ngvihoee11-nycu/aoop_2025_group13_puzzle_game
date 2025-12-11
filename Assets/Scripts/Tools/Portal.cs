using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public GameObject attachedSurface;
    public Portal linkedPortal;
    public MeshRenderer screen;

    RenderTexture viewTexture;
    Camera portalCamera;
    Camera playerCamera;
    List<PortalTraveller> trackedTravellers = new List<PortalTraveller>();
    MeshFilter screenMeshFilter;
    
    void Awake()
    {
        //playerCamera = Camera.main;
        //portalCamera = GetComponentInChildren<Camera>();
        //portalCamera.enabled = false;
        //screenMeshFilter = screen.GetComponent<MeshFilter>();
        //screen.material.SetInt("displayMask", 1);
    }

    void LateUpdate()
    {
        HandleTravellers();
    }
    
    void HandleTravellers()
    {
        for (int i = trackedTravellers.Count - 1; i >= 0; i--)
        {
            PortalTraveller traveller = trackedTravellers[i];
            Matrix4x4 m = linkedPortal.transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * transform.worldToLocalMatrix * traveller.transform.localToWorldMatrix;

            Vector3 offsetFromPortal = traveller.transform.position - transform.position;
            int portalSide = System.Math.Sign(Vector3.Dot(transform.forward, offsetFromPortal));
            int prevPortalSide = System.Math.Sign(Vector3.Dot(transform.forward, traveller.prevOffsetFromPortal));

            // Check if the traveller has crossed the portal plane
            if (portalSide == -1 && prevPortalSide == 1)
            {
                // Teleport the traveller to the linked portal
                traveller.Teleport(transform, linkedPortal.transform, m.GetPosition() /*remove this after implementing velocity*/ + linkedPortal.transform.forward * 0.1f, m.rotation);
                linkedPortal.OnTravellerEnter(traveller); // Notify the linked portal of the traveller's entry
                trackedTravellers.RemoveAt(i);
            }
            else
            {
                // Update the previous offset
                traveller.prevOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    void OnTravellerEnter(PortalTraveller traveller)
    {
        if (!trackedTravellers.Contains(traveller))
        {
            traveller.prevOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add(traveller);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
        {
            OnTravellerEnter(traveller);
        }
    }

    void OnTriggerExit(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
        {
            trackedTravellers.Remove(traveller);
        }
    }

}