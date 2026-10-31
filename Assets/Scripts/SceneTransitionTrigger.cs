using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Scene name to load, must match Build Settings exactly.")]
    public string targetSceneName = "Shift1";

    [Tooltip("Optional: fades to black before the scene changes.")]
    public ScreenFade fade;

    bool leaving;

    void OnTriggerEnter(Collider other)
    {
        if (leaving || other.GetComponent<CharacterController>() == null) return;
        leaving = true;
        StartCoroutine(Leave());
    }

    IEnumerator Leave()
    {
        if (fade != null) yield return fade.FadeOut();
        SceneManager.LoadScene(targetSceneName);
    }
}
