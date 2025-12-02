using UnityEngine;
using UnityEngine.SceneManagement;

public class GamaManager : Singleton<GamaManager>
{
    private void Start()
    {
        SceneManager.LoadScene("MainMenu", LoadSceneMode.Additive);
    }
}