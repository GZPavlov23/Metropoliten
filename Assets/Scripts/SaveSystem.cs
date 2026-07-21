using UnityEngine;

// Basic placeholder save system.
// For now just remembers which scene to return to using PlayerPrefs.
// We'll expand this later to store shift progress, flags, etc. (possibly via JSON to a file instead of PlayerPrefs).
public static class SaveSystem
{
    private const string SAVE_KEY = "Metropoliten_SavedScene";

    public static void SaveProgress(string sceneName)
    {
        PlayerPrefs.SetString(SAVE_KEY, sceneName);
        PlayerPrefs.Save();
    }

    public static bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    public static string GetSavedScene()
    {
        return PlayerPrefs.GetString(SAVE_KEY, "Shift1");
    }

    public static void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
    }
}