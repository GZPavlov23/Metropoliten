using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The title screen, the pause screen and the settings they share. Like the SoundDirector it lasts the whole
// game, so every scene has its Esc menu without any set-up, and the menus are all built here from code.
public class GameMenu : MonoBehaviour
{
    const string TitleScene = "MainMenu", FirstScene = "Opening";
    const float LeftMargin = 150f, TitleLinesTop = 560f, TitleLineStep = 70f, PauseLinesTop = 400f, RowStep = 48f, SectionStep = 66f, HintY = 1008f;
    const float TitleFontSize = 30f, LeaveSeconds = 1.3f, TitleRiseSeconds = 2.4f, VeilSeconds = 0.25f, PauseDarkness = 0.66f, SettingsDarkness = 0.52f;
    static readonly int RoomDim = Shader.PropertyToID("_RoomDim");

    public static GameMenu Instance { get; private set; }
    public static bool Paused { get; private set; }

    // True while the menu has the keyboard, and for the frame after it lets go: the press that
    // closes a menu mustn't also count in the game underneath.
    public static bool HoldsInput => Paused || Time.frameCount <= closedFrame;

    static int closedFrame = -1;
    static bool hushed;

    readonly List<Action> settingsRefresh = new List<Action>();

    MenuScreen titleScreen, pauseScreen, settingsScreen, openScreen, settingsCameFrom;
    MenuButton continueButton, newGameButton, settingsButton, quitButton;
    RawImage logo;
    CanvasGroup shade, veil, blackout;
    Coroutine dimming;
    bool onTitle, leaving, risenOnce;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Create()
    {
        var go = new GameObject("GameMenu");
        DontDestroyOnLoad(go);
        Instance = go.AddComponent<GameMenu>();
    }

    // The little sounds of the menu. Opening a screen selects its first line, which shouldn't make a noise.
    public static void Tick()
    {
        if (!hushed) SoundDirector.Instance.Cue("U_Move");
    }

    public static void Press()
    {
        SoundDirector.Instance.Cue("U_Press");
    }

