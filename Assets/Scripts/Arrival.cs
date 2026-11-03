using UnityEngine;
using UnityEngine.Events;

// A place the player only has to reach. The first time they walk into it, it says so, and that's that.
[RequireComponent(typeof(Collider))]
public class Arrival : MonoBehaviour
{
    public UnityEvent onArrived = new UnityEvent();

    bool arrived;

    void OnTriggerEnter(Collider other)
    {
        if (arrived || other.GetComponent<CharacterController>() == null) return;
        arrived = true;
        onArrived.Invoke();
    }
}
