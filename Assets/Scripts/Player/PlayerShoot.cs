using UnityEngine;

// Attach to the Player GameObject (same object as PlayerController). Provides portal shooting mechanics.
[RequireComponent(typeof(PlayerController))]
public class PlayerShoot : Singleton<PlayerShoot>
{
    [Header("Shooting")]
    public GameObject portalPrefab;
    public float maxShootDistance = 100f;
    bool isAimingLeft = false;
    bool isAimingRight = false;

    [Header("Portal instances")]
    public Portal portal1;
    public Portal portal2;

    PlayerController playerController;
    Transform eyeTransform;

    void Awake()
    {
        playerController = PlayerController.instance;
        eyeTransform = playerController.eyeTransform;
    }

    void Update()
    {
        if (playerController.lockCursor)
        {
            // Input: mouse down enters aiming; mouse up performs Raycast spawn
            // Start aiming on button down
            if (Input.GetMouseButtonDown(0))
            {
                isAimingLeft = true;
            }
            if (Input.GetMouseButtonDown(1))
            {
                isAimingRight = true;
            }

            // On button release, perform Raycast and spawn corresponding prefab
            if (Input.GetMouseButtonUp(0) && isAimingLeft)
            {
                PerformShoot(0);
                isAimingLeft = false;
            }
            if (Input.GetMouseButtonUp(1) && isAimingRight)
            {
                PerformShoot(1);
                isAimingRight = false;
            }
        }
    }

    void PerformShoot(int button)
    {
        int layerMask = ~LayerMask.GetMask("Ignore Raycast", "Portal", "Portal Frame", "Player", "Clone Player", "Portal Traveller", "Clone Traveller");
        if (Physics.Raycast(eyeTransform.position, eyeTransform.forward, out RaycastHit hit, maxShootDistance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("No spawn prefab assigned for portals.");
                return;
            }

            if (button == 0)
            {
                portal1 = Portal.SpawnPortal(portalPrefab, portal2, hit, eyeTransform, false);
            }
            else if (button == 1)
            {
                portal2 = Portal.SpawnPortal(portalPrefab, portal1, hit, eyeTransform, true);
            }
        }
    }
}