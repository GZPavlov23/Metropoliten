using UnityEngine;

// The view behind the title screen: creeping along the tunnel towards the light and back, slowly enough that
// it takes a moment to notice, with the picture pushed sideways to leave the left of the screen for the menu.
[RequireComponent(typeof(Camera))]
public class MenuBackdrop : MonoBehaviour
{
    const float SwayDistance = 0.012f, SwayDegrees = 0.1f, SwayRate = 0.13f, HeadlightDip = 3f;
    static readonly int RoomDim = Shader.PropertyToID("_RoomDim");

    public Vector3 startPosition;
    public Vector3 endPosition;

    [Tooltip("How long the view takes to go along the tunnel one way, in seconds.")]
    public float secondsEachWay = 120f;

    [Tooltip("How far across the screen the middle of the picture sits, as a share of its width.")]
    public float pictureShift = 0.13f;

    [Tooltip("The train's headlight, which falters along with the bad tube.")]
    public Light headlight;

    Camera view;
    Quaternion facing;
    float headlightPower;

    void Awake()
    {
        view = GetComponent<Camera>();
        facing = transform.rotation;
        headlightPower = headlight.intensity;
    }

    void LateUpdate()
    {
        float time = Time.unscaledTime;
        float along = 0.5f - 0.5f * Mathf.Cos(time * Mathf.PI / secondsEachWay);

        float swayX = Mathf.PerlinNoise(time * SwayRate, 0.3f) - 0.5f, swayY = Mathf.PerlinNoise(0.7f, time * SwayRate) - 0.5f;
        transform.position = Vector3.Lerp(startPosition, endPosition, along) + new Vector3(swayX, swayY, 0f) * SwayDistance * 2f;
        transform.rotation = facing * Quaternion.Euler(swayY * SwayDegrees * 2f, swayX * SwayDegrees * 2f, 0f);

        // Moving the picture rather than turning the camera, which keeps the rings of the tunnel round.
        view.ResetProjectionMatrix();
        Matrix4x4 projection = view.projectionMatrix;
        projection.m02 = -pictureShift * 2f;
        view.projectionMatrix = projection;

        headlight.intensity = headlightPower * (1f - HeadlightDip * Shader.GetGlobalFloat(RoomDim));
    }
}
