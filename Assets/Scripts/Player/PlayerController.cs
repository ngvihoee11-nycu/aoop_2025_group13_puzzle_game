using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public float moveSpeed = 5f;
    public Vector3 cameraOffset;
    public Transform fppTransform;

    private CharacterController characterController;

    private Vector3 movement;
    private bool isFPP = false;
    
    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    public float minPitch = -40f;
    public float maxPitch = 85f;
    public float pitch = 0f;
    public float yaw = 0f;

    [Header("Shooting")]
    public GameObject leftSpawnPrefab;
    public GameObject rightSpawnPrefab;
    public float maxShootDistance = 100f;
    public float spawnOffset = 0.02f; // offset from surface to avoid z-fighting

    private bool isAimingLeft = false;
    private bool isAimingRight = false;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing on Player!");
        }

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }

        if (cameraTransform != null)
        {
            Vector3 angles = cameraTransform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }
    }

    void Update()
    {
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");
        // Camera-relative movement: use camera forward/right when available
        if (cameraTransform != null)
        {
            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();
            movement = (right * moveX + forward * moveZ).normalized;
        }
        else
        {
            movement = new Vector3(moveX, 0, moveZ).normalized;
        }

        characterController.Move(movement * moveSpeed * Time.deltaTime);

        // Handle Camera Rotation (mouse look)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraTransform != null)
        {
            // If in first-person aiming, position camera at fppTransform and use pitch/yaw for rotation
            if (isFPP && fppTransform != null)
            {
                cameraTransform.position = fppTransform.position;
                cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
            }
            else
            {
                // Third-person: Recompute camera position from stored offset using yaw/pitch
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                cameraTransform.position = transform.position + rot * cameraOffset;
                cameraTransform.LookAt(transform.position + Vector3.up);
            }

            // Rotate player horizontally to match camera yaw
            Vector3 playerEuler = transform.eulerAngles;
            playerEuler.y = yaw;
            transform.eulerAngles = playerEuler;
        }

        // Input: mouse down enters aiming; mouse up performs Raycast spawn
        // Start aiming on button down
        if (Input.GetMouseButtonDown(0))
        {
            isAimingLeft = true;
            SwitchToFPP();
        }
        if (Input.GetMouseButtonDown(1))
        {
            isAimingRight = true;
            SwitchToFPP();
        }

        // On button release, perform Raycast and spawn corresponding prefab
        if (Input.GetMouseButtonUp(0) && isAimingLeft)
        {
            PerformShoot(0);
            isAimingLeft = false;
            // If no longer aiming with either button, switch back to third-person
            if (!isAimingRight)
            {
                SwitchToTPP();
            }
        }
        if (Input.GetMouseButtonUp(1) && isAimingRight)
        {
            PerformShoot(1);
            isAimingRight = false;
            // If no longer aiming with either button, switch back to third-person
            if (!isAimingLeft)
            {
                SwitchToTPP();
            }
        }
    }

    private void PerformShoot(int button)
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit, maxShootDistance))
        {
            GameObject prefabToSpawn = (button == 0) ? leftSpawnPrefab : rightSpawnPrefab;
            if (prefabToSpawn == null)
            {
                Debug.LogWarning("No spawn prefab assigned for button " + button);
                return;
            }

            // Place slightly above surface along the normal to avoid clipping
            Vector3 spawnPos = hit.point + hit.normal * spawnOffset;

            // Align object's up to the hit normal
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.up, hit.normal);

            Instantiate(prefabToSpawn, spawnPos, spawnRot);
        }
    }

    void SwitchToFPP()
    {
        if (!isFPP)
        {
            isFPP = true;
            PlayerUIManager.instance.SetCrosshair(1);
            if (fppTransform != null && cameraTransform != null)
            {
                cameraTransform.position = fppTransform.position;
                cameraTransform.rotation = fppTransform.rotation;
            }
        }
        
    }

    void SwitchToTPP()
    {
        if (isFPP)
        {
            isFPP = false;
            PlayerUIManager.instance.SetCrosshair(0);
            // When switching back, immediately reposition camera based on current yaw/pitch
            if (cameraTransform != null)
            {
                Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
                cameraTransform.position = transform.position + rot * cameraOffset;
                cameraTransform.LookAt(transform.position + Vector3.up);
            }
        }
    }
}