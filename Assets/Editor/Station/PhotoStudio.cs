using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static SceneForge;

// The photograph in the locker. It's taken the way it would have been: a hospital entrance, a man going up the steps with
// his back to the camera, and someone with a long lens across the road. The set is built and thrown away, a few dozen
// frames are taken with the lens moved a little each time so that only the steps are sharp, and the result is developed
// like fast film. There's a second man in it, by the pillar. It's up to the player whether they ever notice.
public static class PhotoStudio
{
    const int Width = 1024, Height = 768, Exposures = 36;
    const float Aperture = 0.11f, FieldOfView = 10.6f;
    static readonly Vector3 CameraAt = new Vector3(-3.4f, 1.9f, -48f), FocusOn = new Vector3(0.7f, 1.74f, -4f);

    static Dictionary<string, MeshKit> kits;

    static MeshKit Kit(string texture)
    {
        if (!kits.TryGetValue(texture, out MeshKit kit)) kits[texture] = kit = new MeshKit();
        return kit;
    }

    public static void Shoot()
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        kits = new Dictionary<string, MeshKit>();

        BuildFront();
        BuildStreet();
        foreach (KeyValuePair<string, MeshKit> kit in kits) Place(kit.Key, kit.Value);

        var made = new List<GameObject>
        {
            Person(Cast.Nikola, GaitForge.Weary, 0.3f, new Vector3(0.6f, 0.49f, -4.3f), 6f),
            Person(Cast.LongCoat, null, 0f, new Vector3(3.5f, 0.96f, -1.05f), -128f),
            Person(Cast.Nurse, null, 0f, new Vector3(-3.05f, 0.96f, -1.2f), 104f),
        };

