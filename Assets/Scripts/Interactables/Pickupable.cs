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
    Transform holdAnchorTP;
    bool prevHeldByAnchorTP;
    float prevAnchorYaw;
    float targetYaw;
    Quaternion relativeRotation;

    protected virtual void FixedUpdate()
    {
        if (isPicked)
        {
            if(prevHeldByAnchorTP && !holdAnchorTP.gameObject.activeSelf)
            {
                PortalTraveller traveller = transform.GetComponent<PortalTraveller>();
                List<Portal> portals = LevelManager.instance.portals;
                for (int i = 0; i < portals.Count; i++)
                {
                    if (portals[i].linkedPortal && portals[i].trackingTraveller(traveller))
                    {
                        holdPlayer.TeleportHoldPoint(portals[i].linkedPortal.transform, portals[i].transform);
                    }
                }
            }
            float anchorDistance = Vector3.Distance(transform.position, holdAnchor.position);
            float anchorTPDistance = Vector3.Distance(transform.position, holdAnchorTP.position);
            if (holdAnchorTP.gameObject.activeSelf && anchorDistance > anchorTPDistance)
            {
                MoveToAnchor(holdAnchorTP);
                prevHeldByAnchorTP = true;
            }
            else
            {
                MoveToAnchor(holdAnchor);
                prevHeldByAnchorTP = false;
            }
        }
    }

    protected virtual void MoveToAnchor(Transform target)
    {
        float targetDistance = Vector3.Distance(transform.position, target.position);
        if (targetDistance > dropThreshold)
        {
            holdPlayer.Drop(false);
            return;
        }

        if (targetDistance > moveThreshold)
        {
            Vector3 moveDirection = target.position - transform.position;
            rigid.AddForce(moveDirection * pickupForce);
        }

        Vector3 euler = (transform.rotation * relativeRotation).eulerAngles;
        float anchorTurn = Mathf.DeltaAngle(prevAnchorYaw, target.eulerAngles.y);
        targetYaw += anchorTurn;
        float turn = Mathf.DeltaAngle(euler.y, targetYaw);
        if (Mathf.Abs(turn) > rotateThreshold)
        {
            rigid.AddTorque(Vector3.up * pickupTorque * turn);
        }
        prevAnchorYaw = target.eulerAngles.y;
    }

    // Called when player picks up the object. holder is the player who called the function
    public virtual void OnPickup(PlayerPickup holder)
    {
        if (isPicked) return;
        isPicked = true;
        holdPlayer = holder;
        holdAnchor = holder.holdPoint;
        holdAnchorTP = holder.holdPointTP;

        float anchorDistance = Vector3.Distance(transform.position, holdAnchor.position);
        float anchorTPDistance = Vector3.Distance(transform.position, holdAnchorTP.position);
        if (holdAnchorTP.gameObject.activeSelf && anchorDistance > anchorTPDistance)
        {
            prevAnchorYaw = holdAnchorTP.eulerAngles.y;
        }
        else
        {
            prevAnchorYaw = holdAnchor.eulerAngles.y;
        }

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
        holdAnchorTP = null;

        if (rigid != null)
        {
            rigid.useGravity = true;
            rigid.drag = defaultDrag;
            rigid.angularDrag = defaultAngularDrag;
            rigid.velocity = throwVelocity;
        }
    }
}
