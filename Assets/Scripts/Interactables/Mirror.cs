using UnityEngine;
using System.Collections.Generic;

public class Mirror : Pickupable
{
    public float spawnOffset = 0.02f;
    public float emitterTimeToLive = 0.05f; // Very short TTL to prevent ghosts
    
    private Dictionary<int, MirrorEmitterController> spawnedEmitters = new Dictionary<int, MirrorEmitterController>();

    void OnTriggerStay(Collider other)
    {
        // 1. Immediate exit for reflected lasers
        if (other.CompareTag("Generated")) return;

        Laser laser = other.GetComponent<Laser>();
        if (laser == null) return;

        // 2. Identify the source
        int sourceID = other.transform.root.GetInstanceID();

        // 3. Precise Raycast
        Vector3 incomingDirection = other.transform.forward.normalized;
        // Raycast from slightly behind the trigger hit to find the physical surface
        Ray ray = new Ray(other.transform.position - incomingDirection * 0.1f, incomingDirection);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 2.0f))
        {
            // Ignore if we hit another trigger
            if (hit.collider.isTrigger) return; 

            Vector3 reflectPoint = hit.point;
            Vector3 reflectedDir = Vector3.Reflect(incomingDirection, hit.normal).normalized;

            // 4. Update or Create
            if (spawnedEmitters.TryGetValue(sourceID, out MirrorEmitterController controller))
            {
                if (controller != null)
                {
                    UpdateMirrorEmitter(controller, reflectPoint, reflectedDir);
                }
                else
                {
                    // If the controller was destroyed but the key remains, clear it
                    spawnedEmitters.Remove(sourceID);
                }
            }
            else
            {
                LaserEmitter sourceEmitter = other.GetComponentInParent<LaserEmitter>();
                if (sourceEmitter != null)
                {
                    CreateMirrorEmitter(sourceID, sourceEmitter, reflectPoint, reflectedDir);
                }
            }
        }
    }

    private void CreateMirrorEmitter(int sourceID, LaserEmitter source, Vector3 pos, Vector3 dir)
    {
        GameObject newEmitterGO = new GameObject($"Reflector_{sourceID}");
        newEmitterGO.tag = "Generated";
        
        // Position it
        newEmitterGO.transform.position = pos + (dir * spawnOffset);
        newEmitterGO.transform.rotation = Quaternion.LookRotation(dir);
        
        // IMPORTANT: Do NOT child it to the mirror if the mirror moves frequently.
        // Or, if you do, ensure UpdateMirrorEmitter overrides the position fully.
        newEmitterGO.transform.SetParent(this.transform);

        LaserEmitter newEmitter = newEmitterGO.AddComponent<LaserEmitter>();
        newEmitter.laserPrefab = source.laserPrefab;
        newEmitter.InitializeLaser();

        MirrorEmitterController controller = newEmitterGO.AddComponent<MirrorEmitterController>();
        controller.SetTimeToLive(emitterTimeToLive);
        
        spawnedEmitters[sourceID] = controller;
    }

    private void UpdateMirrorEmitter(MirrorEmitterController controller, Vector3 pos, Vector3 dir)
    {
        controller.RefreshTimeToLive();
        // Force the position to the NEW hit point every frame
        controller.transform.position = pos + (dir * spawnOffset);
        controller.transform.rotation = Quaternion.LookRotation(dir);
    }
}