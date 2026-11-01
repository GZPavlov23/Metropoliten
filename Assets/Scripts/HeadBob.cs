using System;
using UnityEngine;
using Random = UnityEngine.Random;

// What a person's head does on the move. It rises and falls with every footstep and sways over each
// foot in turn, but the eyes stay locked on what's ahead, so the view turns against the motion.
// That steadiness, more than the size of the bob, is what separates a person from a camera on a spring.
[Serializable]
public class HeadBob
{
    [Serializable]
    public struct Gait
    {
        [Tooltip("Metres covered by each footstep. Longer steps make for a slower rhythm.")]
        public float stepLength;

        [Tooltip("How far the head rises and falls with each step, in metres.")]
        public float rise;

        [Tooltip("How far the head sways to each side, in metres.")]
        public float sway;

        [Tooltip("How far the head surges ahead and lags behind with each step, in metres.")]
        public float surge;

        [Tooltip("How far the head tilts to each side, in degrees.")]
        public float roll;

        [Tooltip("How much lower the head rides than when standing, in metres.")]
        public float dip;
    }

    // How quickly the motion settles in and out, and how quickly a walk turns into a run.
    const float StartRate = 7f, StopRate = 9f, RunRate = 5f;

    // Roughly the average of the height curve, so the head bobs around its resting height.
    const float MeanHeight = 0.55f;

    [Range(0f, 2f)]
    [Tooltip("Scales all of the motion at once. 0 turns it off.")]
    public float amount = 1f;

    [Tooltip("How far ahead the eyes hold steady, in metres. Nearer things sway more, further things less.")]
    public float gazeDistance = 4f;

    public Gait walking = new Gait { stepLength = 1.05f, rise = 0.03f, sway = 0.008f, surge = 0.006f, roll = 0.1f, dip = 0.004f };
    public Gait running = new Gait { stepLength = 1.3f, rise = 0.05f, sway = 0.006f, surge = 0.01f, roll = 0.25f, dip = 0.02f };

    // A foot has just landed. Says how far the gait is from a walk towards a run, from 0 to 1.
    public event Action<float> Stepped;

    // Where the head sits relative to rest, and its pitch, yaw and roll, in degrees.
    public Vector3 Offset { get; private set; }
    public Vector3 Angles { get; private set; }

    float stepProgress;
    float stepSize = 1f, stepStrength = 1f;
    float moving, runBlend;
    bool leftFoot;

    static float Ease(float value, float target, float rate, float deltaTime)
    {
        return Mathf.Lerp(value, target, 1f - Mathf.Exp(-rate * deltaTime));
    }

    static Gait Blend(Gait a, Gait b, float t)
    {
        return new Gait
        {
            stepLength = Mathf.Lerp(a.stepLength, b.stepLength, t),
            rise = Mathf.Lerp(a.rise, b.rise, t),
            sway = Mathf.Lerp(a.sway, b.sway, t),
            surge = Mathf.Lerp(a.surge, b.surge, t),
            roll = Mathf.Lerp(a.roll, b.roll, t),
            dip = Mathf.Lerp(a.dip, b.dip, t),
        };
    }

    // Walking vaults over each planted leg: a rounded top and a firm landing as the next heel takes the weight.
    static float WalkHeight(float step)
    {
        float arc = Mathf.Sin(step * Mathf.PI);
        float landing = step / 0.08f * Mathf.Exp(1f - step / 0.08f);
        return Mathf.Lerp(arc * arc, arc, 0.4f) - landing * 0.12f;
    }

    // Running sinks into the planted leg and flies between steps: a sharp low and a long high.
    static float RunHeight(float step)
    {
        float arc = Mathf.Sin(step * Mathf.PI);
        return Mathf.Pow(arc * arc, 0.7f);
    }

    // Moves the gait on by the ground actually covered, so the steps always match the speed
    // and walking into a wall doesn't bob the head.
    public void Advance(float moved, float walkSpeed, float runSpeed, float deltaTime)
    {
        float speed = moved / Mathf.Max(deltaTime, 0.0001f);
        float pace = Mathf.Clamp01(speed / walkSpeed);
        moving = Ease(moving, pace, pace > moving ? StartRate : StopRate, deltaTime);
        runBlend = Ease(runBlend, Mathf.InverseLerp(walkSpeed, runSpeed, speed), RunRate, deltaTime);

        // Every walk starts on a fresh step.
        if (moving < 0.01f) stepProgress = 0f;

        Gait gait = Blend(walking, running, runBlend);
        stepProgress += moved / (gait.stepLength * stepSize);
        if (stepProgress >= 1f)
        {
            stepProgress = Mathf.Repeat(stepProgress, 1f);
            leftFoot = !leftFoot;

            // Nobody's two steps are the same.
            stepSize = Random.Range(0.96f, 1.04f);
            stepStrength = Random.Range(0.88f, 1.12f);
            Stepped?.Invoke(runBlend);
        }

        float height = Mathf.Lerp(WalkHeight(stepProgress), RunHeight(stepProgress), runBlend);
        float overFoot = (leftFoot ? -1f : 1f) * Mathf.Sin(stepProgress * Mathf.PI);
        float strength = moving * amount;

        float y = (height * stepStrength - MeanHeight) * gait.rise - gait.dip;
        var head = new Vector3(overFoot * gait.sway, y, Mathf.Sin(stepProgress * Mathf.PI * 2f) * gait.surge);
        Offset = head * strength;

        // The eyes hold on a point ahead, so the head turns against whatever it has just been carried through.
        float pitch = Mathf.Atan2(Offset.y, gazeDistance) * Mathf.Rad2Deg;
        float yaw = -Mathf.Atan2(Offset.x, gazeDistance) * Mathf.Rad2Deg;
        Angles = new Vector3(pitch, yaw, overFoot * gait.roll * strength);
    }
}
