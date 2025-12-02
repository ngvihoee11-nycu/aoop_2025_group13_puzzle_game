    using UnityEngine;

    public class FPSLimiter : MonoBehaviour
    {
        public int targetFPS = 60; // Set your desired FPS here

        void Start()
        {
            QualitySettings.vSyncCount = 0; // Disable VSync to use targetFrameRate
            Application.targetFrameRate = targetFPS;
        }
    }