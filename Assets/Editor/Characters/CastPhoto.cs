using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Lines the whole cast up in plain light and photographs them, to judge the designs by:
// Unity.exe -batchmode -projectPath <project> -executeMethod CastPhoto.Run -captureDir <dir> -quit
public static class CastPhoto
{
    public static void Run()
    {
        string[] args = System.Environment.GetCommandLineArgs();
        string directory = args[System.Array.IndexOf(args, "-captureDir") + 1];
        Directory.CreateDirectory(directory);

        ShaderUtil.allowAsyncCompilation = false;
        SceneForge.Begin("Assets/Art/Generated/Characters", new System.Collections.Generic.Dictionary<string, (string, Color)>());
        TextureForge.ForgeCast();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

        var camera = SceneForge.NewCamera(null);
        camera.farClipPlane = 80f;
        camera.backgroundColor = new Color(0.32f, 0.33f, 0.34f);

        var key = new GameObject("Key").AddComponent<Light>();
        key.type = LightType.Directional;
        key.transform.rotation = Quaternion.Euler(32f, 158f, 0f);
        key.intensity = 1.5f;
        var fill = new GameObject("Fill").AddComponent<Light>();
        fill.type = LightType.Directional;
        fill.transform.rotation = Quaternion.Euler(14f, 220f, 0f);
        fill.intensity = 0.5f;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.34f, 0.34f, 0.36f);

        Look[] everyone = Cast.Everyone;
        var parent = new GameObject("Cast").transform;
        RuntimeAnimatorController idle = OpeningSceneBuilder.Controller("Idle");
        for (int i = 0; i < everyone.Length; i++)
        {
            GameObject person = CharacterForge.Spawn(everyone[i], everyone[i].Name, idle, parent, true);
            person.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            OpeningSceneBuilder.Pose(person);
            person.GetComponent<Animator>().Update(0.5f);
            Bounds bounds = OpeningSceneBuilder.MeasurePose(person);
            person.transform.position = new Vector3(i * 0.95f - bounds.center.x, -bounds.min.y, -bounds.center.z);
        }

        float middle = (everyone.Length - 1) * 0.95f / 2f;
        Shot(camera, directory, "lineup", new Vector3(middle, 1.1f, -9.4f), new Vector3(middle, 0.95f, 0f), 2400, 900);
        for (int i = 0; i < everyone.Length; i++)
        {
            float x = i * 0.95f;
            Shot(camera, directory, $"full_{everyone[i].Name}", new Vector3(x + 0.4f, 1.1f, -2.9f), new Vector3(x, 0.92f, 0f), 700, 1000);
            Shot(camera, directory, $"face_{everyone[i].Name}", new Vector3(x + 0.12f, everyone[i].Height * 0.97f, -0.72f), new Vector3(x, everyone[i].Height * 0.94f, 0f), 700, 700);
        }
        Shot(camera, directory, "back", new Vector3(middle, 1.2f, 8f), new Vector3(middle, 0.95f, 0f), 2400, 900);

        // The walks, four moments of each, side on.
        GaitForge.ForgeAll();
        camera.farClipPlane = 80f;
        GaitForge.Gait[] gaits = { GaitForge.Everyday, GaitForge.Brisk, GaitForge.Weary, GaitForge.Shuffle };
        Transform[] people = new Transform[everyone.Length];
        for (int i = 0; i < everyone.Length; i++) people[i] = parent.GetChild(i);
        for (int g = 0; g < gaits.Length; g++)
        {
            AnimationClip clip = GaitForge.Load(gaits[g]);
            string path = $"{GaitForge.ClipDir}/AC_Photo.controller";
            AssetDatabase.DeleteAsset(path);
            var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddMotion(clip);
            for (int f = 0; f < 6; f++)
            {
                GameObject person = people[f + 1 + (f > 1 ? 1 : 0)].gameObject;
                Animator animator = person.GetComponent<Animator>();
                animator.runtimeAnimatorController = controller;
                animator.Rebind();
                animator.Play(clip.name, 0, f / 6f);
                animator.Update(0f);
                person.transform.SetPositionAndRotation(new Vector3(0f, person.transform.position.y, f * 1.3f), Quaternion.identity);
                float low = OpeningSceneBuilder.MeasurePose(person).min.y;
                person.transform.position += Vector3.down * low;
            }
            Shot(camera, directory, $"walk_{gaits[g].Name}", new Vector3(9f, 1f, 4f), new Vector3(0f, 0.9f, 4f), 2000, 620);
        }

        // None of this was for keeps.
        foreach (string folder in new[] { "Meshes", "Materials" }) AssetDatabase.DeleteAsset($"Assets/Art/Generated/Characters/{folder}");
        AssetDatabase.DeleteAsset($"{GaitForge.ClipDir}/AC_Photo.controller");
        Debug.Log("CastPhoto done");
    }

    static void Shot(Camera camera, string directory, string name, Vector3 from, Vector3 to, int width, int height)
    {
        camera.transform.SetPositionAndRotation(from, Quaternion.LookRotation(to - from));
        camera.fieldOfView = 40f;
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(Path.Combine(directory, name + ".png"), image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        Object.DestroyImmediate(image);
        target.Release();
    }
}
