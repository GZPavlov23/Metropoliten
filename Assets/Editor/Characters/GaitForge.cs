using System.IO;
using UnityEditor;
using UnityEngine;

// Makes walking clips for the people on the platforms. A walk is worked out on the rig the way a body does it:
// each foot is planted on the ground and rolled over, the leg reaches it with the knee in front, and the arms and
// shoulders swing against the hips. Each frame is then read back as a humanoid pose, so that it fits everyone.
public static class GaitForge
{
    public class Gait
    {
        public string Name;

        // In metres per second, on a body of the standard height.
        public float Speed = 1.25f;
        public float StepLength = 0.66f;
        public float FootLift = 0.09f, HipDrop = 0.026f, Bounce = 0.026f, Sway = 0.02f;
        public float ArmSwing = 18f, ElbowBend = 12f, Lean = 3f, Stoop;

        // One leg shorter in the stride than the other, for a tired or a bad hip.
        public float Limp;
    }

    public const string ClipDir = "Assets/Art/Generated/Characters/Clips";
    const int FramesPerCycle = 36;
    const float StanceShare = 0.62f, DesignHeight = 1.75f;

    public static readonly Gait Everyday = new Gait { Name = "Walk" };
    public static readonly Gait Brisk = new Gait { Name = "WalkBrisk", Speed = 1.55f, StepLength = 0.76f, ArmSwing = 24f, ElbowBend = 18f, HipDrop = 0.034f, Bounce = 0.034f, Lean = 5f };
    public static readonly Gait Weary = new Gait { Name = "WalkWeary", Speed = 0.95f, StepLength = 0.54f, ArmSwing = 10f, ElbowBend = 10f, FootLift = 0.06f, HipDrop = 0.018f, Bounce = 0.018f, Lean = 7f, Stoop = 10f, Limp = 0.1f };
    public static readonly Gait Shuffle = new Gait { Name = "WalkOld", Speed = 0.68f, StepLength = 0.4f, ArmSwing = 6f, ElbowBend = 22f, FootLift = 0.04f, HipDrop = 0.012f, Bounce = 0.01f, Sway = 0.032f, Lean = 11f, Stoop = 18f };

    public static float CycleSeconds(Gait gait) => 2f * gait.StepLength / gait.Speed;

    public static string Path(Gait gait) => $"{ClipDir}/A_{gait.Name}.anim";

    public static AnimationClip Load(Gait gait) => AssetDatabase.LoadAssetAtPath<AnimationClip>(Path(gait));

    public static void ForgeAll()
    {
        Directory.CreateDirectory(ClipDir);
        foreach (Gait gait in new[] { Everyday, Brisk, Weary, Shuffle }) Forge(gait);
        AssetDatabase.SaveAssets();
    }

