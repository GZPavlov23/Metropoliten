using UnityEngine;

// The doors down the platform side of the train, which all go together. They open ahead of everything
// else in the scene, so that playing a shift back can close them again.
[DefaultExecutionOrder(-10)]
public class TrainDoors : MonoBehaviour
{
    public SlideOpen[] leaves;

    [Tooltip("Whether the train is standing with its doors open when the scene starts.")]
    public bool startOpen;

    [Tooltip("Where along the train each doorway is, in the train's own space.")]
    public float[] doorways;

    [Tooltip("Where the outside of the doorways is, across the train.")]
    public float doorX;

    public AudioSource motors;
    public AudioClip openSound, closeSound;

    public bool IsOpen { get; private set; }

    void Start()
    {
        if (!startOpen) return;
        IsOpen = true;
        foreach (SlideOpen leaf in leaves) leaf.StartOpen();
    }

    public void Open()
    {
        if (IsOpen) return;
        IsOpen = true;
        foreach (SlideOpen leaf in leaves) leaf.Open();
        if (motors != null && !ShiftChecklist.Resuming) motors.PlayOneShot(openSound);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        foreach (SlideOpen leaf in leaves) leaf.Close();
        if (motors != null && !ShiftChecklist.Resuming) motors.PlayOneShot(closeSound);
    }
}
