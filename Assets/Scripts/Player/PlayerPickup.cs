using UnityEngine;

// Attach to the Player GameObject (same object as PlayerController). Provides simple pickup/drop mechanics.
[RequireComponent(typeof(PlayerController))]
public class PlayerPickup : MonoBehaviour
{
    public float pickupRange = 3f;
    public float throwForce = 6f;
    public Transform holdPoint; // If null, we create a child transform called "HoldPoint"

    private Pickupable heldObject;
    private PlayerController playerController;

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        if (holdPoint == null)
        {
            GameObject hp = new GameObject("HoldPoint");
            hp.transform.SetParent(transform, false);
            // place hold point a bit in front of the eye
            if (playerController != null && playerController.eyeTransform != null)
            {
                hp.transform.position = playerController.eyeTransform.position + playerController.eyeTransform.forward * 1.0f;
                hp.transform.rotation = playerController.eyeTransform.rotation;
            }
            else
            {
                hp.transform.localPosition = new Vector3(0f, 0f, 1f);
            }
            holdPoint = hp.transform;
        }
    }

    void Update()
    {
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

    private void TryPickup()
    {
        Transform origin = playerController != null && playerController.eyeTransform != null ? playerController.eyeTransform : transform;
        Ray ray = new Ray(origin.position, origin.forward);
        RaycastHit hit;
        int mask = ~LayerMask.GetMask("Player");
        if (Physics.Raycast(ray, out hit, pickupRange, mask, QueryTriggerInteraction.Collide))
        {
            Pickupable p = hit.collider.GetComponentInParent<Pickupable>();
            if (p != null)
            {
                heldObject = p;
                heldObject.OnPickup(holdPoint);
            }
        }
    }

    private void Drop(bool throwObject)
    {
        if (heldObject == null) return;

        Vector3 throwVel = Vector3.zero;
        if (throwObject)
        {
            Transform origin = playerController != null && playerController.eyeTransform != null ? playerController.eyeTransform : transform;
            throwVel = origin.forward * throwForce + (playerController != null ? playerController.transform.GetComponent<CharacterController>()?.velocity ?? Vector3.zero : Vector3.zero);
        }

        heldObject.OnDrop(throwVel);
        heldObject = null;
    }
}
