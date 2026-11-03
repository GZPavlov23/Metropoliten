using System;
using System.Collections;
using UnityEngine;

// Someone travelling on the train. They wait where they've been put until the Crowd gives them their cue, then walk
// along the platform, through their doorway and down the car to their seat, or back out again and away.
public class Passenger : MonoBehaviour
{
    enum Body { Standing, Walking, Sitting }

    [Serializable]
    public class Trip
    {
        [Tooltip("What sets this trip off.")]
        public string cue;

        [Tooltip("How long they take to react to it.")]
        public float delay;

        [Tooltip("Up out of the seat and off the train first.")]
        public bool alights;

        [Tooltip("Places on the ground to walk through.")]
        public Vector3[] path = new Vector3[0];

        [Tooltip("Then on to the train and into the seat.")]
        public bool boards;

        [Tooltip("At the end of the path they're gone.")]
        public bool leaves;
    }

    const float TurnRate = 300f, SitSeconds = 0.8f, FirstStep = 0.25f;
    static readonly int PoseParameter = Animator.StringToHash("Pose");

    public Trip[] trips = new Trip[0];
    public Transform train;
    public TrainDoors doors;

    [Tooltip("In metres per second.")]
    public float speed = 1.25f;
    public float stepSeconds = 0.53f;

    [Tooltip("Which doorway they use.")]
    public int doorway;

    [Tooltip("Where they stand in the aisle by their seat, in the train's space.")]
    public Vector3 aisleSpot;

    [Tooltip("How they sit, in the train's space.")]
    public Vector3 seatPosition;
    public Quaternion seatRotation = Quaternion.identity;

    [Tooltip("How high the body stands off the ground.")]
    public float standHeight;

    [Tooltip("Where they stand instead, if the shift moves them while nobody's looking.")]
    public Vector3 windowPosition;
    public Quaternion windowRotation = Quaternion.identity;

    [Tooltip("Where to take their light from while they're on the train.")]
    public Transform aboardLight;

    public AudioSource feet;
    public AudioClip[] steps;

    public bool Busy { get; private set; }

    Animator animator;
    RestHandsOnThighs hands;
    HeadTurn look;
    SkinnedMeshRenderer skin;
    float nextStep;

    void Awake()
    {
        animator = GetComponent<Animator>();
        skin = GetComponentInChildren<SkinnedMeshRenderer>();
        hands = GetComponent<RestHandsOnThighs>();
        look = GetComponent<HeadTurn>();
        hands.enabled = false;
    }

    public void Stare(bool staring)
    {
        look.weight = staring ? 1f : 0f;
    }

    public void Cue(string cue)
    {
        if (!gameObject.activeInHierarchy) return;
        foreach (Trip trip in trips)
        {
            if (trip.cue != cue) continue;
            if (ShiftChecklist.Resuming) Finish(trip);
            else StartCoroutine(Go(trip));
        }
    }

    // Stands them in the car without anyone seeing them get there.
    public void GoToWindow()
    {
        Board();
        transform.SetLocalPositionAndRotation(windowPosition, windowRotation);
        hands.enabled = false;
        Set(Body.Standing);
    }

    void Board()
    {
        transform.SetParent(train, true);
        skin.probeAnchor = aboardLight;
    }

    void GetOff()
    {
        transform.SetParent(null, true);
        skin.probeAnchor = null;
    }

    void Set(Body body)
    {
        animator.SetInteger(PoseParameter, (int)body);
    }

    // Where the trip would have left them.
    void Finish(Trip trip)
    {
        if (trip.leaves)
        {
            gameObject.SetActive(false);
            return;
        }

        if (trip.boards)
        {
            Board();
            transform.SetLocalPositionAndRotation(seatPosition, seatRotation);
            hands.enabled = true;
            Set(Body.Sitting);
            return;
        }

        GetOff();
        hands.enabled = false;
        Set(Body.Standing);
        if (trip.path.Length > 0) transform.position = OnGround(trip.path[trip.path.Length - 1]);
    }

    Vector3 OnGround(Vector3 point)
    {
        point.y += standHeight;
        return point;
    }

    Vector3 DoorStep(float across)
    {
        return train.TransformPoint(new Vector3(doors.doorX + across, 0f, doors.doorways[doorway]));
    }

    IEnumerator Go(Trip trip)
    {
        Busy = true;
        yield return new WaitForSeconds(trip.delay);

        if (trip.alights)
        {
            yield return GetUp();
            yield return WalkTo(train.TransformPoint(new Vector3(train.InverseTransformPoint(transform.position).x, 0f, doors.doorways[doorway])));
            yield return WalkTo(DoorStep(0f));
            yield return WalkTo(DoorStep(0.5f));
            GetOff();
        }

        foreach (Vector3 point in trip.path) yield return WalkTo(point);

        if (trip.boards)
        {
            yield return WalkTo(DoorStep(0.5f));
            Board();
            yield return WalkTo(DoorStep(-0.5f));
            yield return WalkTo(train.TransformPoint(new Vector3(aisleSpot.x, 0f, doors.doorways[doorway])));
            yield return WalkTo(train.TransformPoint(aisleSpot));
            yield return SitDown();
        }
        else
        {
            Set(Body.Standing);
        }

        if (trip.leaves) gameObject.SetActive(false);
        Busy = false;
    }

    IEnumerator WalkTo(Vector3 target)
    {
        Set(Body.Walking);
        target = OnGround(target);

        while (true)
        {
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.05f) break;

            Quaternion wanted = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, wanted, TurnRate * Time.deltaTime);
            float pace = speed * Mathf.Clamp01(1f - Quaternion.Angle(transform.rotation, wanted) / 120f);
            Vector3 next = transform.position + to.normalized * Mathf.Min(pace * Time.deltaTime, to.magnitude);
            next.y = Mathf.MoveTowards(next.y, target.y, Time.deltaTime);
            transform.position = next;

            if (Time.time >= nextStep)
            {
                nextStep = Time.time + stepSeconds;
                if (feet != null && steps.Length > 0) feet.PlayOneShot(steps[UnityEngine.Random.Range(0, steps.Length)], SoundDirector.Presence);
            }
            yield return null;
        }
    }

    IEnumerator SitDown()
    {
        Set(Body.Standing);
        Quaternion start = transform.localRotation;
        for (float t = 0f; t < FirstStep; t += Time.deltaTime)
        {
            transform.localRotation = Quaternion.Slerp(start, seatRotation, t / FirstStep);
            yield return null;
        }

        Set(Body.Sitting);
        hands.enabled = true;
        yield return Settle(transform.localPosition, seatPosition, seatRotation);
    }

    IEnumerator GetUp()
    {
        hands.enabled = false;
        Set(Body.Standing);
        yield return Settle(transform.localPosition, aisleSpot + Vector3.up * standHeight, transform.localRotation);
    }

    IEnumerator Settle(Vector3 from, Vector3 to, Quaternion rotation)
    {
        for (float t = 0f; t < SitSeconds; t += Time.deltaTime)
        {
            transform.SetLocalPositionAndRotation(Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / SitSeconds)), rotation);
            yield return null;
        }
        transform.SetLocalPositionAndRotation(to, rotation);
    }
}
