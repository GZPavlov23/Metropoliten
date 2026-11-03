using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Renders a generated scene to PNGs from fixed viewpoints, so it can be checked without pressing Play:
// Unity.exe -batchmode -projectPath <project> -executeMethod SceneCapture.Run -captureDir <dir> -captureScene Shift1 -quit
public static class SceneCapture
{
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

    static readonly Shot[] Shift1Shots =
    {
        new Shot("1_passage", 6.6f, 1.6f, -5.5f, 0f, 2f),
        new Shot("2_platform", 6.6f, 1.6f, 1.2f, -4f, 3f),
        new Shot("3_train", 8.6f, 1.6f, 21f, -90f, 6f),
        new Shot("4_north", 6.2f, 1.6f, 23f, 0f, 3f),
        new Shot("5_intercom", 7.9f, 1.6f, 5f, 90f, 6f),
        new Shot("6_booth", 5.9f, 1.6f, 9.8f, 65f, 6f),
        new Shot("7_keys", 7.9f, 1.6f, 15.6f, 90f, 6f),
        new Shot("8_signal", 5.4f, 1.6f, 38.6f, 25f, -6f),
        new Shot("9_front", 5.2f, 1.6f, 40.6f, -152f, 6f),
        new Shot("10_cabdoor", 6.2f, 1.6f, 31.7f, -90f, 6f),
        new Shot("11_cab", 3.3f, 1.64f, 31.5f, -30f, 10f),
        new Shot("12_windscreen", 2.5f, 1.64f, 32f, 0f, 2f),
        new Shot("13_south", 6.4f, 1.6f, 34f, 180f, 3f),
        new Shot("14_saloon", 2.5f, 1.64f, 29.8f, 180f, 3f),
        new Shot("15_exit", 6.6f, 1.6f, 3.2f, 180f, -14f),
        new Shot("16_behind", 6.6f, 1.66f, -6.5f, 180f, 0f),
        new Shot("17_barrier", 6.6f, 1.66f, -14.6f, 180f, 0f),
        new Shot("18_seat", 2.5f, 1.52f, 31.8f, 0f, 6f),
        new Shot("19_gauge", 2.1f, 1.52f, 31.8f, -22f, 32f),
        new Shot("20_run", 2.5f, 1.5f, 300f, 0f, 2f),
        new Shot("21_lamp", 2.8f, 1.5f, 311f, 8f, 0f),
        new Shot("22_arrive", 2.5f, 1.52f, 560f, 0f, 2f),
        new Shot("23_severna", 6.2f, 1.6f, 590f, 0f, 3f),
        new Shot("24_board", 2.5f, 1.52f, 612f, 12f, 2f),
        new Shot("25_shutter", 6.4f, 1.6f, 589f, 180f, 2f),
    };

    static readonly Shot[] Shift2Shots =
    {
        new Shot("1_arrive", 10.5f, 1.6f, -5f, 0f, 2f),
        new Shot("2_hall", 11.4f, 1.6f, 2f, -14f, 3f),
        new Shot("3_platform", 6.2f, 1.6f, 6f, 2f, 3f),
        new Shot("4_train", 9.6f, 1.6f, 24f, -90f, 6f),
        new Shot("5_booth", 8.2f, 1.6f, 10.3f, 90f, 5f),
        new Shot("6_vera", 10.3f, 1.6f, 10.3f, 90f, 3f),
        new Shot("7_north", 5.2f, 1.6f, 38f, 180f, 3f),
        new Shot("8_bench", 11f, 1.6f, 30.7f, 90f, 12f),
        new Shot("9_lockers", 10.6f, 1.6f, 45f, 0f, 3f),
        new Shot("10_bolnichna", 6.6f, 1.6f, 590f, 0f, 3f),
        new Shot("11_bolnichna_exit", 6.8f, 1.6f, 600f, 62f, 3f),
        new Shot("12_portal", 2.5f, 1.52f, 1185f, 0f, 2f),
        new Shot("13_cutting", 2.5f, 1.52f, 1240f, 0f, 2f),
        new Shot("14_terminal", 6.5f, 1.6f, 1330f, 0f, 3f),
        new Shot("15_yard", 5f, 1.6f, 1340f, -90f, 3f),
        new Shot("16_hut", 8.5f, 1.6f, 1356f, 14f, 3f),
        new Shot("17_road", 11.4f, 1.6f, 1356f, 6f, 3f),
        new Shot("18_town", 12f, 1.7f, 1362f, 90f, 3f),
        new Shot("19_shelter", 14f, 1.6f, 1362f, -30f, 3f),
        new Shot("20_exit", 10.4f, 1.6f, 19.3f, 90f, 3f),
        new Shot("21_exit_close", 11.8f, 1.6f, 18.6f, 80f, 8f),
        new Shot("22_relay", 6.6f, 1.66f, 991.25f, 180f, 8f),
        new Shot("23_relay_far", 6.6f, 1.66f, 994.2f, 180f, 3f),
        new Shot("24_seat", 2.5f, 1.52f, 31.8f, 0f, 6f),
        new Shot("25_seat_left", 2.5f, 1.52f, 31.8f, -34f, 4f),
        new Shot("26_seat_right", 2.5f, 1.52f, 31.8f, 36f, 4f),
        new Shot("27_cab_back", 2.5f, 1.52f, 32f, 170f, 2f),
        new Shot("28_phone", 12.2f, 1.6f, 1363.6f, -66f, 6f),
        new Shot("29_bus_side", 12.6f, 1.6f, 1364f, 34f, 0f),
        new Shot("30_street", 11.6f, 1.6f, 1361.2f, 90f, 2f),
        new Shot("31_gate", 7.4f, 1.6f, 1352f, 40f, 2f),
        new Shot("32_south", 12f, 1.6f, 1357f, 186f, 2f),
        new Shot("33_stop", 13.1f, 1.6f, 1378f, -148f, 2f),
    };

