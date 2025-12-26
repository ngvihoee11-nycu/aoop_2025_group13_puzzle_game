using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LaserEmitter : MonoBehaviour
{
    public GameObject laserPrefab;
    private List<Laser> lasers;
    public float maxLength = 100f;
    public int maxLasers = 10;
    private int ignoreLayer = -1;

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

        // Put instantiated laser on Ignore Raycast layer to avoid self-hit
        ignoreLayer = LayerMask.NameToLayer("Ignore Raycast");

        // Instantiate laser once and keep updating its transform/scale in Update()
        lasers = new List<Laser>();
        LaserInit(0, transform.position, transform.rotation);

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
        float remainLength = maxLength;

        if (lasers.Count == 0 || lasers[0] == null)
        {
            LaserInit(0, transform.position, transform.rotation);
        }

        for (int i = 1; i < lasers.Count; i++)
        {
            if (lasers[i])
            {
                lasers[i].gameObject.SetActive(false);
            }
        }

        for (int i = 0; i < lasers.Count; i++)
        {
            if(lasers[i].gameObject.activeSelf)
            {
                float usedLength = LaserRaycast(i, remainLength);
                remainLength -= usedLength;
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

        lasers[index].startPosition = position;

        if (ignoreLayer != -1)
        {
            SetLayerRecursively(lasers[index].gameObject, ignoreLayer);
        }
        return lasers[index];
    }

    public float LaserRaycast(int index, float length)
    {
        // Raycast to determine laser length and position
        Laser laser = lasers[index];

        RaycastHit hit;
        float laserLength = length;
        int mask = ~LayerMask.GetMask("Laser Emitter", "Ignore Raycast");

        int layerTemp = 0;
        if (laser.exitingCollider)
        {
            layerTemp = laser.exitingCollider.gameObject.layer;
            laser.exitingCollider.gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        }

        if (Physics.Raycast(laser.startPosition, laser.transform.forward, out hit, length, mask, QueryTriggerInteraction.Ignore))
        {
            laserLength = hit.distance;

            if (hit.collider.CompareTag("Portal"))
            {
                Portal portal = hit.collider.GetComponentInParent<Portal>();
                if (index + 1 < maxLasers && portal.linkedPortal)
                {
                    Transform portalT = portal.transform;
                    Transform linkedPortalT = portal.linkedPortal.transform;
                    Matrix4x4 fromMatrix = Matrix4x4.TRS(hit.point, laser.transform.rotation, new Vector3(1f, 1f, 1f));
                    Matrix4x4 toMatrix = linkedPortalT.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * portalT.worldToLocalMatrix * fromMatrix;

                    if (lasers.Count <= index + 1 || lasers[index + 1] == null)
                    {
                        Laser nextLaser = LaserInit(index + 1, toMatrix.GetPosition(), toMatrix.rotation);
                        nextLaser.exitingCollider = hit.collider;
                    }
                    else
                    {
                        Laser nextLaser = lasers[index + 1];
                        nextLaser.gameObject.SetActive(true);
                        nextLaser.startPosition = toMatrix.GetPosition();
                        nextLaser.transform.rotation = toMatrix.rotation;
                        nextLaser.exitingCollider = hit.collider;
                    }
                }
            }
        }

        if (laser.exitingCollider)
        {
            laser.exitingCollider.gameObject.layer = layerTemp;
        }

        // Position the laser so its center is at half the length along forward
        laser.transform.position = laser.startPosition + laser.transform.forward * (laserLength / 2f);

        // Update scale: keep base X/Y, set Z to laserLength (matches previous behavior)
        Vector3 scale = laser.transform.localScale;
        laser.transform.localScale = new Vector3(scale.x, scale.y, laserLength);
        return laserLength;
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
