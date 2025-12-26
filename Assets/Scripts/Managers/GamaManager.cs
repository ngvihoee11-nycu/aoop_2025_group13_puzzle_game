using UnityEngine;
using UnityEngine.SceneManagement;



public class GamaManager : Singleton<GamaManager>
{
    private void Start()
    {
        int targetFPS = 60;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFPS;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);

        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Portal Traveller"), LayerMask.NameToLayer("Portal"), true);
        Physics.IgnoreLayerCollision(LayerMask.NameToLayer("Clone Traveller"), LayerMask.NameToLayer("Portal"), true);
    }
}
