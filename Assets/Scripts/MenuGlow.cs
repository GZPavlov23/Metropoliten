using TMPro;
using UnityEngine;
using UnityEngine.UI;

// How a line of a menu looks: it arrives with a short slide, and while it's the selected one it lights up,
// moves along a little and has a bar come in beside it. Runs on real time, so it works while the game is paused.
[RequireComponent(typeof(CanvasGroup))]
public class MenuGlow : MonoBehaviour
{
    const float LightRate = 9f, ArriveRate = 3.2f, ArriveDistance = 26f, SelectedShift = 16f, BarLength = 28f;

    public TMP_Text[] texts;
    public Image bar;

    [Tooltip("The part of the line that moves.")]
    public RectTransform inner;

    public Color idleColor = MenuKit.Ink;
    public Color litColor = MenuKit.Amber;

    CanvasGroup group;
    Vector2 home;
    float lit, arrived = 1f, arriveWait;
    bool selected;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
        home = inner.anchoredPosition;
    }

    public void SetSelected(bool on)
    {
        selected = on;
    }

    public void Arrive(float delay)
    {
        arrived = 0f;
        arriveWait = delay;
        Refresh();
    }

    void Update()
    {
        float step = Time.unscaledDeltaTime;
        lit = Mathf.MoveTowards(lit, selected ? 1f : 0f, step * LightRate);

        if (arriveWait > 0f) arriveWait -= step;
        else arrived = Mathf.MoveTowards(arrived, 1f, step * ArriveRate);

        Refresh();
    }

    void Refresh()
    {
        float arrival = 1f - Mathf.Pow(1f - arrived, 3f);
        float light = Mathf.SmoothStep(0f, 1f, lit);

        group.alpha = arrival;
        inner.anchoredPosition = home + Vector2.right * (SelectedShift * light - ArriveDistance * (1f - arrival));

        Color color = Color.Lerp(idleColor, litColor, light);
        foreach (TMP_Text text in texts) text.color = color;

        Vector2 size = bar.rectTransform.sizeDelta;
        size.x = BarLength * light;
        bar.rectTransform.sizeDelta = size;
    }
}
