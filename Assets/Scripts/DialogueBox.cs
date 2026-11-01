using System.Collections;
using TMPro;
using UnityEngine;

// Runs a scene's script file: conversations the player reads and answers, and passing remarks.
public class DialogueBox : MonoBehaviour
{
    [Header("Script")]
    public TextAsset scriptFile;
    public string playerName = "NIKOLA";

    [Header("On Screen")]
    public CanvasGroup panel;
    public TMP_Text speakerText;
    public TMP_Text lineText;
    public TMP_Text continueText;
    public TMP_Text[] replyTexts;
    public TMP_Text hintText;
    public TMP_Text promptText;

    [Header("Colours")]
    public Color playerColor = new Color(0.72f, 0.74f, 0.7f);
    public Color speakerColor = new Color(0.86f, 0.68f, 0.3f);
    public Color replyColor = new Color(0.8f, 0.8f, 0.78f);
    public Color selectedReplyColor = new Color(1f, 0.84f, 0.45f);

    [Header("Pacing")]
    public float charactersPerSecond = 40f;
    [Tooltip("How long a reply the player picked stays up before the conversation moves on.")]
    public float replySeconds = 1.1f;
    [Tooltip("The quiet moment after the player does something rather than says it.")]
    public float actionSeconds = 0.8f;
    [Tooltip("How long a list of replies ignores the confirm key, so skipping text can't pick one by accident.")]
    public float replyGuardSeconds = 0.35f;
    [Tooltip("How long a passing remark stays on screen, plus a little for each letter.")]
    public float remarkSeconds = 1.8f;

    // Who is mid-sentence right now, or null.
    public string Speaking { get; private set; }
    public bool InConversation { get; private set; }

    ConversationScript script;
    Coroutine remark;
    int lastConfirmFrame = -1;

    void Awake()
    {
        script = new ConversationScript(scriptFile);
        panel.alpha = 0f;
        HidePrompt();
        ShowReplies(null, 0);
    }

    public void ShowPrompt(string text)
    {
        promptText.text = text;
        promptText.enabled = true;
    }

    public void HidePrompt()
    {
        promptText.enabled = false;
    }

    // Shows one line without stopping the player. Ignored during a conversation.
    public void Remark(string label)
    {
        int index = script.Find(label);
        if (InConversation || index < 0) return;

        for (; script[index].Sound != null; index++) SoundDirector.Instance.Cue(script[index].Sound);

        if (remark != null) StopCoroutine(remark);
        remark = StartCoroutine(ShowRemark(script[index]));
    }

    IEnumerator ShowRemark(ConversationScript.Step step)
    {
        continueText.enabled = false;
        SetLine(step.Speaker, step.Text);
        lineText.maxVisibleCharacters = step.Text.Length;

        yield return FadePanel(1f, 0.25f);
        yield return new WaitForSeconds(remarkSeconds + step.Text.Length * 0.03f);
        yield return FadePanel(0f, 0.5f);
        remark = null;
    }

    // Runs from a label until the script reaches the end.
    public IEnumerator Play(string label)
    {
        InConversation = true;
        if (remark != null)
        {
            StopCoroutine(remark);
            remark = null;
        }

        // The key press that opened the conversation must not also skip its first line.
        yield return null;
        SetLine("", "");
        yield return FadePanel(1f, 0.2f);

        int index = script.Find(label);
        while (index >= 0 && index < script.Count)
        {
            ConversationScript.Step step = script[index];
            if (step.Jump != null)
            {
                index = script.Find(step.Jump);
            }
            else if (step.Sound != null)
            {
                SoundDirector.Instance.Cue(step.Sound);
                index++;
            }
            else if (step.Replies != null)
            {
                int chosen = -1;
                yield return ChooseReply(step, picked => chosen = picked);
                ConversationScript.Reply reply = step.Replies[chosen];
                if (reply.IsAction) yield return Pause();
                else yield return SayReply(reply.Text);
                index = reply.Target != null ? script.Find(reply.Target) : index + 1;
            }
            else
            {
                yield return Say(step.Speaker, step.Text);
                index++;
            }
        }

        continueText.enabled = false;
        yield return FadePanel(0f, 0.3f);
        InConversation = false;
    }

    IEnumerator Say(string speaker, string text)
    {
        SetLine(speaker, text);
        Speaking = speaker;
        continueText.enabled = false;

        float shown = 0f;
        while (shown < text.Length && !TakeConfirm())
        {
            shown += charactersPerSecond * Time.deltaTime;
            lineText.maxVisibleCharacters = Mathf.FloorToInt(shown);
            yield return null;
        }
        lineText.maxVisibleCharacters = text.Length;
        Speaking = null;
        continueText.enabled = true;

        yield return null;
        while (!TakeConfirm()) yield return null;
    }

    IEnumerator SayReply(string text)
    {
        SetLine(playerName, text);
        lineText.maxVisibleCharacters = text.Length;
        continueText.enabled = false;

        float end = Time.time + replySeconds;
        yield return null;
        while (Time.time < end && !TakeConfirm()) yield return null;
    }

    IEnumerator Pause()
    {
        SetLine("", "");
        continueText.enabled = false;
        yield return new WaitForSeconds(actionSeconds);
    }

    IEnumerator ChooseReply(ConversationScript.Step step, System.Action<int> onChosen)
    {
        continueText.enabled = false;
        int count = step.Replies.Count, selected = 0;
        float guardEnd = Time.time + replyGuardSeconds;

        while (true)
        {
            if (GameMenu.HoldsInput)
            {
                yield return null;
                continue;
            }

            if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)) selected = (selected + count - 1) % count;
            if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow)) selected = (selected + 1) % count;
            ShowReplies(step, selected);

            int pressed = PressedNumber(count);
            if (pressed >= 0)
            {
                selected = pressed;
                break;
            }

            if (TakeConfirm() && Time.time >= guardEnd) break;
            yield return null;
        }

        ShowReplies(null, 0);
        onChosen(selected);
        yield return null;
    }

    static int PressedNumber(int count)
    {
        for (int i = 0; i < count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) return i;
        }
        return -1;
    }

    void ShowReplies(ConversationScript.Step step, int selected)
    {
        hintText.enabled = step != null;
        for (int i = 0; i < replyTexts.Length; i++)
        {
            bool used = step != null && i < step.Replies.Count;
            replyTexts[i].enabled = used;
            if (!used) continue;
            replyTexts[i].text = $"{i + 1}.  {step.Replies[i].Text}";
            replyTexts[i].color = i == selected ? selectedReplyColor : replyColor;
        }
    }

    void SetLine(string speaker, string text)
    {
        speakerText.text = speaker;
        speakerText.color = speaker == playerName ? playerColor : speakerColor;
        lineText.text = text;
        lineText.maxVisibleCharacters = 0;
    }

    // A key reads as pressed for the whole frame, and one step of the conversation can follow another
    // within a frame, so each press may only count once. Otherwise the press that finishes a line also
    // skips the next one's typing, or answers a list of replies before it has been seen.
    bool TakeConfirm()
    {
        if (GameMenu.HoldsInput) return false;

        bool pressed = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Space)
            || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0);
        if (!pressed || lastConfirmFrame == Time.frameCount) return false;

        lastConfirmFrame = Time.frameCount;
        return true;
    }

    IEnumerator FadePanel(float target, float seconds)
    {
        float start = panel.alpha;
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            panel.alpha = Mathf.Lerp(start, target, t / seconds);
            yield return null;
        }
        panel.alpha = target;
    }
}
