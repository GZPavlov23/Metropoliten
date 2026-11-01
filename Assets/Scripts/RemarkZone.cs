using UnityEngine;

// Shows a line from the script file when the player walks into this trigger.
[RequireComponent(typeof(Collider))]
public class RemarkZone : MonoBehaviour
{
    public DialogueBox dialogue;

    [Tooltip("Label of the line in the script file.")]
    public string label;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null) dialogue.Remark(label);
    }
}
