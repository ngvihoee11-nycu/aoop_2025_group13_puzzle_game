using UnityEngine;

// Attach to the Player GameObject (same object as PlayerController). Provides simple pickup/drop mechanics.
[RequireComponent(typeof(PlayerController))]
public class PlayerPickup : MonoBehaviour
{
    [SerializeField] float pickupRange = 3f;
    [SerializeField] float throwForce = 6f;
    [SerializeField] float holdOffset = 2f;
    public Transform holdPoint; // If null, we create a child transform called "HoldPoint"

    Pickupable heldObject;
    PlayerController playerController;
    Transform eyeTransform;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
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
    }

    void Update()
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

    public void TryPickup()
    {
        Transform origin = playerController != null && eyeTransform != null ? eyeTransform : transform;
        Ray ray = new Ray(origin.position, origin.forward);
        int mask = ~LayerMask.GetMask("Player");
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange, mask, QueryTriggerInteraction.Collide))
        {
            Pickupable p = hit.collider.GetComponentInParent<Pickupable>();
            if (p != null)
            {
                heldObject = p;
                heldObject.OnPickup(this);
            }
        }
    }

    public void Drop(bool throwObject)
    {
        if (heldObject == null) return;

        Vector3 throwVel = Vector3.zero;
        if (throwObject)
        {
            Transform origin = playerController != null && eyeTransform != null ? eyeTransform : transform;
            throwVel = origin.forward * throwForce + (playerController != null ? playerController.transform.GetComponent<CharacterController>()?.velocity ?? Vector3.zero : Vector3.zero);
        }

        heldObject.OnDrop(throwVel);
        heldObject = null;
    }
}
