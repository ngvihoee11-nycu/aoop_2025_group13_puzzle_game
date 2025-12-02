using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    public GameObject laserPrefab;
    private GameObject currentLaser;
    private bool isEmitting = false;

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

        EmitLaser();
    }

    public void EmitLaser()
    {
        if (isEmitting) return; // prevent re-entrant updates from triggers
        isEmitting = true;

        // Modify existing laser to keep laser collision working
        if (currentLaser != null)
        {
            Destroy(currentLaser);
        }

        // Raycast to determine laser length and position
        // we use cylinder for laser representation

        RaycastHit hit;
        float laserLength = 100f; // Default length
        // Exclude the Laser Emitter layer and the built-in Ignore Raycast layer to avoid self-collision
        int mask = ~LayerMask.GetMask("Laser Emitter", "Ignore Raycast");
        if (Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, mask))
        {
            laserLength = hit.distance;
            Debug.Log("Laser hit: " + hit.collider.gameObject.name + " at distance: " + laserLength + " hit on" + hit.collider.gameObject.name);
        }
        // Instantiate laser as child of emitter
        currentLaser = Instantiate(laserPrefab, transform.position + transform.forward * (laserLength / 2), transform.rotation, transform);
        currentLaser.transform.localScale = new Vector3(currentLaser.transform.localScale.x, currentLaser.transform.localScale.y, laserLength);

        // Put the instantiated laser (and its children) on the Ignore Raycast layer to avoid future self-hit
        int ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        if (ignoreLayer != -1)
        {
            SetLayerRecursively(currentLaser, ignoreLayer);
        }

        // Clear emitting flag next frame to avoid trigger re-entrancy in the same frame
        StartCoroutine(ResetEmittingFlag());
    }

    private IEnumerator ResetEmittingFlag()
    {
        yield return null; // wait one frame
        isEmitting = false;
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