        Light();
        Canvas film = Expose();
        foreach (GameObject person in made) Object.DestroyImmediate(person);
        TextureForge.WritePhoto(film);
    }

    static void Place(string texture, MeshKit kit)
    {
        var go = new GameObject(texture);
        go.AddComponent<MeshFilter>().sharedMesh = kit.ToMesh(texture);
        var material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path(texture)));
        go.AddComponent<MeshRenderer>().sharedMaterial = material;
    }

    // Someone stood or walking, frozen into a plain mesh so that it stays put through every exposure.
    static GameObject Person(Look look, GaitForge.Gait gait, float through, Vector3 feet, float yaw)
    {
        GameObject body = CharacterForge.Spawn(look, $"Photo{look.Name}", OpeningSceneBuilder.Controller("Idle"), null, true);
        Animator animator = body.GetComponent<Animator>();
        if (gait != null)
        {
            var controller = new UnityEditor.Animations.AnimatorController { name = "PhotoGait" };
            controller.AddLayer("Base");
            controller.layers[0].stateMachine.AddState("Walking").motion = GaitForge.Load(gait);
            animator.runtimeAnimatorController = controller;
        }

        body.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        animator.Rebind();
        animator.Update(gait != null ? GaitForge.CycleSeconds(gait) * through : 0.5f);

        SkinnedMeshRenderer skin = body.GetComponentInChildren<SkinnedMeshRenderer>();
        var posed = new Mesh();
        skin.BakeMesh(posed, true);

        var frozen = new GameObject($"Frozen{look.Name}");
        frozen.AddComponent<MeshFilter>().sharedMesh = posed;
        frozen.AddComponent<MeshRenderer>().sharedMaterial = skin.sharedMaterial;
        frozen.transform.SetPositionAndRotation(skin.transform.position, skin.transform.rotation);
        frozen.transform.localScale = Vector3.one;

        Bounds bounds = frozen.GetComponent<MeshRenderer>().bounds;
        frozen.transform.position += new Vector3(-bounds.center.x, -bounds.min.y, -bounds.center.z);
        var stand = new GameObject($"Standing{look.Name}");
        frozen.transform.SetParent(stand.transform, true);
        stand.transform.SetPositionAndRotation(feet, Quaternion.Euler(0f, yaw, 0f));

        AssetDatabase.DeleteAsset($"{MeshDir}/SK_Photo{look.Name}.asset");
        Object.DestroyImmediate(body);
        return stand;
    }

    // Four storeys of hospital, rendered a long time ago, with a concrete porch stuck on the front of it.
    static void BuildFront()
    {
        const float wall = 0f, floorHeight = 3.4f, bay = 3f;
        Vector2 render = new Vector2(3.6f, 3.6f), concrete = new Vector2(2.4f, 2.4f);
        MeshKit plaster = Kit("T_HospitalRender");

        var holes = new List<Rect>();
        const float halfWidth = 21f;
        for (int floor = 0; floor < 4; floor++)
        {
            for (int column = -7; column <= 6; column++)
            {
                float x = halfWidth + column * bay + 0.45f, y = 1.9f + floor * floorHeight;
                bool door = floor == 0 && (column == -2 || column == -1 || column == 0 || column == 1);
                if (door) continue;
                holes.Add(new Rect(x, y, 2.1f, 2.1f));
            }
        }
        holes.Add(Rect.MinMaxRect(halfWidth - 3.1f, 0.96f, halfWidth + 3.1f, 3.5f));

        // Worked from the far end, so that the wall faces the road.
        var flipped = new Rect[holes.Count];
        for (int i = 0; i < flipped.Length; i++)
            flipped[i] = Rect.MinMaxRect(halfWidth * 2f - holes[i].xMax, holes[i].yMin, halfWidth * 2f - holes[i].xMin, holes[i].yMax);
        plaster.WallWithHoles(new Vector3(halfWidth, 0f, wall), Vector3.left, Vector3.up, halfWidth * 2f, 15f, flipped, render);

        // Behind each hole, a window that's sunk into the wall, with its frame, glass and blind.
        foreach (Rect hole in holes)
        {
            if (hole.width > 3f) continue;
            float left = hole.xMin - halfWidth, bottom = hole.yMin;
            const float depth = 0.28f;
            Kit("T_Concrete").Reveal(new Vector3(-halfWidth, 0f, wall), Vector3.right, Vector3.up, hole, Vector3.forward * depth, concrete);
            Kit("T_HospitalGlass").PanelFacing(new Vector3(left, bottom, wall + depth), Vector3.right * hole.width, Vector3.up * hole.height, Vector3.back, new Vector2(2f, 2f));

            float blind = TextureForge.Hash(Mathf.RoundToInt(left), Mathf.RoundToInt(bottom), 801);
            if (blind > 0.3f)
                Kit("T_Stone").PanelFacing(new Vector3(left, bottom + hole.height * (1f - blind * 0.8f), wall + depth - 0.02f), Vector3.right * hole.width, Vector3.up * (hole.height * blind * 0.8f), Vector3.back, new Vector2(2f, 2f));

            foreach (float across in new[] { 0f, hole.width / 2f - 0.03f, hole.width - 0.06f })
                Kit("T_Stone").Box(new Vector3(left + across + 0.03f, bottom + hole.height / 2f, wall + depth - 0.04f), new Vector3(0.06f, hole.height, 0.05f), new Vector2(2f, 2f));
            Kit("T_Stone").Box(new Vector3(left + hole.width / 2f, bottom + hole.height * 0.66f, wall + depth - 0.04f), new Vector3(hole.width, 0.05f, 0.05f), new Vector2(2f, 2f));
            Kit("T_Concrete").Box(new Vector3(left + hole.width / 2f, bottom - 0.04f, wall - 0.06f), new Vector3(hole.width + 0.2f, 0.07f, 0.2f), concrete);
        }

        // The plinth, and a band of darker render above it.
        Kit("T_Kerb").Box(new Vector3(0f, 0.6f, wall - 0.05f), new Vector3(halfWidth * 2f, 1.2f, 0.1f), new Vector2(2f, 2f));

        // The steps, up to a landing under the porch.
        const int steps = 6;
        const float rise = 0.16f, run = 0.36f, landing = -3f, wide = 8.4f;
        MeshKit stone = Kit("T_Concrete");
        for (int i = 0; i < steps; i++)
        {
            float y = (i + 1) * rise, z = landing - (steps - 1 - i) * run;
            stone.Box(new Vector3(0f, y / 2f, z - run / 2f), new Vector3(wide, y, run), concrete);
        }
        stone.Box(new Vector3(0f, steps * rise / 2f, landing / 2f), new Vector3(wide, steps * rise, -landing), concrete);

        // Handrails, up the sides and down the middle.
        foreach (float x in new[] { -wide / 2f + 0.15f, 0f, wide / 2f - 0.15f })
        {
            Vector3 foot = new Vector3(x, 0.96f, landing - steps * run), head = new Vector3(x, 1.94f, landing);
            Kit("T_Bark").Tube(foot, head, 0.03f, 6, new Vector2(1f, 1f));
            Kit("T_Bark").Tube(head, head + Vector3.forward * 2.8f, 0.03f, 6, new Vector2(1f, 1f));
            foreach (float t in new[] { 0f, 0.5f, 1f })
            {
                Vector3 on = Vector3.Lerp(foot, head, t);
                Kit("T_Bark").Tube(on, new Vector3(on.x, on.y - 0.96f, on.z), 0.02f, 5, new Vector2(1f, 1f));
            }
        }

        // The porch: a slab on two round pillars, and the name of the place along the edge of it.
        const float porchTop = 4.2f, porchFront = -3.4f, thick = 0.44f;
        stone.Box(new Vector3(0f, porchTop - thick / 2f, porchFront / 2f), new Vector3(9.6f, thick, -porchFront), concrete);
        foreach (float x in new[] { -3f, 3f })
            stone.Tube(new Vector3(x, 0.96f, -2.5f), new Vector3(x, porchTop - thick, -2.5f), 0.24f, 16, concrete);
        Kit("T_Hospital").Plate(new Vector3(0f, porchTop + 0.42f, porchFront + 0.1f), Vector3.back, Vector3.up, 6.4f, 0.82f, 0.06f);
        foreach (float x in new[] { -2.8f, 2.8f })
            Kit("T_Bark").Tube(new Vector3(x, porchTop - 0.02f, porchFront + 0.1f), new Vector3(x, porchTop + 0.1f, porchFront + 0.1f), 0.03f, 6, new Vector2(1f, 1f));

        // The doors, in a metal screen.
        Vector3 lobby = new Vector3(3.1f, 0.96f, 0.1f);
        Kit("T_HospitalLobby").QuadUv(lobby, lobby + Vector3.left * 6.2f, lobby + new Vector3(-6.2f, 2.6f, 0f), lobby + Vector3.up * 2.6f, Vector3.back,
            new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f));
        foreach (float x in new[] { -3f, -1.5f, 0f, 1.5f, 3f })
            Kit("T_Bark").Box(new Vector3(x, 2.26f, 0.04f), new Vector3(0.1f, 2.6f, 0.08f), new Vector2(1f, 1f));
        foreach (float y in new[] { 1.02f, 2.96f, 3.5f })
            Kit("T_Bark").Box(new Vector3(0f, y, 0.04f), new Vector3(6.2f, 0.1f, 0.08f), new Vector2(1f, 1f));
        foreach (float x in new[] { -0.74f, 0.74f })
            Kit("T_Stone").Box(new Vector3(x, 1.96f, -0.02f), new Vector3(0.5f, 0.05f, 0.03f), new Vector2(1f, 1f));

        // Ramp and rail at the side, for the people who can't manage the steps, and a bin.
        Kit("T_Concrete").Box(Matrix4x4.TRS(new Vector3(6.6f, 0.5f, -1.4f), Quaternion.Euler(0f, 0f, 3.6f), Vector3.one), new Vector3(5f, 0.1f, 1.8f), concrete);
        Kit("T_Kerb").Tube(new Vector3(-4.8f, 0.96f, -1.6f), new Vector3(-4.8f, 1.8f, -1.6f), 0.2f, 12, new Vector2(1f, 1f));
    }

    static void BuildStreet()
    {
        Vector2 tile = new Vector2(3f, 3f);
        Kit("T_Slabs").Panel(new Vector3(-60f, 0f, -11f), Vector3.forward * 11f, Vector3.right * 120f, tile);
        Kit("T_Road").Panel(new Vector3(-60f, -0.08f, -22f), Vector3.forward * 11f, Vector3.right * 120f, new Vector2(11f, 28f));
        Kit("T_Slabs").Panel(new Vector3(-60f, 0f, -60f), Vector3.forward * 38f, Vector3.right * 120f, tile);
        Kit("T_Kerb").Box(new Vector3(0f, -0.03f, -11f), new Vector3(120f, 0.12f, 0.18f), new Vector2(2f, 2f));

        // The bare trees along the road, and the car parked right under the lens.
        StationBuilder.BareTree(Kit("T_Bark"), new Vector3(8.4f, 0f, -9.4f), 10.5f, 811);
        StationBuilder.BareTree(Kit("T_Bark"), new Vector3(-8.4f, 0f, -9f), 9.5f, 812);
        StationBuilder.BareTree(Kit("T_Bark"), new Vector3(5.6f, 0f, -29f), 13f, 813);
        StationBuilder.ParkedCar(Kit("T_CarDark"), Kit("T_CarGlass"), Kit("T_Bark"), new Vector3(-4.4f, 0f, -33.4f), 96f);
        StationBuilder.ParkedCar(Kit("T_CarPale"), Kit("T_CarGlass"), Kit("T_Bark"), new Vector3(-7.4f, -0.08f, -13.4f), 90f);

        // The lamp post on the kerb, and the wall of the sky behind the building, since nothing's up there.
        Kit("T_Bark").Tube(new Vector3(4.3f, 0f, -10f), new Vector3(4.3f, 7f, -10f), 0.08f, 8, new Vector2(1f, 1f));
    }

    // A flat white morning: nearly all of it comes down from the cloud, with nothing to throw a real shadow.
    static void Light()
    {
        var sky = new GameObject("Overcast").AddComponent<Light>();
        sky.type = LightType.Directional;
        sky.transform.rotation = Quaternion.Euler(64f, 24f, 0f);
        sky.color = new Color(0.94f, 0.96f, 1f);
        sky.intensity = 0.7f;
        sky.shadows = LightShadows.Soft;
        sky.shadowStrength = 0.72f;

        RenderSettings.ambientMode = AmbientMode.Flat;
        var ambient = new SphericalHarmonicsL2();
        ambient.AddAmbientLight(new Color(0.6f, 0.61f, 0.63f));
        RenderSettings.ambientProbe = ambient;
        RenderSettings.fog = false;
        Shader.SetGlobalFloat("_OpenFog", 0f);
    }

    // Takes the same picture many times, with the lens somewhere different across its width and always looking at the
    // same spot: what's at that distance stays still, and everything else smears.
    static Canvas Expose()
    {
        var go = new GameObject("Camera");
        Camera camera = go.AddComponent<Camera>();
        camera.fieldOfView = FieldOfView;
        camera.nearClipPlane = 1f;
        camera.farClipPlane = 200f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.46f, 0.47f, 0.49f);
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;

        Material post = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Generated/M_RetroPost.mat");
        float grain = post.GetFloat("_Grain"), vignette = post.GetFloat("_Vignette");
        post.SetFloat("_Grain", 0f);
        post.SetFloat("_Vignette", 0f);

        var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB) { antiAliasing = 4 };
        var read = new Texture2D(Width, Height, TextureFormat.RGB24, false);
        var light = new float[Width * Height];
        Vector3 right = Vector3.Cross(Vector3.up, FocusOn - CameraAt).normalized;

        // The first frame after a scene opens can be wrong, as ever.
        camera.targetTexture = target;
        camera.transform.SetPositionAndRotation(CameraAt, Quaternion.LookRotation(FocusOn - CameraAt));
        camera.Render();

        for (int i = 0; i < Exposures; i++)
        {
            // A sunflower of points fills the lens evenly.
            float r = Mathf.Sqrt((i + 0.5f) / Exposures) * Aperture, a = i * 2.39996f;
            Vector3 lens = CameraAt + right * (Mathf.Cos(a) * r) + Vector3.up * (Mathf.Sin(a) * r);
            camera.transform.SetPositionAndRotation(lens, Quaternion.LookRotation(FocusOn - lens));
            camera.Render();

            RenderTexture.active = target;
            read.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            Color[] pixels = read.GetPixels();
            for (int p = 0; p < light.Length; p++) light[p] += pixels[p].grayscale / Exposures;
        }

        RenderTexture.active = null;
        camera.targetTexture = null;
        target.Release();
        Object.DestroyImmediate(read);
        Object.DestroyImmediate(go);
        post.SetFloat("_Grain", grain);
        post.SetFloat("_Vignette", vignette);
        AssetDatabase.SaveAssets();
        return TextureForge.Develop(light, Width, Height);
    }
}
