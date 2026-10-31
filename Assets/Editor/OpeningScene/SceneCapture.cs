using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renders the Opening scene to PNGs from fixed viewpoints, so it can be checked without pressing Play:
// Unity.exe -batchmode -projectPath <project> -executeMethod SceneCapture.Run -captureDir <dir> -quit
public static class SceneCapture
{
    const string ScenePath = "Assets/Scenes/Opening.unity";

    readonly struct Shot
    {
        public readonly string Name;
        public readonly Vector3 Position;
        public readonly float Yaw, Pitch;

        public Shot(string name, float x, float y, float z, float yaw, float pitch)
        {
            Name = name;
            Position = new Vector3(x, y, z);
            Yaw = yaw;
            Pitch = pitch;
        }
    }

    static readonly Shot[] Shots =
    {
        new Shot("1_start", 3.5f, 1.6f, 1.0f, 0f, 2f),
        new Shot("2_hall", 3.5f, 1.6f, 4.3f, 0f, 0f),
        new Shot("3_window", 4.8f, 1.6f, 7.45f, 0f, 2f),
        new Shot("4_clerk", 4.8f, 1.6f, 7.95f, 0f, 3f),
        new Shot("5_bench", 3.4f, 1.6f, 2.2f, -40f, 8f),
        new Shot("6_npc", 2.1f, 1.6f, 4.9f, -115f, 12f),
        new Shot("7_exit", 4.2f, 1.6f, 4.4f, 90f, 4f),
        new Shot("8_stairs", 7.9f, 1.6f, 4.4f, 90f, 18f),
        new Shot("9_back", 3.5f, 1.6f, 7.4f, 180f, 2f),
    };

    public static void Run()
    {
        string directory = Argument("-captureDir") ?? "Temp/SceneCapture";
        string[] size = (Argument("-captureSize") ?? "1280x720").Split('x');
        string only = Argument("-captureOnly");
        Directory.CreateDirectory(directory);

        ShaderUtil.allowAsyncCompilation = false;
        RetroLookSetup.Apply();
        EditorSceneManager.OpenScene(ScenePath);
        PoseCharacters();

        Camera camera = Camera.main;
        foreach (Shot shot in Shots)
        {
            if (only != null && !shot.Name.Contains(only)) continue;
            camera.transform.SetPositionAndRotation(shot.Position, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            Render(camera, Path.Combine(directory, shot.Name + ".png"), int.Parse(size[0]), int.Parse(size[1]));
        }
        Debug.Log($"SceneCapture wrote {directory}");
    }

    // Edit mode never ticks Animators, so without this everyone would render in their bind pose.
    static void PoseCharacters()
    {
        foreach (Animator animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            animator.Update(0.5f);
        }
    }

    static void Render(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        camera.targetTexture = target;
        camera.Render();

        RenderTexture.active = target;
        var image = new Texture2D(width, height, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        image.Apply();
        File.WriteAllBytes(path, image.EncodeToPNG());

        RenderTexture.active = null;
        camera.targetTexture = null;
        UnityEngine.Object.DestroyImmediate(image);
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
    }

    static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }
}
