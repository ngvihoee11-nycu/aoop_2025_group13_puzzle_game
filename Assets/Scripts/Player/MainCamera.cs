using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MainCamera : Singleton<MainCamera> {

    Camera mainCamera;
    List<Portal> portals;

    void Awake()
    {

        mainCamera = GetComponent<Camera> ();
        mainCamera.cullingMask = -1;
        mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FPPHide"));

        portals = new List<Portal>(FindObjectsOfType<Portal>());

    }

    public Camera GetCamera()
    {
        return mainCamera;
    }

    public void AddPortal(Portal portal)
    {
        portals.Add(portal);
    }

    public void RemovePortal(Portal portal)
    {
        portals.Remove(portal);
    }

    void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += CustomOnBeginCameraRendering;
    }

    void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= CustomOnBeginCameraRendering;
    }

    void CustomOnBeginCameraRendering(ScriptableRenderContext SRC, Camera camera)
    {
        PlayerController player = PlayerController.instance;
        Transform playerT = player.transform;
        Transform playerEyeT = player.eyeTransform;
        bool cameraTeleported = false;

        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].linkedPortal && CameraUtility.SegmentQuad(playerT.position, playerEyeT.position, portals[i].transform))
            {
                Transform currentPortalT = portals[i].transform;
                Transform linkedPortalT = portals[i].linkedPortal.transform;
                Matrix4x4 m = linkedPortalT.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * currentPortalT.worldToLocalMatrix * playerEyeT.localToWorldMatrix;
                transform.SetPositionAndRotation(m.GetPosition(), m.rotation);
                player.SetLayerMask(true);
                cameraTeleported = true;
            }
        }
        if(!cameraTeleported)
        {
            transform.SetPositionAndRotation(playerEyeT.position, playerEyeT.rotation);
            player.SetLayerMask(false);
        }

        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].linkedPortal) portals[i].PrePortalRender();
        }

        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].linkedPortal) portals[i].Render(SRC);
        }

        for (int i = 0; i < portals.Count; i++) {
            if (portals[i].linkedPortal) portals[i].PostPortalRender();
        }
    }

}