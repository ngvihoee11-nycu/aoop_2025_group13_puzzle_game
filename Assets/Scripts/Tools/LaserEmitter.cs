using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    [Header("Settings")]
    public GameObject laserPrefab;
    public float maxLength = 100f;
    public int maxLasers = 10;
    
    private List<Laser> lasers;
    private int ignoreLayer = -1;

    void Start()
    {
        if (laserPrefab == null)
        {
            Debug.LogError("Laser Prefab is not assigned in LaserEmitter!");
            return;
        }

        if (laserPrefab.GetComponent<Collider>() == null && laserPrefab.GetComponentInChildren<Collider>() == null)
        {
            Debug.LogError("Collider component is missing on Laser prefab or its children!");
        }

        ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

        lasers = new List<Laser>();
        LaserInit(0, transform.position, transform.rotation);

        EmitLaser();
    }

    void Update()
    {
        EmitLaser();
    }

    public void EmitLaser()
    {
        float remainLength = maxLength;

        // Ensure the first laser exists
        if (lasers.Count == 0 || lasers[0] == null)
        {
            LaserInit(0, transform.position, transform.rotation);
        }

        // Deactivate all segments initially (except the first one, technically logic below handles activation)
        for (int i = 1; i < lasers.Count; i++)
        {
            if (lasers[i] != null)
            {
                lasers[i].gameObject.SetActive(false);
            }
        }

        // Process lasers in chain
        for (int i = 0; i < lasers.Count; i++)
        {
            if(lasers[i].gameObject.activeSelf)
            {
                float usedLength = LaserRaycast(i, remainLength);
                remainLength -= usedLength;
                
                // Stop if we run out of length
                if(remainLength <= 0) break;
            }
        }
    }

    public Laser LaserInit(int index, Vector3 position, Quaternion rotation)
    {
        if (lasers.Count <= index)
        {
            lasers.Add(Instantiate(laserPrefab, position, rotation, transform).GetComponent<Laser>());
        }
        
        if (lasers[index] == null)
        {
            lasers[index] = Instantiate(laserPrefab, position, rotation, transform).GetComponent<Laser>();
        }

        lasers[index].gameObject.SetActive(true); // Ensure it's active when initialized
        lasers[index].startPosition = position;
        lasers[index].transform.rotation = rotation;

        if (ignoreLayer != -1)
        {
            SetLayerRecursively(lasers[index].gameObject, ignoreLayer);
        }
        return lasers[index];
    }

    public float LaserRaycast(int index, float length)
    {
        Laser laser = lasers[index];
        RaycastHit hit;
        float laserLength = length;
        
        // Mask: Hit everything except "Laser Emitter" and "Ignore Raycast" layers
        int mask = ~LayerMask.GetMask("Laser Emitter", "Ignore Raycast");

        // --- Handle Exiting Collider (prevent self-hit) ---
        int layerTemp = 0;
        if (laser.exitingCollider)
        {
            layerTemp = laser.exitingCollider.gameObject.layer;
            laser.exitingCollider.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        // --- Raycast ---
        if (Physics.Raycast(laser.startPosition, laser.transform.forward, out hit, length, mask, QueryTriggerInteraction.Ignore))
        {
            laserLength = hit.distance;

            // 1. Handle Portals
            if (hit.collider.CompareTag("Portal"))
            {
                Portal portal = hit.collider.GetComponentInParent<Portal>();
                // Ensure we haven't reached max bounces and the portal is valid
                if (index + 1 < maxLasers && portal && portal.linkedPortal)
                {
                    Transform portalT = portal.transform;
                    Transform linkedPortalT = portal.linkedPortal.transform;
                    
                    // Calculate position and rotation at the other portal
                    Matrix4x4 fromMatrix = Matrix4x4.TRS(hit.point, laser.transform.rotation, new Vector3(1f, 1f, 1f));
                    Matrix4x4 toMatrix = linkedPortalT.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * portalT.worldToLocalMatrix * fromMatrix;

                    SetupNextLaser(index + 1, toMatrix.GetPosition(), toMatrix.rotation, hit.collider);
                }
            }
            // 2. Handle Mirrors (NEW LOGIC)
            else if (hit.collider.CompareTag("Mirror"))
            {
                if (index + 1 < maxLasers)
                {
                    Vector3 incomingDir = laser.transform.forward;
                    Vector3 reflectedDir = Vector3.Reflect(incomingDir, hit.normal);
                    
                    SetupNextLaser(index + 1, hit.point, Quaternion.LookRotation(reflectedDir), hit.collider);
                }
            }
        }

        // --- Restore Exiting Collider Layer ---
        if (laser.exitingCollider)
        {
            laser.exitingCollider.gameObject.layer = layerTemp;
        }

        // --- Visual Update ---
        // Center the laser visuals at half the distance
        laser.transform.position = laser.startPosition + laser.transform.forward * (laserLength / 2f);

        // Scale Z to match length
        Vector3 scale = laser.transform.localScale;
        laser.transform.localScale = new Vector3(scale.x, scale.y, laserLength);

        return laserLength;
    }

    // Helper to reduce code duplication between Portal and Mirror logic
    private void SetupNextLaser(int nextIndex, Vector3 startPos, Quaternion rotation, Collider hitCollider)
    {
        Laser nextLaser;
        if (lasers.Count <= nextIndex || lasers[nextIndex] == null)
        {
            nextLaser = LaserInit(nextIndex, startPos, rotation);
        }
        else
        {
            nextLaser = lasers[nextIndex];
            nextLaser.gameObject.SetActive(true);
            nextLaser.startPosition = startPos;
            nextLaser.transform.rotation = rotation;
        }
        
        // Store the collider we just hit so the next raycast ignores it (prevents getting stuck inside the mirror/portal)
        nextLaser.exitingCollider = hitCollider;
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

    public void InitializeLaser()
    {
        if (laserPrefab == null) return;

        ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");
        
        if (lasers == null) lasers = new List<Laser>();
        
        if (lasers.Count == 0 || lasers[0] == null)
        {
            LaserInit(0, transform.position, transform.rotation);
        }
        
        EmitLaser();
    }
}