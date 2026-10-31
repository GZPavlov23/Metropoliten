using System.Collections;
using UnityEngine;

// Shows a line from the script file when the player walks into this trigger.
[RequireComponent(typeof(Collider))]
public class RemarkZone : MonoBehaviour
{
    public DialogueBox dialogue;

    [Tooltip("Label of the line in the script file.")]
    public string label;

    public float delay;
    public bool onlyOnce = true;

    bool said;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() == null) return;
        if (said && onlyOnce) return;
        said = true;
        StartCoroutine(Say());
    }

    IEnumerator Say()
    {
        yield return new WaitForSeconds(delay);
        dialogue.Remark(label);
    }
}
