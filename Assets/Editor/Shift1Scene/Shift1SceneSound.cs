using UnityEngine;
using static SceneForge;

// What the station sounds like when nobody's talking: the hum and the air of the place, the small sounds
// it makes by itself, whatever is up the dark path, and the train waiting at the platform.
public static partial class Shift1SceneBuilder
{
    static void BuildSound(FirstPersonController player, TrainDrive drive)
    {
        Transform parent = new GameObject("Sound").transform;
        AudioClip tube = Sound("A_Tube"), wind = Sound("A_Wind");

        RoomTone(parent, 0.3f);
        BuildPlayerSound(player, SoundForge.WalkSteps, SoundForge.RunSteps);

        // The lamps, each only heard from nearly underneath it.
        foreach (float z in new[] { 3.5f, 10.5f, 24.5f, 31.5f }) Ambient(parent, "Tube", new Vector3(LampX, LampY, z), tube, 0.16f, 1.5f, 8f);
        foreach (float z in new[] { 10.5f, 17.5f, 31.5f, 38.5f }) Ambient(parent, "Tube", new Vector3(LampX, LampY, NextHallZ + z), tube, 0.16f, 1.5f, 8f);
        BuildTubeSound(parent, new Vector3(LampX, LampY, 38.5f), 1.5f, 10f);
        foreach (float z in new[] { -3.2f, -9f }) Ambient(parent, "Bulb", new Vector3(LampX, PassageTop - 0.3f, z), Sound("A_Bulb"), 0.14f, 1f, 6f);

        // Air moving in the tunnels at either end of the hall, and whatever is up the dark path.
        Ambient(parent, "WindNorth", new Vector3(TrackX, 1f, Length + 5f), wind, 0.45f, 4f, 34f);
        Ambient(parent, "WindSouth", new Vector3(TrackX, 1f, -5f), wind, 0.3f, 4f, 26f);
        AudioSource dark = Ambient(parent, "DarkPath", new Vector3(LampX, 1.2f, PassageBarrierZ - 8f), Sound("A_Dark"), 0.5f, 3f, 30f);
        dark.GetComponent<AmbientLoop>().heardThroughHush = true;

        // The intercom: the line itself up close, and the call, which carries well down the passage.
        var intercom = new Vector3(Width - 0.1f, 1.46f, IntercomZ);
        Ambient(parent, "IntercomLine", intercom, Sound("A_Intercom"), 0.22f, 0.6f, 4.5f);
        Object.FindAnyObjectByType<CallLamp>().buzzer = Emitter(parent, "IntercomCall", intercom, Sound("I_Call"), 0.4f, 1.5f, 24f);

        ReverbZone(parent, "HallReverb", new Vector3(Width / 2f, 2f, Length / 2f), AudioReverbPreset.StoneCorridor, 24f, 32f);
        ReverbZone(parent, "NextHallReverb", new Vector3(Width / 2f, 2f, NextHallZ + Length / 2f), AudioReverbPreset.StoneCorridor, 24f, 32f);
        ReverbZone(parent, "PassageReverb", new Vector3(LampX, 1.5f, PassageBarrierZ + 4f), AudioReverbPreset.StoneCorridor, 9f, 16f);
        ReverbZone(parent, "RunReverb", new Vector3(TrackX, 1f, Length + RunLength / 2f), AudioReverbPreset.SewerPipe, RunLength / 2f - 8f, RunLength / 2f + 4f);

        BuildTrainSound(drive, BuildStationSounds(parent, player));
        BuildFarStep(parent);
    }

    static SoundScatter BuildStationSounds(Transform parent, FirstPersonController player)
    {
        var farTrain = new GameObject("FarTrain").transform;
        farTrain.SetParent(parent);
        farTrain.position = new Vector3(TrackX, 0f, Length + 8f);

        var go = new GameObject("StationSounds");
        go.transform.SetParent(parent);
        var scatter = go.AddComponent<SoundScatter>();
        scatter.listener = player.cameraTransform;
        scatter.kinds = new[]
        {
            new SoundScatter.Kind { name = "Drips", clips = Sounds("A_Drip", SoundForge.Drips), everySeconds = new Vector2(3f, 11f), distance = new Vector2(4f, 15f), volume = new Vector2(0.15f, 0.4f) },
            new SoundScatter.Kind { name = "Ticks", clips = Sounds("A_Tick", SoundForge.Ticks), everySeconds = new Vector2(9f, 26f), distance = new Vector2(6f, 20f), volume = new Vector2(0.15f, 0.35f), behind = 0.3f },
            new SoundScatter.Kind { name = "Groans", clips = Sounds("A_Groan", SoundForge.Groans), everySeconds = new Vector2(35f, 80f), distance = new Vector2(12f, 28f), volume = new Vector2(0.4f, 0.7f), reach = 8f, behind = 0.85f },

            // A train somewhere up the line, on a line that only has the one.
            new SoundScatter.Kind { name = "FarTrain", clips = new[] { Sound("A_Rumble") }, everySeconds = new Vector2(70f, 130f), volume = new Vector2(0.6f, 0.8f), reach = 30f, from = farTrain, hushSeconds = 10f },
        };
        return scatter;
    }

    static void BuildTrainSound(TrainDrive drive, SoundScatter stationSounds)
    {
        var sound = train.gameObject.AddComponent<TrainSound>();
        sound.drive = drive;
        sound.stationSounds = stationSounds;

        // Standing at the platform it's a machine over there. The rest only sounds with the player aboard.
        sound.idle = Emitter(train, "Idle", new Vector3(TrackX, 0f, 27f), Sound("T_Idle"), 0f, 7f, 34f);
        sound.idle.loop = true;
        sound.idle.playOnAwake = true;
        sound.motor = AllAround("Motor", Sound("T_Motor"));
        sound.wheels = AllAround("Wheels", Sound("T_Wheels"));
        sound.brake = AllAround("Brake", Sound("T_Brake"));
        sound.effects = AllAround("Effects", null);
        sound.effects.loop = false;
        sound.effects.volume = 1f;

        sound.joints = Sounds("T_Joint", SoundForge.Joints);
        sound.airRelease = Sound("T_Air");
        sound.tripBell = Sound("T_Bell");

        train.gameObject.AddComponent<StepSurface>().clips = Sounds("F_Cab", SoundForge.CabSteps);
    }

    static AudioSource AllAround(string name, AudioClip clip)
    {
        AudioSource source = Emitter(train, name, new Vector3(TrackX, 0f, SeatZ), clip, 0f, 1f, 2f);
        source.spatialBlend = 0f;
        source.reverbZoneMix = 0.35f;
        source.loop = true;
        source.playOnAwake = true;
        return source;
    }

    // The player who goes right to the end of the dark path gets an answer from further up it.
    static void BuildFarStep(Transform parent)
    {
        var go = new GameObject("FarStepZone");
        go.transform.SetParent(parent);
        go.transform.position = new Vector3(LampX, 1f, PassageBarrierZ + 1.4f);
        var box = go.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(DoorXMax - DoorXMin + 0.4f, 3f, 2f);

        var zone = go.AddComponent<SoundZone>();
        zone.source = Emitter(parent, "FarStep", new Vector3(LampX, 0.5f, PassageBarrierZ - 14f), null, 0.9f, 8f, 40f);
        zone.clips = Sounds("A_FarStep", SoundForge.FarSteps);
    }
}
