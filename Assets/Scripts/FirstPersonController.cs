using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 3.5f;
    public float sprintSpeed = 6f;
    public float gravity = -9.81f;
    [Tooltip("How sharply the body answers the keys, in metres per second per second. High enough to feel instant.")]
    public float acceleration = 30f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public Transform cameraTransform;
    public float lookXLimit = 80f;

    [Header("Head")]
    public bool viewBobbing = true;
    public HeadBob headBob = new HeadBob();
    [Tooltip("How quickly the camera catches up with hard height changes like stair steps.")]
    public float heightSmoothing = 14f;

    [Header("Conversations")]
    [Tooltip("How quickly the view turns towards whoever you're talking to.")]
    public float attentionTurnSpeed = 4f;

    [Header("Sitting")]
    [Tooltip("How far the player can look to either side while seated, in degrees.")]
    public float seatedLookLimit = 115f;
    public float settleSpeed = 5f;

    // A seat's own movement, added to the head on top of everything else.
    public Vector3 Shake { get; set; }

    private CharacterController controller;
    private Vector3 walkVelocity;
    private float fallSpeed;
    private float rotationX = 0f;

    private Vector3 cameraRestPosition;
    private Vector3 lastPosition;
    private float smoothedHeight;
    private Transform attention;
    private Transform seat;

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

        if (seat != null) SettleIntoSeat();
        else HandleMovement();

        HandleHead();
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

    // Hands the body over to a seat. The player can still look round, but the walking keys are free for other things.
    public void Sit(Transform place)
    {
        seat = place;
        controller.enabled = false;
        walkVelocity = Vector3.zero;
        transform.SetParent(place, true);
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        // Rotate player body left/right
        transform.Rotate(Vector3.up * mouseX);

        if (seat != null)
        {
            float yaw = Mathf.DeltaAngle(0f, transform.localEulerAngles.y);
            transform.localRotation = Quaternion.Euler(0f, Mathf.Clamp(yaw, -seatedLookLimit, seatedLookLimit), 0f);
        }

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

        // Raw axes: Unity's smoothed ones take a third of a second to answer a key.
        Vector3 keys = Vector3.zero;
        if (attention == null) keys = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical")); // A/D, W/S

        // Clamped so that going diagonally is no faster than going straight.
        Vector3 wanted = transform.TransformDirection(Vector3.ClampMagnitude(keys, 1f)) * currentSpeed;
        walkVelocity = Vector3.MoveTowards(walkVelocity, wanted, acceleration * Time.deltaTime);

        // Gravity
        if (controller.isGrounded && fallSpeed < 0f)
        {
            fallSpeed = -2f; // small downward force to keep grounded
        }
        fallSpeed += gravity * Time.deltaTime;

        controller.Move((walkVelocity + Vector3.up * fallSpeed) * Time.deltaTime);
    }

    void SettleIntoSeat()
    {
        float blend = 1f - Mathf.Exp(-settleSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, Vector3.zero, blend);
    }

    void HandleHead()
    {
        // Measured from how far the body really moved along the ground, so sitting in a moving train doesn't count.
        Vector3 moved = transform.position - lastPosition;
        moved.y = 0f;
        lastPosition = transform.position;

        headBob.Advance(seat == null ? moved.magnitude : 0f, walkSpeed, sprintSpeed, Time.deltaTime);

        // The controller climbs stairs in hard jumps; the head glides over them instead.
        float height = transform.position.y;
        smoothedHeight = Mathf.Lerp(smoothedHeight, height, 1f - Mathf.Exp(-heightSmoothing * Time.deltaTime));
        float heightLag = Mathf.Clamp(smoothedHeight - height, -0.3f, 0.3f);

        // The gait keeps going with the bobbing turned off, because the footsteps come from it.
        Vector3 bob = viewBobbing ? headBob.Offset : Vector3.zero;
        Vector3 tilt = viewBobbing ? headBob.Angles : Vector3.zero;

        cameraTransform.localPosition = cameraRestPosition + bob + Vector3.up * heightLag + Shake;
        cameraTransform.localRotation = Quaternion.Euler(rotationX + tilt.x, tilt.y, tilt.z);
    }
}
