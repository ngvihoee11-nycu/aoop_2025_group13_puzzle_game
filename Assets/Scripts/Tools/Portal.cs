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

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;

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
                var positionOld = traveller.transform.position;
                var rotOld = traveller.transform.rotation;
                // Teleport the traveller to the linked portal
                traveller.Teleport(transform, linkedPortal.transform, m.GetPosition(), m.rotation);
                traveller.graphicsClone.transform.SetPositionAndRotation (positionOld, rotOld);
                linkedPortal.OnTravellerEnter(traveller); // Notify the linked portal of the traveller's entry
                trackedTravellers.RemoveAt(i);
            }
            else
            {
                traveller.graphicsClone.transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
                // Update the previous offset
                traveller.prevOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    void OnTravellerEnter(PortalTraveller traveller)
    {
        if (!trackedTravellers.Contains(traveller))
        {
            traveller.EnterPortalTrigger();
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
        if (traveller && trackedTravellers.Contains(traveller))
        {
            traveller.ExitPortalTrigger();
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
        // Display Inactive color if viewing back of the portal
        if (linkedPortal.SideOfPortal(playerCamera.transform.position) == -1)
        {
            linkedPortal.screen.material.SetInt("displayMask", 0);
            return;
        }

        CreateViewTexture();

        Matrix4x4 localToWorldMatrix = playerCamera.transform.localToWorldMatrix;
        Vector3[] renderPositions = new Vector3[recursionLimit];
        Quaternion[] renderRotations = new Quaternion[recursionLimit];

        int startIndex = 0;
        for (int i = 0; i < recursionLimit; i++)
        {
            if (i > 0)
            {
                // Stop recursion if the linked portal is not visible from the portal camera
                if (!CameraUtility.BoundsOverlap(screenMeshFilter, linkedPortal.screenMeshFilter, portalCamera)) break;
                // Stop recursion if viewing back of the portal
                if (SideOfPortal(portalCamera.transform.position) == -1) break;
            }

            localToWorldMatrix = transform.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * linkedPortal.transform.worldToLocalMatrix * localToWorldMatrix;
            int renderOrder = recursionLimit - 1 - i;
            renderPositions[renderOrder] = localToWorldMatrix.GetPosition();
            renderRotations[renderOrder] = localToWorldMatrix.rotation;

            portalCamera.transform.SetPositionAndRotation(renderPositions[renderOrder], renderRotations[renderOrder]);
            startIndex = renderOrder;
        }

        screen.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        linkedPortal.screen.material.SetInt("displayMask", 0);
        portalCamera.cullingMask = -1;

        for (int i = startIndex; i < recursionLimit; i++)
        {
            portalCamera.transform.SetPositionAndRotation(renderPositions[i], renderRotations[i]);
            SetNearClipPlane();

            if (PlayerController.instance.isFPP && i == recursionLimit - 1)
            {
                portalCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FPPHidePortal"));
            }

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

    public void PostPortalRender()
    {
        ProtectScreenFromClipping(playerCamera.transform.position);
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
    float ProtectScreenFromClipping(Vector3 viewPoint)
    {
        Vector3 playerPosition = PlayerController.instance.transform.position;
        Vector3 playerPositionAtViewPointHeight = new Vector3(playerPosition.x, viewPoint.y, playerPosition.z);
        float nearClipPlaneDist = Vector3.Dot (transform.forward, viewPoint - playerPositionAtViewPointHeight);

        float halfHeight = playerCamera.nearClipPlane * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCamera.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, nearClipPlaneDist).magnitude;
        float screenThickness = dstToNearClipPlaneCorner * 0.5f; // Cylinder height is 0.5 times this distance

        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - playerPosition) > 0;
        screenT.localScale = new Vector3(screenT.localScale.x, screenThickness, screenT.localScale.z);
        screenT.localPosition = Vector3.forward * screenThickness *(camFacingSameDirAsPortal ? 1f : -1f);
        return screenThickness;
    }

    // Use custom projection matrix to align portal camera's near clip plane with the surface of the portal
    // Note that this affects precision of the depth buffer, which can cause issues with effects like screenspace AO
    void SetNearClipPlane()
    {
        // Learning resource:
        // http://www.terathon.com/lengyel/Lengyel-Oblique.pdf
        Transform clipPlane = transform;
        int dot = System.Math.Sign(Vector3.Dot(clipPlane.forward, transform.position - portalCamera.transform.position));

        Vector3 camSpacePos = portalCamera.worldToCameraMatrix.MultiplyPoint (clipPlane.position);
        Vector3 camSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector (clipPlane.forward) * dot;
        float camSpaceDst = -Vector3.Dot (camSpacePos, camSpaceNormal) + nearClipOffset;

        // Don't use oblique clip plane if very close to portal as it seems this can cause some visual artifacts
        if (Mathf.Abs(camSpaceDst) > nearClipLimit)
        {
            Vector4 clipPlaneCameraSpace = new Vector4(camSpaceNormal.x, camSpaceNormal.y, camSpaceNormal.z, camSpaceDst);

            // Update projection based on new clip plane
            // Calculate matrix with player cam so that player camera settings (fov, etc) are used
            portalCamera.projectionMatrix = playerCamera.CalculateObliqueMatrix(clipPlaneCameraSpace);
        }
        else
        {
            portalCamera.projectionMatrix = playerCamera.projectionMatrix;
        }
    }

    int SideOfPortal(Vector3 pos)
    {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }

}
