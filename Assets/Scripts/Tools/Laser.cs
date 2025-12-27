using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Laser : MonoBehaviour
{
    public Vector3 startPosition;
    public Collider exitingCollider;
    private bool recentlyTeleported = false;
    private float teleportCooldown = 0.15f;
    private Coroutine teleportCoroutine;

    void OnTriggerEnter(Collider other)
    {
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

    void OnTriggerExit(Collider other)
    {
        GetComponentInParent<LaserEmitter>().EmitLaser();
    }

    private IEnumerator ResetTeleportFlag(float duration)
    {
        yield return new WaitForSeconds(duration);
        recentlyTeleported = false;
        teleportCoroutine = null;
    }

    // Public method to begin a teleport/reflection cooldown (called by Mirror and portal teleport logic)
    public void BeginTeleportCooldown(float duration)
    {
        recentlyTeleported = true;
        if (teleportCoroutine != null)
            StopCoroutine(teleportCoroutine);
        teleportCoroutine = StartCoroutine(ResetTeleportFlag(duration));
    }
}
