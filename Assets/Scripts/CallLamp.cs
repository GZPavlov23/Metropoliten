using UnityEngine;

// A small lamp that blinks until someone answers it, with a buzz every few blinks.
[RequireComponent(typeof(Renderer))]
public class CallLamp : MonoBehaviour
{
    static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    public Color lit = new Color(4f, 0.3f, 0.2f);
    public Color unlit = new Color(0.1f, 0.02f, 0.02f);
    public float period = 1.2f;

    public AudioSource buzzer;

    [Tooltip("How many blinks go by for each buzz.")]
    public int blinksPerBuzz = 3;

    Material material;
    bool answered, wasOn;
    int blinks;

    void Awake()
    {
        material = GetComponent<Renderer>().material;
    }

    void Update()
    {
        bool on = !answered && Mathf.Repeat(Time.time, period) < period * 0.5f;
        material.SetColor(BaseColor, on ? lit : unlit);

        if (on && !wasOn && buzzer != null && blinks++ % blinksPerBuzz == 0) buzzer.Play();
        wasOn = on;
    }

    public void Answer()
    {
        answered = true;
        if (buzzer != null) buzzer.Stop();
    }
}
