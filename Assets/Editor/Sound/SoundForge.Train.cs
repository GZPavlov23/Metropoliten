using UnityEngine;
using static Dsp;

// The train. Everything that follows the speed is a loop whose pitch and level the game moves.
public static partial class SoundForge
{
    public const int Joints = 3;

    static void ForgeTrain()
    {
        var rng = new System.Random(4411);

        Write("T_Idle", Idle(rng));
        Write("T_Motor", Motor(rng));
        Write("T_Wheels", Wheels(rng));
        Write("T_Brake", Brake(rng));
        Write("T_Air", AirRelease(rng));
        Write("T_Bell", TripBell(rng));
        for (int i = 0; i < Joints; i++) Write($"T_Joint{i}", RailJoint(rng));
    }

    // A parked train: its blowers and transformers, and the compressor grumbling in the background.
    static float[] Idle(System.Random rng)
    {
        const float loop = 8f;
        int samples = Samples(loop), overlap = Samples(1f);

        float[] blower = BandPass(Pink(rng, samples + overlap), 420f, 0.8f);
        Envelope(blower, t => 0.85f + 0.15f * Mathf.Sin(Tau * 6f * t));
        Gain(blower, 1.4f);
        AddInto(blower, LowPass(Brown(rng, samples + overlap), 110f), 0, 0.8f);

        float[] looped = Loop(blower, samples);
        for (int i = 0; i < samples; i++) looped[i] += Sine(100f, i) * 0.5f + Sine(200f, i) * 0.25f + Sine(300f, i) * 0.12f;
        Normalize(looped, -10f);
        return looped;
    }

    // Traction motors and their gears. Pitched up and down in the game with the speed of the train;
    // the gear partials sit off the motor's harmonics, which is what gives a train its whine.
    static float[] Motor(System.Random rng)
    {
        const float loop = 4f, root = 110f;
        int samples = Samples(loop);
        float[] ratios = { 1f, 2f, 3f, 4f, 5f, 6f, 8f, 7.3f, 9.65f, 13.2f };
        float[] levels = { 1f, 0.6f, 0.5f, 0.3f, 0.25f, 0.15f, 0.1f, 0.55f, 0.35f, 0.15f };
        var x = new float[samples];
        for (int p = 0; p < ratios.Length; p++)
        {
            // Whole numbers of cycles to the loop, so it joins up without needing a crossfade.
            float hz = Fit(root * ratios[p], loop);
            for (int i = 0; i < samples; i++) x[i] += Sine(hz, i) * levels[p];
        }
        Normalize(x, -8f);
        return x;
    }

    // Steel wheels on steel rail in a tunnel: a wide roar with the rail's ring in it.
    static float[] Wheels(System.Random rng)
    {
        const float loop = 8f;
        int samples = Samples(loop + 1f);
        var x = new float[samples];

        float[] roar = LowPass(Brown(rng, samples), 260f);
        Envelope(roar, t => 0.82f + 0.18f * Mathf.Sin(Tau * 9f * t) * Mathf.Sin(Tau * 1.3f * t));
        AddInto(x, roar, 0, 1f);

        float[] tunnel = BandPass(Pink(rng, samples), 520f, 0.7f);
        AddInto(x, tunnel, 0, 1.3f);
        float[] rail = BandPass(Pink(rng, samples), 1150f, 2.2f);
        AddInto(x, rail, 0, 0.6f);
        float[] ring = BandPass(Pink(rng, samples), 2100f, 7f);
        AddInto(x, ring, 0, 0.3f);

        float[] looped = Loop(x, Samples(loop));
        Normalize(looped, -7f);
        return looped;
    }

    // Brake blocks on the wheels: a grinding hiss and a squeal that wanders.
    static float[] Brake(System.Random rng)
    {
        const float loop = 5f;
        int samples = Samples(loop + 1f);
        var x = new float[samples];
        float[] grind = BandPass(White(rng, samples), 2600f, 1f);
        AddInto(x, grind, 0, 0.25f);
        foreach ((float hz, float gain) in new[] { (2380f, 1f), (3170f, 0.6f), (4510f, 0.3f) })
        {
            float centre = hz;
            float[] squeal = Sweep(White(rng, samples), (f, t) => f.BandPass(centre * (1f + 0.02f * Mathf.Sin(Tau * t / 1.7f)), 60f));
            Envelope(squeal, t => 0.5f + 0.5f * Mathf.Sin(Tau * t / 2.3f + centre));
            AddInto(x, squeal, 0, gain * 2f);
        }
        float[] looped = Loop(x, Samples(loop));
        Normalize(looped, -10f);
        return looped;
    }

    // Air letting go as the brakes come off.
    static float[] AirRelease(System.Random rng)
    {
        const float seconds = 1.4f;
        float[] x = Sweep(White(rng, Samples(seconds)), (f, t) => f.BandPass(Mathf.Lerp(5200f, 2600f, t / seconds), 0.9f));
        Envelope(x, t => Mathf.Exp(-t / 0.4f) * Smooth(t / 0.015f));
        return Finish(x, -10f, 0.1f);
    }

    // The joint between two lengths of rail, going under one wheel.
    static float[] RailJoint(System.Random rng)
    {
        var x = new float[Samples(0.25f)];
        float body = 120f + (float)rng.NextDouble() * 40f;
        Knock(x, 0, body, body * 2.5f, 0.008f, 0.04f, 1f);
        Burst(rng, x, 0, 500f, 3400f, 0.009f, 3.2f);
        Ping(x, 0, 760f + (float)rng.NextDouble() * 200f, 0.035f, 0.8f);
        return Finish(x, -5f, 0.04f);
    }

    // The alarm in the cab when the trip takes over: an electric bell, flat and insistent.
    static float[] TripBell(System.Random rng)
    {
        const float seconds = 2.2f;
        var x = new float[Samples(seconds)];
        for (float t = 0f; t < seconds - 0.2f; t += 0.055f) Ping(x, Samples(t), 1320f, 0.04f, 0.5f);
        return Finish(x, -9f, 0.1f);
    }
}
