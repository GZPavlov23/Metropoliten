using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public enum Top { Suit, Jacket, Coat, Cardigan, Hoodie, Tracksuit, Quilted, Uniform, Nurse }
public enum Bottom { Trousers, Skirt }
public enum Hat { None, Beanie, FlatCap, Headscarf, Ushanka, Trilby }
public enum HairStyle { Short, Receding, Bun, Bob, Long }
public enum Carried { None, ToolPouch, Backpack, ShoppingBag, Briefcase }

// Everything that makes one person look like themselves. See Cast for who they all are.
public class Look
{
    public string Name;
    public int TextureSize = 512;
    public int Seed = 1;

    public float Height = 1.75f, Build = 1f, Shoulders = 1f, Belly, Bust, Hips = 1f, Stoop;

    public float HeadSize = 1.22f, FaceLength = 1f, FaceWidth = 1f, Jaw = 0.8f, Nose = 1f, NoseWidth = 1f, Brow = 1f, Cheek = 1f, Gaunt;
    public float Age = 0.3f, Stubble, Mouth, BrowTilt;
    public bool Moustache, Beard, Glasses, RedNose, Woman, Scratched;
    public Color Skin = new Color(0.78f, 0.6f, 0.48f), HairColor = new Color(0.14f, 0.11f, 0.09f), Eyes = new Color(0.22f, 0.2f, 0.16f);

    public HairStyle Hair = HairStyle.Short;
    public Hat Hat;
    public Color HatColor = Color.gray, HatPrint = Color.white;

    public Top Top = Top.Jacket;
    public Color TopColor = Color.gray, TopTrim = Color.gray, Under = Color.white;
    public bool HiVis, Tie;
    public Color TieColor = Color.gray;

    public Bottom Bottom;
    public Color BottomColor = Color.gray, ShoeColor = new Color(0.1f, 0.08f, 0.07f);

    public Carried Carried;
    public Color CarriedColor = Color.gray;
}

// Builds a person around the skeleton of one of the rigged models, so that every animation still fits. The mesh
// that came with the model is thrown away: the body is made of slices along the bones, the head is a sphere pushed
// about into a face, and clothes are just the shape of the body. Everything's laid out on one texture per person.
public static class CharacterForge
{
    public const string Rig = OpeningSceneBuilder.ClerkModel;

    // The long arm of the models, which gets a seated hand down to the thigh.
    const float ArmLength = 1.14f;

    // Where each part goes on the texture.
    public static readonly Rect TorsoArea = new Rect(0f, 0f, 0.5f, 0.5f);
    public static readonly Rect HeadArea = new Rect(0f, 0.5f, 0.5f, 0.5f);
    public static readonly Rect HairArea = new Rect(0.5f, 0.75f, 0.25f, 0.25f);
    public static readonly Rect HatArea = new Rect(0.75f, 0.75f, 0.25f, 0.25f);
    public static readonly Rect ArmArea = new Rect(0.5f, 0.25f, 0.25f, 0.25f);
    public static readonly Rect LegArea = new Rect(0.75f, 0.25f, 0.25f, 0.25f);
    public static readonly Rect SkirtArea = new Rect(0.5f, 0f, 0.25f, 0.25f);
    public static readonly Rect HandArea = new Rect(0.75f, 0.125f, 0.125f, 0.125f);
    public static readonly Rect ShoeArea = new Rect(0.875f, 0.125f, 0.125f, 0.125f);
    public static readonly Rect TrimArea = new Rect(0.75f, 0f, 0.125f, 0.125f);
    public static readonly Rect CarriedArea = new Rect(0.875f, 0f, 0.125f, 0.125f);
    public static readonly Rect EarArea = new Rect(0.5f, 0.5f, 0.0625f, 0.0625f);

    // The face has the middle of the texture to itself: going round the head, the columns open out towards the front.
    const float FaceSpread = 0.7f;
    public const float EyeTheta = 24f, EyePhi = 7f;
    static readonly float[] Thetas = { -180f, -150f, -120f, -95f, -72f, -55f, -41f, -30f, -21f, -14f, -8f, -3.5f, 0f, 3.5f, 8f, 14f, 21f, 30f, 41f, 55f, 72f, 95f, 120f, 150f, 180f };
    static readonly float[] Phis = { -89f, -76f, -66f, -56f, -46f, -38f, -31f, -25f, -19f, -13f, -6f, 1f, 7f, 13f, 19f, 26f, 36f, 48f, 62f, 76f, 89f };

    public static float ThetaToU(float theta) => 0.5f + 0.5f * Mathf.Sign(theta) * Mathf.Pow(Mathf.Abs(theta) / 180f, FaceSpread);
    public static float UToTheta(float u) => Mathf.Sign(u - 0.5f) * 180f * Mathf.Pow(Mathf.Abs(2f * u - 1f), 1f / FaceSpread);
    public static float PhiToV(float phi) => (phi + 90f) / 180f;

    static float Gauss(float x, float width) => Mathf.Exp(-(x / width) * (x / width));

