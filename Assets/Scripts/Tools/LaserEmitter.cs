using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    public GameObject laserPrefab;
    private GameObject currentLaser;
    public float defaultLength = 100f;
    private int ignoreLayer = -1;
    private Vector3 baseScale;

    void Start()
    {
        // Ensure prefab assigned before accessing it
        if (laserPrefab == null)
        {
            Debug.LogError("Laser Prefab is not assigned in LaserEmitter!");
            return;
        }

        // Check if collider exists on the prefab (or its children)
        if (laserPrefab.GetComponent<Collider>() == null && laserPrefab.GetComponentInChildren<Collider>() == null)
        {
            Debug.LogError("Collider component is missing on Laser prefab or its children!");
        }

        // Instantiate laser once and keep updating its transform/scale in Update()
        currentLaser = Instantiate(laserPrefab, transform.position, transform.rotation, transform);
        baseScale = currentLaser.transform.localScale;

        // Put instantiated laser on Ignore Raycast layer to avoid self-hit
        ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer != -1)
        {
            SetLayerRecursively(currentLaser, ignoreLayer);
        }

        // Initial update
        EmitLaser();
    }

    void Update()
    {
        // Continuously update laser length/position so it reflects scene changes
        EmitLaser();
    }

    public void EmitLaser()
    {
        if (currentLaser == null)
        {
            // If laser was destroyed for some reason, recreate it
            currentLaser = Instantiate(laserPrefab, transform.position, transform.rotation, transform);
            baseScale = currentLaser.transform.localScale;
            if (ignoreLayer != -1)
                SetLayerRecursively(currentLaser, ignoreLayer);
        }

        // Raycast to determine laser length and position
        RaycastHit hit;
        float laserLength = defaultLength;
        int mask = ~LayerMask.GetMask("Laser Emitter", "Ignore Raycast");
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, mask))
        {
            laserLength = hit.distance;
        }

        // Position the laser so its center is at half the length along forward
        currentLaser.transform.position = transform.position + transform.forward * (laserLength / 2f);
        currentLaser.transform.rotation = transform.rotation;

        // Update scale: keep base X/Y, set Z to laserLength (matches previous behavior)
        currentLaser.transform.localScale = new Vector3(baseScale.x, baseScale.y, laserLength);
    }

    private IEnumerator ResetEmittingFlag()
    {
        yield return null; // wait one frame (no-op if unused)
    }

    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursively(child.gameObject, layer);
        }
    }
}
