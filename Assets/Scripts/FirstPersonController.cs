using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    public float lookXLimit = 80f;

    private CharacterController controller;
    private Vector3 velocity;
    private float rotationX = 0f;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        // Rotate camera up/down, clamped so you can't flip upside down
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
    }

    void HandleMovement()
    {
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        float moveX = Input.GetAxis("Horizontal"); // A/D
        float moveZ = Input.GetAxis("Vertical");   // W/S

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f; // small downward force to keep grounded
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}