using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The score, and how much of the place is getting through. It lasts the whole game, so the music carries
// on across scene changes and the main menu needs nothing set up in it.
public class SoundDirector : MonoBehaviour
{
    const string Folder = "Sound/";
    const float BedFadeSeconds = 5f, SceneRiseSeconds = 2.5f;

    // What's left of the place, and of the music, while it holds its breath.
    const float HushedPresence = 0.2f, HushedBed = 0.4f;

    // What plays under each scene, and how loudly.
    static readonly Dictionary<string, (string clip, float level)> SceneBeds = new Dictionary<string, (string, float)>
    {
        { "MainMenu", ("M_Dread", 0.75f) },
        { "Opening", ("M_Dread", 0.25f) },
        { "Shift1", ("M_Dread", 0.5f) },
    };

    // A value on its way to another value.
    struct Glide
    {
        public float Value;
        float target, rate;

        public void To(float wanted, float seconds)
        {
            target = wanted;
            rate = Mathf.Abs(wanted - Value) / Mathf.Max(seconds, 0.01f);
        }

        public void Step(float deltaTime)
        {
            Value = Mathf.MoveTowards(Value, target, rate * deltaTime);
        }
    }

    public static SoundDirector Instance { get; private set; }

    // How much of the scene's own sound is getting through, from 0 to 1. Everything that belongs to the place follows it.
    public static float Presence { get; private set; } = 1f;

    AudioSource bed, oldBed, cues;
    Glide bedLevel, oldBedLevel, presence, duck;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        // A run with no window has nobody listening, and shouldn't startle whoever's at the machine.
        if (Application.isBatchMode) AudioListener.pause = true;

        var go = new GameObject("SoundDirector");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<SoundDirector>();
    }

    public static AudioClip Load(string name)
    {
        var clip = Resources.Load<AudioClip>(Folder + name);
        if (clip == null) Debug.LogError($"There's no sound called \"{name}\" in Resources/{Folder}.");
        return clip;
    }

    void Awake()
    {
        bed = NewSource(true);
        oldBed = NewSource(true);
        cues = NewSource(false);
        presence.Value = 1f;
        duck.Value = 1f;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    AudioSource NewSource(bool music)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.priority = 0;
        source.loop = music;
        source.bypassReverbZones = music;
        return source;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopAllCoroutines();
        presence.Value = 0f;
        presence.To(1f, SceneRiseSeconds);
        duck.To(1f, SceneRiseSeconds);
        if (SceneBeds.TryGetValue(scene.name, out (string clip, float level) score)) PlayBed(score.clip, score.level, BedFadeSeconds);
    }

    void Update()
    {
        bedLevel.Step(Time.deltaTime);
        oldBedLevel.Step(Time.deltaTime);
        presence.Step(Time.deltaTime);
        duck.Step(Time.deltaTime);

        Presence = presence.Value;
        bed.volume = bedLevel.Value * duck.Value;
        oldBed.volume = oldBedLevel.Value * duck.Value;
        if (oldBed.isPlaying && oldBedLevel.Value <= 0f) oldBed.Stop();
    }

    // Crosses to another bed, or glides to a new level if that one is already playing.
    public void PlayBed(string clip, float level, float seconds)
    {
        AudioClip next = Load(clip);
        if (bed.clip != next)
        {
            (bed, oldBed) = (oldBed, bed);
            (bedLevel, oldBedLevel) = (oldBedLevel, bedLevel);
            oldBedLevel.To(0f, seconds);
            if (bed.clip != next)
            {
                bed.clip = next;
                bed.Play();
            }
        }
        bedLevel.To(level, seconds);
    }

    // Plays a sound by name, close to the player's ear rather than anywhere in the room.
    public void Cue(string clip)
    {
        cues.PlayOneShot(Load(clip));
    }

    // Everything sinks for a while and then comes back, as if the place had stopped to listen.
    public void Hush(float sinkSeconds, float holdSeconds, float returnSeconds)
    {
        StopAllCoroutines();
        StartCoroutine(HoldBreath(sinkSeconds, holdSeconds, returnSeconds));
    }

    IEnumerator HoldBreath(float sinkSeconds, float holdSeconds, float returnSeconds)
    {
        presence.To(HushedPresence, sinkSeconds);
        duck.To(HushedBed, sinkSeconds);
        yield return new WaitForSeconds(sinkSeconds + holdSeconds);
        presence.To(1f, returnSeconds);
        duck.To(1f, returnSeconds);
    }

    // Lets the scene's own sound go, ahead of leaving it.
    public void SinkScene(float seconds)
    {
        StopAllCoroutines();
        presence.To(0f, seconds);
    }
}