    void Awake()
    {
        GameSettings.Load();

        gameObject.AddComponent<EventSystem>();
        gameObject.AddComponent<StandaloneInputModule>();

        Transform canvas = BuildCanvas();
        BuildTitle(canvas);
        BuildPause(canvas);
        BuildSettings(canvas);
        blackout = Layer("Blackout", canvas, Color.black);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    Transform BuildCanvas()
    {
        var go = new GameObject("MenuCanvas");
        go.transform.SetParent(transform, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();

        // Darkness down the left of the screen, for the menus to stand on.
        var ramp = new Texture2D(256, 1, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, name = "MenuShade" };
        for (int x = 0; x < ramp.width; x++)
        {
            float across = x / (ramp.width - 1f);
            ramp.SetPixel(x, 0, new Color(0f, 0f, 0f, 0.93f * (1f - Mathf.SmoothStep(0.3f, 0.6f, across))));
        }
        ramp.Apply();

        RectTransform shadeRect = MenuKit.Fill("Shade", go.transform);
        var shadeImage = shadeRect.gameObject.AddComponent<RawImage>();
        shadeImage.texture = ramp;
        shadeImage.raycastTarget = false;
        shade = shadeRect.gameObject.AddComponent<CanvasGroup>();
        shade.blocksRaycasts = false;
        shade.alpha = 0f;

        veil = Layer("Veil", go.transform, Color.black);
        veil.alpha = 0f;
        return go.transform;
    }

    static CanvasGroup Layer(string name, Transform parent, Color color)
    {
        RectTransform rect = MenuKit.Fill(name, parent);
        var image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        var group = rect.gameObject.AddComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        return group;
    }

    void BuildTitle(Transform parent)
    {
        titleScreen = MenuKit.Screen("TitleScreen", parent);

        logo = MenuKit.Box("Logo", titleScreen.transform, new Vector2(LeftMargin, 262f), new Vector2(768f, 192f)).gameObject.AddComponent<RawImage>();
        logo.texture = Resources.Load<Texture2D>("Menu/T_Logo");
        logo.raycastTarget = false;

        continueButton = MenuKit.Button(titleScreen, "Button_Continue", "CONTINUE", Vector2.zero, TitleFontSize, () => Leave(SaveSystem.GetSavedScene()));
        newGameButton = MenuKit.Button(titleScreen, "Button_NewGame", "NEW GAME", Vector2.zero, TitleFontSize, NewGame);
        settingsButton = MenuKit.Button(titleScreen, "Button_Settings", "SETTINGS", Vector2.zero, TitleFontSize, OpenSettings);
        quitButton = MenuKit.Button(titleScreen, "Button_Quit", "QUIT", Vector2.zero, TitleFontSize, QuitGame);
    }

    void BuildPause(Transform parent)
    {
        pauseScreen = MenuKit.Screen("PauseScreen", parent);
        MenuKit.Label(pauseScreen.transform, "PAUSED", new Vector2(LeftMargin, PauseLinesTop - 118f), new Vector2(700f, 70f), 46f, MenuKit.Ink, 22f);
        MenuKit.Block("Rule", pauseScreen.transform, new Vector2(LeftMargin, PauseLinesTop - 40f), new Vector2(420f, 2f), MenuKit.Rule);

        string[] names = { "Button_Resume", "Button_PauseSettings", "Button_QuitToMenu", "Button_QuitGame" };
        string[] texts = { "RESUME", "SETTINGS", "QUIT TO MENU", "QUIT GAME" };
        Action[] actions = { () => SetPaused(false), OpenSettings, () => Leave(TitleScene), QuitGame };
        MenuKit.Label(pauseScreen.transform, "Esc   Resume", new Vector2(LeftMargin, HintY), new Vector2(400f, 30f), 18f, MenuKit.Faint, 2f);

        var column = new List<Selectable>();
        for (int i = 0; i < names.Length; i++)
            column.Add(MenuKit.Button(pauseScreen, names[i], texts[i], new Vector2(LeftMargin, PauseLinesTop + i * TitleLineStep), TitleFontSize, actions[i]));
        MenuKit.Chain(column);
        pauseScreen.first = column[0];
    }

    void BuildSettings(Transform parent)
    {
        settingsScreen = MenuKit.Screen("SettingsScreen", parent);
        Transform screen = settingsScreen.transform;
        MenuKit.Label(screen, "SETTINGS", new Vector2(LeftMargin, 72f), new Vector2(700f, 60f), 38f, MenuKit.Ink, 16f);
        MenuKit.Label(screen, "Esc   Back", new Vector2(LeftMargin, HintY), new Vector2(400f, 30f), 18f, MenuKit.Faint, 2f);

        var column = new List<Selectable>();
        float y = 146f;

        void Section(string name)
        {
            MenuKit.Label(screen, name, new Vector2(LeftMargin, y), new Vector2(240f, 30f), 17f, MenuKit.Amber, 9f);
            MenuKit.Block("Rule", screen, new Vector2(LeftMargin + 200f, y + 15f), new Vector2(580f, 1f), MenuKit.Rule);
            y += 40f;
        }

        void Slider(string name, string text, float min, float max, bool wholeNumbers, Func<float> read, Action<float> write, Func<float, string> show)
        {
            MenuSlider slider = MenuKit.Slider(settingsScreen, name, text, new Vector2(LeftMargin, y), min, max, wholeNumbers, show, value =>
            {
                write(value);
                GameSettings.Apply();
            });
            settingsRefresh.Add(() =>
            {
                slider.SetValueWithoutNotify(read());
                slider.onValueChanged.Invoke(read());
            });
            column.Add(slider);
            y += RowStep;
        }

        void Choice(string name, string text, Func<string[]> answers, Func<int> read, Action<int> write)
        {
            MenuChoice choice = MenuKit.Choice(settingsScreen, name, text, new Vector2(LeftMargin, y), answers(), write);
            settingsRefresh.Add(() =>
            {
                choice.answers = answers();
                choice.Show(read());
            });
            column.Add(choice);
            y += RowStep;
        }

        string[] offOn = { "Off", "On" };
        string Percent(float value) => $"{Mathf.RoundToInt(value * 100f)}%";

        void Toggle(string name, string text, Func<bool> read, Action<bool> write)
        {
            Choice(name, text, () => offOn, () => read() ? 1 : 0, index =>
            {
                write(index == 1);
                GameSettings.Apply();
            });
        }

        Section("SOUND");
        Slider("Row_MasterVolume", "Master volume", 0f, 1f, false, () => GameSettings.MasterVolume, value => GameSettings.MasterVolume = value, Percent);
        Slider("Row_MusicVolume", "Music", 0f, 1f, false, () => GameSettings.MusicVolume, value => GameSettings.MusicVolume = value, Percent);
        y += SectionStep - RowStep;

        Section("CONTROLS");
        Slider("Row_MouseSensitivity", "Mouse sensitivity", 0.2f, 6f, false, () => GameSettings.MouseSensitivity, value => GameSettings.MouseSensitivity = value, value => value.ToString("0.0", CultureInfo.InvariantCulture));
        Toggle("Row_InvertMouse", "Invert mouse", () => GameSettings.InvertMouse, value => GameSettings.InvertMouse = value);
        Slider("Row_HeadMovement", "Head movement", 0f, 1f, false, () => GameSettings.HeadMovement, value => GameSettings.HeadMovement = value, Percent);
        y += SectionStep - RowStep;

        Section("DISPLAY");
        Choice("Row_DisplayMode", "Display", () => new[] { "Fullscreen", "Windowed" }, () => GameSettings.DisplayMode == FullScreenMode.Windowed ? 1 : 0, index =>
        {
            GameSettings.DisplayMode = index == 1 ? FullScreenMode.Windowed : FullScreenMode.FullScreenWindow;
            GameSettings.ApplyDisplay();
        });
        Choice("Row_Resolution", "Resolution", ResolutionNames, () => GameSettings.Resolutions().IndexOf(new Vector2Int(GameSettings.ScreenWidth, GameSettings.ScreenHeight)), index =>
        {
            Vector2Int size = GameSettings.Resolutions()[index];
            GameSettings.ScreenWidth = size.x;
            GameSettings.ScreenHeight = size.y;
            GameSettings.ApplyDisplay();
        });
        Toggle("Row_VSync", "V-Sync", () => GameSettings.VSync, value => GameSettings.VSync = value);
        Slider("Row_FieldOfView", "Field of view", 60f, 95f, true, () => GameSettings.FieldOfView, value => GameSettings.FieldOfView = value, value => Mathf.RoundToInt(value).ToString());
        Slider("Row_Brightness", "Brightness", -0.5f, 0.5f, false, () => GameSettings.Brightness, value => GameSettings.Brightness = value, value => Percent(value + 0.5f));
        y += SectionStep - RowStep;

        column.Add(MenuKit.Button(settingsScreen, "Button_Defaults", "RESET TO DEFAULTS", new Vector2(LeftMargin, y), 21f, ResetSettings));
        y += RowStep;
        column.Add(MenuKit.Button(settingsScreen, "Button_Back", "BACK", new Vector2(LeftMargin, y), 21f, CloseSettings));

        MenuKit.Chain(column);
        settingsScreen.first = column[0];

        foreach (MenuGlow line in settingsScreen.lines) line.gameObject.SetActive(true);
    }

    static string[] ResolutionNames()
    {
        List<Vector2Int> sizes = GameSettings.Resolutions();
        var names = new string[sizes.Count];
        for (int i = 0; i < names.Length; i++) names[i] = $"{sizes[i].x} x {sizes[i].y}";
        return names;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        leaving = false;
        Paused = false;
        Time.timeScale = 1f;
        SoundDirector.PauseWorld(false);
        veil.alpha = 0f;

        onTitle = scene.name == TitleScene;
        if (onTitle) ShowTitle();
        else ShowGame(scene.name);
    }

    void ShowTitle()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        shade.alpha = 1f;

        // The first thing on the screen is the dark, with the place coming up out of it.
        StartCoroutine(Fade(blackout, 1f, 0f, TitleRiseSeconds));
        float delay = risenOnce ? 0.5f : 1.3f;
        risenOnce = true;

        var column = new List<Selectable>();
        continueButton.gameObject.SetActive(SaveSystem.HasSave());
        foreach (MenuButton button in new[] { continueButton, newGameButton, settingsButton, quitButton })
        {
            if (!button.gameObject.activeSelf) continue;
            ((RectTransform)button.transform).anchoredPosition = new Vector2(LeftMargin, -(TitleLinesTop + column.Count * TitleLineStep));
            column.Add(button);
        }
        MenuKit.Chain(column);
        titleScreen.first = column[0];
        Open(titleScreen, delay);
    }

    void ShowGame(string sceneName)
    {
        shade.alpha = 0f;
        blackout.alpha = 0f;
        SaveSystem.SaveProgress(sceneName);
        Open(null, 0f);
    }

    void Update()
    {
        if (!leaving && Input.GetKeyDown(KeyCode.Escape))
        {
            if (openScreen == settingsScreen) CloseSettings();
            else if (!onTitle) SetPaused(!Paused);
        }

        // A click on nothing clears the selection, which would leave the keyboard with nowhere to start from.
        if (openScreen != null && EventSystem.current.currentSelectedGameObject == null) SelectQuietly(openScreen.first);

        // The title is lit by the same bad tube as everything else.
        if (onTitle)
        {
            float dip = Shader.GetGlobalFloat(RoomDim);
            logo.color = new Color(1f, 1f, 1f, 1f - dip * 3f);
        }
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused && !Application.isBatchMode && !onTitle && !Paused && !leaving) SetPaused(true);
    }

