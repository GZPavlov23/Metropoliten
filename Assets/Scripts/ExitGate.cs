using System.Collections;
using UnityEngine;

// A folding lattice gate. Its mesh and collider start at the hinge, so folding is just scaling along Z.
public class ExitGate : MonoBehaviour
{
    public float openSeconds = 1.8f;

    [Range(0.05f, 1f)]
    [Tooltip("How much of its width the gate takes up once folded.")]
    public float foldedWidth = 0.12f;

    public AudioSource sound;

    public void Open()
    {
        if (sound != null) sound.Play();
        StartCoroutine(Fold());
    }

    IEnumerator Fold()
    {
        Vector3 scale = transform.localScale;
        for (float t = 0f; t < openSeconds; t += Time.deltaTime)
        {
            scale.z = Mathf.Lerp(1f, foldedWidth, Mathf.SmoothStep(0f, 1f, t / openSeconds));
            transform.localScale = scale;
            yield return null;
        }
        scale.z = foldedWidth;
        transform.localScale = scale;
    }
}
