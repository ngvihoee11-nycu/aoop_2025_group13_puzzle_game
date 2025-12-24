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
    public Transform eyeTransform;
    public Vector3 eyeOffset = new Vector3(0f, 0.375f, 0f);
    public float mouseSensitivity = 100f;
    public float minPitch = -90f;
    public float maxPitch = 90f;
    public float smoothRotationTime = 0.1f;
    public float pitch = 0f;
    public float yaw = 0f;
    public float smoothAxisChangeTime = 0.2f;
    public float smoothModelRotationTime = 0.25f;
    private Camera mainCamera;
    private float smoothPitch;
    private float smoothYaw;
    private float pitchSmoothV;
    private float yawSmoothV;
    private Vector3 eyeSmoothPosV;
    private float eyeSmoothRotV;
    private Vector3 modelRotSmoothV;

    [Header("Shooting")]
    public GameObject portalPrefab;
    public float maxShootDistance = 100f;
    private bool isAimingLeft = false;
    private bool isAimingRight = false;

    private bool layerMaskSwapped = false;

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

        if (eyeTransform == null)
        {
            Debug.LogError("Eye Transform missing!");
        }

        eyeTransform.localPosition = eyeOffset;
        Vector3 angles = eyeTransform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
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
            verticalVelocity = -0.02f;
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

        yaw = (yaw + mouseX) % 360f;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, smoothRotationTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, smoothRotationTime);
        
        transform.eulerAngles = Vector3.up * smoothYaw;

        if (graphicsObject)
        {
            Vector3 modelRot = graphicsObject.transform.localEulerAngles;
            modelRot.x = Mathf.SmoothDampAngle(modelRot.x, 0, ref modelRotSmoothV.x, smoothModelRotationTime);
            modelRot.y = Mathf.SmoothDampAngle(modelRot.y, 0, ref modelRotSmoothV.y, smoothModelRotationTime);
            modelRot.z = Mathf.SmoothDampAngle(modelRot.z, 0, ref modelRotSmoothV.z, smoothModelRotationTime);
            graphicsObject.transform.localEulerAngles = modelRot;
        }

        eyeTransform.localPosition = Vector3.SmoothDamp(eyeTransform.localPosition, eyeOffset, ref eyeSmoothPosV, smoothAxisChangeTime);
        
        Vector3 eyeRot = eyeTransform.eulerAngles;
        eyeRot.x = smoothPitch;
        eyeRot.y = smoothYaw;
        eyeRot.z = Mathf.SmoothDampAngle(eyeRot.z, 0, ref eyeSmoothRotV, smoothAxisChangeTime);
        eyeTransform.eulerAngles = eyeRot;

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

    private void PerformShoot(int button)
    {
        Ray ray = new Ray(eyeTransform.position, eyeTransform.forward);
        RaycastHit hit;
        int layerMask = ~(1 << LayerMask.NameToLayer("FPPHide") | 1 << LayerMask.NameToLayer("Portal")); // Ignore FPPHide layer
        if (Physics.Raycast(ray, out hit, maxShootDistance, layerMask))
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("No spawn prefab assigned for portals.");
                return;
            }

            if (button == 0)
            {
                if (portal1 != null)
                {
                    Destroy(portal1.gameObject);
                }
                portal1 = Portal.SpawnPortal(portalPrefab, portal2, hit, eyeTransform, false);
            }
            else if (button == 1)
            {
                if (portal2 != null)
                {
                    Destroy(portal2.gameObject);
                }
                portal2 = Portal.SpawnPortal(portalPrefab, portal1, hit, eyeTransform, true);
            }
        }
    }

    public void SetLayerMask(bool swap)
    {
        if (swap == layerMaskSwapped) return;

        layerMaskSwapped = swap;
        if (swap)
        {
            graphicsObject.layer = LayerMask.NameToLayer("FPPHidePortal");
            foreach (Transform child in graphicsObject.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("FPPHidePortal");
            }

            if (graphicsClone)
            {
                graphicsClone.layer = LayerMask.NameToLayer("FPPHide");
                foreach (Transform child in graphicsClone.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("FPPHide");
                }
            }
        }
        else
        {
            graphicsObject.layer = LayerMask.NameToLayer("FPPHide");
            foreach (Transform child in graphicsObject.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("FPPHide");
            }

            if (graphicsClone)
            {
                graphicsClone.layer = LayerMask.NameToLayer("FPPHidePortal");
                foreach (Transform child in graphicsClone.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("FPPHidePortal");
                }
            }
        }
    }

    public override void IgnoreCollision(Collider other, bool ignore)
    {
        Physics.IgnoreCollision(GetComponent<CharacterController>(), other, ignore);
    }

    public override void AdjustClone(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        graphicsClone.transform.position = pos;
        graphicsClone.transform.rotation = (toPortal.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * fromPortal.worldToLocalMatrix * graphicsObject.transform.localToWorldMatrix).rotation;
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {

        Matrix4x4 teleportMatrix = toPortal.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * fromPortal.worldToLocalMatrix;

        Matrix4x4 eyeM = teleportMatrix * eyeTransform.localToWorldMatrix;
        Quaternion graphicsRotation = (teleportMatrix * graphicsObject.transform.localToWorldMatrix).rotation;

        transform.position = pos;

        eyeTransform.position = eyeM.GetPosition();

        Vector3 euler = eyeM.rotation.eulerAngles;
        float deltaPitch = Mathf.DeltaAngle(smoothPitch, euler.x);
        float deltaYaw = Mathf.DeltaAngle(smoothYaw, euler.y);
        pitch += deltaPitch;
        pitch = Mathf.Clamp(180f - (180f - pitch) % 360f, minPitch, maxPitch);
        smoothPitch += deltaPitch;
        yaw += deltaYaw;
        smoothYaw += deltaYaw;
        transform.eulerAngles = Vector3.up * smoothYaw;

        eyeTransform.rotation = eyeM.rotation;

        graphicsObject.transform.rotation = graphicsRotation;

        velocity = toPortal.TransformVector(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyVector(fromPortal.InverseTransformVector(velocity)));

        Physics.SyncTransforms(); // Ensure physics is updated after teleportation
    }
}