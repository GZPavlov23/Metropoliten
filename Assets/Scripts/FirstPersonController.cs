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

    [Header("View Bobbing")]
    public bool viewBobbing = true;
    [Tooltip("Metres walked per footstep. Longer steps bob more slowly.")]
    public float stepLength = 0.85f;
    [Tooltip("How far the head rises and falls with each step, in metres.")]
    public float bobHeight = 0.028f;
    [Tooltip("How far the head sways sideways with each stride, in metres.")]
    public float bobSway = 0.014f;
    [Tooltip("How far the head tilts with each stride, in degrees.")]
    public float bobRoll = 0.3f;
    [Tooltip("How quickly the bobbing settles in and out when you start or stop walking.")]
    public float bobSettle = 7f;
    [Tooltip("How quickly the camera catches up with hard height changes like stair steps.")]
    public float heightSmoothing = 14f;

    [Header("Conversations")]
    [Tooltip("How quickly the view turns towards whoever you're talking to.")]
    public float attentionTurnSpeed = 4f;

    private CharacterController controller;
    private Vector3 velocity;
    private float rotationX = 0f;

    private Vector3 cameraRestPosition;
    private Vector3 lastPosition;
    private float stepPhase;
    private float bobStrength;
    private float smoothedHeight;
    private Transform attention;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        cameraRestPosition = cameraTransform.localPosition;
        lastPosition = transform.position;
        smoothedHeight = transform.position.y;
    }

    void Update()
    {
        if (attention != null) TurnTowards(attention.position);
        else HandleMouseLook();

        HandleMovement();
        HandleViewBob();
    }

    // Stops the player walking and looking around, and turns the view to the target until released.
    public void HoldAttention(Transform target)
    {
        attention = target;
    }

    public void ReleaseAttention()
    {
        attention = null;
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
    }

    void TurnTowards(Vector3 point)
    {
        Vector3 toPoint = point - cameraTransform.position;
        float targetYaw = Mathf.Atan2(toPoint.x, toPoint.z) * Mathf.Rad2Deg;
        float targetPitch = -Mathf.Atan2(toPoint.y, new Vector2(toPoint.x, toPoint.z).magnitude) * Mathf.Rad2Deg;

        float blend = 1f - Mathf.Exp(-attentionTurnSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0f, Mathf.LerpAngle(transform.eulerAngles.y, targetYaw, blend), 0f);
        rotationX = Mathf.Lerp(rotationX, targetPitch, blend);
    }

    void HandleMovement()
    {
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float currentSpeed = isSprinting ? sprintSpeed : walkSpeed;

        float moveX = attention != null ? 0f : Input.GetAxis("Horizontal"); // A/D
        float moveZ = attention != null ? 0f : Input.GetAxis("Vertical");   // W/S

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

    void HandleViewBob()
    {
        // Measured from how far the body really moved, so walking into a wall doesn't bob the head.
        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        lastPosition = transform.position;

        float distance = moved.magnitude;
        float speed = distance / Mathf.Max(Time.deltaTime, 0.0001f);
        float targetStrength = viewBobbing ? Mathf.Clamp(speed / walkSpeed, 0f, 1.35f) : 0f;
        bobStrength = Mathf.Lerp(bobStrength, targetStrength, 1f - Mathf.Exp(-bobSettle * Time.deltaTime));

        // One footstep is half a turn of the phase: the head dips as each foot lands and sways over it.
        stepPhase = (stepPhase + distance / stepLength * Mathf.PI) % (Mathf.PI * 2f);
        float dip = -Mathf.Cos(stepPhase * 2f) * bobHeight * bobStrength;
        float sway = Mathf.Sin(stepPhase) * bobSway * bobStrength;
        float roll = Mathf.Sin(stepPhase) * bobRoll * bobStrength;

        // The controller climbs stairs in hard jumps; the head glides over them instead.
        float height = transform.position.y;
        smoothedHeight = Mathf.Lerp(smoothedHeight, height, 1f - Mathf.Exp(-heightSmoothing * Time.deltaTime));
        float heightLag = Mathf.Clamp(smoothedHeight - height, -0.3f, 0.3f);

        cameraTransform.localPosition = cameraRestPosition + new Vector3(sway, dip + heightLag, 0f);
        cameraTransform.localRotation = Quaternion.Euler(rotationX, 0f, roll);
    }
}
