using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// One screen of a menu. It fades up as a whole while its lines arrive one after another.
[RequireComponent(typeof(CanvasGroup))]
public class MenuScreen : MonoBehaviour
{
    const float FadeInSeconds = 0.3f, FadeOutSeconds = 0.14f, LineGap = 0.055f;

    public readonly List<MenuGlow> lines = new List<MenuGlow>();

    // What's selected when the screen opens.
    public Selectable first;

    CanvasGroup group;

    void Awake()
    {
        group = GetComponent<CanvasGroup>();
    }

    public void Show(float delay)
    {
        gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeIn(delay));
    }

    public void Hide()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(FadeOut());
    }

    IEnumerator FadeIn(float delay)
    {
        group.alpha = 0f;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].gameObject.activeSelf) lines[i].Arrive(delay + i * LineGap);
        }

        for (float t = -delay; t < FadeInSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Clamp01(t / FadeInSeconds);
            yield return null;
        }
        group.alpha = 1f;
    }

    IEnumerator FadeOut()
    {
        float start = group.alpha;
        for (float t = 0f; t < FadeOutSeconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(start, 0f, t / FadeOutSeconds);
            yield return null;
        }
        gameObject.SetActive(false);
    }
}
