using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using static SceneForge;

// Everything in the Opening scene the player acts on: the on-screen text, the clerk's window,
// the gate that keeps them from the stairs until they've signed in, and the remarks along the way.
public static partial class OpeningSceneBuilder
{
    const string ScriptPath = "Assets/Dialogue/Opening.txt";

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

    static void BuildIntro(Transform characters, FirstPersonController player, Transform playerView)
    {
        Transform parent = new GameObject("Intro").transform;
        GameUi ui = BuildGameUi(ScriptPath);
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

        // Nothing is said until the player does something: these only answer walking into something locked.
        Zone("Remark_Door", parent, new Vector3(3.5f, 1f, 0.3f), new Vector3(2.2f, 2f, 0.5f), dialogue, "door_locked");
        RemarkZone locked = Zone("Remark_Gate", parent, new Vector3(Width - 0.25f, 1f, (ExitZMin + ExitZMax) / 2f),
            new Vector3(0.6f, 2f, ExitZMax - ExitZMin), dialogue, "gate_locked");

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
