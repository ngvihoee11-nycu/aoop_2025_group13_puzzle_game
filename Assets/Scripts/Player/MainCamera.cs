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
        if (PlayerController.instance.isFPP)
        {
            mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FPPHide"));
        }

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

    void CustomOnBeginCameraRendering(ScriptableRenderContext SRC, Camera mainCamera)
    {
        for (int i = 0; i < portals.Count; i++) {
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