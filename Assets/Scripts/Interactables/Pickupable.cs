using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine;
// Base class for objects the player can pick up and drop.
public class Pickupable : MonoBehaviour
{
    public Rigidbody rigid;
    [SerializeField] float dropThreshold = 2f;
    [SerializeField] float moveThreshold = 0.1f;
    [SerializeField] float pickupForce = 150f;
    [SerializeField] float rotateThreshold = 1f;
    [SerializeField] float pickupTorque = 1f;
    [SerializeField] float defaultDrag = 1f;
    [SerializeField] float pickedDrag = 10f;
    [SerializeField] float defaultAngularDrag = 0.05f;
    [SerializeField] float pickedAngularDrag = 10f;
    protected bool isPicked = false;
    PlayerPickup holdPlayer;
    Transform holdAnchor;
    float prevAnchorYaw;
    float targetYaw;
    Quaternion relativeRotation;

    protected virtual void FixedUpdate()
    {
        if (isPicked)
        {
            MoveToAnchor();
        }
    }

    protected virtual void MoveToAnchor()
    {
        float anchorDistance = Vector3.Distance(transform.position, holdAnchor.position);
        if (anchorDistance > dropThreshold)
        {
            holdPlayer.Drop(false);
            return;
        }

        if (anchorDistance > moveThreshold)
        {
            Vector3 moveDirection = holdAnchor.position - transform.position;
            rigid.AddForce(moveDirection * pickupForce);
        }

        Vector3 euler = (transform.rotation * relativeRotation).eulerAngles;
        float anchorTurn = Mathf.DeltaAngle(prevAnchorYaw, holdAnchor.eulerAngles.y);
        targetYaw += anchorTurn;
        float turn = Mathf.DeltaAngle(euler.y, targetYaw);
        if (Mathf.Abs(turn) > rotateThreshold)
        {
            rigid.AddTorque(Vector3.up * pickupTorque * turn);
        }
        prevAnchorYaw = holdAnchor.eulerAngles.y;
    }

    // Called when player picks up the object. holder is the player who called the function
    public virtual void OnPickup(PlayerPickup holder)
    {
        if (isPicked) return;
        isPicked = true;
        holdPlayer = holder;
        holdAnchor = holder.holdPoint;
        prevAnchorYaw = holdAnchor.eulerAngles.y;
        relativeRotation = Quaternion.Inverse(transform.rotation);
        targetYaw = (transform.rotation * relativeRotation).eulerAngles.y;

        // Make kinematic while carried and parent to hold point
        if (rigid != null)
        {
            rigid.useGravity = false;
            rigid.drag = pickedDrag;
            rigid.angularDrag = pickedAngularDrag;
        }
    }

    // Called to drop the object; optional throwVelocity will be applied if Rigidbody exists
    public virtual void OnDrop(Vector3 throwVelocity)
    {
        if (!isPicked) return;
        isPicked = false;
        holdPlayer = null;
        holdAnchor = null;

        if (rigid != null)
        {
            rigid.useGravity = true;
            rigid.drag = defaultDrag;
            rigid.angularDrag = defaultAngularDrag;
            rigid.velocity = throwVelocity;
        }
    }
}
