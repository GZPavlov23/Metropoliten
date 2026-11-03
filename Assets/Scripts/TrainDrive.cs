using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Driving the train: straight runs of track, W for power and S for the brake, with the dispatcher on the
// radio and a trip past the stopping board that brings the train to a halt if the driver doesn't.
public class TrainDrive : MonoBehaviour
{
    // One stretch of driving, from wherever the train is to a board further along the line.
    [Serializable]
    public class Run
    {
        [Tooltip("How far the train travels to bring its nose level with the board, in metres, counted from where the shift started.")]
        public float stopAt = 585f;

        [Tooltip("What this run's lines are called in the script: drive_moving, drive_stopped and so on.")]
        public string labels = "drive";

        [Tooltip("A signal at danger: the train stops itself in time, instead of a long way past.")]
        public bool stopsAtBoard;

        [Tooltip("What's said on the way past, as distances along the line and labels in the script.")]
        public Call[] calls = new Call[0];

        [Tooltip("What the music turns to as the train comes in.")]
        public string arrivalBed = "M_Dread";
        public float arrivalBedLevel = 0.5f;
    }

    [Serializable]
    public class Call
    {
        public float at;
        public string label;
    }

    public const float RailLength = 18f;
    const float MetresPerSecondToKmh = 3.6f, LeverRate = 3f, LeverDelay = 0.4f, NeedleSweep = 2.3f * Mathf.Rad2Deg, LeverSweep = 30f;

    public FirstPersonController player;
    public DialogueBox dialogue;

    [Tooltip("Where the player sits. Moves with the train.")]
    public Transform seat;

    [Tooltip("Where the player ends up after getting out of the seat. Moves with the train.")]
    public Transform standingSpot;

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

    [Header("The Runs")]
    public Run[] runs = { new Run() };
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

    // Where the train stands between runs. A run that's cut short starts again from here.
    public float RestingAt => Driving ? runStart : Travelled;

    // Which run this is, counting from 1. It's 0 before the first has started.
    public int RunNumber { get; private set; }
    public Run CurrentRun => runs[Mathf.Clamp(RunNumber - 1, 0, runs.Length - 1)];
    public float StopAt => CurrentRun.stopAt;

    // Where the lever is: 1 is full power and -1 is full brake.
    public float Lever => lever;
    public bool Tripped => tripped;

    Vector3 startPosition;
    Quaternion needleRest, leverRest;
    float lever, runStart;
    bool rolling, tripped, calledMoving, calledSpeed, calledStation;
    int nextCall;

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

    public void LeaveSeat()
    {
        player.Stand(standingSpot);
    }

    public void BeginRun()
    {
        RunNumber++;
        runStart = Travelled;
        rolling = tripped = calledMoving = calledSpeed = calledStation = false;
        nextCall = 0;

        Driving = true;
        headlight.enabled = true;
        dialogue.ShowPrompt(keysPrompt);
    }

    // Stands the train where it was when the game was saved. Between runs the arrival's music carries on.
    public void Park(float distance, bool stillDriving)
    {
        Travelled = runStart = distance;
        transform.position = startPosition + Vector3.forward * distance;
        Driving = stillDriving;
        SetCab();

        if (stillDriving) return;
        dialogue.HidePrompt();
        if (RunNumber > 0) SoundDirector.Instance.PlayBed(CurrentRun.arrivalBed, CurrentRun.arrivalBedLevel, 1f);
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

        string labels = CurrentRun.labels;
        if (!calledMoving && Speed > 3f)
        {
            calledMoving = true;
            dialogue.Remark($"{labels}_moving");
        }

        if (!calledSpeed && Speed * MetresPerSecondToKmh > speedLimit)
        {
            calledSpeed = true;
            dialogue.Remark($"{labels}_fast");
        }

        Call[] calls = CurrentRun.calls;
        if (nextCall < calls.Length && Travelled > calls[nextCall].at) dialogue.Remark(calls[nextCall++].label);

        if (!calledStation && Travelled > StopAt - callStationAt)
        {
            calledStation = true;
            dialogue.Remark($"{labels}_station");
        }
    }

    void CheckForStop()
    {
        float past = Travelled - StopAt;
        if (past > tripAfter) tripped = true;

        // A signal at danger takes the train out of the driver's hands as late as it safely can.
        if (CurrentRun.stopsAtBoard && Speed * Speed / (2f * tripBraking) + Speed * LeverDelay >= overTolerance - 1f - past) tripped = true;

        if (Speed > 0f)
        {
            rolling = true;
            return;
        }
        if (!rolling) return;
        rolling = false;

        if (past < -shortTolerance)
        {
            dialogue.Remark($"{CurrentRun.labels}_short");
            dialogue.ShowPrompt(keysPrompt);
            return;
        }

        StartCoroutine(FinishRun($"{CurrentRun.labels}_{(past > overTolerance ? "overran" : "stopped")}"));
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
