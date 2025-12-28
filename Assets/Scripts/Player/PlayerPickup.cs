using System.Collections.Generic;
using UnityEngine;

// Attach to the Player GameObject (same object as PlayerController). Provides simple pickup/drop mechanics.
[RequireComponent(typeof(PlayerController))]
public class PlayerPickup : Singleton<PlayerPickup>
{
    [SerializeField] float pickupRange = 3f;
    [SerializeField] float throwForce = 6f;
    [SerializeField] float holdOffset = 2f;
    public Transform holdPoint; // If null, we create a child transform called "HoldPoint"
    public Transform holdPointTP;
    bool holdPointTeleported;

    Pickupable heldObject;
    PlayerController playerController;
    Transform eyeTransform;

    void Awake()
    {
        playerController = PlayerController.instance;
        eyeTransform = playerController.eyeTransform;
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            holdPoint = hp.transform;
            // place hold point a bit in front of the eye
            if (eyeTransform)
            {
                holdPoint.SetParent(eyeTransform, false);
            }
            else
            {
                holdPoint.SetParent(transform, false);
            }
            holdPoint.localPosition = new Vector3(0f, 0f, holdOffset);
        }
        if (holdPointTP == null)
        {
            GameObject hptp = new GameObject("HoldPointTP");
            holdPointTP = hptp.transform;
            // place hold point a bit in front of the eye
            if (eyeTransform)
            {
                holdPointTP.SetParent(eyeTransform, false);
            }
            else
            {
                holdPointTP.SetParent(transform, false);
            }
            hptp.SetActive(false);
        }
    }

    void Update()
    {
        if (playerController.lockCursor)
        {
            //holdPoint.position = eyeTransform.position + eyeTransform.forward * holdOffset;

            // Pickup with E, Drop/throw with Q
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (heldObject == null) TryPickup();
                else Drop(false);
            }

            if (Input.GetKeyDown(KeyCode.Q))
            {
                if (heldObject != null) Drop(true);
            }
        }

        List<Portal> portals = LevelManager.instance.portals;
        Transform playerT = playerController.transform;
        holdPointTeleported = false;

        for (int i = 0; i < portals.Count; i++)
        {
            if (portals[i].linkedPortal && CameraUtility.SegmentQuad(playerT.position, holdPoint.position, portals[i].transform))
            {
                Transform currentPortalT = portals[i].transform;
                Transform linkedPortalT = portals[i].linkedPortal.transform;
                Matrix4x4 m = linkedPortalT.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * currentPortalT.worldToLocalMatrix * holdPoint.localToWorldMatrix;
                holdPointTP.gameObject.SetActive(true);
                holdPointTP.SetPositionAndRotation(m.GetPosition(), m.rotation);
                holdPointTeleported = true;
            }
        }

        if (!holdPointTeleported)
        {
            holdPointTP.localPosition = Vector3.zero;
            holdPointTP.gameObject.SetActive(false);
        }
    }

    public void TeleportHoldPoint(Transform fromPortal, Transform toPortal)
    {
        Matrix4x4 m = toPortal.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * fromPortal.worldToLocalMatrix * holdPoint.localToWorldMatrix;
        holdPointTP.gameObject.SetActive(true);
        holdPointTP.SetPositionAndRotation(m.GetPosition(), m.rotation);
    }

    public void TryPickup()
    {
        Transform origin = playerController != null && eyeTransform != null ? eyeTransform : transform;
        Ray ray = new Ray(origin.position, origin.forward);
        int mask = ~LayerMask.GetMask("Player");
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, mask, QueryTriggerInteraction.Ignore))
        {
            Pickupable pickupable = hit.collider.GetComponentInParent<Pickupable>();
            Portal portal = hit.collider.GetComponentInParent<Portal>();
            if (pickupable)
            {
                heldObject = pickupable;
                heldObject.OnPickup(this);
            }
            else if (portal)
            {
                Transform fromPortal = portal.transform;
                Transform toPortal = portal.linkedPortal.transform;
                Vector3 newPos = toPortal.TransformPoint(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyPoint(fromPortal.InverseTransformPoint(hit.point)));
                Vector3 newDir = toPortal.TransformVector(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyVector(fromPortal.InverseTransformVector(origin.forward)));
                int newMask = ~LayerMask.GetMask("Portal");
                if (Physics.Raycast(newPos, newDir, out RaycastHit newHit, pickupRange - hit.distance, newMask, QueryTriggerInteraction.Ignore))
                {
                    Pickupable newPickupable = newHit.collider.GetComponentInParent<Pickupable>();
                    if (newPickupable)
                    {
                        heldObject = newPickupable;
                        heldObject.OnPickup(this);
                    }
                }
            }
        }
    }

    public void Drop(bool throwObject)
    {
        if (heldObject == null) return;

        Vector3 throwVel = Vector3.zero;
        if (throwObject)
        {
            Transform origin = holdPointTeleported ? holdPointTP : holdPoint;
            throwVel = origin.forward * throwForce + (playerController != null ? playerController.transform.GetComponent<CharacterController>()?.velocity ?? Vector3.zero : Vector3.zero);
        }

        heldObject.OnDrop(throwVel);
        heldObject = null;
    }
}
