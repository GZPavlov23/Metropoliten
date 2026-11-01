using System.Collections;
using UnityEngine;

// A door that slides aside and back when told to.
public class SlideOpen : MonoBehaviour
{
    [Tooltip("How far it moves when it opens.")]
    public Vector3 offset;

    public float seconds = 1.4f;

    public AudioSource sound;

    Vector3 closed;

    void Awake()
    {
        closed = transform.localPosition;
    }

    public void Open()
    {
        StopAllCoroutines();
        StartCoroutine(Slide(closed + offset));
    }

    public void Close()
    {
        StopAllCoroutines();
        StartCoroutine(Slide(closed));
    }

    IEnumerator Slide(Vector3 target)
    {
        if (sound != null) sound.Play();

        Vector3 start = transform.localPosition;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            transform.localPosition = Vector3.Lerp(start, target, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        transform.localPosition = target;
    }
}
