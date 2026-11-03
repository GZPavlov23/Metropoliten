using UnityEngine;

// The lamp from the locker, held out in front of the player. It trails the view a little, the way a hand does.
public class HandLamp : MonoBehaviour
{
    const float FollowRate = 11f;
    static readonly Vector3 HeldAt = new Vector3(0.18f, -0.22f, 0.2f);

    public Light beam;

    [Tooltip("The player's head.")]
    public Transform view;

    public bool Carried { get; private set; }

    public void Carry()
    {
        Carried = true;
        if (!ShiftChecklist.Resuming) Switch(true);
    }

    // Hands that are driving aren't holding a lamp.
    public void PutAway()
    {
        if (beam.enabled) Switch(false);
    }

    // The lamp doing what it likes, with no hand on the switch.
    public void Force(bool on)
    {
        beam.enabled = on;
    }

    void Update()
    {
        if (Carried && !GameMenu.HoldsInput && Input.GetKeyDown(KeyCode.F)) Switch(!beam.enabled);
    }

    void Switch(bool on)
    {
        beam.enabled = on;
        SoundDirector.Instance.Cue("I_Lamp");
    }

    void LateUpdate()
    {
        beam.transform.position = view.TransformPoint(HeldAt);
        beam.transform.rotation = Quaternion.Slerp(beam.transform.rotation, view.rotation, 1f - Mathf.Exp(-FollowRate * Time.deltaTime));
    }
}
