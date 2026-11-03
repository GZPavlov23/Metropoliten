using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// A black image over the whole screen: fades up when the scene starts, and back down on request.
[RequireComponent(typeof(Image))]
public class ScreenFade : MonoBehaviour
{
    public float fadeInSeconds = 2.5f;
    public float fadeOutSeconds = 1.2f;

    [Tooltip("Words shown on the black before the scene comes up. Optional.")]
    public TMP_Text card;

    public float cardSeconds = 3f;

    Image image;

    void Awake()
    {
        image = GetComponent<Image>();
        SetDarkness(1f);
    }

    IEnumerator Start()
    {
        // Picking up part way through a shift, the card has already been seen.
        if (card != null && SaveSystem.LoadCheckpoint(SceneManager.GetActiveScene().name) == null) yield return ShowCard();
        yield return Fade(1f, 0f, fadeInSeconds);
    }

    IEnumerator ShowCard()
    {
        for (float t = 0f; t < cardSeconds; t += Time.deltaTime)
        {
            card.alpha = Mathf.Min(Mathf.Clamp01(t / 0.7f), Mathf.Clamp01((cardSeconds - t) / 0.7f));
            yield return null;
        }
        card.alpha = 0f;
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