    // Every frame starts over from the bind pose, so nothing carries from one to the next.
    static void Forge(Gait gait)
    {
        GameObject rig = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CharacterForge.Rig));
        rig.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        rig.transform.localScale = Vector3.one;
        rig.transform.localScale = Vector3.one * (DesignHeight / OpeningSceneBuilder.MeasurePose(rig).size.y);

        Animator animator = rig.GetComponent<Animator>();
        var body = new Rig(animator);
        var handler = new HumanPoseHandler(animator.avatar, rig.transform);
        var pose = new HumanPose();

        string[] muscles = HumanTrait.MuscleName;
        var curves = new AnimationCurve[muscles.Length];
        for (int i = 0; i < curves.Length; i++) curves[i] = new AnimationCurve();
        var rootPosition = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
        var rootRotation = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };

        float seconds = CycleSeconds(gait);
        for (int frame = 0; frame <= FramesPerCycle; frame++)
        {
            float phase = (float)frame / FramesPerCycle, time = phase * seconds;
            body.Set(gait, phase);
            handler.GetHumanPose(ref pose);

            for (int i = 0; i < curves.Length; i++) curves[i].AddKey(time, pose.muscles[i]);
            for (int i = 0; i < 3; i++) rootPosition[i].AddKey(time, pose.bodyPosition[i]);
            for (int i = 0; i < 4; i++) rootRotation[i].AddKey(time, pose.bodyRotation[i]);
        }
        handler.Dispose();
        Object.DestroyImmediate(rig);

        AnimationClip clip = Load(gait);
        if (clip == null)
        {
            clip = new AnimationClip { name = $"A_{gait.Name}" };
            AssetDatabase.CreateAsset(clip, Path(gait));
        }
        clip.ClearCurves();

        for (int i = 0; i < curves.Length; i++) Bind(clip, muscles[i], curves[i]);
        string[] axes = { "x", "y", "z", "w" };
        for (int i = 0; i < 3; i++) Bind(clip, $"RootT.{axes[i]}", rootPosition[i]);
        for (int i = 0; i < 4; i++) Bind(clip, $"RootQ.{axes[i]}", rootRotation[i]);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = true;
        settings.loopBlend = false;
        settings.loopBlendOrientation = true;
        settings.loopBlendPositionXZ = true;
        settings.loopBlendPositionY = true;
        settings.keepOriginalOrientation = true;
        settings.keepOriginalPositionXZ = true;
        settings.keepOriginalPositionY = true;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
    }

    static void Bind(AnimationClip clip, string property, AnimationCurve curve)
    {
        AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Animator), property), curve);
    }

    // The skeleton, and what it looked like in the bind pose.
    class Rig
    {
        readonly Transform hips, spine, chest, neck, head;
        readonly Limb leftLeg, rightLeg, leftArm, rightArm;
        readonly Transform leftFoot, rightFoot;
        readonly Vector3 hipsAt;
        readonly Quaternion hipsRest, spineRest, chestRest, neckRest, headRest, leftFootRest, rightFootRest;
        readonly float ankleHeight;

        class Limb
        {
            public readonly Transform Upper, Lower, End;
            public readonly Quaternion UpperRest, LowerRest, EndRest;
            public readonly float UpperLength, LowerLength;
            public readonly Vector3 UpperAim, LowerAim;

            public Limb(Transform upper, Transform lower, Transform end)
            {
                Upper = upper;
                Lower = lower;
                End = end;
                UpperRest = upper.rotation;
                LowerRest = lower.rotation;
                EndRest = end.rotation;
                UpperLength = Vector3.Distance(upper.position, lower.position);
                LowerLength = Vector3.Distance(lower.position, end.position);
                UpperAim = (lower.position - upper.position).normalized;
                LowerAim = (end.position - lower.position).normalized;
            }

            public void Rest()
            {
                Upper.rotation = UpperRest;
                Lower.rotation = LowerRest;
                End.rotation = EndRest;
            }

            // Aims the two bones at a point, with the joint between them pushed towards pole.
            public void Reach(Vector3 target, Vector3 pole)
            {
                Vector3 start = Upper.position, toTarget = target - start;
                float distance = Mathf.Min(toTarget.magnitude, UpperLength + LowerLength - 0.002f);
                float along = (UpperLength * UpperLength - LowerLength * LowerLength + distance * distance) / (2f * distance);
                float offset = Mathf.Sqrt(Mathf.Max(UpperLength * UpperLength - along * along, 0f));
                Vector3 bend = Vector3.ProjectOnPlane(pole, toTarget).normalized;
                Vector3 joint = start + toTarget.normalized * along + bend * offset;
                Aim(joint, start + toTarget.normalized * distance);
            }

            public void Aim(Vector3 joint, Vector3 target)
            {
                Upper.rotation = Quaternion.FromToRotation(UpperAim, (joint - Upper.position).normalized) * UpperRest;
                Lower.rotation = Quaternion.FromToRotation(LowerAim, (target - joint).normalized) * LowerRest;
            }
        }

        public Rig(Animator animator)
        {
            Transform Bone(HumanBodyBones bone) => animator.GetBoneTransform(bone);
            hips = Bone(HumanBodyBones.Hips);
            spine = Bone(HumanBodyBones.Spine);
            chest = Bone(HumanBodyBones.Chest);
            neck = Bone(HumanBodyBones.Neck);
            head = Bone(HumanBodyBones.Head);
            leftFoot = Bone(HumanBodyBones.LeftFoot);
            rightFoot = Bone(HumanBodyBones.RightFoot);
            leftLeg = new Limb(Bone(HumanBodyBones.LeftUpperLeg), Bone(HumanBodyBones.LeftLowerLeg), leftFoot);
            rightLeg = new Limb(Bone(HumanBodyBones.RightUpperLeg), Bone(HumanBodyBones.RightLowerLeg), rightFoot);
            leftArm = new Limb(Bone(HumanBodyBones.LeftUpperArm), Bone(HumanBodyBones.LeftLowerArm), Bone(HumanBodyBones.LeftHand));
            rightArm = new Limb(Bone(HumanBodyBones.RightUpperArm), Bone(HumanBodyBones.RightLowerArm), Bone(HumanBodyBones.RightHand));

            hipsAt = hips.position;
            hipsRest = hips.rotation;
            spineRest = spine.rotation;
            chestRest = chest.rotation;
            neckRest = neck.rotation;
            headRest = head.rotation;
            leftFootRest = leftFoot.rotation;
            rightFootRest = rightFoot.rotation;
            ankleHeight = leftFoot.position.y;
        }

        public void Set(Gait gait, float phase)
        {
            float cycle = phase * Mathf.PI * 2f, step = cycle * 2f;

            // Lowest as the feet are furthest apart, and leaning over each foot as it takes the weight.
            float swayLeft = Mathf.Cos(cycle + 0.5f);
            Vector3 pelvis = hipsAt + new Vector3(-swayLeft * gait.Sway, -gait.HipDrop + gait.Bounce * -Mathf.Cos(step), 0f);
            float twist = Mathf.Sin(cycle) * 7f;

            hips.SetPositionAndRotation(pelvis, Quaternion.Euler(gait.Lean * 0.5f, twist, swayLeft * 3f) * hipsRest);
            spine.rotation = Quaternion.Euler(gait.Lean * 0.9f + gait.Stoop * 0.4f, -twist * 0.4f, -swayLeft * 1.5f) * spineRest;
            chest.rotation = Quaternion.Euler(gait.Lean * 1.3f + gait.Stoop * 0.8f, -twist * 1.3f, -swayLeft * 2f) * chestRest;
            neck.rotation = Quaternion.Euler(gait.Lean * 0.4f - gait.Stoop * 0.3f, -twist * 0.3f, 0f) * neckRest;
            head.rotation = Quaternion.Euler(-gait.Lean * 0.4f - gait.Stoop * 0.8f, 0f, 0f) * headRest;

            StepLeg(leftLeg, leftFoot, leftFootRest, gait, phase, 1f, pelvis, gait.Limp);
            StepLeg(rightLeg, rightFoot, rightFootRest, gait, phase + 0.5f, -1f, pelvis, 0f);

            // Each arm goes with the opposite leg.
            SwingArm(leftArm, gait, phase + 0.5f, -1f);
            SwingArm(rightArm, gait, phase, 1f);
        }

        void StepLeg(Limb leg, Transform foot, Quaternion footRest, Gait gait, float phase, float side, Vector3 pelvis, float limp)
        {
            leg.Rest();
            phase = Mathf.Repeat(phase, 1f);
            float reach = gait.StepLength * (1f - limp);

            float z, lift = 0f, pitch;
            if (phase < StanceShare)
            {
                // On the ground: rolling from the heel over to the toes.
                float t = phase / StanceShare;
                z = Mathf.Lerp(reach / 2f, -reach / 2f, t);
                pitch = t < 0.16f ? Mathf.Lerp(-16f, 0f, t / 0.16f) : t < 0.66f ? 0f : Mathf.Lerp(0f, 40f, (t - 0.66f) / 0.34f);
                lift = t > 0.66f ? Mathf.Lerp(0f, 0.065f, (t - 0.66f) / 0.34f) : 0f;
            }
            else
            {
                float t = (phase - StanceShare) / (1f - StanceShare), smooth = Mathf.SmoothStep(0f, 1f, t);
                z = Mathf.Lerp(-reach / 2f, reach / 2f, smooth);
                lift = Mathf.Lerp(0.065f, 0f, t) + Mathf.Sin(t * Mathf.PI) * gait.FootLift;
                pitch = Mathf.Lerp(40f, -16f, Mathf.SmoothStep(0f, 1f, t * 1.3f));
            }

            Vector3 ankle = new Vector3(leg.End.position.x * 0.92f + (pelvis.x - hipsAt.x) * 0.3f, ankleHeight + lift, z);

            // The knee bends forward, which on this rig is the way the character faces.
            leg.Reach(ankle, Vector3.forward);
            foot.rotation = Quaternion.Euler(pitch, 0f, 0f) * footRest;
        }

        void SwingArm(Limb arm, Gait gait, float phase, float side)
        {
            arm.Rest();
            float swing = Mathf.Sin(phase * Mathf.PI * 2f) * gait.ArmSwing + 2f;
            Vector3 down = Quaternion.AngleAxis(-swing, Vector3.right) * Vector3.down;
            down = (down + Vector3.right * side * 0.11f).normalized;

            Vector3 shoulder = arm.Upper.position;
            Vector3 elbow = shoulder + down * arm.UpperLength;
            float bend = gait.ElbowBend + Mathf.Max(swing, 0f) * 0.3f;
            Vector3 forearm = Quaternion.AngleAxis(-bend, Vector3.right) * down;
            arm.Aim(elbow, elbow + forearm * arm.LowerLength);
        }
    }
}
