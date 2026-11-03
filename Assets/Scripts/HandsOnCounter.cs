using UnityEngine;

// Keeps a standing character's hands flat on the counter in front of them. The standing animation lets the arms hang
// straight down behind it, so this runs after the Animator each frame and brings them back up.
public class HandsOnCounter : MonoBehaviour
{
    public RestHandsOnThighs.Hand left = new RestHandsOnThighs.Hand();
    public RestHandsOnThighs.Hand right = new RestHandsOnThighs.Hand();

    [Tooltip("Where the middle of each palm rests, in world space.")]
    public Vector3 leftPalm, rightPalm;

    [Tooltip("Height of the wrist above the counter, in metres.")]
    public float palmHeight = 0.045f;

    [Tooltip("Distance from the wrist to the middle of the palm, in metres.")]
    public float palmLength = 0.08f;

    [Tooltip("How far the fingers turn in towards each other, in degrees.")]
    public float turnIn = 14f;

    void LateUpdate()
    {
        Rest();
    }

    public void Rest()
    {
        Lay(left, leftPalm, 1f);
        Lay(right, rightPalm, -1f);
    }

    void Lay(RestHandsOnThighs.Hand hand, Vector3 palm, float side)
    {
        Vector3 along = Quaternion.AngleAxis(turnIn * side, Vector3.up) * transform.forward;
        Vector3 wrist = palm + Vector3.up * palmHeight - along * palmLength;
        RestHandsOnThighs.Reach(hand, wrist, transform.TransformDirection(hand.elbow));
        hand.wrist.rotation = Quaternion.LookRotation(along, Vector3.down) * Quaternion.Inverse(Quaternion.LookRotation(hand.fingers, hand.palm));
    }
}
