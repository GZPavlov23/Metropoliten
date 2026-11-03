using UnityEngine;

// A sound that's always there, coming from somewhere in the room. Each one starts at its own point in the
// loop and a hair off its neighbours' pitch, so that a row of them drift against each other instead of adding up.
[RequireComponent(typeof(AudioSource))]
public class AmbientLoop : MonoBehaviour
{
    const float FadeRate = 0.2f;

    [Range(0f, 0.05f)]
    [Tooltip("How far from its true pitch this can start out.")]
    public float pitchSpread = 0.012f;

    [Tooltip("Keeps going when the rest of the place falls quiet, so that it's what's left to listen to.")]
    public bool heardThroughHush;

    // Lets something turn this sound down for a moment, like a lamp going out.
    public float Level { get; set; } = 1f;

    AudioSource source;
    float volume, share = 1f, wantedShare = 1f;

    void Start()
    {
        source = GetComponent<AudioSource>();
        volume = source.volume;
        source.pitch = 1f + Random.Range(-pitchSpread, pitchSpread);
        source.timeSamples = Random.Range(0, source.clip.samples);
        source.Play();
    }

    // Slowly settles at a share of its usual level, for a place that's emptier than the last.
    public void FadeTo(float level)
    {
        wantedShare = level;
    }

    void Update()
    {
        share = Mathf.MoveTowards(share, wantedShare, FadeRate * Time.deltaTime);
        source.volume = volume * share * Level * (heardThroughHush ? 1f : SoundDirector.Presence);
    }
}
