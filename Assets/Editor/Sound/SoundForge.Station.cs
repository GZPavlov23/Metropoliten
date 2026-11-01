using UnityEngine;
using static Dsp;

// The presence of the place: its hum, its air, and the things it does when nobody's looking.
public static partial class SoundForge
{
    public const int Drips = 4, Groans = 3, Ticks = 3, TubeBuzzes = 3, FarSteps = 3;

    static void ForgeStation()
    {
        var rng = new System.Random(2204);

        Write("A_Room", Room(rng));
        Write("A_Tube", TubeHum(rng));
        Write("A_Bulb", BulbHum(rng));
        Write("A_Wind", Wind(rng));
        Write("A_Dark", DarkBreath(rng));
        Write("A_Rumble", DistantRumble(rng));
        Write("A_Intercom", IntercomHum(rng));
        Write("A_Clock", Clock(rng));
        Write("A_Stamp", Stamp(rng));

        for (int i = 0; i < Drips; i++) Write($"A_Drip{i}", Drip(rng));
        for (int i = 0; i < Groans; i++) Write($"A_Groan{i}", Groan(rng, 3f + i * 1.5f));
        for (int i = 0; i < Ticks; i++) Write($"A_Tick{i}", Tick(rng));
        for (int i = 0; i < TubeBuzzes; i++) Write($"A_Buzz{i}", TubeBuzz(rng));
        for (int i = 0; i < FarSteps; i++) Write($"A_FarStep{i}", FarStep(rng));
    }

    // The sound of an underground room with nothing happening in it: air, and the weight above.
    static Stereo Room(System.Random rng)
    {
        const float loop = 40f, overlap = 4f;
        int samples = Samples(loop + overlap);
        var mix = new Stereo(samples);
        foreach (float pan in new[] { -0.7f, 0.7f })
        {
            float[] air = LowPass(HighPass(Pink(rng, samples), 70f), 520f);
            float seed = (float)rng.NextDouble() * 10f;
            Envelope(air, t => 0.7f + 0.3f * Mathf.Sin(Tau * t / 13f + seed) * Mathf.Sin(Tau * t / 7.3f + seed * 2f));
            mix.Add(air, 0, 1f, pan);
        }
        float[] floor = LowPass(Brown(rng, samples), 70f);
        mix.Add(floor, 0, 0.4f, 0f);
        mix.ScaleToRms(-18f);
        return Loop(mix, Samples(loop));
    }

