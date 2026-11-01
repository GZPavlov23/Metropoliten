using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Driving the train: one straight run of track, W for power and S for the brake, with the dispatcher on the
// radio and a trip past the stopping board that brings the train to a halt if the driver doesn't.
public class TrainDrive : MonoBehaviour
{
    public const float RailLength = 18f;
    const float MetresPerSecondToKmh = 3.6f, LeverRate = 3f, NeedleSweep = 2.3f * Mathf.Rad2Deg, LeverSweep = 30f;

    public FirstPersonController player;
    public DialogueBox dialogue;

    [Tooltip("Where the player sits. Moves with the train.")]
    public Transform seat;

    public Light headlight;
    public Transform speedNeedle;
    public Transform powerLever;

    [Header("Train")]
    [Tooltip("Acceleration from rest at full power, in metres per second per second.")]
    public float power = 1.1f;
    public float braking = 1.4f;
    [Tooltip("How quickly the train slows by itself with the lever in the middle.")]
    public float drag = 0.05f;
    [Tooltip("In metres per second. Full power fades away as the train gets near it.")]
    public float topSpeed = 16f;

    [Header("Cab")]
    [Tooltip("The speed at the far end of the speedometer, in km/h.")]
    public float gaugeFullScale = 80f;
    public string keysPrompt = "W   Power        S   Brake";

    [Header("The Run")]
    [Tooltip("How far the train travels to bring its nose level with the board at the next station, in metres.")]
    public float stopAt = 585f;
    [Tooltip("How far short of the board still counts as stopping at it.")]
    public float shortTolerance = 6f;
    [Tooltip("How far past the board still counts as stopping at it.")]
    public float overTolerance = 3f;
    [Tooltip("How far past the board the trip takes over and stops the train.")]
    public float tripAfter = 8f;
    public float tripBraking = 3.5f;
    [Tooltip("The speed the dispatcher won't stand for, in km/h.")]
    public float speedLimit = 50f;
    [Tooltip("How far before the board the dispatcher calls the station.")]
    public float callStationAt = 200f;

    public UnityEvent onRunFinished = new UnityEvent();

    public float Speed { get; private set; }
    public float Travelled { get; private set; }
    public bool Driving { get; private set; }

    // Where the lever is: 1 is full power and -1 is full brake.
    public float Lever => lever;
    public bool Tripped => tripped;

    Vector3 startPosition;
    Quaternion needleRest, leverRest;
    float lever;
    bool rolling, tripped, calledMoving, calledSpeed, calledStation;

    void Awake()
    {
        startPosition = transform.position;
        needleRest = speedNeedle.localRotation;
        leverRest = powerLever.localRotation;
        SetCab();
    }

    public void TakeSeat()
    {
        player.Sit(seat);
    }

    public void BeginRun()
    {
        Driving = true;
        headlight.enabled = true;
        dialogue.ShowPrompt(keysPrompt);
    }

    void Update()
    {
        if (!Driving) return;

        bool forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool back = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        float wanted = tripped ? -1f : (forward ? 1f : 0f) - (back ? 1f : 0f);
        lever = Mathf.MoveTowards(lever, wanted, LeverRate * Time.deltaTime);

        // Motors fade with speed; brakes don't.
        float pull = lever > 0f ? lever * power * (1f - Speed / topSpeed) : lever * (tripped ? tripBraking : braking);
        Speed = Mathf.Max(Speed + (pull - drag) * Time.deltaTime, 0f);
        Travelled += Speed * Time.deltaTime;
        transform.position = startPosition + Vector3.forward * Travelled;

        SetCab();
        ShakeCab();
        CallTheRun();
        CheckForStop();
    }

    void SetCab()
    {
        float kmh = Speed * MetresPerSecondToKmh;
        float reading = Mathf.Lerp(-NeedleSweep, NeedleSweep, Mathf.Clamp01(kmh / gaugeFullScale));
        speedNeedle.localRotation = needleRest * Quaternion.Euler(0f, 0f, -reading);
        powerLever.localRotation = leverRest * Quaternion.Euler(lever * LeverSweep, 0f, 0f);
    }

    // The rattle of the cab, with a knock over each rail joint.
    void ShakeCab()
    {
        float time = Time.time;
        float joint = Mathf.Exp(-Mathf.Repeat(Travelled, RailLength) * 1.5f);
        float side = (Mathf.PerlinNoise(time * 7f, 1.3f) - 0.5f) * 0.012f;
        float lift = (Mathf.PerlinNoise(2.1f, time * 11f) - 0.5f) * 0.01f - joint * 0.006f;
        player.Shake = new Vector3(side, lift, 0f) * (Speed / topSpeed);
    }

    void CallTheRun()
    {
        if (Speed > 2f) dialogue.HidePrompt();

        if (!calledMoving && Speed > 3f)
        {
            calledMoving = true;
            dialogue.Remark("drive_moving");
        }

        if (!calledSpeed && Speed * MetresPerSecondToKmh > speedLimit)
        {
            calledSpeed = true;
            dialogue.Remark("drive_fast");
        }

        if (!calledStation && Travelled > stopAt - callStationAt)
        {
            calledStation = true;
            dialogue.Remark("drive_station");
        }
    }

    void CheckForStop()
    {
        float past = Travelled - stopAt;
        if (past > tripAfter) tripped = true;

        if (Speed > 0f)
        {
            rolling = true;
            return;
        }
        if (!rolling) return;
        rolling = false;

        if (past < -shortTolerance)
        {
            dialogue.Remark("drive_short");
            dialogue.ShowPrompt(keysPrompt);
            return;
        }

        StartCoroutine(FinishRun(past > overTolerance ? "drive_overran" : "drive_stopped"));
    }

    IEnumerator FinishRun(string label)
    {
        Driving = false;
        lever = 0f;
        SetCab();
        player.Shake = Vector3.zero;
        dialogue.HidePrompt();

        yield return new WaitForSeconds(1f);
        yield return dialogue.Play(label);
        onRunFinished.Invoke();
    }
}
