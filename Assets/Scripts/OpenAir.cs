using UnityEngine;

// Where the line comes up out of the ground the dark gives way to a grey morning: the fog, the far distance and what's
// behind everything all change with how far along the line the player has got.
public class OpenAir : MonoBehaviour
{
    static readonly int OpenFog = Shader.PropertyToID("_OpenFog"), OpenFogFrom = Shader.PropertyToID("_OpenFogFrom");

    public Camera view;

    [Tooltip("How far along the line the daylight starts to show, and where it's all the way there.")]
    public float startZ, fullZ;

    [Tooltip("From here on the tunnel is short enough to see the end of.")]
    public float seeOutZ;

    [Tooltip("Where the ground fog begins, at the mouth of the tunnel.")]
    public float mouthZ;

    public Color tunnelFog = new Color(0.012f, 0.011f, 0.008f), outsideFog = new Color(0.3f, 0.32f, 0.35f);
    public float tunnelDensity = 0.028f, outsideDensity = 0.004f;
    public float tunnelFar = 60f, outsideFar = 420f;

    void LateUpdate()
    {
        Apply(view.transform.position.z);
    }

    // The fog's a global setting, and it mustn't follow the player back to the title.
    void OnDisable()
    {
        Shader.SetGlobalFloat(OpenFog, 0f);
    }

    public void Apply(float z)
    {
        float open = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(startZ, fullZ, z));
        RenderSettings.fogColor = Color.Lerp(tunnelFog, outsideFog, open);
        RenderSettings.fogDensity = Mathf.Lerp(tunnelDensity, outsideDensity, open);
        view.farClipPlane = z > seeOutZ ? outsideFar : tunnelFar;
        view.backgroundColor = RenderSettings.fogColor;

        Shader.SetGlobalFloat(OpenFog, z > seeOutZ ? 1f : 0f);
        Shader.SetGlobalFloat(OpenFogFrom, mouthZ);
    }
}
