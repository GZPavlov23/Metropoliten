using UnityEngine;

// The player's feet. The head bob decides when a foot lands, so what's heard always matches what's seen.
public class Footsteps : MonoBehaviour
{
    const float GroundRayLength = 2.5f;

    public FirstPersonController player;
    public AudioSource source;
    public AudioClip[] walking;
    public AudioClip[] running;

    [Range(0f, 1f)]
    public float walkVolume = 0.5f;
    [Range(0f, 1f)]
    public float runVolume = 0.7f;

    AudioClip lastClip;

    void OnEnable()
    {
        player.headBob.Stepped += Step;
    }

    void OnDisable()
    {
        player.headBob.Stepped -= Step;
    }

    void Step(float run)
    {
        StepSurface surface = null;
        if (Physics.Raycast(player.transform.position, Vector3.down, out RaycastHit ground, GroundRayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            surface = ground.collider.GetComponentInParent<StepSurface>();

        source.pitch = Random.Range(0.94f, 1.06f);
        source.PlayOneShot(Pick(surface != null ? surface.clips : run > 0.5f ? running : walking), Mathf.Lerp(walkVolume, runVolume, run) * Random.Range(0.85f, 1f));
    }

    // Never the same one twice running: a repeated footstep is the first thing an ear catches.
    AudioClip Pick(AudioClip[] clips)
    {
        AudioClip clip = clips[Random.Range(0, clips.Length)];
        if (clip == lastClip) clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
        lastClip = clip;
        return clip;
    }
}
