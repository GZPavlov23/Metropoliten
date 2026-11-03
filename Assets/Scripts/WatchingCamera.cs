using UnityEngine;

// A camera on a bracket that keeps the player in frame, and only ever moves while they aren't looking at it.
// Turn your back and there's a small motor running behind you.
public class WatchingCamera : MonoBehaviour
{
    const float TurnRate = 18f, SeenWithin = 56f, Reach = 36f, MotorRate = 2.5f, Settled = 1f;

    [Tooltip("The player's head.")]
    public Transform view;

    public AudioSource motor;
    public float motorLevel = 0.5f;

    void Update()
    {
        Vector3 toPlayer = view.position - transform.position;
        Quaternion wanted = Quaternion.LookRotation(toPlayer);

        bool seen = Vector3.Angle(view.forward, -toPlayer) < SeenWithin;
        bool turning = !seen && toPlayer.magnitude < Reach && Quaternion.Angle(transform.rotation, wanted) > Settled;
        if (turning) transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, TurnRate * Time.deltaTime);

        motor.volume = Mathf.MoveTowards(motor.volume, turning ? motorLevel * SoundDirector.Presence : 0f, MotorRate * Time.deltaTime);
    }
}
