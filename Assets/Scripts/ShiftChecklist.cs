using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

// The player's list of jobs for the shift. Jobs come in steps: a step's jobs can only be done once the
// step before it is finished, and finishing a step can open up the world. A step with no spots
// stays open until something outside the list finishes it.
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

    public string title = "SHIFT 1";
    public TMP_Text listText;
    public Step[] steps;

    readonly HashSet<TalkSpot> done = new HashSet<TalkSpot>();
    int current;

    void Awake()
    {
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
            if (!done.Contains(needed)) return;
        }

        FinishStep();
    }

    public void FinishStep()
    {
        steps[current].onFinished.Invoke();
        current++;
        if (current < steps.Length) OpenStep();
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
        listText.text = list.ToString();
    }
}
