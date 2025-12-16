using UnityEngine;
using UnityEngine.Rendering;

public class MainCamera : MonoBehaviour {

    Camera mainCamera;
    Portal[] portals;

    void Awake()
    {

        mainCamera = GetComponent<Camera> ();
        mainCamera.cullingMask = -1;
        if (PlayerController.instance.isFPP)
        {
            mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FPPHide"));
        }

        portals = FindObjectsOfType<Portal> ();

    }

    protected void OnEnable()
    {
        RenderPipelineManager.beginCameraRendering += CustomOnBeginCameraRendering;
    }

    private void OnDisable()
    {
        RenderPipelineManager.beginCameraRendering -= CustomOnBeginCameraRendering;
    }

    private void CustomOnBeginCameraRendering(ScriptableRenderContext SRC, Camera camera)
    {
        for (int i = 0; i < portals.Length; i++) {
            portals[i].PrePortalRender();
        }

        for (int i = 0; i < portals.Length; i++)
        {
            if (portals[i].linkedPortal) portals[i].Render(SRC);
        }

        for (int i = 0; i < portals.Length; i++) {
            if (portals[i].linkedPortal) portals[i].PostPortalRender();
        }
    }

}