    void Open(MenuScreen screen, float delay)
    {
        if (openScreen != null && openScreen != screen) openScreen.Hide();
        openScreen = screen;
        if (screen == null) return;

        screen.Show(delay);
        SelectQuietly(screen.first);
    }

    void SelectQuietly(Selectable line)
    {
        hushed = true;
        EventSystem.current.SetSelectedGameObject(line.gameObject);
        hushed = false;
    }

    void SetPaused(bool paused)
    {
        Paused = paused;
        if (!paused) closedFrame = Time.frameCount;

        Time.timeScale = paused ? 0f : 1f;
        SoundDirector.PauseWorld(paused);
        Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = paused;

        Dim(paused ? PauseDarkness : 0f, paused ? 1f : 0f);
        Open(paused ? pauseScreen : null, 0f);
    }

    void Dim(float veilTo, float shadeTo)
    {
        if (dimming != null) StopCoroutine(dimming);
        dimming = StartCoroutine(Dimming(veilTo, shadeTo));
    }

    IEnumerator Dimming(float veilTo, float shadeTo)
    {
        StartCoroutine(Fade(shade, shade.alpha, shadeTo, VeilSeconds));
        yield return Fade(veil, veil.alpha, veilTo, VeilSeconds);
    }

    void OpenSettings()
    {
        foreach (Action refresh in settingsRefresh) refresh();
        settingsCameFrom = openScreen;
        if (onTitle) Dim(SettingsDarkness, 1f);
        Open(settingsScreen, 0f);
    }

    void CloseSettings()
    {
        GameSettings.Save();
        if (onTitle) Dim(0f, 1f);
        Open(settingsCameFrom, 0f);
    }

    void ResetSettings()
    {
        GameSettings.ResetToDefaults();
        GameSettings.Apply();
        GameSettings.ApplyDisplay();
        foreach (Action refresh in settingsRefresh) refresh();
    }

    void NewGame()
    {
        SaveSystem.DeleteSave();
        Leave(FirstScene);
    }

    void Leave(string sceneName)
    {
        if (!leaving) StartCoroutine(FadeAndLoad(sceneName));
    }

    IEnumerator FadeAndLoad(string sceneName)
    {
        leaving = true;
        Open(null, 0f);
        SoundDirector.Instance.SinkScene(LeaveSeconds);
        yield return Fade(blackout, blackout.alpha, 1f, LeaveSeconds);
        SceneManager.LoadScene(sceneName);
    }

    static IEnumerator Fade(CanvasGroup group, float from, float to, float seconds)
    {
        for (float t = 0f; t < seconds; t += Time.unscaledDeltaTime)
        {
            group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / seconds));
            yield return null;
        }
        group.alpha = to;
    }

    void QuitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
