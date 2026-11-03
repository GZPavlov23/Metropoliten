using System.Collections;
using UnityEngine;

// Something on a hinge that swings once when told to: a door, a locker, a heavy handle.
public class SwingOpen : MonoBehaviour
{
    [Tooltip("The turn it makes, in degrees around each axis.")]
    public Vector3 swing = new Vector3(0f, -100f, 0f);

    public float seconds = 1.1f;

    public AudioSource sound;

    bool swung;

    public void Open()
    {
        if (swung) return;
        swung = true;

        if (ShiftChecklist.Resuming)
        {
            transform.localRotation *= Quaternion.Euler(swing);
            return;
        }

        if (sound != null) sound.Play();
        StartCoroutine(Swing());
    }

    IEnumerator Swing()
    {
        Quaternion start = transform.localRotation, end = start * Quaternion.Euler(swing);
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(start, end, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        transform.localRotation = end;
    }
}
