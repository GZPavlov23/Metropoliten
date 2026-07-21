using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Names (must match Build Settings exactly)")]
    public string newGameSceneName = "Shift1";

    [Header("Continue Button")]
    public GameObject continueButton; // drag the Continue button GameObject here

    void Start()
    {
        // Only show Continue if a save actually exists
        if (continueButton != null)
        {
            continueButton.SetActive(SaveSystem.HasSave());
        }
    }

    public void OnNewGamePressed()
    {
        // Starting a new game should wipe any existing save
        SaveSystem.DeleteSave();
        SceneManager.LoadScene(newGameSceneName);
    }

    public void OnContinuePressed()
    {
        SceneManager.LoadScene(SaveSystem.GetSavedScene());
    }

    public void OnSettingsPressed()
    {
        Debug.Log("Settings menu not built yet.");
        // We'll wire this up once we build a Settings panel
    }

    public void OnQuitPressed()
    {
        Debug.Log("Quitting game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}