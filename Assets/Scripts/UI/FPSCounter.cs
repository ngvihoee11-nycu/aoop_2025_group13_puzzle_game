using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FrameRate : MonoBehaviour
{
    private TextMeshProUGUI TextMeshPro;
    
    // Variables to track timing and frames
    private float timer;
    private int frameCount;

    // Configuration for how often to update (0.5 seconds)
    private float pollingTime = 0.5f;

    private void Awake()
    {
        TextMeshPro = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        // Add the time passed since last frame to the timer
        timer += Time.deltaTime;
        
        // Increment the frame count
        frameCount++;

        // If the timer exceeds our polling time (0.5s)
        if (timer >= pollingTime)
        {
            // Calculate Average FPS: Frames / Time
            int fps = Mathf.RoundToInt(frameCount / timer);

            // Update Text
            TextMeshPro.text = fps + " FPS";

            // Reset timer and frame count
            timer -= pollingTime;
            frameCount = 0;
        }
    }
}