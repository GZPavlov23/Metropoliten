using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Changes scene when the player walks into this object's trigger, or whenever Leave is called.
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Scene name to load, must match Build Settings exactly.")]
    public string targetSceneName = "Shift1";

    [Tooltip("Optional: fades to black before the scene changes.")]
    public ScreenFade fade;

    bool leaving;

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() != null) Leave();
    }

    public void Leave()
    {
        if (leaving) return;
        leaving = true;
        StartCoroutine(FadeAndLoad());
    }

    IEnumerator FadeAndLoad()
    {
        if (fade != null)
        {
            SoundDirector.Instance.SinkScene(fade.fadeOutSeconds);
            yield return fade.FadeOut();
        }
        SceneManager.LoadScene(targetSceneName);
    }
}
