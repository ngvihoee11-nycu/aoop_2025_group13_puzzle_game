using UnityEngine;
using UnityEngine.SceneManagement;

int targetFPS = 60 

public class GamaManager : Singleton<GamaManager>
{
    private void Start()
    {
        QualitySettings.vSyncCount = 0; // Disable VSync to use targetFrameRate
        Application.targetFrameRate = targetFPS;
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}