    // Mains hum and its harmonics, with the fizz of an ageing tube on top. Whole numbers of cycles fit the loop.
    static float[] TubeHum(System.Random rng)
    {
        const float loop = 4f;
        int samples = Samples(loop);
        float[] fizz = BandPass(White(rng, samples + Samples(0.5f)), 7200f, 1.1f);
        Envelope(fizz, t => 0.6f + 0.4f * Mathf.Abs(Mathf.Sin(Tau * 50f * t)));
        float[] looped = Loop(fizz, samples);
        Gain(looped, 0.5f);

        float[] levels = { 1f, 0.5f, 0.28f, 0.2f, 0.12f, 0.08f };
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / Rate, sum = 0f;
            for (int h = 0; h < levels.Length; h++) sum += Sine(100f * (h + 1), i) * levels[h];
            looped[i] += sum * (0.9f + 0.1f * Mathf.Sin(Tau * 0.75f * t));
        }
        Normalize(looped, -12f);
        return looped;
    }

    // A tungsten bulb on a poor connection: a low sing, almost nothing.
    static float[] BulbHum(System.Random rng)
    {
        const float loop = 6f;
        int samples = Samples(loop + 1f);
        float[] crackle = HighPass(White(rng, samples), 2500f);
        float next = 0.2f;
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / Rate;
            if (t > next + 0.004f)
            {
                next = t + (float)rng.NextDouble() * 1.4f + 0.2f;
            }
            if (t < next) crackle[i] = 0f;
        }
        float[] looped = Loop(crackle, Samples(loop));
        Gain(looped, 0.4f);
        for (int i = 0; i < looped.Length; i++)
        {
            float t = (float)i / Rate;
            looped[i] += (Sine(100f, i) + 0.3f * Sine(200f, i)) * (0.8f + 0.2f * Mathf.Sin(Tau * 0.5f * t));
        }
        Normalize(looped, -14f);
        return looped;
    }

    // Air moving through a long tube: broad resonances wandering up and down, never settling on a note.
    static float[] Wind(System.Random rng)
    {
        const float loop = 36f, overlap = 6f;
        int samples = Samples(loop + overlap);
        var x = new float[samples];
        float[] centres = { 120f, 210f, 330f };
        for (int r = 0; r < centres.Length; r++)
        {
            float centre = centres[r], rate = 1f / (11f + r * 5.3f), seed = (float)rng.NextDouble() * Tau;
            float[] band = Sweep(Pink(rng, samples), (f, t) => f.BandPass(centre * (1f + 0.3f * Mathf.Sin(Tau * rate * t + seed)), 5f));
            Envelope(band, t => 0.45f + 0.55f * Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Tau * rate * 1.7f * t + seed * 2f), 2f));
            AddInto(x, band, 0, 1f / (1f + r * 0.6f));
        }
        float[] looped = Loop(x, Samples(loop));
        Normalize(looped, -10f);
        return looped;
    }

    // What's down the dark path: slow air, and under it something very nearly like a voice.
    static float[] DarkBreath(System.Random rng)
    {
        const float loop = 34f, overlap = 6f;
        int samples = Samples(loop + overlap);
        var x = new float[samples];

        float[] air = Sweep(Pink(rng, samples), (f, t) => f.BandPass(240f + 90f * Mathf.Sin(Tau * t / 8.5f), 2.2f));
        Envelope(air, t => 0.3f + 0.7f * Mathf.Pow(Mathf.Sin(Mathf.PI * t / 8.5f), 2f));
        AddInto(x, air, 0, 1f);

        // A falling drone with vocal formants, too low and too slow to be anything you could name.
        float phase = 0f;
        var throat = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / Rate;
            phase += (72f - 9f * Mathf.Repeat(t / 12.7f, 1f)) / Rate;
            phase -= Mathf.Floor(phase);
            throat[i] = Saw(phase);
        }
        float[] first = BandPass(throat, 320f, 6f), second = BandPass(throat, 780f, 8f);
        var voice = new float[samples];
        AddInto(voice, first, 0, 1f);
        AddInto(voice, second, 0, 0.5f);
        Envelope(voice, t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Tau * t / 25.4f)), 3f));
        AddInto(x, voice, 0, 0.5f);

        float[] looped = Loop(x, Samples(loop));
        Normalize(looped, -10f);
        return looped;
    }

    // A train somewhere else on the line, when there's only the one train.
    static float[] DistantRumble(System.Random rng)
    {
        const float seconds = 21f;
        int samples = Samples(seconds);
        float[] body = LowPass(Brown(rng, samples), 170f);
        float[] grind = BandPass(Pink(rng, samples), 310f, 1.5f);
        var x = new float[samples];
        AddInto(x, body, 0, 1f);
        AddInto(x, grind, 0, 0.35f);

        // The joints of a far-off track, felt as much as heard.
        for (float t = 3f; t < seconds - 3f; t += 0.86f + (float)rng.NextDouble() * 0.06f)
        {
            Knock(x, Samples(t), 70f, 120f, 0.01f, 0.05f, 0.5f);
            Knock(x, Samples(t + 0.14f), 66f, 110f, 0.01f, 0.05f, 0.35f);
        }

        float[] squeal = BandPass(White(rng, samples), 1160f, 30f);
        Envelope(squeal, t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * (t - 8f) / 7f)), 3f));
        AddInto(x, squeal, 0, 0.5f);

        Envelope(x, t => Mathf.Pow(Mathf.Max(0f, Mathf.Sin(Mathf.PI * t / seconds)), 2.5f));
        return Finish(x, -6f, 0.2f);
    }

    // The line, open and waiting: mains hum squeezed through a tiny speaker, with the odd crackle.
    static float[] IntercomHum(System.Random rng)
    {
        const float loop = 6f;
        int samples = Samples(loop), overlap = Samples(1f);

        float[] noise = BandPass(White(rng, samples + overlap), 1800f, 0.8f);
        Gain(noise, 0.1f);
        for (int c = 0; c < 8; c++) Burst(rng, noise, rng.Next(samples), 900f, 4000f, 0.003f, 0.5f);
        float[] looped = Loop(noise, samples);

        // The hum goes through the speaker's filters for a moment first, so that what's kept is steady.
        var square = new float[samples + overlap];
        for (int i = 0; i < square.Length; i++)
        {
            for (int h = 1; h < 14; h += 2) square[i] += Sine(50f * h, i) / h;
        }
        AddInto(looped, Settled(HighPass(LowPass(square, 2200f), 280f), samples), 0, 1f);
        Normalize(looped, -12f);
        return looped;
    }

    // An office wall clock: tick, and a slightly different tock. The second tock comes a shade late.
    static float[] Clock(System.Random rng)
    {
        const float loop = 2f;
        var x = new float[Samples(loop)];
        Burst(rng, x, 0, 1500f, 7000f, 0.0016f, 1f);
        Knock(x, 0, 2300f, 2300f, 1f, 0.006f, 0.35f);
        Burst(rng, x, Samples(1.02f), 1100f, 5200f, 0.0016f, 0.8f);
        Knock(x, Samples(1.02f), 1700f, 1700f, 1f, 0.006f, 0.3f);
        Normalize(x, -8f);
        return x;
    }

    // A rubber stamp coming down on a counter, behind a window.
    static float[] Stamp(System.Random rng)
    {
        var x = new float[Samples(0.7f)];
        Burst(rng, x, 0, 400f, 2600f, 0.012f, 0.7f);
        Knock(x, 0, 120f, 230f, 0.012f, 0.05f, 1f);
        Knock(x, Samples(0.004f), 310f, 400f, 0.01f, 0.03f, 0.4f);
        Burst(rng, x, Samples(0.2f), 1200f, 6000f, 0.05f, 0.1f);
        return Finish(x, -6f);
    }

    // A drop falling into a shallow puddle: a tiny bubble rising in pitch as it closes.
    static float[] Drip(System.Random rng)
    {
        var x = new float[Samples(0.4f)];
        float hz = 800f + (float)rng.NextDouble() * 700f;
        float phase = 0f;
        for (int i = 0; i < x.Length; i++)
        {
            float t = (float)i / Rate;
            phase += Tau * hz * (1f + Mathf.Min(t / 0.05f, 1f) * 0.8f) / Rate;
            x[i] = Mathf.Sin(phase) * Mathf.Exp(-t / 0.035f);
        }
        Knock(x, Samples(0.03f + (float)rng.NextDouble() * 0.03f), hz * 1.7f, hz * 1.4f, 0.03f, 0.02f, 0.4f);
        Burst(rng, x, 0, 2000f, 6000f, 0.0015f, 0.3f);
        return Finish(x, -6f, 0.05f);
    }

    // Steel moving against steel, under load: slow stick-slip driving the resonances of a girder.
    static float[] Groan(System.Random rng, float seconds)
    {
        int samples = Samples(seconds);
        var pulses = new float[samples];
        float at = 0f, wander = (float)rng.NextDouble() * Tau;
        while (at < seconds)
        {
            float progress = at / seconds;
            float rate = Mathf.Lerp(14f, 44f, 0.5f + 0.5f * Mathf.Sin(Tau * progress * 1.4f + wander));
            pulses[Samples(at) % samples] = 0.6f + (float)rng.NextDouble() * 0.4f;
            at += (1f / rate) * (0.85f + (float)rng.NextDouble() * 0.3f);
        }

        float[] resonances = { 140f, 410f, 980f, 2300f };
        float[] levels = { 1f, 0.8f, 0.4f, 0.12f };
        var x = new float[samples];
        for (int r = 0; r < resonances.Length; r++)
        {
            float centre = resonances[r];
            float[] ring = Sweep(pulses, (f, t) => f.BandPass(centre * (1f + 0.05f * Mathf.Sin(Tau * t / seconds + r)), 28f));
            AddInto(x, ring, 0, levels[r]);
        }
        Envelope(x, t => Smooth(t / (seconds * 0.3f)) * Smooth((seconds - t) / (seconds * 0.4f)));
        return Finish(x, -6f, 0.1f);
    }

    // Metal cooling or warming, somewhere in the wall.
    static float[] Tick(System.Random rng)
    {
        var x = new float[Samples(0.5f)];
        Ping(x, 0, 620f + (float)rng.NextDouble() * 900f, 0.05f, 1f);
        Burst(rng, x, 0, 1800f, 8000f, 0.001f, 0.6f);
        return Finish(x, -6f, 0.05f);
    }

    // One footstep, some way down a passage nobody else should be in. Nearly all of it is the echo.
    static float[] FarStep(System.Random rng)
    {
        const float seconds = 3.2f;
        var dry = new Stereo(Samples(seconds));
        var step = new float[Samples(0.32f)];
        float body = 90f + (float)rng.NextDouble() * 25f;
        Knock(step, 0, body, body * 2.2f, 0.012f, 0.05f, 1f);
        Burst(rng, step, 0, 300f, 1800f, 0.008f, 0.5f);
        dry.Add(step, 0, 1f, 0f);

        Stereo wet = Reverb(dry, 2.6f, 0.65f);
        var x = new float[dry.Length];
        for (int i = 0; i < x.Length; i++) x[i] = dry.Left[i] * 0.3f + (wet.Left[i] + wet.Right[i]) * 1.6f;
        return Finish(LowPass(x, 1500f), -6f, 0.2f);
    }

    // A failing tube trying to strike: a stutter of arcs.
    static float[] TubeBuzz(System.Random rng)
    {
        const float seconds = 0.6f;
        int samples = Samples(seconds);
        var x = new float[samples];
        float[] hum = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float sum = 0f;
            for (int h = 1; h <= 8; h++) sum += Sine(100f * h, i) / h;
            hum[i] = sum;
        }

        for (int b = 0; b < 5; b++)
        {
            int at = rng.Next(samples - Samples(0.08f)), length = Samples(0.02f + (float)rng.NextDouble() * 0.05f);
            for (int i = 0; i < length; i++) x[at + i] += hum[at + i] * Mathf.Sin(Mathf.PI * i / length);
            Burst(rng, x, at, 2500f, 9000f, 0.002f, 0.8f);
        }
        return Finish(x, -6f);
    }
}
