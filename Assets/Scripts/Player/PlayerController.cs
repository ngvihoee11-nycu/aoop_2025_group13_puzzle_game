using UnityEngine;

public class PlayerController : Singleton<PlayerController>
{
    public float moveSpeed = 5f;
    public Vector3 cameraOffset;
    private CharacterController characterController;

    private Vector3 movement;
    
    [Header("Camera")]
    public Transform cameraTransform;
    public float mouseSensitivity = 100f;
    public float minPitch = -40f;
    public float maxPitch = 85f;
    private float pitch = 0f;
    private float yaw = 0f;

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

        // Handle Camera Rotation (mouse look)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        if (cameraTransform != null)
        {
            // Recompute camera position from stored offset using yaw/pitch
            Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
            cameraTransform.position = transform.position + rot * cameraOffset;
            cameraTransform.LookAt(transform.position + Vector3.up);

            // Rotate player horizontally to match camera yaw
            Vector3 playerEuler = transform.eulerAngles;
            playerEuler.y = yaw;
            transform.eulerAngles = playerEuler;
        }
    }

    void FixedUpdate()
    {
        characterController.Move(movement * moveSpeed * Time.fixedDeltaTime);
    }
}