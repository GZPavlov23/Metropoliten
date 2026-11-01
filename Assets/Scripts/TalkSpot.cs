using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Somewhere the player can start a conversation: stand close, look at the person, press E.
public class TalkSpot : MonoBehaviour
{
    public DialogueBox dialogue;
    public FirstPersonController player;

    [Tooltip("Who the player looks at to talk, and what the view turns to while talking.")]
    public Transform person;

    [Tooltip("Label in the script file for the first conversation.")]
    public string firstLabel = "clerk";

    [Tooltip("Label in the script file for talking to them again afterwards.")]
    public string laterLabel = "again";

    public string prompt = "E   Talk";
    public float reach = 1.9f;

    [Range(0f, 1f)]
    [Tooltip("How directly the player has to look at the person. 1 is dead centre.")]
    public float lookAccuracy = 0.86f;

    [Tooltip("Runs the first time the player presses the key, before anything is said.")]
    public UnityEvent onTalkStarted = new UnityEvent();

    public UnityEvent onFirstConversationEnded = new UnityEvent();

    bool talking;
    bool talkedBefore;
    bool promptShown;

    void Update()
    {
        if (talking) return;

        // Several spots share one prompt, so each only clears what it put up itself.
        bool canTalk = !dialogue.InConversation && PlayerCanTalk();
        if (canTalk) dialogue.ShowPrompt(prompt);
        else if (promptShown) dialogue.HidePrompt();
        promptShown = canTalk;

        if (canTalk && Input.GetKeyDown(KeyCode.E)) StartCoroutine(Talk());
    }

    bool PlayerCanTalk()
    {
        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0f;
        if (offset.magnitude > reach) return false;

        Transform view = player.cameraTransform;
        return Vector3.Dot(view.forward, (person.position - view.position).normalized) > lookAccuracy;
    }

    IEnumerator Talk()
    {
        talking = true;
        promptShown = false;
        dialogue.HidePrompt();
        player.HoldAttention(person);
        if (!talkedBefore) onTalkStarted.Invoke();

        yield return dialogue.Play(talkedBefore ? laterLabel : firstLabel);

        player.ReleaseAttention();
        talking = false;
        if (talkedBefore) yield break;

        talkedBefore = true;
        onFirstConversationEnded.Invoke();
    }
}
