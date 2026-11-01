using System;
using UnityEngine;

// Lays a seated character's hands flat on their thighs. The sitting animation was made for a different
// body, so on our models the hands would otherwise hang in the air. Runs after the Animator each frame
// and bends the arms down from wherever the animation left them.
public class RestHandsOnThighs : MonoBehaviour
{
    [Serializable]
    public class Hand
    {
        public Transform upperArm, lowerArm, wrist, upperLeg, lowerLeg;

        [Tooltip("Which way the fingers point, in the wrist bone's own space.")]
        public Vector3 fingers;

        [Tooltip("Which way the palm faces, in the wrist bone's own space.")]
        public Vector3 palm;

        [Tooltip("Which way the elbow points, in the character's own space.")]
        public Vector3 elbow;

        [Tooltip("How far the top of the thigh sits above the leg bone, where the hand rests.")]
        public float thighTop;
    }

    public Hand left = new Hand();
    public Hand right = new Hand();

    [Range(0f, 1f)]
    [Tooltip("Where the palm rests, from the hip (0) to the knee (1).")]
    public float alongThigh = 0.55f;

    [Tooltip("Distance from the wrist to the middle of the palm, in metres.")]
    public float palmLength = 0.08f;

    [Tooltip("Height of the wrist above the thigh, in metres.")]
    public float palmHeight = 0.035f;

    [Tooltip("How far in from the middle of the thigh the hand rests, in metres.")]
    public float inset = 0.03f;

    void LateUpdate()
    {
        Rest();
    }

    public void Rest()
    {
        Lay(left);
        Lay(right);
    }

    void Lay(Hand hand)
    {
        Vector3 hip = hand.upperLeg.position, knee = hand.lowerLeg.position;
        Vector3 along = (knee - hip).normalized;
        Vector3 up = Vector3.ProjectOnPlane(Vector3.up, along).normalized;

        Vector3 inwards = transform.right * -Mathf.Sign(hand.elbow.x);
        Vector3 palmOnThigh = Vector3.Lerp(hip, knee, alongThigh) + up * (hand.thighTop + palmHeight) + inwards * inset;
        Vector3 wristTarget = palmOnThigh - along * palmLength;

        // Two bones reaching for a point: the rule of cosines gives where the elbow has to be.
        Vector3 shoulder = hand.upperArm.position;
        float upperLength = Vector3.Distance(shoulder, hand.lowerArm.position);
        float lowerLength = Vector3.Distance(hand.lowerArm.position, hand.wrist.position);
        Vector3 reach = wristTarget - shoulder;
        float distance = Mathf.Min(reach.magnitude, upperLength + lowerLength - 0.001f);
        float elbowAlong = (upperLength * upperLength - lowerLength * lowerLength + distance * distance) / (2f * distance);
        float elbowOut = Mathf.Sqrt(Mathf.Max(upperLength * upperLength - elbowAlong * elbowAlong, 0f));
        Vector3 bend = Vector3.ProjectOnPlane(transform.TransformDirection(hand.elbow), reach).normalized;
        Vector3 elbow = shoulder + reach.normalized * elbowAlong + bend * elbowOut;

        hand.upperArm.rotation = Quaternion.FromToRotation(hand.lowerArm.position - shoulder, elbow - shoulder) * hand.upperArm.rotation;
        hand.lowerArm.rotation = Quaternion.FromToRotation(hand.wrist.position - hand.lowerArm.position, wristTarget - elbow) * hand.lowerArm.rotation;
        hand.wrist.rotation = Quaternion.LookRotation(along, -up) * Quaternion.Inverse(Quaternion.LookRotation(hand.fingers, hand.palm));
    }
}
