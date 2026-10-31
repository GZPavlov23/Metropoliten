using System.Collections;
using UnityEngine;

// A tube on its way out: steady for a while, then a short stutter that dips the whole room a little.
[RequireComponent(typeof(Renderer))]
public class LampFlicker : MonoBehaviour
{
    static readonly int RoomDim = Shader.PropertyToID("_RoomDim");
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    [Tooltip("Shortest and longest wait between stutters, in seconds.")]
    public Vector2 calmSeconds = new Vector2(7f, 20f);

    [Range(0f, 1f)]
    [Tooltip("How much of its glow the tube keeps while it's out.")]
    public float outBrightness = 0.1f;

    [Range(0f, 1f)]
    [Tooltip("How much darker the whole image gets while the tube is out.")]
    public float roomDim = 0.1f;

    Material material;
    Color litColor;

    void Awake()
    {
        material = GetComponent<Renderer>().material;
        litColor = material.GetColor(BaseColor);
    }

    IEnumerator Start()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(calmSeconds.x, calmSeconds.y));

            int blinks = Random.Range(2, 5);
            for (int i = 0; i < blinks; i++)
            {
                SetLit(false);
                yield return new WaitForSeconds(Random.Range(0.05f, 0.14f));
                SetLit(true);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.2f));
            }
        }
    }

    void SetLit(bool lit)
    {
        material.SetColor(BaseColor, lit ? litColor : litColor * outBrightness);
        Shader.SetGlobalFloat(RoomDim, lit ? 0f : roomDim);
    }

    void OnDisable()
    {
        Shader.SetGlobalFloat(RoomDim, 0f);
    }
}
