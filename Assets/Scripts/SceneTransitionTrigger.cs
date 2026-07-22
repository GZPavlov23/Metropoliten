using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Tooltip("Scene name to load, must match Build Settings exactly.")]
    public string targetSceneName = "Shift1";

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<CharacterController>() == null) return;
        SceneManager.LoadScene(targetSceneName);
    }
}