    // Spawns the person standing at the origin in their bind pose, ready to be posed and placed.
    public static GameObject Spawn(Look look, string name, RuntimeAnimatorController controller, Transform parent, bool realtimeLit)
    {
        var character = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Rig), parent);
        character.name = name;
        character.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        character.transform.localScale = Vector3.one;
        character.transform.localScale = Vector3.one * (look.Height / OpeningSceneBuilder.MeasurePose(character).size.y);

        Animator animator = character.GetComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        foreach (HumanBodyBones arm in new[] { HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm })
            animator.GetBoneTransform(arm).localScale = Vector3.one * ArmLength;

        foreach (Renderer renderer in character.GetComponentsInChildren<Renderer>())
            if (!(renderer is SkinnedMeshRenderer)) renderer.enabled = false;

        var body = new Body(look, animator);
        SkinnedMeshRenderer skin = character.GetComponentInChildren<SkinnedMeshRenderer>();
        Mesh mesh = body.Build(skin.transform);
        AssetDatabase.CreateAsset(mesh, $"{SceneForge.MeshDir}/SK_{name}.asset");

        string texture = TextureForge.CharacterTexture(look);
        Material material = SceneForge.LoadOrCreateMaterial(texture, realtimeLit ? "Universal Render Pipeline/Simple Lit" : "Universal Render Pipeline/Baked Lit");
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(texture)));
        EditorUtility.SetDirty(material);

        skin.sharedMesh = mesh;
        skin.bones = body.Bones;
        skin.sharedMaterial = material;
        skin.localBounds = mesh.bounds;
        skin.updateWhenOffscreen = true;

        if (look.Glasses) OpeningSceneBuilder.AddSpectacles(character, body.EyePosition(-1f), body.EyePosition(1f), name);
        return character;
    }

    class Body
    {
        readonly Look look;
        readonly Animator animator;
        readonly BodyMesh mesh = new BodyMesh();
        readonly List<Transform> bones = new List<Transform>();
        readonly float s;
        readonly Vector3 headCenter;
        float headScale;

        public Transform[] Bones => bones.ToArray();

        public Body(Look look, Animator animator)
        {
            this.look = look;
            this.animator = animator;
            s = look.Height / 1.75f;
            headScale = look.HeadSize * s;
            headCenter = Bone(HumanBodyBones.Head).position + new Vector3(0f, 0.082f * s, 0.016f * s + look.Stoop * 0.05f);
        }

        Transform Bone(HumanBodyBones bone)
        {
            Transform found = animator.GetBoneTransform(bone);
            if (found == null) throw new InvalidOperationException($"The rig has no {bone}");
            return found;
        }

        int Index(HumanBodyBones bone)
        {
            Transform transform = Bone(bone);
            int index = bones.IndexOf(transform);
            if (index >= 0) return index;
            bones.Add(transform);
            return bones.Count - 1;
        }

        static BoneWeight Weigh(params (int bone, float weight)[] shares)
        {
            Array.Sort(shares, (a, b) => b.weight.CompareTo(a.weight));
            float total = 0f;
            for (int i = 0; i < Mathf.Min(shares.Length, 4); i++) total += shares[i].weight;

            var result = new BoneWeight();
            if (shares.Length > 0) { result.boneIndex0 = shares[0].bone; result.weight0 = shares[0].weight / total; }
            if (shares.Length > 1) { result.boneIndex1 = shares[1].bone; result.weight1 = shares[1].weight / total; }
            if (shares.Length > 2) { result.boneIndex2 = shares[2].bone; result.weight2 = shares[2].weight / total; }
            if (shares.Length > 3) { result.boneIndex3 = shares[3].bone; result.weight3 = shares[3].weight / total; }
            return result;
        }

        Func<Vector3, BoneWeight> Rigid(HumanBodyBones bone)
        {
            int index = Index(bone);
            return p => Weigh((index, 1f));
        }

        // Follows a chain of joints, handing over softly from one bone to the next around each joint.
        Func<Vector3, BoneWeight> AlongChain(float blend, params HumanBodyBones[] chain)
        {
            var indices = new int[chain.Length];
            var joints = new Vector3[chain.Length];
            for (int i = 0; i < chain.Length; i++)
            {
                indices[i] = Index(chain[i]);
                joints[i] = Bone(chain[i]).position;
            }

            return point =>
            {
                int segment = 0;
                float best = float.MaxValue, bestAlong = 0f;
                for (int i = 0; i < chain.Length - 1; i++)
                {
                    Vector3 axis = joints[i + 1] - joints[i];
                    float along = Mathf.Clamp01(Vector3.Dot(point - joints[i], axis) / axis.sqrMagnitude);
                    float distance = Vector3.Distance(point, joints[i] + axis * along);
                    if (distance >= best) continue;
                    best = distance;
                    segment = i;
                    bestAlong = along;
                }

                float length = Vector3.Distance(joints[segment], joints[segment + 1]);
                float toEnd = (1f - bestAlong) * length, fromStart = bestAlong * length;
                if (toEnd < blend) return Weigh((indices[segment], 1f), (indices[segment + 1], 1f - toEnd / blend));
                if (fromStart < blend && segment > 0) return Weigh((indices[segment], 1f), (indices[segment - 1], 1f - fromStart / blend));
                return Weigh((indices[segment], 1f));
            };
        }

        // Below the hips a coat or a skirt goes with whichever leg is on its side, and with both down the middle.
        Func<Vector3, BoneWeight> Torso()
        {
            Func<Vector3, BoneWeight> trunk = AlongChain(0.09f * s, HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head);
            int hips = Index(HumanBodyBones.Hips), left = Index(HumanBodyBones.LeftUpperLeg), right = Index(HumanBodyBones.RightUpperLeg);
            int leftShin = Index(HumanBodyBones.LeftLowerLeg), rightShin = Index(HumanBodyBones.RightLowerLeg);
            float top = Bone(HumanBodyBones.LeftUpperLeg).position.y, knee = Bone(HumanBodyBones.LeftLowerLeg).position.y;

            return point =>
            {
                if (point.y >= top) return trunk(point);
                float down = Mathf.InverseLerp(top, top - 0.16f * s, point.y), toKnee = Mathf.InverseLerp(knee + 0.05f, knee - 0.15f, point.y) * 0.6f;
                float rightShare = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(-0.05f * s, 0.05f * s, point.x));
                return Weigh((hips, 1f - down), (right, down * rightShare * (1f - toKnee)), (left, down * (1f - rightShare) * (1f - toKnee)),
                    (rightShare > 0.5f ? rightShin : leftShin, down * toKnee));
            };
        }

        public Mesh Build(Transform skin)
        {
            BuildTorso();
            BuildArm(HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftHand, -1f);
            BuildArm(HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand, 1f);
            BuildLeg(HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftFoot);
            BuildLeg(HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.RightFoot);
            BuildHead();
            BuildHair();
            BuildHat();
            BuildCarried();

            var bindposes = new Matrix4x4[bones.Count];
            for (int i = 0; i < bones.Count; i++) bindposes[i] = bones[i].worldToLocalMatrix * skin.localToWorldMatrix;
            return mesh.ToMesh($"SK_{look.Name}", skin.worldToLocalMatrix, bindposes);
        }

        static Ring Level(float y, float z, float width, float front, float back, float squareness = 2.5f)
        {
            return new Ring(new Vector3(0f, y, z), Vector3.right, Vector3.forward, width, front, back, squareness);
        }

        void BuildTorso()
        {
            float hips = Bone(HumanBodyBones.Hips).position.y, spine = Bone(HumanBodyBones.Spine).position.y, chest = Bone(HumanBodyBones.Chest).position.y;
            float shoulder = Bone(HumanBodyBones.LeftUpperArm).position.y, neck = Bone(HumanBodyBones.Neck).position.y;
            float knee = Bone(HumanBodyBones.LeftLowerLeg).position.y;

            bool longCoat = look.Top == Top.Coat, nurse = look.Top == Top.Nurse;
            float pad = longCoat || look.Top == Top.Quilted ? 0.03f : look.Top == Top.Suit || look.Top == Top.Uniform ? 0.012f : 0.02f;
            pad = (pad + 0.01f) * s;

            float g = look.Build * s, b = look.Shoulders, w = look.Hips, bust = look.Bust * s, belly = look.Belly * s;
            float lean = look.Stoop;
            float Z(float y) => lean * 0.07f * s * Mathf.InverseLerp(spine, neck, y) * Mathf.InverseLerp(spine, neck, y);

            var rings = new List<Ring>();
            void Add(float y, float width, float front, float back, float square = 2.5f) => rings.Add(Level(y, Z(y), width * g + pad, front * g + pad, back * g + pad, square));

            if (longCoat || nurse)
            {
                float hem = nurse ? knee + 0.08f * s : knee - 0.1f * s;
                Add(hem, 0.2f * w, 0.13f, 0.16f);
                Add(Mathf.Lerp(hem, hips, 0.5f), 0.17f * w, 0.11f + belly * 0.3f, 0.135f);
            }
            else
            {
                Add(hips - (look.Top == Top.Suit ? 0.1f : 0.08f) * s, 0.155f * w, 0.1f + belly * 0.7f, 0.115f, 3f);
            }

            Add(hips + 0.02f * s, 0.15f * w, 0.09f + belly, 0.112f, 2.6f);
            Add(Mathf.Lerp(hips, spine, 0.75f), (0.14f + 0.02f * (w - 1f)) * (look.Woman ? 0.9f : 1f), 0.085f + belly * 1.1f, 0.09f);
            Add(Mathf.Lerp(spine, chest, 0.5f), look.Woman ? 0.125f : 0.14f, 0.09f + belly * 0.5f + bust * 0.3f, 0.09f);
            Add(chest + 0.03f * s, 0.155f * b, 0.1f + bust, 0.092f + lean * 0.015f);
            Add(shoulder - 0.09f * s, 0.168f * b, 0.1f + bust * 0.8f, 0.094f + lean * 0.03f);
            Add(shoulder - 0.03f * s, 0.172f * b, 0.09f, 0.09f + lean * 0.03f);
            Add(shoulder + 0.02f * s, 0.15f * b, 0.07f, 0.075f + lean * 0.02f, 2.2f);
            Add(shoulder + 0.05f * s, 0.095f * b, 0.055f, 0.06f, 2f);
            Add(neck + 0.0f * s, 0.052f * g + 0.012f, 0.045f, 0.045f, 2f);
            mesh.Loft(rings, 18, TorsoArea, Torso(), true);

            if (look.Bottom == Bottom.Skirt && !longCoat && !nurse) BuildSkirt(hips, knee);
            if (look.Top != Top.Nurse) BuildCollar(neck, shoulder);
        }

        void BuildSkirt(float hips, float knee)
        {
            float g = look.Build * s, w = look.Hips;
            var rings = new List<Ring>
            {
                Level(hips + 0.08f * s, 0f, 0.14f * w * g, 0.1f * g, 0.11f * g),
                Level(hips - 0.06f * s, 0f, 0.165f * w * g, 0.115f * g, 0.13f * g),
                Level(knee + 0.1f * s, 0f, 0.18f * w * g, 0.125f * g, 0.14f * g),
                Level(knee - 0.2f * s, 0f, 0.19f * w * g, 0.135f * g, 0.15f * g),
            };
            mesh.Loft(rings, 18, SkirtArea, Torso(), true, true);
        }

        void BuildCollar(float neck, float shoulder)
        {
            float z = look.Stoop * 0.05f * s;
            float y = neck + 0.015f * s;
            bool high = look.Top == Top.Hoodie || look.Top == Top.Quilted || look.Top == Top.Tracksuit;
            float width = (high ? 0.082f : 0.06f) * s, depth = high ? 0.1f : 0.066f;
            var rings = new List<Ring>
            {
                Level(y - 0.05f * s, z, width * 1.15f, depth * s * 0.95f, depth * s * 1.1f, 2.2f),
                Level(y + (high ? 0.035f : 0.012f) * s, z, width, depth * s * 0.85f, depth * s, 2.2f),
                Level(y + (high ? 0.05f : 0.02f) * s, z, width * 0.86f, depth * s * 0.72f, depth * s * 0.85f, 2.2f),
            };
            mesh.Loft(rings, 12, TrimArea, AlongChain(0.06f * s, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head), false, true);
        }

        void BuildArm(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones wrist, float side)
        {
            Vector3 shoulder = Bone(upper).position, elbow = Bone(lower).position, hand = Bone(wrist).position;
            Vector3 upperAxis = (elbow - shoulder).normalized, lowerAxis = (hand - elbow).normalized;
            float sleeve = (look.Top == Top.Coat || look.Top == Top.Quilted ? 0.012f : look.Top == Top.Suit ? 0f : 0.006f) * s;
            float g = Mathf.Lerp(1f, look.Build, 0.7f) * s;

            Ring Slice(Vector3 center, Vector3 axis, float width, float front, float back)
            {
                Vector3 frontWay = Vector3.ProjectOnPlane(Vector3.forward, axis).normalized;
                return new Ring(center, Vector3.Cross(axis, frontWay).normalized, frontWay, width * g + sleeve, front * g + sleeve, back * g + sleeve, 2.2f);
            }

            float upperLength = Vector3.Distance(shoulder, elbow), lowerLength = Vector3.Distance(elbow, hand);
            var rings = new List<Ring>
            {
                Slice(shoulder - upperAxis * 0.01f * s, upperAxis, 0.036f, 0.04f, 0.042f),
                Slice(shoulder + upperAxis * 0.05f * s, upperAxis, 0.056f, 0.058f, 0.062f),
                Slice(shoulder + upperAxis * upperLength * 0.35f, upperAxis, 0.056f, 0.056f, 0.058f),
                Slice(shoulder + upperAxis * upperLength * 0.75f, upperAxis, 0.048f, 0.05f, 0.05f),
                Slice(elbow, (upperAxis + lowerAxis).normalized, 0.045f, 0.046f, 0.05f),
                Slice(elbow + lowerAxis * lowerLength * 0.3f, lowerAxis, 0.046f, 0.046f, 0.046f),
                Slice(elbow + lowerAxis * lowerLength * 0.75f, lowerAxis, 0.038f, 0.038f, 0.038f),
                Slice(elbow + lowerAxis * lowerLength * 0.96f, lowerAxis, 0.036f, 0.036f, 0.036f),
            };
            Func<Vector3, BoneWeight> along = AlongChain(0.08f * s, HumanBodyBones.Chest, upper, lower, wrist);
            mesh.Loft(rings, 10, ArmArea, along, true, true);

            // The hand, palm down in the bind pose: wide across the knuckles and thumb forwards.
            Vector3 axis2 = lowerAxis;
            Vector3 up = Vector3.ProjectOnPlane(Vector3.up, axis2).normalized, forward = Vector3.Cross(axis2, up).normalized;
            if (forward.z < 0f) forward = -forward;
            float h = 1.08f * s;
            Ring HandSlice(float along2, float width, float thick) => new Ring(hand + axis2 * along2 * h, forward, up, width * h, thick * h, thick * h, 2.6f);
            mesh.Loft(new List<Ring>
            {
                HandSlice(-0.035f, 0.024f, 0.021f),
                HandSlice(0.01f, 0.031f, 0.019f),
                HandSlice(0.045f, 0.038f, 0.016f),
                HandSlice(0.078f, 0.04f, 0.0135f),
                HandSlice(0.09f, 0.036f, 0.011f),
            }, 10, HandArea, Rigid(wrist), true, true);

            // Four fingers from the knuckles, the middle one longest, spread a little.
            float[] reach = { 0.064f, 0.072f, 0.068f, 0.056f };
            for (int finger = 0; finger < 4; finger++)
            {
                float spread = finger - 1.5f;
                Vector3 root = hand + axis2 * 0.082f * h + forward * (spread * 0.0198f * h);
                Vector3 way = (axis2 + forward * spread * 0.07f).normalized;
                Vector3 across = Vector3.ProjectOnPlane(forward, way).normalized, back = Vector3.ProjectOnPlane(up, way).normalized;
                Ring Joint(float t, float radius) => new Ring(root + way * (reach[finger] * t * h), across, back, radius * h, radius * h * 0.86f, radius * h * 0.86f, 2.1f);
                mesh.Loft(new List<Ring> { Joint(0f, 0.0094f), Joint(0.5f, 0.0084f), Joint(1f, 0.0068f) }, 7, HandArea, Rigid(wrist), false, true);
            }

            Vector3 thumbRoot = hand + axis2 * 0.03f * h + forward * 0.03f * h;
            Vector3 thumbAxis = (axis2 + forward * 0.8f).normalized;
            Ring ThumbSlice(float along3, float width) => new Ring(thumbRoot + thumbAxis * along3 * h, Vector3.Cross(thumbAxis, up).normalized, up, width * h, width * h * 0.8f, width * h * 0.8f, 2f);
            mesh.Loft(new List<Ring> { ThumbSlice(-0.01f, 0.013f), ThumbSlice(0.03f, 0.012f), ThumbSlice(0.058f, 0.008f) }, 6, HandArea, Rigid(wrist), false, true);
        }

        void BuildLeg(HumanBodyBones upper, HumanBodyBones lower, HumanBodyBones foot)
        {
            Vector3 hip = Bone(upper).position, knee = Bone(lower).position, ankle = Bone(foot).position;
            bool bare = look.Bottom == Bottom.Skirt || look.Top == Top.Nurse;
            float g = Mathf.Lerp(1f, look.Build, 0.8f) * s, loose = bare ? -0.01f * s : 0.004f * s;

            Ring Slice(Vector3 center, float width, float front, float back)
            {
                return new Ring(center, Vector3.right, Vector3.forward, width * g + loose, front * g + loose, back * g + loose, 2.2f);
            }

            var rings = new List<Ring>
            {
                Slice(hip + Vector3.up * 0.02f * s, 0.075f, 0.085f, 0.09f),
                Slice(Vector3.Lerp(hip, knee, 0.25f), 0.078f, 0.085f, 0.09f),
                Slice(Vector3.Lerp(hip, knee, 0.65f), 0.066f, 0.07f, 0.072f),
                Slice(knee, 0.054f, 0.06f, 0.056f),
                Slice(Vector3.Lerp(knee, ankle, 0.3f), 0.052f, 0.054f, 0.062f),
                Slice(Vector3.Lerp(knee, ankle, 0.7f), 0.044f, 0.046f, 0.05f),
                Slice(Vector3.Lerp(knee, ankle, 0.97f), bare ? 0.034f : 0.05f, bare ? 0.036f : 0.056f, bare ? 0.036f : 0.056f),
            };
            mesh.Loft(rings, 12, LegArea, AlongChain(0.075f * s, HumanBodyBones.Hips, upper, lower, foot), true, true);

            // The shoe, built forwards from the heel.
            float x = ankle.x, sole = 0.008f * s, size = 1.05f * s;
            Ring Slice2(float z, float width, float top, float y0)
            {
                float height = (top - y0) * size / 2f;
                return new Ring(new Vector3(x, sole + (y0 + height / size) * size + 0f, ankle.z + z * size), Vector3.right, Vector3.up, width * size, height, height, 3f);
            }

            mesh.Loft(new List<Ring>
            {
                Slice2(-0.1f, 0.028f, 0.05f, 0f),
                Slice2(-0.08f, 0.044f, 0.085f, 0f),
                Slice2(-0.02f, 0.05f, 0.095f, 0f),
                Slice2(0.05f, 0.052f, 0.08f, 0f),
                Slice2(0.1f, 0.056f, 0.058f, 0f),
                Slice2(0.15f, 0.054f, 0.05f, 0f),
                Slice2(0.185f, 0.036f, 0.038f, 0f),
            }, 10, ShoeArea, Rigid(foot), true, true);
        }

        // How far the skull is pushed in or out at a point, in the head's own units. Theta runs round from the
        // nose, phi up from the mouth end of the head. These are the same places the texture paints the features.
        float Features(float theta, float phi)
        {
            float side = Mathf.Abs(theta), push = 0f;

            // Gouged away, with nothing left of the shape of a face.
            if (look.Scratched)
            {
                float reach = (theta / 56f) * (theta / 56f) + ((phi + 8f) / 52f) * ((phi + 8f) / 52f);
                float rough = Mathf.Sin(theta * 0.9f + phi * 0.4f) * Mathf.Sin(phi * 0.7f - theta * 0.3f);
                float gone = 0.018f * Mathf.Clamp01((1.15f - reach) * 2.4f) * (1f + rough * 0.3f);
                gone += 0.016f * Gauss(phi - EyePhi, 9f) * Gauss(side - EyeTheta, 10f);
                gone += 0.03f * Gauss(phi + 34f, 13f) * Gauss(theta, 15f);
                return -gone;
            }

            float noseRise = Mathf.InverseLerp(26f, -13f, phi) * Mathf.InverseLerp(-21f, -13f, phi);
            push += 0.027f * look.Nose * noseRise * noseRise * Gauss(theta, 7f * look.NoseWidth);
            push += 0.007f * Gauss(phi + 14f, 4f) * Gauss(side - 8f * look.NoseWidth, 4f);

            push += 0.0085f * look.Brow * Gauss(phi - 17f, 6f) * Gauss(side - 22f, 20f);
            push -= 0.012f * Gauss(phi - EyePhi, 6.5f) * Gauss(side - EyeTheta, 10f);
            push += 0.008f * look.Cheek * Gauss(phi + 9f, 11f) * Gauss(side - 44f, 15f);
            push -= look.Gaunt * 0.012f * Gauss(phi + 25f, 12f) * Gauss(side - 40f, 13f);
            push += 0.0055f * Gauss(phi + 32f, 5f) * Gauss(theta, 14f);
            push -= 0.003f * Gauss(phi + 31f, 1.7f) * Gauss(theta, 13f);
            push += 0.012f * Gauss(phi + 56f, 11f) * Gauss(theta, 20f);
            return push;
        }

        Vector3 HeadPoint(float theta, float phi, float lift)
        {
            float t = theta * Mathf.Deg2Rad, p = phi * Mathf.Deg2Rad;
            float width = 0.078f * look.FaceWidth, height = 0.104f * look.FaceLength, deep = 0.098f;

            float jaw = Mathf.Lerp(1f, look.Jaw, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(6f, -74f, phi)));
            float faceFlat = Mathf.Cos(t) > 0f ? Mathf.Lerp(1f, 0.9f, Gauss(theta, 38f)) : 1.04f;
            var surface = new Vector3(width * Mathf.Sin(t) * Mathf.Cos(p) * jaw, height * Mathf.Sin(p), deep * faceFlat * Mathf.Cos(t) * Mathf.Cos(p) * Mathf.Lerp(1f, jaw, 0.45f));

            Vector3 outward = new Vector3(surface.x / width, surface.y / height, surface.z / deep).normalized;
            return headCenter + (surface + outward * (Features(theta, phi) + lift)) * headScale;
        }

        public Vector3 EyePosition(float side) => HeadPoint(side * EyeTheta, EyePhi, 0f);

        delegate float Thickness(float theta, float phi);

        void HeadShell(Rect area, Thickness thickness)
        {
            var points = new Vector3[Phis.Length, Thetas.Length];
            var texture = new Vector2[Phis.Length, Thetas.Length];
            for (int j = 0; j < Phis.Length; j++)
            {
                for (int i = 0; i < Thetas.Length; i++)
                {
                    points[j, i] = HeadPoint(Thetas[i], Phis[j], thickness(Thetas[i], Phis[j]));
                    texture[j, i] = new Vector2(area.x + area.width * ThetaToU(Thetas[i]), area.y + area.height * PhiToV(Phis[j]));
                }
            }
            mesh.Grid(points, texture, Rigid(HumanBodyBones.Head), headCenter);
        }

        void BuildHead()
        {
            HeadShell(HeadArea, (theta, phi) => 0f);

            Func<Vector3, BoneWeight> neck = AlongChain(0.05f * s, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head);
            float z = look.Stoop * 0.04f * s + 0.004f, width = 0.046f * s * Mathf.Lerp(1f, look.Build, 0.6f);
            Vector3 root = Bone(HumanBodyBones.Neck).position;
            mesh.Loft(new List<Ring>
            {
                Level(root.y - 0.03f * s, z * 0.4f, width * 1.15f, width * 1.15f, width * 1.15f, 2f),
                Level(root.y + 0.04f * s, z, width, width, width, 2f),
                Level(headCenter.y - 0.07f * headScale, z * 2f, width * 0.98f, width, width, 2f),
            }, 10, new Rect(HeadArea.x + 0.2f, HeadArea.y, 0.1f, 0.03f), neck);

            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 center = HeadPoint(side * 88f, 2f, 0f);
                Vector3 back = Vector3.back + Vector3.right * side * 0.45f;
                back.Normalize();
                Vector3 up = Vector3.up;
                Vector3 across = Vector3.Cross(back, up);
                float size = 0.03f * headScale;
                mesh.Loft(new List<Ring>
                {
                    new Ring(center - back * size * 0.2f, across, up, size * 0.2f, size * 0.7f, size * 0.7f, 2f),
                    new Ring(center + back * size * 0.55f, across, up, size * 0.26f, size * 0.95f, size * 0.85f, 2f),
                    new Ring(center + back * size * 1.05f, across, up, size * 0.12f, size * 0.55f, size * 0.5f, 2f),
                }, 7, EarArea, Rigid(HumanBodyBones.Head), false, true);
            }
        }

        void BuildHair()
        {
            HairStyle style = look.Hair;
            bool hatCovers = look.Hat == Hat.Headscarf || look.Hat == Hat.Ushanka;
            if (hatCovers) return;

            // Where a shell goes under the skin it's simply not seen, which saves working out a hairline.
            float Hairline(float theta)
            {
                float side = Mathf.Abs(theta);
                float front = style == HairStyle.Receding ? 60f : look.Woman ? 42f : 38f, temple = style == HairStyle.Long || style == HairStyle.Bob ? -20f : 16f;
                float nape = style == HairStyle.Long || style == HairStyle.Bob ? -70f : -34f;
                return side < 70f ? Mathf.Lerp(front, temple, Mathf.SmoothStep(0f, 1f, side / 70f)) : Mathf.Lerp(temple, nape, Mathf.SmoothStep(0f, 1f, (side - 70f) / 80f));
            }

            float Thick(float theta, float phi)
            {
                float depth = 0.012f + (look.Woman ? 0.004f : 0f);
                if (style == HairStyle.Bob || style == HairStyle.Long) depth = 0.02f;
                float past = phi - Hairline(theta);

                // A receding head is bare on top, with what's left around the sides.
                if (style == HairStyle.Receding && phi > 40f && Mathf.Abs(theta) < 130f) return -0.012f;
                return past > 0f ? depth * Mathf.Clamp01(past / 7f) - 0.001f : -0.012f;
            }

            HeadShell(HairArea, Thick);

            if (style == HairStyle.Bun)
            {
                Vector3 center = HeadPoint(180f, 28f, 0.03f);
                float r = 0.03f * headScale;
                var rings = new List<Ring>();
                for (int i = 0; i < 6; i++)
                {
                    float a = Mathf.Lerp(-80f, 80f, i / 5f) * Mathf.Deg2Rad;
                    float radius = r * Mathf.Cos(a);
                    rings.Add(new Ring(center + Vector3.up * Mathf.Sin(a) * r, Vector3.right, Vector3.forward, radius, radius, radius, 2f));
                }
                mesh.Loft(rings, 10, HairArea, Rigid(HumanBodyBones.Head), true, true);
            }

            if (style == HairStyle.Long || style == HairStyle.Bob)
            {
                // What hangs down the back.
                float y = headCenter.y - 0.08f * headScale, length = style == HairStyle.Long ? 0.3f * s : 0.12f * s;
                float width = 0.085f * headScale, back = -0.075f * headScale + look.Stoop * 0.03f;
                mesh.Loft(new List<Ring>
                {
                    new Ring(new Vector3(0f, y + 0.05f * s, headCenter.z + back), Vector3.right, Vector3.forward, width, 0.03f * s, 0.035f * s),
                    new Ring(new Vector3(0f, y - length * 0.6f, headCenter.z + back - 0.012f), Vector3.right, Vector3.forward, width * 1.05f, 0.022f * s, 0.03f * s),
                    new Ring(new Vector3(0f, y - length, headCenter.z + back - 0.02f), Vector3.right, Vector3.forward, width * 0.8f, 0.012f * s, 0.016f * s),
                }, 10, HairArea, AlongChain(0.06f * s, HumanBodyBones.Chest, HumanBodyBones.Neck, HumanBodyBones.Head), true, true);
            }
        }

        void BuildHat()
        {
            float Band(float theta, float phi, float edge, float thick)
            {
                float past = phi - edge;
                return past > 0f ? thick * Mathf.Clamp01(past / 5f) : -0.012f;
            }

            switch (look.Hat)
            {
                case Hat.Beanie:
                    HeadShell(HatArea, (theta, phi) =>
                    {
                        float brim = Mathf.Lerp(30f, 0f, Mathf.Abs(theta) / 180f);
                        return Band(theta, phi, brim, phi > brim + 16f ? 0.026f : 0.03f) + 0.012f * Mathf.InverseLerp(50f, 89f, phi);
                    });
                    break;

                case Hat.FlatCap:
                    HeadShell(HatArea, (theta, phi) => Band(theta, phi, 34f + 0f * theta, 0.024f) + 0.006f * Mathf.InverseLerp(50f, 80f, phi));
                    BuildPeak(0.075f, 0.016f);
                    break;

                case Hat.Trilby:
                    HeadShell(HatArea, (theta, phi) => Band(theta, phi, 30f, 0.03f) + 0.02f * Mathf.InverseLerp(40f, 80f, phi));
                    BuildBrim(0.052f);
                    break;

                case Hat.Ushanka:
                    HeadShell(HatArea, (theta, phi) =>
                    {
                        float side = Mathf.Abs(theta);
                        if (side > 62f) return 0.038f * Mathf.Clamp01((phi + 66f) / 8f) - 0.012f * Mathf.Clamp01((-58f - phi) / 8f);
                        return Band(theta, phi, 26f, 0.04f);
                    });
                    break;

                case Hat.Headscarf:
                    HeadShell(HatArea, (theta, phi) =>
                    {
                        // A frame around the face, knotted under the chin.
                        float side = Mathf.Abs(theta);
                        bool face = side < 58f && phi < 40f && phi > -66f;
                        return face ? -0.014f : 0.016f;
                    });
                    Vector3 knot = HeadPoint(0f, -80f, 0.02f);
                    float r = 0.02f * headScale;
                    mesh.Loft(new List<Ring>
                    {
                        new Ring(knot + Vector3.up * r, Vector3.right, Vector3.forward, r * 0.6f, r * 0.6f, r * 0.6f, 2f),
                        new Ring(knot, Vector3.right, Vector3.forward, r, r, r, 2f),
                        new Ring(knot - Vector3.up * r, Vector3.right, Vector3.forward, r * 0.5f, r * 0.5f, r * 0.5f, 2f),
                    }, 7, HatArea, Rigid(HumanBodyBones.Head), true, true);
                    break;
            }
        }

        void BuildPeak(float length, float thick)
        {
            Vector3 center = HeadPoint(0f, 32f, 0.03f);
            float w = 0.078f * headScale;
            mesh.Loft(new List<Ring>
            {
                new Ring(center + new Vector3(0f, 0f, -0.02f), Vector3.right, Vector3.up, w, thick, thick * 0.5f),
                new Ring(center + new Vector3(0f, -0.01f, length * 0.7f * headScale), Vector3.right, Vector3.up, w * 0.86f, thick * 0.7f, thick * 0.4f),
                new Ring(center + new Vector3(0f, -0.022f, length * headScale), Vector3.right, Vector3.up, w * 0.5f, thick * 0.3f, thick * 0.3f),
            }, 10, HatArea, Rigid(HumanBodyBones.Head), true, true);
        }

        void BuildBrim(float reach)
        {
            float y = HeadPoint(0f, 30f, 0f).y - headCenter.y;
            float width = 0.078f * look.FaceWidth * headScale * 1.1f + reach * headScale, deep = 0.1f * headScale + reach * headScale;
            Vector3 center = headCenter + Vector3.up * y;
            mesh.Loft(new List<Ring>
            {
                new Ring(center + Vector3.down * 0.004f, Vector3.right, Vector3.forward, width * 0.7f, deep * 0.7f, deep * 0.7f, 2f),
                new Ring(center + Vector3.down * 0.003f, Vector3.right, Vector3.forward, width, deep, deep, 2f),
                new Ring(center + Vector3.up * 0.004f, Vector3.right, Vector3.forward, width * 0.98f, deep * 0.98f, deep * 0.98f, 2f),
                new Ring(center + Vector3.up * 0.01f, Vector3.right, Vector3.forward, width * 0.68f, deep * 0.68f, deep * 0.68f, 2f),
            }, 14, HatArea, Rigid(HumanBodyBones.Head));
        }

        void BuildCarried()
        {
            float chest = Bone(HumanBodyBones.Chest).position.y, hips = Bone(HumanBodyBones.Hips).position.y;
            switch (look.Carried)
            {
                case Carried.Backpack:
                    float y0 = hips + 0.12f * s, y1 = chest + 0.2f * s, z = -0.15f * s * look.Build - 0.05f * s;
                    mesh.Loft(new List<Ring>
                    {
                        new Ring(new Vector3(0f, y0, z), Vector3.right, Vector3.forward, 0.11f * s, 0.05f * s, 0.05f * s, 4f),
                        new Ring(new Vector3(0f, y0 + 0.04f * s, z), Vector3.right, Vector3.forward, 0.135f * s, 0.075f * s, 0.075f * s, 4f),
                        new Ring(new Vector3(0f, y1 - 0.06f * s, z), Vector3.right, Vector3.forward, 0.13f * s, 0.07f * s, 0.07f * s, 4f),
                        new Ring(new Vector3(0f, y1, z), Vector3.right, Vector3.forward, 0.09f * s, 0.04f * s, 0.05f * s, 3f),
                    }, 12, CarriedArea, Rigid(HumanBodyBones.Chest), true, true);
                    break;

                case Carried.ToolPouch:
                    Vector3 hip = Bone(HumanBodyBones.RightUpperLeg).position + new Vector3(0.145f * s * look.Build, -0.03f * s, 0.01f);
                    mesh.Loft(new List<Ring>
                    {
                        new Ring(hip + Vector3.up * 0.05f * s, Vector3.forward, Vector3.right, 0.06f * s, 0.03f * s, 0.018f * s, 4f),
                        new Ring(hip - Vector3.up * 0.09f * s, Vector3.forward, Vector3.right, 0.055f * s, 0.035f * s, 0.018f * s, 4f),
                    }, 10, CarriedArea, Rigid(HumanBodyBones.Hips), true, true);
                    break;

                case Carried.ShoppingBag:
                case Carried.Briefcase:
                    BuildHandBag(look.Carried == Carried.Briefcase);
                    break;
            }
        }

        // Hangs from the right hand, straight down in the bind pose.
        void BuildHandBag(bool stiff)
        {
            Vector3 hand = Bone(HumanBodyBones.RightHand).position + Vector3.right * 0.07f * s;
            float top = hand.y - 0.13f * s, bottom = top - (stiff ? 0.28f : 0.3f) * s;
            float width = (stiff ? 0.2f : 0.15f) * s, depth = (stiff ? 0.045f : 0.07f) * s;
            mesh.Loft(new List<Ring>
            {
                new Ring(new Vector3(hand.x, top, hand.z), Vector3.forward, Vector3.right, width * 0.92f, depth * 0.6f, depth * 0.6f, 5f),
                new Ring(new Vector3(hand.x, Mathf.Lerp(top, bottom, 0.5f), hand.z), Vector3.forward, Vector3.right, width, depth, depth, 5f),
                new Ring(new Vector3(hand.x, bottom, hand.z), Vector3.forward, Vector3.right, width * (stiff ? 1f : 0.86f), depth, depth, 5f),
            }, 10, CarriedArea, Rigid(HumanBodyBones.RightHand), true, true);

            Vector3 Handle(float angle) => new Vector3(hand.x, top + Mathf.Sin(angle) * 0.11f * s, hand.z + Mathf.Cos(angle) * 0.07f * s);
            var loop = new List<Ring>();
            for (int i = 0; i <= 5; i++)
            {
                float a = Mathf.Lerp(0f, Mathf.PI, i / 5f);
                Vector3 axis = (Handle(a + 0.1f) - Handle(a - 0.1f)).normalized;
                loop.Add(new Ring(Handle(a), Vector3.right, Vector3.Cross(axis, Vector3.right).normalized, 0.008f * s, 0.008f * s, 0.008f * s, 2f));
            }
            mesh.Loft(loop, 5, CarriedArea, Rigid(HumanBodyBones.RightHand));
        }
    }
}
