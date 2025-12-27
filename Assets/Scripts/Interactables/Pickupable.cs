using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
// Base class for objects the player can pick up and drop.
public class Pickupable : MonoBehaviour
{
    protected Rigidbody rb;
    protected bool isPicked = false;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            // Add a Rigidbody if none exists so physics works when dropped
            rb = gameObject.AddComponent<Rigidbody>();
            rb.mass = 1f;
        }
    }

    // Called when player picks up the object. holdParent is the transform to parent under (eg. a hold point on the player)
    public virtual void OnPickup(Transform holdParent)
    {
        if (isPicked) return;
        isPicked = true;

        // Make kinematic while carried and parent to hold point
        if (rb != null)
        {
            rb.isKinematic = true;
        }

        transform.SetParent(holdParent, true);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // Called to drop the object; optional throwVelocity will be applied if Rigidbody exists
    public virtual void OnDrop(Vector3 throwVelocity)
    {
        if (!isPicked) return;
        isPicked = false;

        transform.SetParent(null, true);
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = throwVelocity;
        }
    }
}
