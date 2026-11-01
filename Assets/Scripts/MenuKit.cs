using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Builds the parts of a menu from code. Everything is placed from the top left of a 1920 by 1080 screen.
public static class MenuKit
{
    const float LineHeight = 44f, BarGap = 38f, ControlX = 400f, ControlWidth = 380f, TrackWidth = 290f, RowWidth = 800f;

    public static readonly Color Ink = new Color(0.78f, 0.77f, 0.72f);
    public static readonly Color Faint = new Color(0.5f, 0.49f, 0.45f);
    public static readonly Color Amber = new Color(0.93f, 0.7f, 0.32f);
    public static readonly Color Rule = new Color(0.93f, 0.7f, 0.32f, 0.3f);

    public static RectTransform Box(string name, Transform parent, Vector2 position, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)go.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(position.x, -position.y);
        rect.sizeDelta = size;
        return rect;
    }

    public static RectTransform Fill(string name, Transform parent)
    {
        RectTransform rect = Box(name, parent, Vector2.zero, Vector2.zero);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return rect;
    }

    public static Image Block(string name, Transform parent, Vector2 position, Vector2 size, Color color)
    {
        Image image = Box(name, parent, position, size).gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    public static TMP_Text Label(Transform parent, string text, Vector2 position, Vector2 size, float fontSize, Color color, float spacing = 0f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        var label = Box("Text", parent, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.characterSpacing = spacing;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    public static MenuScreen Screen(string name, Transform parent)
    {
        RectTransform rect = Fill(name, parent);
        rect.gameObject.AddComponent<CanvasGroup>();
        MenuScreen screen = rect.gameObject.AddComponent<MenuScreen>();
        rect.gameObject.SetActive(false);
        return screen;
    }

    // A line the mouse can reach, ready for whatever goes in it.
    static MenuGlow Line(MenuScreen screen, string name, Vector2 position, Vector2 size)
    {
        RectTransform line = Box(name, screen.transform, position, size);
        line.gameObject.AddComponent<Image>().color = Color.clear;

        RectTransform inner = Box("Inner", line, Vector2.zero, size);
        Image bar = Block("Bar", inner, new Vector2(-BarGap, size.y / 2f - 1f), new Vector2(0f, 2f), Amber);

        line.gameObject.SetActive(false);
        var glow = line.gameObject.AddComponent<MenuGlow>();
        glow.inner = inner;
        glow.bar = bar;
        line.gameObject.SetActive(true);

        screen.lines.Add(glow);
        return glow;
    }

    public static MenuButton Button(MenuScreen screen, string name, string text, Vector2 position, float fontSize, Action pressed)
    {
        MenuGlow glow = Line(screen, name, position, new Vector2(RowWidth, fontSize * 1.7f));
        glow.texts = new[] { Label(glow.inner, text, Vector2.zero, glow.inner.sizeDelta, fontSize, Ink, fontSize * 0.42f) };

        var button = glow.gameObject.AddComponent<MenuButton>();
        button.glow = glow;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            GameMenu.Press();
            pressed();
        });
        return button;
    }

    public static MenuSlider Slider(MenuScreen screen, string name, string text, Vector2 position, float min, float max, bool wholeNumbers, Func<float, string> show, Action<float> changed)
    {
        MenuGlow glow = Line(screen, name, position, new Vector2(RowWidth, LineHeight));
        TMP_Text label = Label(glow.inner, text, Vector2.zero, new Vector2(ControlX, LineHeight), 25f, Ink, 1f);
        TMP_Text value = Label(glow.transform, "", new Vector2(ControlX + TrackWidth + 24f, 0f), new Vector2(ControlWidth - TrackWidth - 24f, LineHeight), 23f, Ink, 1f);
        glow.texts = new[] { label, value };

        RectTransform grab = Box("Grab", glow.transform, new Vector2(ControlX - 10f, 0f), new Vector2(TrackWidth + 20f, LineHeight));
        Block("Track", grab, new Vector2(10f, LineHeight / 2f - 1f), new Vector2(TrackWidth, 2f), Faint);

        RectTransform fillArea = Box("Fill", grab, new Vector2(10f, LineHeight / 2f - 1f), new Vector2(TrackWidth, 2f));
        RectTransform fill = Fill("Bar", fillArea);
        Image fillImage = fill.gameObject.AddComponent<Image>();
        fillImage.color = Amber;
        fillImage.raycastTarget = false;

        RectTransform handleArea = Box("HandleArea", grab, new Vector2(10f, LineHeight / 2f - 1f), new Vector2(TrackWidth, 2f));
        RectTransform handle = Box("Handle", handleArea, Vector2.zero, new Vector2(6f, 20f));
        handle.anchorMin = handle.anchorMax = handle.pivot = new Vector2(0.5f, 0.5f);
        Image handleImage = handle.gameObject.AddComponent<Image>();
        handleImage.color = Amber;
        handleImage.raycastTarget = false;

        var slider = glow.gameObject.AddComponent<MenuSlider>();
        slider.glow = glow;
        slider.grabArea = grab;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fill;
        slider.handleRect = handle;
        slider.direction = UnityEngine.UI.Slider.Direction.LeftToRight;
        slider.minValue = min;
        slider.maxValue = max;
        slider.wholeNumbers = wholeNumbers;
        slider.onValueChanged.AddListener(number =>
        {
            value.text = show(number);
            changed(number);
        });
        return slider;
    }

    public static MenuChoice Choice(MenuScreen screen, string name, string text, Vector2 position, string[] answers, Action<int> changed)
    {
        MenuGlow glow = Line(screen, name, position, new Vector2(RowWidth, LineHeight));
        TMP_Text label = Label(glow.inner, text, Vector2.zero, new Vector2(ControlX, LineHeight), 25f, Ink, 1f);

        RectTransform area = Box("Answer", glow.transform, new Vector2(ControlX, 0f), new Vector2(TrackWidth, LineHeight));
        TMP_Text back = Label(area, "<", Vector2.zero, new Vector2(40f, LineHeight), 23f, Ink);
        TMP_Text answer = Label(area, "", new Vector2(40f, 0f), new Vector2(TrackWidth - 80f, LineHeight), 23f, Ink, 1f, TextAlignmentOptions.Center);
        TMP_Text next = Label(area, ">", new Vector2(TrackWidth - 40f, 0f), new Vector2(40f, LineHeight), 23f, Ink, 0f, TextAlignmentOptions.MidlineRight);
        glow.texts = new[] { label, back, answer, next };

        var choice = glow.gameObject.AddComponent<MenuChoice>();
        choice.glow = glow;
        choice.transition = Selectable.Transition.None;
        choice.answerArea = area;
        choice.answerText = answer;
        choice.answers = answers;
        choice.onChanged = changed;
        return choice;
    }

    // Links a column of controls for the keyboard, with the ends joined up. Left and right are left free for the controls.
    public static void Chain(IList<Selectable> column)
    {
        for (int i = 0; i < column.Count; i++)
        {
            column[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = column[(i + column.Count - 1) % column.Count],
                selectOnDown = column[(i + 1) % column.Count],
            };
        }
    }
}
