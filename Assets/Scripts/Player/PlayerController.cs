using UnityEngine;

public class PlayerController : PortalTravellerSingleton<PlayerController>
{
    [Header("Movement")]
    public float walkSpeed = 4f;
    public float runSpeed = 8f;
    public float airSpeed = 2f;
    public float gravity = -9.81f;
    public float jumpHeight = 1.5f;
    public float smoothMoveTime = 0.1f;
    public float airDampTime = 1f;
    private bool customGrounded = true;
    private Vector3 smoothV;
    private Vector3 preMovePosition;
    private Vector3 velocity;
    private CharacterController characterController;
    private bool movedAgain;
    private bool layerMaskSwapped = false;

    [Header("Camera")]
    public bool lockCursor;
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
    private Vector3 modelSmoothRotV;


    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        mainCamera = MainCamera.instance.GetCamera();
        characterController = GetComponent<CharacterController>();
        if (characterController == null)
        {
            Debug.LogError("CharacterController component is missing on Player!");
        }

        if (graphicsObject == null)
        {
            Debug.LogError("Graphics object is missing on player!");
        }

        graphicsObject.layer = LayerMask.NameToLayer("Player");
        foreach (Transform child in graphicsObject.GetComponentsInChildren<Transform>())
        {
            child.gameObject.layer = LayerMask.NameToLayer("Player");
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
        // Reset Player Position if Player falls below certain Y level
        if (transform.position.y < -20f)
        {
            LevelManager.instance.ResetPlayerPosition();
            velocity = Vector3.zero;
            return;
        }

        if ((lockCursor && Input.GetKeyDown(KeyCode.Tab)) || (!lockCursor && Input.GetMouseButtonUp(0)))
        {
            lockCursor = !lockCursor;
            if (lockCursor)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        bool grounded = characterController.isGrounded;

        Vector2 input = new Vector2(0f, 0f);

        if (lockCursor)
        {
            input.x = Input.GetAxis("Horizontal");
            input.y = Input.GetAxis("Vertical");
        }

        Vector3 inputDir = new Vector3(input.x, 0, input.y).normalized;
        Vector3 worldInputDir = transform.TransformDirection(inputDir);

        float movingSpeed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;

        if (grounded && customGrounded)
        {
            Vector3 inputVelocity = worldInputDir * movingSpeed;
            float verticalVelocity = velocity.y;
            // Small negative value to keep the controller grounded
            if (velocity.y < 0f)
            {
                verticalVelocity = -0.2f;
            }
            // v = sqrt(2 * g * h) but gravity is negative so use -2 * gravity
            if (lockCursor && Input.GetButtonDown("Jump"))
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            velocity = Vector3.SmoothDamp(velocity, inputVelocity, ref smoothV, smoothMoveTime);
            velocity.y = verticalVelocity;
        }
        else
        {
            Vector3 inputVelocity = worldInputDir * airSpeed;
            velocity.y += gravity * Time.deltaTime;
            velocity.y = Mathf.SmoothDamp(velocity.y, 0f, ref smoothV.y, airDampTime);

            if (inputVelocity.x != 0 && (Mathf.Sign(inputVelocity.x) != Mathf.Sign(velocity.x) || Mathf.Abs(inputVelocity.x) > Mathf.Abs(velocity.x)))
            {
                velocity.x = Mathf.SmoothDamp(velocity.x, inputVelocity.x, ref smoothV.x, smoothMoveTime);
            }
            else
            {
                velocity.x = Mathf.SmoothDamp(velocity.x, inputVelocity.x, ref smoothV.x, airDampTime);
            }

            if (inputVelocity.z != 0 && (Mathf.Sign(inputVelocity.z) != Mathf.Sign(velocity.z) || Mathf.Abs(inputVelocity.z) > Mathf.Abs(velocity.z)))
            {
                velocity.z = Mathf.SmoothDamp(velocity.z, inputVelocity.z, ref smoothV.z, smoothMoveTime);
            }
            else
            {
                velocity.z = Mathf.SmoothDamp(velocity.z, inputVelocity.z, ref smoothV.z, airDampTime);   
            }
        }


        movedAgain = false;
        preMovePosition = transform.position;
        characterController.Move(velocity * Time.deltaTime);

        customGrounded = movedAgain ? false : characterController.isGrounded;

        if (lockCursor)
        {
            // Handle Camera Rotation (mouse look)
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

            yaw = (yaw + mouseX) % 360f;
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        smoothPitch = Mathf.SmoothDampAngle(smoothPitch, pitch, ref pitchSmoothV, smoothRotationTime);
        smoothYaw = Mathf.SmoothDampAngle(smoothYaw, yaw, ref yawSmoothV, smoothRotationTime);
        
        transform.eulerAngles = Vector3.up * smoothYaw;

        if (graphicsObject)
        {
            Vector3 modelRot = graphicsObject.transform.localEulerAngles;
            modelRot.x = Mathf.SmoothDampAngle(modelRot.x, 0, ref modelSmoothRotV.x, smoothModelRotationTime);
            modelRot.y = Mathf.SmoothDampAngle(modelRot.y, 0, ref modelSmoothRotV.y, smoothModelRotationTime);
            modelRot.z = Mathf.SmoothDampAngle(modelRot.z, 0, ref modelSmoothRotV.z, smoothModelRotationTime);
            graphicsObject.transform.localEulerAngles = modelRot;
        }

        eyeTransform.localPosition = Vector3.SmoothDamp(eyeTransform.localPosition, eyeOffset, ref eyeSmoothPosV, smoothAxisChangeTime);
        
        Vector3 eyeRot = eyeTransform.eulerAngles;
        eyeRot.x = smoothPitch;
        eyeRot.y = smoothYaw;
        eyeRot.z = Mathf.SmoothDampAngle(eyeRot.z, 0, ref eyeSmoothRotV, smoothAxisChangeTime);
        eyeTransform.eulerAngles = eyeRot;
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.collider.CompareTag("Portal"))
        {
            Portal portal = hit.collider.GetComponentInParent<Portal>();
            if (portal.linkedPortal)
            {
                portal.OnTravellerEnter(this, true);
                if(!movedAgain)
                {
                    movedAgain = true;
                    transform.position = preMovePosition;
                    Physics.SyncTransforms();
                    characterController.Move(velocity * Time.deltaTime);
                }
            }
        }
        else if (Vector3.Distance(hit.normal, Vector3.up) >= 0.01f && Vector3.Dot(velocity, hit.normal) < 0f)
        {
            velocity -= Vector3.Dot(velocity, hit.normal) * hit.normal;
        }
    }

    public void SetLayerMask(bool swap)
    {
        if (swap == layerMaskSwapped) return;

        layerMaskSwapped = swap;
        if (swap)
        {
            graphicsObject.layer = LayerMask.NameToLayer("Clone Player");
            foreach (Transform child in graphicsObject.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("Clone Player");
            }

            if (graphicsClone)
            {
                graphicsClone.layer = LayerMask.NameToLayer("Player");
                foreach (Transform child in graphicsClone.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Player");
                }
            }
        }
        else
        {
            graphicsObject.layer = LayerMask.NameToLayer("Player");
            foreach (Transform child in graphicsObject.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("Player");
            }

            if (graphicsClone)
            {
                graphicsClone.layer = LayerMask.NameToLayer("Clone Player");
                foreach (Transform child in graphicsClone.GetComponentsInChildren<Transform>())
                {
                    child.gameObject.layer = LayerMask.NameToLayer("Clone Player");
                }
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
            
            layerMaskSwapped = false;
            graphicsClone.layer = LayerMask.NameToLayer("Clone Player");
            foreach (Transform child in graphicsClone.GetComponentsInChildren<Transform>())
            {
                child.gameObject.layer = LayerMask.NameToLayer("Clone Player");
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

    public override void AdjustClone(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {
        Matrix4x4 modelM = toPortal.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * fromPortal.worldToLocalMatrix * graphicsObject.transform.localToWorldMatrix;
        graphicsClone.transform.position = modelM.GetPosition();
        graphicsClone.transform.rotation = modelM.rotation;
    }

    public override void Teleport(Transform fromPortal, Transform toPortal, Vector3 pos, Quaternion rot)
    {

        Matrix4x4 teleportMatrix = toPortal.localToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)) * fromPortal.worldToLocalMatrix;

        Matrix4x4 eyeM = teleportMatrix * eyeTransform.localToWorldMatrix;
        Matrix4x4 modelM = teleportMatrix * graphicsObject.transform.localToWorldMatrix;

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

        graphicsObject.transform.position = modelM.GetPosition();
        graphicsObject.transform.rotation = modelM.rotation;

        velocity = toPortal.TransformVector(Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f)).MultiplyVector(fromPortal.InverseTransformVector(velocity)));

        if (Vector3.Distance(toPortal.forward, Vector3.up) < 0.1f)
        {
            velocity.y = Mathf.Max(velocity.y, -0.2f * gravity);
            customGrounded = false;
        }

        PlayerPickup playerPickup = PlayerPickup.instance;
        playerPickup.TeleportHoldPoint(toPortal, fromPortal);

        Physics.SyncTransforms(); // Ensure physics is updated after teleportation
    }

    public override Collider GetCollider()
    {
        return characterController;
    }

}