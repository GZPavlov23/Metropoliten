using System;
using UnityEngine;
using static Dsp;

// What Shift 2 adds: the closed station's music, the cameras, and the things the player gets their hands on.
public static partial class SoundForge
{
    static void ForgeShift2()
    {
        var rng = new System.Random(5206);

        Write("M_Hollow", Hollow());
        Write("A_Servo", Servo(rng));
        Write("I_Lever", Lever(rng));
        Write("I_Locker", LockerDoor(rng));
        Write("I_Door", HeavyDoor(rng));
        Write("I_Lamp", LampClick(rng));
        Write("T_DoorOpen", TrainDoor(rng, true));
        Write("T_DoorClose", TrainDoor(rng, false));
        Write("I_Stamp", TimeStamp(rng));
        Write("I_PhoneRing", PhoneRing());
        Write("I_PhoneUp", Handset(rng, true));
        Write("I_PhoneDown", Handset(rng, false));
        Write("A_Morning", Morning());
        Write("I_Arc", Arc(rng));
        Write("S_Breath", CloseBreath(rng));
        Write("S_Scare", Scare());
    }

    // Contacts parting under load: a crack, and a second of something trying to carry on across the gap.
    static float[] Arc(System.Random rng)
    {
        var x = new float[Samples(0.7f)];
        Burst(rng, x, 0, 900f, 9000f, 0.012f, 1f);
        Burst(rng, x, Samples(0.05f), 1500f, 9000f, 0.008f, 0.7f);
        float[] buzz = new float[Samples(0.42f)];
        for (int i = 0; i < buzz.Length; i++)
        {
            float saw = Saw(Mathf.Repeat((float)i / Rate * 100f, 1f));
            buzz[i] = saw * (rng.NextDouble() < 0.82 ? 1f : 0f);
        }
        buzz = HighPass(buzz, 500f);
        Envelope(buzz, t => Mathf.Exp(-t * 7f));
        AddInto(x, buzz, Samples(0.02f), 0.5f);
        Knock(x, Samples(0.06f), 70f, 160f, 0.02f, 0.14f, 0.7f);
        return Finish(x, -5f, 0.06f);
    }

    // Someone letting a breath out, about a foot from the back of your neck.
    static float[] CloseBreath(System.Random rng)
    {
        const float seconds = 1.9f;
        float[] air = Sweep(Pink(rng, Samples(seconds)), (f, t) => f.BandPass(Mathf.Lerp(900f, 420f, t / seconds), 0.9f));
        Envelope(air, t => Smooth(t / 0.3f) * Smooth((seconds - t) / 1.1f));
        return Finish(air, -12f, 0.1f);
    }

    // The sound the whole game has been keeping back: wheels screaming on a curve, a bow dragged across every string
    // at once, and a blow low enough to feel. It's over in a moment, and it leaves nothing behind it.
    static Stereo Scare()
    {
        const float seconds = 3.4f;
        int samples = Samples(seconds);
        var rng = new System.Random(1404);

        // Steel on steel, sliding upward.
        float[] partials = { 1630f, 2210f, 2970f, 3890f, 5140f };
        var screech = new float[samples];
        for (int p = 0; p < partials.Length; p++)
        {
            float phase = 0f, level = 1f / (1f + p * 0.5f), wobble = 5.3f + p * 1.7f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                phase += Tau * partials[p] * (1f + 0.07f * Mathf.Clamp01(t / 0.5f) + 0.006f * Mathf.Sin(Tau * wobble * t)) / Rate;
                screech[i] += Mathf.Sin(phase) * level;
            }
        }
        for (int i = 0; i < samples; i++) screech[i] = (float)Math.Tanh(screech[i] * 1.8f);
        Envelope(screech, t => Smooth(t / 0.006f) * Mathf.Exp(-t * 3.2f));

