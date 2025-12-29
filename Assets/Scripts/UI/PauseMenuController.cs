using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI Components")]
    public GameObject settingPanel;
    public Button continueButton;
    public Button retryButton;

    private bool isPaused = false;

    void Start()
    {
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(Resume);
        }

        if (retryButton != null)
        {
            retryButton.onClick.AddListener(Retry);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }
    }

    public void Pause()
    {
        isPaused = true;
        if (settingPanel != null)
        {
            settingPanel.SetActive(true);
        }
        
        Time.timeScale = 0f;
        
        if (PlayerController.instance != null)
        {
            PlayerController.instance.lockCursor = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        isPaused = false;
        if (settingPanel != null)
        {
            settingPanel.SetActive(false);
        }
        
        Time.timeScale = 1f;

        if (PlayerController.instance != null)
        {
            PlayerController.instance.lockCursor = true;
        }
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
