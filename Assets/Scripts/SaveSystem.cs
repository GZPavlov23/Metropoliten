using System;
using UnityEngine;

// Remembers which scene to return to, how far through its shift the player got, and what the story has seen them do.
public static class SaveSystem
{
    private const string SAVE_KEY = "Metropoliten_SavedScene";
    private const string FLAGS_KEY = "Metropoliten_Flags";
    private const string CHECKPOINT_KEY = "Metropoliten_Checkpoint";

    // Where in a shift Continue picks up from.
    [Serializable]
    public class Checkpoint
    {
        public string scene;
        public int step;

        // Which of the open step's jobs are done, one bit for each.
        public int done;

        public bool seated;
        public Vector3 position;
        public float yaw, pitch;

        // How far along the line the train is standing, and whether it was out on a run.
        public float trainAt;
        public bool driving;
    }

    public static event Action<string> FlagSet;

    public static void SaveProgress(string sceneName)
    {
        PlayerPrefs.SetString(SAVE_KEY, sceneName);

        // A new scene is a new shift: what was saved in the last one doesn't apply to it.
        Checkpoint checkpoint = LoadCheckpoint();
        if (checkpoint != null && checkpoint.scene != sceneName) PlayerPrefs.DeleteKey(CHECKPOINT_KEY);
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
        PlayerPrefs.DeleteKey(FLAGS_KEY);
        PlayerPrefs.DeleteKey(CHECKPOINT_KEY);
    }

    public static void SaveCheckpoint(Checkpoint checkpoint)
    {
        PlayerPrefs.SetString(CHECKPOINT_KEY, JsonUtility.ToJson(checkpoint));
        PlayerPrefs.Save();
    }

    public static void ClearCheckpoint()
    {
        PlayerPrefs.DeleteKey(CHECKPOINT_KEY);
        PlayerPrefs.Save();
    }

    public static Checkpoint LoadCheckpoint()
    {
        string saved = PlayerPrefs.GetString(CHECKPOINT_KEY, "");
        return saved.Length == 0 ? null : JsonUtility.FromJson<Checkpoint>(saved);
    }

    public static Checkpoint LoadCheckpoint(string sceneName)
    {
        Checkpoint checkpoint = LoadCheckpoint();
        return checkpoint != null && checkpoint.scene == sceneName ? checkpoint : null;
    }

    // Things the player has done that the story remembers, set and read from the dialogue files.
    public static void SetFlag(string flag)
    {
        if (HasFlag(flag)) return;
        PlayerPrefs.SetString(FLAGS_KEY, PlayerPrefs.GetString(FLAGS_KEY, "") + flag + ";");
        PlayerPrefs.Save();
        FlagSet?.Invoke(flag);
    }

    public static bool HasFlag(string flag)
    {
        return PlayerPrefs.GetString(FLAGS_KEY, "").Contains(flag + ";");
    }
}
