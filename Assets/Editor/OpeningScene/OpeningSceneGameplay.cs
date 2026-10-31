using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;

// Everything in the Opening scene the player acts on: the on-screen text, the clerk's window,
// the gate that keeps them from the stairs until they've signed in, and the remarks along the way.
public static partial class OpeningSceneBuilder
{
    const string ScriptPath = "Assets/Dialogue/Opening.txt";
    const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    const float PlayerWalkSpeed = 2.2f, PlayerSprintSpeed = 3.6f;

    struct IntroUi
    {
        public DialogueBox Dialogue;
        public ScreenFade Fade;
    }

    static RectTransform UiChild(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    // The default font with a soft dark halo, so text stays readable over the pale counter.
    static Material TextMaterial()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        string path = $"{MaterialDir}/M_UiText.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(font.material);
            AssetDatabase.CreateAsset(material, path);
        }
        material.CopyPropertiesFromMaterial(font.material);
        material.EnableKeyword("UNDERLAY_ON");
        material.SetColor("_UnderlayColor", new Color(0f, 0f, 0f, 0.95f));
        material.SetFloat("_UnderlaySoftness", 0.55f);
        material.SetFloat("_UnderlayDilate", 0.35f);
        EditorUtility.SetDirty(material);
        return material;
    }

    static TMP_Text UiText(string name, Transform parent, Vector2 position, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var bottomCentre = new Vector2(0.5f, 0f);
        var text = UiChild(name, parent, bottomCentre, bottomCentre, position, size).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        text.fontSharedMaterial = TextMaterial();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    static IntroUi BuildIntroUi()
    {
        var canvasGo = new GameObject("IntroUI");
        canvasGo.AddComponent<UnityEngine.Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform panelRect = UiChild("Conversation", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var panel = panelRect.gameObject.AddComponent<CanvasGroup>();
        panel.blocksRaycasts = false;
        panel.interactable = false;

        var shade = UiChild("Shade", panelRect, Vector2.zero, Vector2.right, new Vector2(0f, 230f), new Vector2(0f, 460f)).gameObject.AddComponent<RawImage>();
        shade.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TextureForge.Path("T_UiShade"));
        shade.raycastTarget = false;

        const float lineY = 262f, replyTop = 168f, replyPitch = 42f;
        TMP_Text speaker = UiText("Speaker", panelRect, new Vector2(0f, lineY + 62f), new Vector2(1400f, 40f), 26f, TextAlignmentOptions.Center);
        speaker.characterSpacing = 12f;
        TMP_Text line = UiText("Line", panelRect, new Vector2(0f, lineY), new Vector2(1400f, 92f), 38f, TextAlignmentOptions.Center);
        TMP_Text continueMark = UiText("Continue", panelRect, new Vector2(0f, lineY - 62f), new Vector2(200f, 32f), 22f, TextAlignmentOptions.Center);
        continueMark.text = "E";
        continueMark.color = new Color(0.62f, 0.62f, 0.6f);

        var replies = new TMP_Text[4];
        for (int i = 0; i < replies.Length; i++)
            replies[i] = UiText($"Reply{i + 1}", panelRect, new Vector2(0f, replyTop - i * replyPitch), new Vector2(1200f, 40f), 32f, TextAlignmentOptions.Center);

        TMP_Text hint = UiText("Hint", panelRect, new Vector2(0f, 18f), new Vector2(800f, 28f), 20f, TextAlignmentOptions.Center);
        hint.text = "W / S   Choose        E   Confirm";
        hint.color = new Color(0.62f, 0.62f, 0.6f);

        TMP_Text prompt = UiText("Prompt", canvasGo.transform, new Vector2(0f, 200f), new Vector2(500f, 44f), 28f, TextAlignmentOptions.Center);

        RectTransform fadeRect = UiChild("Fade", canvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var black = fadeRect.gameObject.AddComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = false;

        var dialogue = canvasGo.AddComponent<DialogueBox>();
        dialogue.scriptFile = AssetDatabase.LoadAssetAtPath<TextAsset>(ScriptPath);
        dialogue.panel = panel;
        dialogue.speakerText = speaker;
        dialogue.lineText = line;
        dialogue.continueText = continueMark;
        dialogue.replyTexts = replies;
        dialogue.hintText = hint;
        dialogue.promptText = prompt;

        return new IntroUi { Dialogue = dialogue, Fade = fadeRect.gameObject.AddComponent<ScreenFade>() };
    }

    // A folding lattice across the top of the stairs, hinged at its north end.
    static ExitGate BuildExitGate(Transform parent)
    {
        const int bays = 12, rows = 4;
        float span = ExitZMax - ExitZMin, height = ExitTop - 0.03f, bay = span / bays;
        Vector2 tile = MetalTile;
        var kit = new MeshKit();

        kit.Box(new Vector3(0f, height - 0.02f, -span / 2f), new Vector3(0.05f, 0.04f, span), tile);
        kit.Box(new Vector3(0f, 0.02f, -span / 2f), new Vector3(0.05f, 0.04f, span), tile);
        for (int i = 0; i <= bays; i++)
            kit.Tube(new Vector3(0f, 0.04f, -i * bay), new Vector3(0f, height - 0.04f, -i * bay), 0.011f, 5, tile);

        float rowHeight = (height - 0.08f) / rows;
        for (int row = 0; row < rows; row++)
        {
            float y0 = 0.04f + row * rowHeight, y1 = y0 + rowHeight;
            for (int i = 0; i < bays; i++)
            {
                kit.Tube(new Vector3(0f, y0, -i * bay), new Vector3(0f, y1, -(i + 1) * bay), 0.007f, 4, tile);
                kit.Tube(new Vector3(0f, y1, -i * bay), new Vector3(0f, y0, -(i + 1) * bay), 0.007f, 4, tile);
            }
        }

        Mesh mesh = kit.ToMesh("ExitGate");
        AssetDatabase.CreateAsset(mesh, $"{MeshDir}/ExitGate.asset");

        var go = new GameObject("ExitGate");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(Width + 0.09f, 0f, ExitZMax);
        go.AddComponent<MeshFilter>().sharedMesh = mesh;
        go.AddComponent<MeshRenderer>().sharedMaterial = SurfaceMaterial("GateSteel");

        var collider = go.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, ExitTop / 2f, -span / 2f);
        collider.size = new Vector3(0.1f, ExitTop, span);
        return go.AddComponent<ExitGate>();
    }

    static RemarkZone Zone(string name, Transform parent, Vector3 center, Vector3 size, DialogueBox dialogue, string label, float delay, bool onlyOnce)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        var zone = go.AddComponent<RemarkZone>();
        zone.dialogue = dialogue;
        zone.label = label;
        zone.delay = delay;
        zone.onlyOnce = onlyOnce;
        return zone;
    }

    static void BuildIntro(Transform characters, FirstPersonController player, Transform playerView)
    {
        Transform parent = new GameObject("Intro").transform;
        IntroUi ui = BuildIntroUi();
        DialogueBox dialogue = ui.Dialogue;
        ExitGate gate = BuildExitGate(parent);

        Transform clerk = characters.Find("Clerk");
        HeadTurn clerkHead = clerk.gameObject.AddComponent<HeadTurn>();
        clerkHead.target = playerView;
        clerkHead.weight = 0.85f;
        clerkHead.dialogue = dialogue;

        var talkGo = new GameObject("ClerkWindow");
        talkGo.transform.SetParent(parent);
        talkGo.transform.position = new Vector3(OpenWindowX, 1.2f, Length);
        var talk = talkGo.AddComponent<TalkSpot>();
        talk.dialogue = dialogue;
        talk.player = player;
        talk.person = clerk.GetComponent<Animator>().GetBoneTransform(HumanBodyBones.Head);

        Zone("Remark_Arrive", parent, new Vector3(Width / 2f, 1f, 1f), new Vector3(3f, 2f, 2f), dialogue, "arrive", 3f, true);
        Zone("Remark_Next", parent, new Vector3(Width / 2f, 1f, 5.2f), new Vector3(Width, 2f, 0.6f), dialogue, "next", 0f, true);
        Zone("Remark_Door", parent, new Vector3(3.5f, 1f, 0.3f), new Vector3(2.2f, 2f, 0.5f), dialogue, "door_locked", 0f, false);
        RemarkZone locked = Zone("Remark_Gate", parent, new Vector3(Width - 0.25f, 1f, (ExitZMin + ExitZMax) / 2f),
            new Vector3(0.6f, 2f, ExitZMax - ExitZMin), dialogue, "gate_locked", 0f, false);

        // Signing in opens the way, and the two on the benches have noticed the player by the time they leave.
        UnityEventTools.AddPersistentListener(talk.onFirstConversationEnded, gate.Open);
        UnityEventTools.AddBoolPersistentListener(talk.onFirstConversationEnded, locked.gameObject.SetActive, false);
        foreach (string waiting in new[] { "Waiting_Adult", "Waiting_Young" })
        {
            HeadTurn head = characters.Find(waiting).gameObject.AddComponent<HeadTurn>();
            head.target = playerView;
            head.weight = 0f;
            head.turnSpeed = 0.35f;
            UnityEventTools.AddPersistentListener(talk.onFirstConversationEnded, head.LookAtTarget);
        }

        Object.FindAnyObjectByType<SceneTransitionTrigger>().fade = ui.Fade;
    }
}
