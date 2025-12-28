using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;

public class Portal : MonoBehaviour
{
    [Header("Main Settings")]
    public Collider attachedSurface;
    public Portal linkedPortal;
    public Collider screenCollider;
    public MeshRenderer screen;
    public MeshRenderer frame;
    public GameObject trigger;
    public int recursionLimit = 3;
    public bool isSecondPortal = false;
    public static float spawnOffset = 0.02f;

    [Header("Advanced Settings")]
    public float nearClipOffset = 0.05f;
    public float nearClipLimit = 0.2f;
    public float forceExitThicknessRatio = 3f;


    RenderTexture viewTexture;
    Camera portalCamera;
    Camera playerCamera;
    [SerializeField] List<PortalTraveller> trackedTravellers;
    MeshFilter screenMeshFilter;

    public static Portal SpawnPortal(GameObject portalPrefab, Portal linkedPortal, RaycastHit hit, Transform eyeT, bool isSecondPortal)
    {
        Portal newPortal = Instantiate(portalPrefab).GetComponent<Portal>();
        if (linkedPortal)
        {
            newPortal.linkedPortal = linkedPortal;
            newPortal.linkedPortal.linkedPortal = newPortal;
        }
        newPortal.transform.SetPositionAndRotation(hit.point + hit.normal * spawnOffset, Quaternion.LookRotation(hit.normal, (Mathf.Abs(hit.normal.x) < 0.001f && Mathf.Abs(hit.normal.z) < 0.001f) ? eyeT.up : Vector3.up));
        newPortal.attachedSurface = hit.collider;
        newPortal.isSecondPortal = isSecondPortal;
        newPortal.UpdateFrameColor();
        return newPortal;
    }
    
    void Awake()
    {
        portalCamera = GetComponentInChildren<Camera>();
        portalCamera.enabled = false;
        trackedTravellers = new List<PortalTraveller>();
        screenMeshFilter = screen.GetComponent<MeshFilter>();
        screen.material.SetInt("displayMask", 1);
        UpdateFrameColor();
    }

    void Start()
    {
        playerCamera = MainCamera.instance.GetCamera();
        MainCamera.instance.AddPortal(this);
    }

    void Update()
    {
        if (linkedPortal)
        {
            CheckTravellers();
        }
    }

    void CheckTravellers()
    {
        for (int i = trackedTravellers.Count - 1; i >= 0; i--)
        {
            PortalTraveller traveller = trackedTravellers[i];
            Collider travellerCollider = traveller.GetCollider();
            if (travellerCollider && Vector3.Dot(transform.forward, traveller.prevOffsetFromPortal) > trigger.transform.localScale.z)
            {
                Vector3 checkRange = new Vector3(transform.localScale.x * 0.5f, transform.localScale.y * 0.5f, trigger.transform.localScale.z * forceExitThicknessRatio);
                List<Collider> colliders = Physics.OverlapBox(transform.position, checkRange, transform.rotation).ToList();
                if (!colliders.Contains(travellerCollider))
                {
                    Debug.Log("Forced exit!");
                    OnTravellerExit(traveller);
                }
            }
        }
    }

