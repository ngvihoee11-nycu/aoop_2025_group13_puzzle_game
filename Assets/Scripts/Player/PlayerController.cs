using UnityEngine;

public class PlayerController : PortalTravellerSingleton<PlayerController>
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 10f;
    public float smoothMoveTime = 0.1f;

    [Header("Physics")]
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    private Vector3 velocity;
    private float verticalVelocity;
    private Vector3 smoothV;
    private CharacterController characterController;
    
    [Header("Camera")]
    public bool isFPP = true;
    public float eyeHeight = 0.375f;
    public Vector3 cameraOffset;
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    public float minPitch = -40f;
    public float maxPitch = 85f;
    public float smoothRotationTime = 0.1f;
    public float pitch = 0f;
    public float yaw = 0f;
    private Camera mainCamera;
    private float smoothPitch;
    private float smoothYaw;
    private float pitchSmoothV;
    private float yawSmoothV;

    [Header("Shooting")]
    public GameObject portalPrefab;
    public float maxShootDistance = 100f;
    public float spawnOffset = 0.02f; // offset from surface to avoid z-fighting

    private bool isAimingLeft = false;
    private bool isAimingRight = false;

    private Portal portal1;
    private Portal portal2;

    void Start()
    {
        mainCamera = MainCamera.instance.GetCamera();
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing on Player!");
        }

        if (cameraTransform == null && mainCamera != null)
        {
            cameraTransform = mainCamera.transform;
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
        Vector2 input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        Vector3 inputDir = new Vector3(input.x, 0, input.y).normalized;
        Vector3 worldInputDir = transform.TransformDirection(inputDir);
        
        float targetSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 targetVelocity = worldInputDir * targetSpeed;
        velocity = Vector3.SmoothDamp(velocity, targetVelocity, ref smoothV, smoothMoveTime);
        

        // Apply gravity and jumping
        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            // Small negative value to keep the controller grounded
            verticalVelocity = -2f;
        }

        if (grounded && Input.GetButtonDown("Jump"))
        {
            // v = sqrt(2 * g * h) but gravity is negative so use -2 * gravity
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // Integrate gravity
        verticalVelocity += gravity * Time.deltaTime;

        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        // Handle Camera Rotation (mouse look)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, smoothRotationTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, smoothRotationTime);
        
        transform.eulerAngles = Vector3.up * smoothYaw;

        if (cameraTransform != null)
        {
            // If in first-person aiming, position camera at eye height
            if (isFPP)
            {
                cameraTransform.position = transform.position + Vector3.up * eyeHeight;
                cameraTransform.rotation = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
            }
            else
            {
                // Third-person: Recompute camera position from stored offset using yaw/pitch
                Quaternion rot = Quaternion.Euler(smoothPitch, smoothYaw, 0f);
                cameraTransform.position = transform.position + rot * cameraOffset;
                cameraTransform.LookAt(transform.position + Vector3.up);
            }
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
                //SwitchToTPP();
            }
        }
        if (Input.GetMouseButtonUp(1) && isAimingRight)
        {
            PerformShoot(1);
            isAimingRight = false;
            // If no longer aiming with either button, switch back to third-person
            if (!isAimingLeft)
            {
                //SwitchToTPP();
            }
        }
    }

    private void PerformShoot(int button)
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("FPPHide") | 1 << LayerMask.NameToLayer("Portal")); // Ignore FPPHide layer
        if (Physics.Raycast(ray, out hit, maxShootDistance, layerMask))
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("No spawn prefab assigned for portals.");
                return;
            }

            // Place slightly above surface along the normal to avoid clipping
            Vector3 spawnPos = hit.point + hit.normal * spawnOffset;

            // Align object's up to the hit normal
            Quaternion spawnRot = Quaternion.FromToRotation(Vector3.forward, hit.normal);

            if (button == 0)
            {
                if (portal1 != null)
                {
                    Destroy(portal1.gameObject);
                }
                portal1 = Instantiate(portalPrefab, spawnPos, spawnRot).GetComponent<Portal>();
                portal1.attachedSurface = hit.collider.gameObject;
            }
            else if (button == 1)
            {
                if (portal2 != null)
                {
                    Destroy(portal2.gameObject);
                }
                portal2 = Instantiate(portalPrefab, spawnPos, spawnRot).GetComponent<Portal>();
                portal2.attachedSurface = hit.collider.gameObject;
                portal2.isSecondPortal = true;
                portal2.UpdateFrameColor();
            }
            // Link portals if both exist
            if (portal1 != null && portal2 != null)
            {
                portal1.linkedPortal = portal2;
                portal2.linkedPortal = portal1;
            }
        }
    }

    void SwitchToFPP()
    {
        if (!isFPP)
        {
            isFPP = true;
            PlayerUIManager.instance.SetCrosshair(1);
            if (cameraTransform != null)
            {
                cameraTransform.position = transform.position + Vector3.up * eyeHeight;
                cameraTransform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                mainCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("FPPHide"));
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
                mainCamera.cullingMask |= 1 << LayerMask.NameToLayer("FPPHide");
            }
        }
    }

    public override void EnterPortalTrigger()
    {
        if (graphicsClone == null)
        {
            graphicsClone = Instantiate(graphicsObject);
            graphicsClone.transform.parent = graphicsObject.transform.parent;
            graphicsClone.transform.localScale = graphicsObject.transform.localScale;
            graphicsClone.layer = LayerMask.NameToLayer("FPPHidePortal");

            foreach (Transform child in graphicsClone.transform)
            {
                child.gameObject.layer = LayerMask.NameToLayer("FPPHidePortal");
            }

            originalMaterials = GetMaterials(graphicsObject);
            cloneMaterials = GetMaterials(graphicsClone);
        }
        else
        {
            graphicsClone.SetActive(true);
        }
    }

    public override void IgnoreCollision(Collider other, bool ignore)
    {
        Physics.IgnoreCollision(GetComponent<CharacterController>(), other, ignore);
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        // Directly set position and rotation
        transform.position = pos;
        Vector3 eular = rot.eulerAngles;
        float delta = Mathf.DeltaAngle(smoothYaw, eular.y);
        yaw += delta;
        smoothYaw += delta;
        transform.eulerAngles = Vector3.up * smoothYaw;
        velocity = toPortal.TransformVector(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyVector(fromPortal.InverseTransformVector(velocity)));
        Physics.SyncTransforms(); // Ensure physics is updated after teleportation
    }
}