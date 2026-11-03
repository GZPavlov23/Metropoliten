using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

// Wires the grain-and-grade pass into the PC renderer. Safe to run repeatedly.
public static class RetroLookSetup
{
    const string RendererPath = "Assets/Settings/PC_Renderer.asset";
    const string MaterialPath = "Assets/Art/Generated/M_RetroPost.mat";
    const string ShaderName = "Hidden/Metropoliten/RetroPost";
    const string FogPass = "OpenFog", FogMaterialPath = "Assets/Art/Generated/M_OpenFog.mat", FogShaderName = "Hidden/Metropoliten/OpenFog";

    [MenuItem("Metropoliten/Apply Retro Look To Renderer")]
    public static void Apply()
    {
        var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
        Material material = LoadOrCreateMaterial();

        // Screen-space AO only darkens URP Lit surfaces; everything here is lit by baked lightmaps instead.
        // It has to leave the list altogether: builds strip its resources but still create disabled features,
        // and the render pipeline then fails to start.
        foreach (ScriptableRendererFeature feature in data.rendererFeatures.ToArray())
            if (feature != null && feature.GetType().Name == "ScreenSpaceAmbientOcclusion") RemoveFeature(data, feature);

        FullScreenPassRendererFeature pass = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name != FogPass);
        if (pass == null) pass = AddFullScreenPass(data, "RetroPost");
        pass.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        pass.fetchColorBuffer = true;
        pass.passMaterial = material;
        pass.passIndex = 0;
        Grade(material);

        // The open-air fog goes on before anything see-through, so that a fence or a lamp's glow is drawn in front of it.
        FullScreenPassRendererFeature fog = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f.name == FogPass);
        if (fog == null) fog = AddFullScreenPass(data, FogPass);
        fog.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.BeforeRenderingTransparents;
        fog.fetchColorBuffer = false;
        fog.requirements = ScriptableRenderPassInput.Depth;
        fog.passMaterial = FogMaterial();
        fog.passIndex = 0;
        EditorUtility.SetDirty(fog);

        EditorUtility.SetDirty(pass);
        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
    }

    // Tuned against frames of the reference game: median around 60/255, whites near clipping, low chroma.
    static void Grade(Material material)
    {
        material.SetFloat("_Exposure", 1.6f);
        material.SetFloat("_Saturation", 0.9f);
        material.SetFloat("_Contrast", 1.1f);
        material.SetColor("_Blacks", new Color(0.012f, 0.011f, 0.01f));
        material.SetColor("_Whites", new Color(1f, 0.99f, 0.97f));
        material.SetFloat("_Vignette", 0.7f);
        material.SetFloat("_Grain", 0.11f);
        EditorUtility.SetDirty(material);
    }

    static Material FogMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(FogMaterialPath);
        if (material == null)
        {
            material = new Material(Shader.Find(FogShaderName));
            AssetDatabase.CreateAsset(material, FogMaterialPath);
        }
        material.SetTexture("_Noise", FogForge.Noise());
        material.SetFloat("_Density", 0.046f);
        material.SetFloat("_Ceiling", 16f);
        material.SetFloat("_Banks", 38f);
        material.SetFloat("_Wisps", 9f);
        material.SetFloat("_Lumps", 2.4f);
        material.SetColor("_Low", new Color(0.46f, 0.475f, 0.5f));
        material.SetColor("_High", new Color(0.42f, 0.44f, 0.48f));
        material.SetColor("_Glow", new Color(0.27f, 0.22f, 0.18f));
        EditorUtility.SetDirty(material);
        return material;
    }

    static Material LoadOrCreateMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material != null) return material;

        material = new Material(Shader.Find(ShaderName));
        System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(MaterialPath));
        AssetDatabase.CreateAsset(material, MaterialPath);
        return material;
    }

    static void RemoveFeature(UniversalRendererData data, ScriptableRendererFeature feature)
    {
        var serialized = new SerializedObject(data);
        SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
        SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
        for (int i = features.arraySize - 1; i >= 0; i--)
        {
            if (features.GetArrayElementAtIndex(i).objectReferenceValue != feature) continue;
            features.GetArrayElementAtIndex(i).objectReferenceValue = null;
            features.DeleteArrayElementAtIndex(i);
            map.DeleteArrayElementAtIndex(i);
        }
        serialized.ApplyModifiedProperties();
        AssetDatabase.RemoveObjectFromAsset(feature);
        Object.DestroyImmediate(feature, true);
    }

    // The same steps the renderer's Add Renderer Feature button performs.
    static FullScreenPassRendererFeature AddFullScreenPass(UniversalRendererData data, string name)
    {
        var pass = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
        pass.name = name;
        AssetDatabase.AddObjectToAsset(pass, data);
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(pass, out string _, out long localId);

        var serialized = new SerializedObject(data);
        SerializedProperty features = serialized.FindProperty("m_RendererFeatures");
        SerializedProperty map = serialized.FindProperty("m_RendererFeatureMap");
        features.arraySize++;
        features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = pass;
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
        serialized.ApplyModifiedProperties();
        return pass;
    }
}
