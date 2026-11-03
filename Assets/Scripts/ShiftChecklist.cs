using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// The player's list of jobs for the shift. Jobs come in steps: a step's jobs can only be done once the
// step before it is finished, and finishing a step can open up the world. A step with no spots
// stays open until something outside the list finishes it.
//
// Every job done saves the game. Continue plays the shift back up to that point in a single frame, with
// Resuming set so that whatever would make a noise or take its time just ends up the way it finished.
public class ShiftChecklist : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        public string[] tasks;

        [Tooltip("Where each task gets done, in the same order as the tasks.")]
        public TalkSpot[] spots;

        public UnityEvent onFinished = new UnityEvent();
    }

    public static ShiftChecklist Current { get; private set; }

    // True while the shift is being played back on Continue.
    public static bool Resuming { get; private set; }

    public string title = "SHIFT 1";
    public TMP_Text listText;
    public Step[] steps;

    readonly HashSet<TalkSpot> done = new HashSet<TalkSpot>();
    FirstPersonController player;
    TrainDrive drive;
    int current;
    string hint;

    void Awake()
    {
        Current = this;
        foreach (Step step in steps)
        {
            foreach (TalkSpot spot in step.spots)
            {
                spot.enabled = false;
                spot.onFirstConversationEnded.AddListener(() => Finish(spot));
            }
        }
        OpenStep();
    }

    // Everything in the scene has woken up by now, so playing the shift back reaches all of it.
    void Start()
    {
        player = FindAnyObjectByType<FirstPersonController>();
        drive = FindAnyObjectByType<TrainDrive>();

        SaveSystem.Checkpoint checkpoint = SaveSystem.LoadCheckpoint(SceneManager.GetActiveScene().name);
        if (checkpoint != null) Resume(checkpoint);
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    void OpenStep()
    {
        foreach (TalkSpot spot in steps[current].spots) spot.enabled = true;
        Refresh();
    }

    void Finish(TalkSpot spot)
    {
        done.Add(spot);
        Refresh();

        Step step = steps[current];
        foreach (TalkSpot needed in step.spots)
        {
            if (!done.Contains(needed))
            {
                Save();
                return;
            }
        }

        FinishStep();
    }

    // A reminder that stays under the list, like which key does what.
    public void ShowHint(string text)
    {
        hint = text;
        Refresh();
    }

    public void FinishStep()
    {
        steps[current].onFinished.Invoke();
        current++;
        if (current < steps.Length) OpenStep();
        Save();
    }

    // Keeps the player's place, so that leaving the game costs them nothing but the walk they were on.
    public void Save()
    {
        if (Resuming) return;

        // The end of the shift has nothing left to come back to.
        if (current >= steps.Length)
        {
            SaveSystem.ClearCheckpoint();
            return;
        }

        var checkpoint = new SaveSystem.Checkpoint
        {
            scene = SceneManager.GetActiveScene().name,
            step = current,
            seated = player.Seated,
            position = player.transform.position,
            yaw = player.transform.eulerAngles.y,
            pitch = player.Pitch,
        };

        TalkSpot[] spots = steps[current].spots;
        for (int i = 0; i < spots.Length; i++)
            if (done.Contains(spots[i])) checkpoint.done |= 1 << i;

        if (drive != null)
        {
            checkpoint.trainAt = drive.RestingAt;
            checkpoint.driving = drive.Driving;
        }
        SaveSystem.SaveCheckpoint(checkpoint);
    }

    void Resume(SaveSystem.Checkpoint checkpoint)
    {
        Resuming = true;
        while (current < checkpoint.step)
        {
            TalkSpot[] spots = steps[current].spots;
            if (spots.Length == 0) FinishStep();
            else foreach (TalkSpot spot in spots) spot.Replay();
        }

        TalkSpot[] open = steps[current].spots;
        for (int i = 0; i < open.Length; i++)
            if ((checkpoint.done & (1 << i)) != 0) open[i].Replay();

        if (drive != null) drive.Park(checkpoint.trainAt, checkpoint.driving);
        if (checkpoint.seated) player.SettleNow();
        else player.Place(checkpoint.position, checkpoint.yaw, checkpoint.pitch);
        Resuming = false;
    }

    void Refresh()
    {
        var list = new StringBuilder(title);
        list.Append('\n');

        Step step = steps[Mathf.Min(current, steps.Length - 1)];
        for (int i = 0; i < step.tasks.Length; i++)
        {
            list.Append('\n');
            bool ticked = i < step.spots.Length && done.Contains(step.spots[i]);
            list.Append(ticked ? $"<color=#8a8a84><s>{step.tasks[i]}</s></color>" : step.tasks[i]);
        }
        if (hint != null) list.Append($"\n\n<color=#8a8a84>{hint}</color>");
        listText.text = list.ToString();
    }
}
