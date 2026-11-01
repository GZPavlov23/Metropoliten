using UnityEngine;

// What the train sounds like, worked out from how it's being driven, and the music that goes with the run.
public class TrainSound : MonoBehaviour
{
    // How far back along the train each axle is, in metres. Every joint in the rail goes under all of them in
    // turn: a pair of knocks close together from the bogie under the cab, then a quieter pair from the next one.
    static readonly float[] Axles = { 0f, 2.4f, 12.6f, 15f };
    const float NearBogieLength = 5f, FarBogieLevel = 0.45f;

    public TrainDrive drive;
    public AudioSource idle, motor, wheels, brake, effects;
    public AudioClip[] joints;
    public AudioClip airRelease, tripBell;

    [Tooltip("The sounds of the station, which stop mattering once the train is moving.")]
    public SoundScatter stationSounds;

    [Header("Levels")]
    [Range(0f, 1f)] public float idleLevel = 0.22f;
    [Range(0f, 1f)] public float motorLevel = 0.42f;
    [Range(0f, 1f)] public float wheelsLevel = 0.85f;
    [Range(0f, 1f)] public float brakeLevel = 0.3f;
    [Range(0f, 1f)] public float jointLevel = 0.8f;

    [Header("Music")]
    public string runBed = "M_Deep";
    public float runBedLevel = 0.7f;
    public float runBedFadeSeconds = 9f;
    public string arrivalBed = "M_Dread";
    public float arrivalBedLevel = 0.5f;
    public float arrivalBedFadeSeconds = 14f;

    int[] jointsPassed;
    bool underway, arriving, moving, tripped;

    void Start()
    {
        jointsPassed = new int[Axles.Length];
        for (int i = 0; i < Axles.Length; i++) jointsPassed[i] = JointsPassed(Axles[i]);
    }

    int JointsPassed(float axle)
    {
        return Mathf.FloorToInt((drive.Travelled - axle) / TrainDrive.RailLength);
    }

    void Update()
    {
        float pace = drive.Speed / drive.topSpeed;
        float power = Mathf.Max(drive.Lever, 0f), braking = Mathf.Max(-drive.Lever, 0f);
        float presence = SoundDirector.Presence;

        idle.volume = idleLevel * presence;

        motor.pitch = Mathf.Lerp(0.4f, 1.5f, pace);
        motor.volume = motorLevel * (power * Mathf.Lerp(0.6f, 1f, pace) + pace * 0.25f) * presence;

        wheels.pitch = Mathf.Lerp(0.7f, 1.2f, pace);
        wheels.volume = wheelsLevel * Mathf.Sqrt(pace) * presence;

        // Brakes sing loudest as the train comes down to a crawl, and stop when it does.
        brake.volume = brakeLevel * braking * Mathf.Clamp01(drive.Speed / 1.5f) * Mathf.Lerp(1f, 0.4f, pace) * presence;

        KnockOverJoints(pace);
        Mark(pace);
    }

    void KnockOverJoints(float pace)
    {
        for (int i = 0; i < Axles.Length; i++)
        {
            int passed = JointsPassed(Axles[i]);
            if (passed == jointsPassed[i]) continue;
            jointsPassed[i] = passed;

            float level = jointLevel * Mathf.Lerp(0.2f, 1f, pace) * (Axles[i] < NearBogieLength ? 1f : FarBogieLevel);
            effects.PlayOneShot(joints[Random.Range(0, joints.Length)], level * SoundDirector.Presence);
        }
    }

    void Mark(float pace)
    {
        if (!underway && pace > 0.04f)
        {
            underway = true;
            stationSounds.enabled = false;
            SoundDirector.Instance.PlayBed(runBed, runBedLevel, runBedFadeSeconds);
        }

        if (!arriving && drive.Travelled > drive.stopAt - drive.callStationAt)
        {
            arriving = true;
            SoundDirector.Instance.PlayBed(arrivalBed, arrivalBedLevel, arrivalBedFadeSeconds);
        }

        if (drive.Tripped && !tripped)
        {
            tripped = true;
            effects.PlayOneShot(tripBell);
        }

        // Stopping lets the air out of the brakes.
        if (moving && drive.Speed <= 0f) effects.PlayOneShot(airRelease);
        moving = drive.Speed > 0f;
    }
}