        // Strings, a semitone apart, all of them too high.
        var bow = new float[samples];
        foreach (float hz in new[] { 466.2f, 493.9f, 698.5f, 739.99f, 1046.5f, 1108.7f })
        {
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                bow[i] += Saw(Mathf.Repeat(hz * t * (1f + 0.004f * Mathf.Sin(Tau * 6.1f * t)), 1f));
            }
        }
        bow = BandPass(bow, 1500f, 0.6f);
        Envelope(bow, t => Smooth(t / 0.01f) * Mathf.Exp(-t * 2.6f));

        var blow = new float[samples];
        Knock(blow, 0, 42f, 140f, 0.03f, 0.6f, 1f);
        Burst(rng, blow, 0, 200f, 6000f, 0.05f, 0.9f);

        var mix = new Stereo(samples);
        mix.Add(screech, 0, 0.5f, -0.25f);
        mix.Add(screech, Samples(0.012f), 0.42f, 0.3f);
        mix.Add(bow, 0, 0.5f, 0.2f);
        mix.Add(bow, Samples(0.006f), 0.4f, -0.3f);
        mix.Add(blow, 0, 0.9f, 0f);
        mix.Add(Reverb(mix, 2.2f, 0.45f), 0.3f);
        SoftLimit(mix.Left, -2f);
        SoftLimit(mix.Right, -2f);
        return mix;
    }

    // The air going out, the doors running back, and the thump of them finishing. Closing, the two notes go first.
    static float[] TrainDoor(System.Random rng, bool opening)
    {
        const float seconds = 2.6f;
        var x = new float[Samples(seconds)];
        float start = 0.05f;
        if (!opening)
        {
            foreach ((float at, float hz) in new[] { (0.04f, 880f), (0.34f, 660f) })
            {
                float[] note = new float[Samples(0.5f)];
                for (int i = 0; i < note.Length; i++) note[i] = Sine(hz, i) * 0.6f + Sine(hz * 2f, i) * 0.2f;
                Envelope(note, t => Smooth(t / 0.01f) * Mathf.Exp(-t * 6f));
                AddInto(x, note, Samples(at), 0.5f);
            }
            start = 0.8f;
        }

        float[] hiss = HighPass(Pink(rng, Samples(0.5f)), 1800f);
        Envelope(hiss, t => Smooth(t / 0.02f) * Smooth((0.5f - t) / 0.3f));
        AddInto(x, hiss, Samples(start), 0.35f);

        float[] run = LowPass(Pink(rng, Samples(1.1f)), 420f);
        for (int i = 0; i < run.Length; i++) run[i] += Sine(92f, i) * 0.2f;
        Envelope(run, t => Smooth(t / 0.12f) * Smooth((1.1f - t) / 0.15f));
        AddInto(x, run, Samples(start + 0.1f), 0.5f);

        Knock(x, Samples(start + 1.2f), 104f, 200f, 0.012f, 0.1f, 0.9f);
        Ping(x, Samples(start + 1.2f), 520f, 0.05f, 0.25f);
        return Finish(x, -7f, 0.1f);
    }

    // The old clock taking a card: the bell, the bite of the stamp and the card coming back.
    static float[] TimeStamp(System.Random rng)
    {
        var x = new float[Samples(0.9f)];
        Ping(x, 0, 1760f, 0.16f, 0.5f);
        Knock(x, Samples(0.12f), 180f, 420f, 0.006f, 0.05f, 1f);
        Burst(rng, x, Samples(0.12f), 900f, 5000f, 0.012f, 0.7f);
        Knock(x, Samples(0.3f), 240f, 500f, 0.004f, 0.03f, 0.5f);
        return Finish(x, -7f, 0.06f);
    }

    // A real bell in a metal hood: two bursts, and the rest of the time nothing.
    static float[] PhoneRing()
    {
        const float seconds = 2f;
        var x = new float[Samples(seconds)];
        foreach ((float from, float to) in new[] { (0.02f, 0.42f), (0.62f, 1.02f) })
        {
            int length = Samples(to - from);
            var burst = new float[length];
            for (int i = 0; i < length; i++)
            {
                float t = (float)i / Rate, clapper = Mathf.Sign(Mathf.Sin(t * Tau * 22f));
                burst[i] = (Sine(1120f, i) + Sine(1693f, i) * 0.6f + Sine(2410f, i) * 0.3f) * (0.72f + 0.28f * clapper);
            }
            Envelope(burst, t => Smooth(t / 0.01f) * Smooth((to - from - t) / 0.04f));
            AddInto(x, burst, Samples(from), 0.4f);
        }
        return Finish(x, -9f, 0.2f);
    }

    static float[] Handset(System.Random rng, bool lifting)
    {
        var x = new float[Samples(0.5f)];
        Knock(x, 0, 340f, 700f, 0.004f, 0.03f, 0.8f);
        Burst(rng, x, 0, 700f, 4000f, 0.006f, 0.5f);
        Knock(x, Samples(lifting ? 0.14f : 0.08f), 220f, 420f, 0.004f, 0.02f, 0.5f);
        if (lifting)
        {
            float[] line = LowPass(White(rng, Samples(0.3f)), 2400f);
            Envelope(line, t => Smooth(t / 0.05f) * Smooth((0.3f - t) / 0.1f));
            AddInto(x, line, Samples(0.18f), 0.12f);
        }
        return Finish(x, -8f, 0.04f);
    }

    // Wide open air on the edge of town: wind low down, a dog a very long way off, and the depot shunting.
    static Stereo Morning()
    {
        const float loop = 58f, overlap = 6f, total = loop + overlap;
        int samples = Samples(total);
        var rng = new System.Random(7704);

        var air = new Stereo(samples);
        air.Add(LowPass(Pink(rng, samples), 900f), 0, 1f, -0.4f);
        air.Add(LowPass(Pink(rng, samples), 900f), 0, 1f, 0.4f);
        Action<Biquad, float> gust = (f, t) => f.LowPass(420f + 340f * (0.5f + 0.5f * Mathf.Sin(Tau * t / 19f)), 0.7f);
        var windy = new Stereo(samples);
        Array.Copy(Sweep(air.Left, gust), windy.Left, samples);
        Array.Copy(Sweep(air.Right, gust), windy.Right, samples);
        windy.ScaleToRms(-30f);

        var far = new Stereo(samples);
        foreach ((float at, float pan) in new[] { (7f, -0.6f), (8.1f, -0.6f), (9.4f, -0.6f), (39f, 0.5f), (40.2f, 0.5f) })
        {
            var bark = new float[Samples(0.26f)];
            for (int i = 0; i < bark.Length; i++) bark[i] = Saw(Mathf.Repeat((float)i / Rate * 420f, 1f)) * 0.6f;
            bark = BandPass(bark, 720f, 1.2f);
            Envelope(bark, t => Smooth(t / 0.015f) * Mathf.Exp(-t * 11f));
            far.Add(bark, Samples(at), 0.11f, pan);
        }
        foreach (float at in new[] { 22f, 22.7f, 23.9f })
        {
            var clank = new float[Samples(1.4f)];
            Knock(clank, 0, 140f, 300f, 0.01f, 0.12f, 1f);
            Ping(clank, 0, 460f, 0.3f, 0.5f);
            far.Add(clank, Samples(at), 0.09f, -0.75f);
        }

        var mix = new Stereo(samples);
        mix.Add(windy, 1f);
        mix.Add(far, 0.5f);
        mix.Add(Reverb(far, 4f, 0.5f), 0.9f);

        Stereo looped = Loop(mix, Samples(loop));
        SoftLimit(looped.Left, -8f);
        SoftLimit(looped.Right, -8f);
        return looped;
    }

    // The station's low note with nearly everything taken away from it, and metal moving somewhere a long way off.
    static Stereo Hollow()
    {
        const float loop = 74f, overlap = 8f, total = loop + overlap;
        int samples = Samples(total);
        var rng = new System.Random(1961);

        var pads = new Stereo(samples);
        Pad(pads, rng, B1, 0f, total, 0.1f, 1f, -0.2f);
        Pad(pads, rng, C3 / 2f, 26f, 34f, 12f, 0.5f, 0.35f);
        Action<Biquad, float> dark = (f, t) => f.LowPass(150f + 70f * (0.5f + 0.5f * Mathf.Sin(Tau * t / 53f)), 0.9f);
        var low = new Stereo(samples);
        Array.Copy(Sweep(pads.Left, dark), low.Left, samples);
        Array.Copy(Sweep(pads.Right, dark), low.Right, samples);
        low.ScaleToRms(-27f);

        var far = new Stereo(samples);
        far.Add(low, 1f);

        foreach ((float at, float hz, float seconds, float pan, float gain) in new[] { (9f, 118f, 17f, 0.7f, 0.1f), (41f, 173f, 21f, -0.65f, 0.09f), (60f, 92f, 12f, 0.2f, 0.11f) })
        {
            float[] metal = BowedMetal(rng, hz, seconds);
            Normalize(metal, 0f);
            far.Add(metal, Samples(at), gain, pan);
        }

        var air = new Stereo(samples);
        air.Add(HighPass(Breathing(rng, samples, new[] { 30f, 37.5f }), 160f), 0, 1f, -0.3f);
        air.ScaleToRms(-44f);
        far.Add(air, 1f);

        var mix = new Stereo(samples);
        mix.Add(far, 0.7f);
        mix.Add(Reverb(far, 6f, 0.5f), 1.2f);

        Stereo looped = Loop(mix, Samples(loop));
        float[] sub = Sub(loop, B1 * 0.665f, 0.21f, 2f);
        Gain(sub, Db(-27f) / Rms(sub));
        looped.Add(sub, 0, 1f, 0f);

        SoftLimit(looped.Left, -6f);
        SoftLimit(looped.Right, -6f);
        return looped;
    }

    // A small motor and its gears, turning something slowly.
    static float[] Servo(System.Random rng)
    {
        const float loop = 2f;
        int samples = Samples(loop);
        var x = new float[samples];
        foreach ((float hz, float level) in new[] { (312f, 1f), (624f, 0.55f), (1093f, 0.4f), (1877f, 0.22f) })
        {
            float fitted = Fit(hz, loop);
            for (int i = 0; i < samples; i++) x[i] += Sine(fitted, i) * level * (0.8f + 0.2f * Sine(Fit(23f, loop), i));
        }
        float[] hiss = Loop(BandPass(White(rng, samples + Samples(0.4f)), 2600f, 1.2f), samples);
        AddInto(x, hiss, 0, 0.5f);
        Normalize(x, -10f);
        return x;
    }

    // A heavy handle coming over: the weight of it, then a relay dropping out somewhere in the cabinet.
    static float[] Lever(System.Random rng)
    {
        var x = new float[Samples(1.3f)];
        float[] drag = LowPass(Pink(rng, Samples(0.5f)), 700f);
        Envelope(drag, t => Smooth(t / 0.1f) * Smooth((0.5f - t) / 0.1f));
        AddInto(x, drag, 0, 0.5f);

        Knock(x, Samples(0.5f), 96f, 190f, 0.012f, 0.09f, 1f);
        Burst(rng, x, Samples(0.5f), 400f, 3000f, 0.01f, 0.8f);
        Ping(x, Samples(0.5f), 420f, 0.1f, 0.4f);
        Ping(x, Samples(0.86f), 1180f, 0.03f, 0.3f);
        Burst(rng, x, Samples(0.86f), 1500f, 6000f, 0.003f, 0.5f);
        return Finish(x, -6f, 0.1f);
    }

    // A thin steel door, its hinge, and the rattle of it settling.
    static float[] LockerDoor(System.Random rng)
    {
        var x = new float[Samples(0.8f)];
        float[] hinge = Groan(rng, 0.42f);
        AddInto(x, hinge, 0, 0.3f);
        Ping(x, Samples(0.02f), 310f, 0.11f, 0.8f);
        Ping(x, Samples(0.44f), 470f, 0.07f, 0.6f);
        Ping(x, Samples(0.5f), 640f, 0.04f, 0.3f);
        Knock(x, Samples(0.44f), 120f, 210f, 0.01f, 0.04f, 0.6f);
        return Finish(x, -7f, 0.06f);
    }

    // A steel door that hasn't been opened in a while.
    static float[] HeavyDoor(System.Random rng)
    {
        const float seconds = 1.5f;
        var x = new float[Samples(seconds)];
        Knock(x, 0, 150f, 260f, 0.01f, 0.03f, 0.7f);
        Ping(x, 0, 640f, 0.03f, 0.4f);

        float[] hinge = Groan(rng, 1.05f);
        AddInto(x, hinge, Samples(0.04f), 0.5f);
        Knock(x, Samples(1.1f), 84f, 150f, 0.012f, 0.08f, 0.9f);
        return Finish(x, -6f, 0.1f);
    }

    static float[] LampClick(System.Random rng)
    {
        var x = new float[Samples(0.09f)];
        Burst(rng, x, 0, 1500f, 7000f, 0.0016f, 1f);
        Knock(x, Samples(0.018f), 980f, 1400f, 0.003f, 0.006f, 0.4f);
        return Finish(x, -15f, 0.01f);
    }
}