    void LateUpdate()
    {
        if (linkedPortal)
        {
            trigger.SetActive(true);
            HandleTravellers();
        }
        else
        {
            trigger.SetActive(false);
        }
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
                traveller.AdjustClone(linkedPortal.transform, transform, positionOld, rotOld);
                OnTravellerExit(traveller, true);
                linkedPortal.OnTravellerEnter(traveller); // Notify the linked portal of the traveller's entry
                linkedPortal.ProtectScreenFromClipping(playerCamera.transform.position);
            }
            else
            {
                traveller.AdjustClone(transform, linkedPortal.transform, m.GetPosition(), m.rotation);
                // Update the previous offset
                traveller.prevOffsetFromPortal = offsetFromPortal;
            }
        }
    }

    public void OnTravellerEnter(PortalTraveller traveller)
    {
        if (!trackedTravellers.Contains(traveller))
        {
            traveller.EnterPortalTrigger();
            traveller.prevOffsetFromPortal = traveller.transform.position - transform.position;
            trackedTravellers.Add(traveller);
            traveller.IgnoreCollision(screenCollider, true);
            if (attachedSurface)
            {
                traveller.IgnoreCollision(attachedSurface, true);
            }
        }
    }

    public void OnTravellerExit(PortalTraveller traveller, bool isTeleport = false)
    {
        if (trackedTravellers.Contains(traveller))
        {
            if (!isTeleport)
            {
                traveller.ExitPortalTrigger();
            }
            trackedTravellers.Remove(traveller);
            traveller.IgnoreCollision(screenCollider, false);
            if (attachedSurface)
            {
                traveller.IgnoreCollision(attachedSurface, false);
            }
        }
    }

    public void ChildTriggerEnter(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
        {
            OnTravellerEnter(traveller);
        }
    }

    public void ChildTriggerExit(Collider other)
    {
        PortalTraveller traveller = other.GetComponent<PortalTraveller>();
        if (traveller)
        {
            OnTravellerExit(traveller);
        }
    }

    void OnDestroy()
    {
        if (linkedPortal)
        {
            for (int i = trackedTravellers.Count - 1; i >= 0; i--)
            {
                PortalTraveller traveller = trackedTravellers[i];
                OnTravellerExit(traveller);
            }
        }
        if (linkedPortal && linkedPortal.linkedPortal == this)
        {
            linkedPortal.linkedPortal = null;
            linkedPortal.screen.material.SetTexture("_MainTex", null);
        }
        if (viewTexture)
        {
            viewTexture.Release();
        }
        if (MainCamera.instance)
        {
            MainCamera.instance.RemovePortal(this);
        }
    }

    public void UpdateFrameColor()
    {
        if (isSecondPortal)
        {
            frame.material.SetColor("_Color", new Color(1f, 0.6470588f, 0f, 1f));
        }
        else
        {
            frame.material.SetColor("_Color", new Color(0f, 0.6705089f, 1f, 1f));
        }
    }


    public void PrePortalRender()
    {
        foreach (var traveller in trackedTravellers)
        {
            UpdateSliceParams(traveller);
        }
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
                if (linkedPortal.SideOfPortal(portalCamera.transform.position) == -1) break;
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

            if (i == recursionLimit - 1)
            {
                portalCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Clone Player"));
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
        foreach (var traveller in trackedTravellers)
        {
            UpdateSliceParams(traveller);
        }
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
        }
        linkedPortal.screen.material.SetTexture("_MainTex", viewTexture);
    }

    // Sets the thickness of the portal screen so as not to clip with camera near plane when player goes through
    float ProtectScreenFromClipping(Vector3 viewPoint)
    {
        Transform screenT = screen.transform;
        bool camFacingSameDirAsPortal = Vector3.Dot(transform.forward, transform.position - viewPoint) > 0;

        float halfHeight = playerCamera.nearClipPlane * Mathf.Tan(playerCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float halfWidth = halfHeight * playerCamera.aspect;
        float dstToNearClipPlaneCorner = new Vector3(halfWidth, halfHeight, playerCamera.nearClipPlane).magnitude;
        float screenThickness = dstToNearClipPlaneCorner * 0.5f; // Cylinder height is 0.5 times this distance

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

        Vector3 camSpacePos = portalCamera.worldToCameraMatrix.MultiplyPoint(clipPlane.position);
        Vector3 camSpaceNormal = portalCamera.worldToCameraMatrix.MultiplyVector(clipPlane.forward) * dot;
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

    void UpdateSliceParams(PortalTraveller traveller)
    {
        // Calculate slice normal
        Vector3 sliceNormal = -transform.forward;
        Vector3 cloneSliceNormal = -linkedPortal.transform.forward;

        // Calculate slice center
        Vector3 sliceCenter = transform.position;
        Vector3 cloneSliceCenter = linkedPortal.transform.position;

        // Calculate offset distance
        float screenThickness = screen.transform.localScale.y;
        float sliceOffsetDst = screenThickness * 0.5f;
        float cloneSliceOffsetDst = screenThickness * 0.5f;

        // Apply parameters to traveller materials
        for (int i = 0; i < traveller.originalMaterials.Length; i++)
        {
            traveller.originalMaterials[i].SetVector("_sliceNormal", sliceNormal);
            traveller.originalMaterials[i].SetVector("_sliceCenter", sliceCenter);
            traveller.originalMaterials[i].SetFloat("_sliceOffsetDst", sliceOffsetDst);

            traveller.cloneMaterials[i].SetVector("_sliceNormal", cloneSliceNormal);
            traveller.cloneMaterials[i].SetVector("_sliceCenter", cloneSliceCenter);
            traveller.cloneMaterials[i].SetFloat("_sliceOffsetDst", cloneSliceOffsetDst);
        }
    }


    int SideOfPortal(Vector3 pos)
    {
        return System.Math.Sign(Vector3.Dot(pos - transform.position, transform.forward));
    }

}
