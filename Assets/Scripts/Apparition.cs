using System.Collections;
using UnityEngine;

// Stoyan. From the moment the handle comes down he's standing in the passage behind the player, and he stays there until
// they turn round. It's a dead end, so sooner or later they have to. He comes once, and nobody ever speaks of it.
public class Apparition : MonoBehaviour
{
    const float SparkDelay = 0.42f, LungeSeconds = 0.1f, StrobeSeconds = 0.55f, DarkSeconds = 1.25f;

    [Tooltip("The body, switched off until it's wanted.")]
    public GameObject figure;

    public Transform head;
    public FirstPersonController player;
    public HandLamp lamp;

    [Tooltip("How high the body stands off the ground.")]
    public float standHeight;

    [Tooltip("The middle of the passage, which he stands in.")]
    public float passageX;

    [Tooltip("How far up the passage from the player he waits.")]
    public float waitsAt = 1.15f;

    [Tooltip("How close to the player's face he gets.")]
    public float comesTo = 0.34f;

    [Tooltip("How far his head can be from the middle of the view, in degrees, for him to count as seen.")]
    public float seenWithin = 31f;

    [Tooltip("So close that he'd be walked into, seen or not.")]
    public float tooClose = 0.62f;

    [Tooltip("The spark off the switch.")]
    public Light spark;

    [Tooltip("Where he's heard from.")]
    public AudioSource nearby;

    public AudioClip footstep, breath;

    public bool Struck { get; private set; }

    Transform view, gaze;

    // The head bone is at the base of the skull. What the view locks on to is the middle of what's left of his face.
    Vector3 FaceAt => head.position + Vector3.up * 0.09f;

    public void Arm()
    {
        if (ShiftChecklist.Resuming) return;
        view = player.cameraTransform;
        gaze = new GameObject("Gaze").transform;
        StartCoroutine(Haunt());
    }

    IEnumerator Haunt()
    {
        yield return new WaitForSeconds(SparkDelay);

        // The contacts part, and whatever else was on that circuit goes with them.
        SoundDirector.Instance.Cue("I_Arc");
        SoundDirector.Instance.Hush(0.2f, 600f, 1f);
        lamp.Force(false);
        yield return Spark();
        yield return new WaitForSeconds(0.35f);

        // In the dark, he arrives.
        figure.transform.SetPositionAndRotation(new Vector3(passageX, standHeight, view.position.z + waitsAt), Quaternion.Euler(0f, 180f, 0f));
        figure.SetActive(true);

        // He doesn't shift his weight. He doesn't do anything.
        figure.GetComponent<Animator>().speed = 0f;
        lamp.Force(true);
        yield return new WaitForSeconds(0.07f);
        lamp.Force(false);
        yield return new WaitForSeconds(0.2f);
        lamp.Force(true);

        yield return new WaitForSeconds(0.8f);
        Say(footstep, 0.55f);

        float nextBreath = Time.time + 2.8f;
        while (!Seen())
        {
            if (Time.time >= nextBreath)
            {
                nextBreath = Time.time + 5.5f;
                Say(breath, 0.5f);
            }
            yield return null;
        }

        yield return Strike();
    }

    IEnumerator Spark()
    {
        foreach ((float on, float off) in new[] { (0.05f, 0.04f), (0.09f, 0f) })
        {
            spark.enabled = true;
            yield return new WaitForSeconds(on);
            spark.enabled = false;
            yield return new WaitForSeconds(off);
        }
    }

    void Say(AudioClip clip, float level)
    {
        nearby.transform.position = head.position;
        nearby.PlayOneShot(clip, level);
    }

    bool Seen()
    {
        Vector3 toHead = head.position - view.position;
        return toHead.magnitude < tooClose || Vector3.Angle(view.forward, toHead) < seenWithin;
    }

    IEnumerator Strike()
    {
        Struck = true;
        SoundDirector.Instance.Cue("S_Scare");
        lamp.Force(true);

        float turnSpeed = player.attentionTurnSpeed;
        player.attentionTurnSpeed = 24f;
        gaze.position = FaceAt;
        player.HoldAttention(gaze);

        // He covers the distance in less time than it takes to flinch.
        Vector3 from = figure.transform.position, flat = Vector3.ProjectOnPlane(from - view.position, Vector3.up).normalized;
        Vector3 to = view.position + flat * comesTo;
        to.y = from.y;
        for (float t = 0f; t < LungeSeconds; t += Time.deltaTime)
        {
            float along = t / LungeSeconds;
            figure.transform.position = Vector3.Lerp(from, to, 1f - (1f - along) * (1f - along));
            gaze.position = FaceAt;
            yield return null;
        }
        figure.transform.position = to;

        // The lamp can't hold it.
        for (float t = 0f; t < StrobeSeconds; t += Time.deltaTime)
        {
            lamp.Force(Mathf.Repeat(t, 0.11f) < 0.075f);
            player.Shake = Random.insideUnitSphere * 0.012f;
            gaze.position = FaceAt;
            yield return null;
        }

        lamp.Force(false);
        figure.SetActive(false);
        player.Shake = Vector3.zero;
        yield return new WaitForSeconds(DarkSeconds);

        // It comes back on to an empty passage.
        foreach ((float on, float off) in new[] { (0.06f, 0.16f), (0.05f, 0.1f), (0.12f, 0.12f) })
        {
            lamp.Force(true);
            yield return new WaitForSeconds(on);
            lamp.Force(false);
            yield return new WaitForSeconds(off);
        }
        lamp.Force(true);

        player.ReleaseAttention();
        player.attentionTurnSpeed = turnSpeed;
        SoundDirector.Instance.Hush(0f, 0f, 6f);
        Destroy(gaze.gameObject);
    }
}
