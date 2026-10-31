using UnityEngine;

// Turns a Humanoid character's head towards a target, and nods it along while they're talking.
// The Animator layer needs its IK Pass ticked.
[RequireComponent(typeof(Animator))]
public class HeadTurn : MonoBehaviour
{
    public Transform target;

    [Range(0f, 1f)]
    [Tooltip("How far they turn to look. 0 leaves the animation alone.")]
    public float weight = 1f;

    [Tooltip("How quickly the look fades in or out.")]
    public float turnSpeed = 0.6f;

    [Header("Talking")]
    [Tooltip("Optional: nods while this box shows them speaking.")]
    public DialogueBox dialogue;
    public string speakerName = "CLERK";
    public float nodDegrees = 2.4f;

    Animator animator;
    Transform head;
    float currentWeight;
    float nod;

    void Awake()
    {
        animator = GetComponent<Animator>();
        head = animator.GetBoneTransform(HumanBodyBones.Head);
    }

    public void LookAtTarget()
    {
        weight = 1f;
    }

    void OnAnimatorIK(int layerIndex)
    {
        currentWeight = Mathf.MoveTowards(currentWeight, weight, turnSpeed * Time.deltaTime);
        animator.SetLookAtWeight(currentWeight, 0.08f, 0.75f, 1f, 0.55f);
        animator.SetLookAtPosition(target.position);
    }

    void LateUpdate()
    {
        bool talking = dialogue != null && dialogue.Speaking == speakerName;
        nod = Mathf.MoveTowards(nod, talking ? 1f : 0f, 5f * Time.deltaTime);
        if (nod <= 0f) return;

        float angle = (Mathf.Sin(Time.time * 7f) + Mathf.Sin(Time.time * 4.3f) * 0.6f) * nodDegrees * nod;
        head.rotation = Quaternion.AngleAxis(angle, transform.right) * head.rotation;
    }
}
