using UnityEditor.Events;
using UnityEngine;
using static SceneForge;

// What the registry sounds like while everyone waits: the tubes, the clock, the radiator, the clerk at
// work behind the glass, and a draught coming up the stairs.
public static partial class OpeningSceneBuilder
{
    static void BuildSound(FirstPersonController player)
    {
        Transform parent = new GameObject("Sound").transform;
        AudioClip tube = Sound("A_Tube");
        Vector3 stairs = new Vector3(StairEnd + 5f, StairBottom + 1f, (ExitZMin + ExitZMax) / 2f);

        RoomTone(parent, 0.14f);
        BuildPlayerSound(player, SoundForge.WalkSteps, SoundForge.RunSteps);

        foreach (float z in new[] { 4.3f, 7.15f }) Ambient(parent, "Tube", new Vector3(Width / 2f, LampHeight, z), tube, 0.12f, 1f, 6f);
        BuildTubeSound(parent, new Vector3(Width / 2f, LampHeight, 1.5f), 1f, 7f);

        AudioSource clock = Ambient(parent, "Clock", new Vector3(Width / 2f, 2.22f, Length), Sound("A_Clock"), 0.5f, 1f, 11f);
        clock.GetComponent<AmbientLoop>().pitchSpread = 0f;

        Ambient(parent, "StairDraught", stairs, Sound("A_Wind"), 0.4f, 3f, 17f);

        ReverbZone(parent, "HallReverb", new Vector3(Width / 2f, 1.5f, Length / 2f), AudioReverbPreset.Stoneroom, 6f, 9f);
        ReverbZone(parent, "StairReverb", stairs, AudioReverbPreset.StoneCorridor, 5f, 9f);

        Object.FindAnyObjectByType<ExitGate>().sound = Emitter(parent, "GateSound", new Vector3(Width, 1f, (ExitZMin + ExitZMax) / 2f), Sound("I_Gate"), 0.8f, 2f, 16f);

        var radiator = new GameObject("Radiator").transform;
        radiator.SetParent(parent);
        radiator.position = new Vector3(Width - 0.15f, 0.5f, 1.8f);
        var roomSounds = new GameObject("RoomSounds").AddComponent<SoundScatter>();
        roomSounds.transform.SetParent(parent);
        roomSounds.listener = player.cameraTransform;
        roomSounds.kinds = new[]
        {
            new SoundScatter.Kind { name = "Radiator", clips = Sounds("A_Tick", SoundForge.Ticks), everySeconds = new Vector2(6f, 19f), volume = new Vector2(0.12f, 0.3f), from = radiator },
        };

        // The clerk stamps away behind the glass until the player comes to the window.
        var window = new GameObject("ClerkWindowSound").transform;
        window.SetParent(parent);
        window.position = new Vector3(OpenWindowX, WindowSill, Length + 0.6f);
        var clerkAtWork = new GameObject("ClerkAtWork").AddComponent<SoundScatter>();
        clerkAtWork.transform.SetParent(parent);
        clerkAtWork.listener = player.cameraTransform;
        clerkAtWork.kinds = new[]
        {
            new SoundScatter.Kind { name = "Stamp", clips = new[] { Sound("A_Stamp") }, everySeconds = new Vector2(5f, 14f), volume = new Vector2(0.4f, 0.55f), from = window },
        };
        UnityEventTools.AddBoolPersistentListener(Object.FindAnyObjectByType<TalkSpot>().onTalkStarted, clerkAtWork.gameObject.SetActive, false);
    }
}
