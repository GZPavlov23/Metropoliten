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

        FullScreenPassRendererFeature pass = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault();
        if (pass == null) pass = AddFullScreenPass(data);
        pass.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
        pass.fetchColorBuffer = true;
        pass.passMaterial = material;
        pass.passIndex = 0;
        Grade(material);

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
    static FullScreenPassRendererFeature AddFullScreenPass(UniversalRendererData data)
    {
        var pass = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
        pass.name = "RetroPost";
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
