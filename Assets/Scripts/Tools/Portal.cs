using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public GameObject attachedSurface;
    public Portal linkedPortal;
    public MeshRenderer screen;
    public int recursionLimit = 3;

    RenderTexture viewTexture;
    Camera portalCamera;
    Camera playerCamera;
    List<PortalTraveller> trackedTravellers;
    MeshFilter screenMeshFilter;
    
    void Awake()
    {
        playerCamera = Camera.main;
        portalCamera = GetComponentInChildren<Camera>();
        portalCamera.enabled = false;
        trackedTravellers = new List<PortalTraveller>();
        screenMeshFilter = screen.GetComponent<MeshFilter>();
        screen.material.SetInt("displayMask", 1);
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
                traveller.Teleport(transform, linkedPortal.transform, m.GetPosition(), m.rotation);
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

    public void PrePortalRender()
    {
    }

    public void Render(ScriptableRenderContext SRC)
    {

        // Skip rendering if linked portal is not set
        if (linkedPortal == null) return;
        // Skip rendering if not visible to player camera
        if (!CameraUtility.VisibleFromCamera(linkedPortal.screen, playerCamera)) return;


        CreateViewTexture();

        Matrix4x4 localToWorldMatrix = playerCamera.transform.localToWorldMatrix;
        Vector3[] renderPositions = new Vector3[recursionLimit];
        Quaternion[] renderRotations = new Quaternion[recursionLimit];

        int startIndex = 0;
        for (int i = 0; i < recursionLimit; i++)
        {
            if (i > 0)
            {
                // Check if the portal is visible from the portal camera
                if (!CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCamera)) break; 
            }

            localToWorldMatrix = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrder = recursionLimit - 1 - i;
            renderPositions[renderOrder] = localToWorldMatrix.GetPosition();
            renderRotations[renderOrder] = localToWorldMatrix.rotation;
            startIndex = renderOrder;
        }

        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        linkedPortal.screen.material.SetInt("displayMask", 0);

        for (int i = startIndex; i < recursionLimit; i++)
        {
            portalCamera.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);

            // Warning says RenderSingleCamera is obsolete, but the alternative is broken in current version
            #pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(SRC, portalCamera);
            #pragma warning restore CS0618

            if (i == startIndex)
            {
                linkedPortal.screen.material.SetInt("displayMask", 1);
            }
        }
        
        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
    }

    public void PostPortalRender () {
        //foreach (PortalTraveller traveller in trackedTravellers) {
        //    UpdateSliceParams (traveller);
        //}
        ProtectScreenFromClipping (playerCamera.transform.position);
    }

    void CreateViewTexture()
    {
        // This method can be used to create the render texture for the portal view
        if (viewTexture == null || viewTexture.width != Screen.width || viewTexture.height != Screen.height)
        {
            if (viewTexture)
            {
                viewTexture.Release();
            }
            viewTexture = new RenderTexture(Screen.width, Screen.height, 0);
            portalCamera.targetTexture = viewTexture;
            linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
        }
    }

    // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
    float ProtectScreenFromClipping (Vector3 viewPoint)
    {
        Vector3 playerPosition = PortalTravellerSingleton<PlayerController>.instance.transform.position;
        Vector3 playerPositionAtViewPointHeight = new Vector3 (playerPosition.x, viewPoint.y, playerPosition.z);
        float nearClipPlaneDist = Vector3.Dot (transform.forward, viewPoint - playerPositionAtViewPointHeight);

        float halfHeight = playerCamera.nearClipPlane * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCamera.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, nearClipPlaneDist).magnitude;
        float screenThickness = dstToNearClipPlaneCorner * 0.5f; // Cylinder height is 0.5 times this distance

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot (transform.forward, transform.position - playerPosition) > 0;
        screenT.localScale = new Vector3 (screenT.localScale.x, screenThickness, screenT.localScale.z);
        screenT.localPosition = Vector3.forward * screenThickness *(camFacingSameDirAsPortal ? 1f : -1f);
        return screenThickness;
    }

}
