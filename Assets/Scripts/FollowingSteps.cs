using System.Collections;
using UnityEngine;

// Someone else's feet, some way behind the player. After a few paces they fall into step, and when the
// player stops they take one more than they should. Then nothing, for long enough to start doubting it.
public class FollowingSteps : MonoBehaviour
{
    public FirstPersonController player;

    [Tooltip("Moved to somewhere behind the player for each step.")]
    public AudioSource source;

    public AudioClip[] clips;

    [Tooltip("How many paces the player takes before anyone joins in.")]
    public int pacesAlone = 7;

    [Tooltip("Nearest and furthest behind the player, in metres.")]
    public Vector2 distance = new Vector2(11f, 17f);

    [Tooltip("How long after the player stops the last step comes, in seconds.")]
    public float lateBy = 0.75f;

    [Tooltip("How long it all stays quiet afterwards, in seconds.")]
    public float quietSeconds = 9f;

    int paces;
    float lastPace, quietUntil;
    bool following;

    void OnEnable()
    {
        player.headBob.Stepped += Pace;
    }

    void OnDisable()
    {
        player.headBob.Stepped -= Pace;
        StopAllCoroutines();
    }

    void Pace(float run)
    {
        lastPace = Time.time;
        if (Time.time < quietUntil || ++paces < pacesAlone) return;

        following = true;
        StartCoroutine(Step(Random.Range(0.09f, 0.17f)));
    }

    void Update()
    {
        if (!following || Time.time - lastPace < lateBy) return;

        following = false;
        paces = 0;
        quietUntil = Time.time + quietSeconds;
        StartCoroutine(Step(0f));
    }

    IEnumerator Step(float delay)
    {
        yield return new WaitForSeconds(delay);
        Transform body = player.transform;
        source.transform.position = body.position - body.forward * Random.Range(distance.x, distance.y);
        source.PlayOneShot(clips[Random.Range(0, clips.Length)]);
    }
}
