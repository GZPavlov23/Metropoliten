using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// The player's choices, kept between sessions. They take effect as soon as they change: what belongs to
// the whole game is applied here, and whatever lives in a scene listens for Changed.
public static class GameSettings
{
    static readonly int BrightnessId = Shader.PropertyToID("_Brightness");

    public static event Action Changed;

    public static float MasterVolume, MusicVolume, MouseSensitivity, HeadMovement, FieldOfView, Brightness;
    public static bool InvertMouse, VSync;
    public static FullScreenMode DisplayMode;
    public static int ScreenWidth, ScreenHeight;

    public static void ResetToDefaults()
    {
        MasterVolume = 1f;
        MusicVolume = 1f;
        MouseSensitivity = 2f;
        InvertMouse = false;
        HeadMovement = 1f;
        FieldOfView = 68f;
        Brightness = 0f;
        VSync = true;
        DisplayMode = FullScreenMode.FullScreenWindow;
        ScreenWidth = Display.main.systemWidth;
        ScreenHeight = Display.main.systemHeight;
    }

    public static void Load()
    {
        ResetToDefaults();
        MasterVolume = PlayerPrefs.GetFloat(nameof(MasterVolume), MasterVolume);
        MusicVolume = PlayerPrefs.GetFloat(nameof(MusicVolume), MusicVolume);
        MouseSensitivity = PlayerPrefs.GetFloat(nameof(MouseSensitivity), MouseSensitivity);
        InvertMouse = PlayerPrefs.GetInt(nameof(InvertMouse), InvertMouse ? 1 : 0) == 1;
        HeadMovement = PlayerPrefs.GetFloat(nameof(HeadMovement), HeadMovement);
        FieldOfView = PlayerPrefs.GetFloat(nameof(FieldOfView), FieldOfView);
        Brightness = PlayerPrefs.GetFloat(nameof(Brightness), Brightness);
        VSync = PlayerPrefs.GetInt(nameof(VSync), VSync ? 1 : 0) == 1;
        DisplayMode = (FullScreenMode)PlayerPrefs.GetInt(nameof(DisplayMode), (int)DisplayMode);
        ScreenWidth = PlayerPrefs.GetInt(nameof(ScreenWidth), ScreenWidth);
        ScreenHeight = PlayerPrefs.GetInt(nameof(ScreenHeight), ScreenHeight);

        Apply();
        ApplyDisplay();
    }

    public static void Save()
    {
        PlayerPrefs.SetFloat(nameof(MasterVolume), MasterVolume);
        PlayerPrefs.SetFloat(nameof(MusicVolume), MusicVolume);
        PlayerPrefs.SetFloat(nameof(MouseSensitivity), MouseSensitivity);
        PlayerPrefs.SetInt(nameof(InvertMouse), InvertMouse ? 1 : 0);
        PlayerPrefs.SetFloat(nameof(HeadMovement), HeadMovement);
        PlayerPrefs.SetFloat(nameof(FieldOfView), FieldOfView);
        PlayerPrefs.SetFloat(nameof(Brightness), Brightness);
        PlayerPrefs.SetInt(nameof(VSync), VSync ? 1 : 0);
        PlayerPrefs.SetInt(nameof(DisplayMode), (int)DisplayMode);
        PlayerPrefs.SetInt(nameof(ScreenWidth), ScreenWidth);
        PlayerPrefs.SetInt(nameof(ScreenHeight), ScreenHeight);
        PlayerPrefs.Save();
    }

    public static void Apply()
    {
        AudioListener.volume = MasterVolume;
        QualitySettings.vSyncCount = VSync ? 1 : 0;
        Shader.SetGlobalFloat(BrightnessId, Brightness);
        Changed?.Invoke();
    }

    public static void ApplyDisplay()
    {
        Screen.SetResolution(ScreenWidth, ScreenHeight, DisplayMode);
    }

    // What the screen can show, largest first, with whatever's chosen now always among them.
    public static List<Vector2Int> Resolutions()
    {
        List<Vector2Int> sizes = Screen.resolutions.Select(option => new Vector2Int(option.width, option.height)).Distinct()
            .OrderByDescending(size => size.x).ThenByDescending(size => size.y).ToList();
        var chosen = new Vector2Int(ScreenWidth, ScreenHeight);
        if (!sizes.Contains(chosen)) sizes.Insert(0, chosen);
        return sizes;
    }
}
