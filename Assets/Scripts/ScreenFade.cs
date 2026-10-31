using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// A black image over the whole screen: fades up when the scene starts, and back down on request.
[RequireComponent(typeof(Image))]
public class ScreenFade : MonoBehaviour
{
    public float fadeInSeconds = 2.5f;
    public float fadeOutSeconds = 1.2f;

    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        SetDarkness(1f);
    }

    IEnumerator Start()
    {
        yield return Fade(1f, 0f, fadeInSeconds);
    }

    public IEnumerator FadeOut()
    {
        yield return Fade(image.color.a, 1f, fadeOutSeconds);
    }

    IEnumerator Fade(float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.deltaTime)
        {
            SetDarkness(Mathf.Lerp(from, to, t / seconds));
            yield return null;
        }
        SetDarkness(to);
    }

    void SetDarkness(float amount)
    {
        Color color = image.color;
        color.a = amount;
        image.color = color;
    }
}
