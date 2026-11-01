using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

// The small sounds a place makes by itself: drips, ticks and groans, from somewhere around the player, now and then.
public class SoundScatter : MonoBehaviour
{
    const int Voices = 6;
    const float FarDistance = 70f, HushSinkSeconds = 3f, HushReturnSeconds = 7f;

    [Serializable]
    public class Kind
    {
        public string name;
        public AudioClip[] clips;

        [Tooltip("Shortest and longest wait between sounds, in seconds.")]
        public Vector2 everySeconds = new Vector2(8f, 20f);

        [Tooltip("Nearest and furthest from the player they come from, in metres.")]
        public Vector2 distance = new Vector2(6f, 18f);

        [Tooltip("Quietest and loudest.")]
        public Vector2 volume = new Vector2(0.2f, 0.4f);

        [Tooltip("How close the player has to be to hear them at full strength, in metres. Big sounds carry further.")]
        public float reach = 3f;

        [Range(0f, 1f)]
        [Tooltip("How strongly they favour the direction the player isn't looking.")]
        public float behind;

        [Tooltip("Comes from here every time, instead of from around the player.")]
        public Transform from;

        [Tooltip("How long the whole place holds its breath around this sound, in seconds. 0 for not at all.")]
        public float hushSeconds;
    }

    [Tooltip("Who the sounds gather around.")]
    public Transform listener;

    public Kind[] kinds;

    readonly AudioSource[] voices = new AudioSource[Voices];
    int nextVoice;

    void Awake()
    {
        for (int i = 0; i < Voices; i++)
        {
            var go = new GameObject($"Voice{i}");
            go.transform.SetParent(transform, false);
            AudioSource voice = go.AddComponent<AudioSource>();
            voice.playOnAwake = false;
            voice.spatialBlend = 1f;
            voice.dopplerLevel = 0f;
            voice.rolloffMode = AudioRolloffMode.Logarithmic;
            voice.maxDistance = FarDistance;
            voices[i] = voice;
        }
    }

    void OnEnable()
    {
        foreach (Kind kind in kinds) StartCoroutine(Run(kind));
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }

    IEnumerator Run(Kind kind)
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(kind.everySeconds.x, kind.everySeconds.y));

            if (kind.hushSeconds > 0f)
            {
                SoundDirector.Instance.Hush(HushSinkSeconds, kind.hushSeconds, HushReturnSeconds);
                yield return new WaitForSeconds(HushSinkSeconds + kind.hushSeconds * 0.4f);
            }
            Play(kind);
        }
    }

    void Play(Kind kind)
    {
        AudioSource voice = voices[nextVoice];
        nextVoice = (nextVoice + 1) % Voices;
        voice.transform.position = kind.from != null ? kind.from.position : Around(kind);
        voice.minDistance = kind.reach;
        voice.PlayOneShot(kind.clips[Random.Range(0, kind.clips.Length)], Random.Range(kind.volume.x, kind.volume.y));
    }

    // Somewhere on a circle around the player, narrowing towards their back.
    Vector3 Around(Kind kind)
    {
        float spread = Mathf.Lerp(180f, 50f, kind.behind);
        float heading = listener.eulerAngles.y + 180f + Random.Range(-spread, spread);
        return listener.position + Quaternion.Euler(0f, heading, 0f) * Vector3.forward * Random.Range(kind.distance.x, kind.distance.y);
    }
}
