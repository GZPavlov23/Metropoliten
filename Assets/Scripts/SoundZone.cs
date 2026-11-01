using System.Collections;
using UnityEngine;

// The first time the player walks into this trigger the place goes quiet, and then a sound comes from
// somewhere else, whether or not they've stayed to hear it.
[RequireComponent(typeof(Collider))]
public class SoundZone : MonoBehaviour
{
    const float SinkSeconds = 2.5f, ReturnSeconds = 8f;

    [Tooltip("Where the sound comes from.")]
    public AudioSource source;

    public AudioClip[] clips;

    [Tooltip("How long the quiet lasts before the sound, in seconds.")]
    public float quietSeconds = 2f;

    bool happened;

    void OnTriggerEnter(Collider other)
    {
        if (happened || other.GetComponent<CharacterController>() == null) return;
        happened = true;
        StartCoroutine(Happen());
    }

    IEnumerator Happen()
    {
        SoundDirector.Instance.Hush(SinkSeconds, quietSeconds, ReturnSeconds);
        yield return new WaitForSeconds(SinkSeconds + quietSeconds);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