    static readonly Shot[] OpeningShots =
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
        new Shot("10_gate", 5.4f, 1.6f, 4.4f, 90f, 3f),
        new Shot("11_tunnel", 13f, -0.8f, 4.4f, 90f, 0f),
        new Shot("12_lap_front", 1.4f, 1.05f, 3.95f, -90f, 31f),
        new Shot("13_lap_side", 0.72f, 0.7f, 6.2f, 0f, 12f),
    };

    static readonly float[] RunShots = { 30f, 90f, 200f, 255f, 300f, 420f };

    static readonly Shot[] MenuShots =
    {
        new Shot("1_start", 2.5f, 1.35f, 67f, 180f, 0f),
        new Shot("2_end", 2.5f, 1.35f, 52f, 180f, 0f),
    };

    static readonly Dictionary<string, Shot[]> Shots = new Dictionary<string, Shot[]>
    {
        { "Opening", OpeningShots },
        { "Shift1", Shift1Shots },
        { "Shift2", Shift2Shots },
        { "MainMenu", MenuShots },
    };

    public static void Run()
    {
        string directory = Argument("-captureDir") ?? "Temp/SceneCapture";
        string[] size = (Argument("-captureSize") ?? "1280x720").Split('x');
        string only = Argument("-captureOnly");
        string scene = Argument("-captureScene") ?? "Opening";
        Directory.CreateDirectory(directory);

        ShaderUtil.allowAsyncCompilation = false;
        RetroLookSetup.Apply();
        EditorSceneManager.OpenScene($"Assets/Scenes/{scene}.unity");
        PoseCharacters();

        Camera camera = Camera.main;

        // The first frame after opening a scene can draw with the wrong textures, so it's thrown away.
        Render(camera, Path.Combine(directory, "first_frame.tmp"), 64, 64);
        File.Delete(Path.Combine(directory, "first_frame.tmp"));

        // Two frames a hair apart show up anything fighting over the same pixels.
        bool jitter = Argument("-captureJitter") != null;
        Material post = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/M_RetroPost.mat");
        float grain = post.GetFloat("_Grain");
        if (jitter) post.SetFloat("_Grain", 0f);

        foreach (Shot shot in Shots[scene])
        {
            if (only != null && !shot.Name.Contains(only)) continue;
            camera.transform.SetPositionAndRotation(shot.Position, Quaternion.Euler(shot.Pitch, shot.Yaw, 0f));
            if (camera.TryGetComponent(out OpenAir _) || camera.GetComponentInParent<OpenAir>() != null) camera.GetComponentInParent<OpenAir>().Apply(shot.Position.z);
            HoldLamp(camera, shot.Name.Contains("relay") || shot.Name.Contains("locker"));
            Render(camera, Path.Combine(directory, shot.Name + ".png"), int.Parse(size[0]), int.Parse(size[1]));
            if (!jitter) continue;

            camera.transform.SetPositionAndRotation(shot.Position + new Vector3(0.0007f, 0.0004f, 0.0009f), Quaternion.Euler(shot.Pitch + 0.012f, shot.Yaw + 0.016f, 0f));
            Render(camera, Path.Combine(directory, shot.Name + "_b.png"), int.Parse(size[0]), int.Parse(size[1]));
        }
        if (jitter)
        {
            post.SetFloat("_Grain", grain);
            AssetDatabase.SaveAssets();
        }

        if (scene == "Shift1" && (only == null || only == "drive")) RenderTheRun(camera, directory, int.Parse(size[0]), int.Parse(size[1]));
        Debug.Log($"SceneCapture wrote {directory}");
    }

    // The view from the driver's seat with the train moved along the line and its headlight on.
    static void RenderTheRun(Camera camera, string directory, int width, int height)
    {
        Transform train = GameObject.Find("Train").transform;
        Transform seat = train.Find("SeatPoint");
        train.Find("Headlight").GetComponent<Light>().enabled = true;

        foreach (float distance in RunShots)
        {
            train.position = Vector3.forward * distance;
            Vector3 eye = seat.position + Vector3.up * 0.68f;
            camera.transform.SetPositionAndRotation(eye, Quaternion.Euler(4f, 0f, 0f));
            Render(camera, Path.Combine(directory, $"run_{distance:000}.png"), width, height);
        }
    }

    // Edit mode never ticks Animators, so without this everyone would render in their bind pose.
    static void PoseCharacters()
    {
        foreach (Animator animator in UnityEngine.Object.FindObjectsByType<Animator>(FindObjectsSortMode.None))
        {
            if (animator.runtimeAnimatorController == null) continue;
            animator.Rebind();
            animator.Update(0.5f);
            if (animator.TryGetComponent(out RestHandsOnThighs hands)) hands.Rest();
            if (animator.TryGetComponent(out HandsOnCounter counter)) counter.Rest();
        }
    }

    // The player's lamp, for shots of places that have no light of their own.
    static void HoldLamp(Camera camera, bool on)
    {
        HandLamp lamp = UnityEngine.Object.FindAnyObjectByType<HandLamp>();
        if (lamp == null) return;
        lamp.beam.enabled = on;
        lamp.beam.transform.SetPositionAndRotation(camera.transform.TransformPoint(new Vector3(0.18f, -0.22f, 0.2f)), camera.transform.rotation);
    }

    static void Render(Camera camera, string path, int width, int height)
    {
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
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
