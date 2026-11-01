using System;
using UnityEngine;
using static Dsp;

// The score. There's no tune in it to hold on to: low notes drift against each other, things breathe
// somewhere under the floor, and just as it seems about to settle, it doesn't.
public static partial class SoundForge
{
    const float E1 = 41.2f, E2 = 82.41f, F2 = 87.31f, B1 = 61.74f, Bb2 = 116.54f, Eb3 = 155.56f, C3 = 130.81f;

    static void ForgeScore()
    {
        Write("M_Dread", Dread());
        Write("M_Deep", Deep());
        Write("S_Cold", Cold());
        Write("S_Low", Low());
    }

    // A swarm of detuned saws around one note, fading slowly in and out.
    static void Pad(Stereo bus, System.Random rng, float hz, float start, float length, float fade, float gain, float pan)
    {
        const int voices = 5;
        int from = Samples(start), samples = Samples(length);
        for (int v = 0; v < voices; v++)
        {
            float detune = 1f + ((float)rng.NextDouble() - 0.5f) * 0.008f;
            float drift = (float)rng.NextDouble() * 0.11f + 0.03f, driftPhase = (float)rng.NextDouble() * Tau;
            float phase = (float)rng.NextDouble();
            var voice = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                phase += hz * detune * (1f + 0.0016f * Mathf.Sin(Tau * drift * t + driftPhase)) / Rate;
                phase -= Mathf.Floor(phase);
                voice[i] = Saw(phase);
            }
            Envelope(voice, t => Smooth(t / fade) * Smooth((length - t) / fade));
            bus.Add(voice, from, gain / voices, Mathf.Clamp(pan + (v - 2) * 0.22f, -1f, 1f));
        }
    }

    // The bottom of the mix: two tones close enough to beat against each other. Everything repeats
    // exactly over the length of the loop, so this goes in after the crossfade instead of through it.
    static float[] Sub(float loop, float hz, float beatHz, float swellsPerLoop)
    {
        int samples = Samples(loop);
        float low = Fit(hz, loop), high = Fit(hz + beatHz, loop);
        var x = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / samples;
            float tone = Sine(low, i) + 0.7f * Sine(high, i);
            float swell = 0.65f + 0.35f * Mathf.Sin(Tau * t * swellsPerLoop);

            // Rounded off, which adds the harmonics that let small speakers hear it at all.
            x[i] = (float)System.Math.Tanh(tone * 1.4f) * swell;
        }
        return x;
    }

    // Something breathing, a long way off: in, a held moment, out, and then a gap you start to wait through.
    static float[] Breathing(System.Random rng, int samples, float[] starts)
    {
        var x = new float[samples];
        foreach (float start in starts)
        {
            const float inhale = 2.4f, hold = 0.5f, exhale = 3.2f, total = inhale + hold + exhale;
            float[] air = Sweep(Pink(rng, Samples(total)), (f, t) =>
            {
                float rising = Mathf.Clamp01(t / inhale);
                float centre = t < inhale ? Mathf.Lerp(300f, 620f, rising) : Mathf.Lerp(560f, 260f, (t - inhale) / (hold + exhale));
                f.BandPass(centre, 1.3f);
            });
            Envelope(air, t =>
            {
                if (t < inhale) return Mathf.Sin(t / inhale * Mathf.PI / 2f);
                if (t < inhale + hold) return 0.15f * (1f - (t - inhale) / hold);
                float out0 = (t - inhale - hold) / exhale;
                return Mathf.Sin(out0 * Mathf.PI) * 0.8f;
            });
            AddInto(x, air, Samples(start), 1f);
        }
        return x;
    }

    // A sheet of metal somewhere being bowed: partials that never line up, each swelling in its own time.
    static float[] BowedMetal(System.Random rng, float hz, float seconds)
    {
        float[] ratios = { 1f, 1.59f, 2.14f, 2.83f, 3.71f, 4.66f, 5.92f };
        int samples = Samples(seconds);
        var x = new float[samples];
        for (int p = 0; p < ratios.Length; p++)
        {
            float partial = hz * ratios[p], level = 1f / (1f + p * 0.8f);
            float rise = (float)rng.NextDouble() * seconds * 0.3f + seconds * 0.25f;
            float wobble = (float)rng.NextDouble() * 3f + 2f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / Rate;
                float envelope = Smooth(t / rise) * Smooth((seconds - t) / (seconds - rise));
                x[i] += Mathf.Sin(Tau * partial * t + 0.6f * Mathf.Sin(Tau * wobble * t * 0.1f)) * envelope * level;
            }
        }
        return x;
    }

    // The sound of a tinnitus you didn't have until now: high, thin and never quite steady.
    static float[] HighThin(int samples, float seconds)
    {
        float[] tones = { 3120f, 3341f, 4187f };
        var x = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / Rate;
            float sum = 0f;
            for (int p = 0; p < tones.Length; p++) sum += Sine(tones[p], i) * (0.6f + 0.4f * Mathf.Sin(Tau * t / (7f + p * 3.1f)));
            x[i] = sum * Smooth(t / (seconds * 0.4f)) * Smooth((seconds - t) / (seconds * 0.4f));
        }
        return x;
    }

    static float[] Undertow(System.Random rng, int samples)
    {
        float[] rumble = LowPass(Brown(rng, samples), 90f);
        Envelope(rumble, t => 0.6f + 0.4f * Mathf.Sin(Tau * t / 23f) * Mathf.Sin(Tau * t / 31f));
        return rumble;
    }

    static Stereo Dread()
    {
        const float loop = 96f, overlap = 8f, total = loop + overlap;
        int samples = Samples(total);
        var rng = new System.Random(1931);

        // E holds throughout. The tritone leans in, then the semitone starts to grind against it,
        // and the major seventh never gets the resolution it's asking for.
        var pads = new Stereo(samples);
        Pad(pads, rng, E2, 0f, total, 0.1f, 1f, -0.25f);
        Pad(pads, rng, Bb2, 9f, 34f, 11f, 0.75f, 0.4f);
        Pad(pads, rng, F2, 30f, 38f, 13f, 0.8f, -0.45f);
        Pad(pads, rng, Eb3, 56f, 30f, 11f, 0.5f, 0.3f);
        Pad(pads, rng, B1, 68f, 24f, 9f, 0.55f, -0.1f);

        // Moving the filter is most of what keeps it feeling alive.
        Action<Biquad, float> dark = (f, t) => f.LowPass(230f + 170f * (0.5f + 0.5f * Mathf.Sin(Tau * t / 41f)) * (0.7f + 0.3f * Mathf.Sin(Tau * t / 17f)), 1.1f);
        var padsDark = new Stereo(samples);
        Array.Copy(Sweep(pads.Left, dark), padsDark.Left, samples);
        Array.Copy(Sweep(pads.Right, dark), padsDark.Right, samples);
        padsDark.ScaleToRms(-22f);

        var far = new Stereo(samples);
        far.Add(padsDark, 1f);

        float[] breath = HighPass(Breathing(rng, samples, new[] { 21f, 29.6f, 40f, 63f, 71.5f, 83f }), 180f);
        var air = new Stereo(samples);
        air.Add(breath, 0, 1f, 0.2f);
        air.ScaleToRms(-40f);
        far.Add(air, 1f);

        foreach ((float at, float hz, float seconds, float pan, float gain) in new[] { (15f, 148f, 16f, -0.6f, 0.09f), (48f, 196f, 19f, 0.65f, 0.08f), (76f, 131f, 15f, -0.2f, 0.1f) })
        {
            float[] metal = BowedMetal(rng, hz, seconds);
            Normalize(metal, 0f);
            far.Add(metal, Samples(at), gain, pan);
        }

        var mix = new Stereo(samples);
        mix.Add(far, 0.75f);
        mix.Add(Reverb(far, 5f, 0.45f), 1.1f);

        var undertow = new Stereo(samples);
        undertow.Add(Undertow(rng, samples), 0, 1f, -0.1f);
        undertow.Add(Undertow(rng, samples), 0, 1f, 0.3f);
        undertow.ScaleToRms(-30f);
        mix.Add(undertow, 1f);

        var thin = new Stereo(samples);
        thin.Add(HighThin(Samples(34f), 34f), Samples(60f), 1f, 0.5f);
        thin.ScaleToRms(-48f);
        mix.Add(thin, 1f);

        // Partway through, the music draws back and leaves the room on its own, which is worse than the music.
        Func<float, float> withdraw = t => 1f - 0.88f * Smooth((t - 36f) / 7f) * Smooth((59f - t) / 7f);
        Envelope(mix.Left, withdraw);
        Envelope(mix.Right, withdraw);
        Stereo looped = Loop(mix, Samples(loop));

        float[] sub = Sub(loop, E1, 0.31f, 3f);
        Envelope(sub, withdraw);
        Gain(sub, Db(-23f) / Rms(sub));
        looped.Add(sub, 0, 1f, 0f);

        SoftLimit(looped.Left, -6f);
        SoftLimit(looped.Right, -6f);
        return looped;
    }

    // A tone that keeps climbing and never arrives: octaves rising together, fading in at the bottom and out at the top.
    static float[] EndlessRise(int samples, float secondsPerOctave, float lowest, int octaves)
    {
        var phases = new double[octaves];
        var x = new float[samples];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / Rate;
            float climb = t / secondsPerOctave;
            float sum = 0f;
            for (int o = 0; o < octaves; o++)
            {
                float place = Mathf.Repeat(o + climb, octaves);
                phases[o] += lowest * Mathf.Pow(2f, place) / Rate;
                sum += Mathf.Sin((float)(phases[o] % 1.0) * Tau) * Mathf.Pow(Mathf.Sin(place / octaves * Mathf.PI), 2f);
            }
            x[i] = sum;
        }
        return x;
    }

    // A heart going a little too fast, felt more than heard, fading in and out over the length of the loop.
    static float[] Pulse(float loop, int beats)
    {
        var x = new float[Samples(loop)];
        for (int b = 0; b < beats; b++)
        {
            float beat = loop * b / beats;
            float strength = Mathf.Pow(0.5f + 0.5f * Mathf.Sin(Tau * beat / loop), 2f);
            Knock(x, Samples(beat), 52f, 90f, 0.02f, 0.11f, strength);
            Knock(x, Samples(beat + 0.26f), 46f, 80f, 0.02f, 0.14f, strength * 0.7f);
        }
        return x;
    }

    static Stereo Deep()
    {
        const float loop = 80f, overlap = 6f, total = loop + overlap;
        int samples = Samples(total);
        var rng = new System.Random(1958);

        var pads = new Stereo(samples);
        Pad(pads, rng, B1, 0f, total, 0.1f, 1f, 0.2f);
        Pad(pads, rng, C3 / 2f, 0f, total, 0.1f, 0.8f, -0.3f);
        Pad(pads, rng, F2, 20f, 44f, 12f, 0.6f, 0.45f);
        float[] left = LowPass(pads.Left, 260f, 1f), right = LowPass(pads.Right, 260f, 1f);
        var dark = new Stereo(samples);
        Array.Copy(left, dark.Left, samples);
        Array.Copy(right, dark.Right, samples);
        dark.ScaleToRms(-24f);

        // The climb takes 80 seconds over three octaves, so it comes back round to where it started.
        var far = new Stereo(samples);
        far.Add(EndlessRise(samples, loop / 3f, 110f, 3), 0, 1f, -0.15f);
        far.ScaleToRms(-33f);

        var mix = new Stereo(samples);
        mix.Add(dark, 0.8f);
        mix.Add(far, 0.8f);
        Stereo wet = new Stereo(samples);
        wet.Add(dark, 1f);
        wet.Add(far, 1f);
        mix.Add(Reverb(wet, 4f, 0.5f), 0.9f);

        Stereo looped = Loop(mix, Samples(loop));

        float[] sub = Sub(loop, B1 * 0.665f, 0.47f, 3f);
        Gain(sub, Db(-24f) / Rms(sub));
        looped.Add(sub, 0, 1f, 0f);

        float[] pulse = Pulse(loop, 98);
        Gain(pulse, Db(-30f) / Rms(pulse));
        looped.Add(pulse, 0, 1f, 0f);

        SoftLimit(looped.Left, -6f);
        SoftLimit(looped.Right, -6f);
        return looped;
    }

    // Something's wrong, and it arrives quietly: a cold cluster rising out of nothing, and a low note under it.
    static Stereo Cold()
    {
        const float seconds = 11f;
        int samples = Samples(seconds);
        var rng = new System.Random(77);

        float[] tones = { 329.63f, 349.23f, 466.16f, 739.99f };
        var bus = new Stereo(samples);
        for (int p = 0; p < tones.Length; p++)
        {
            var tone = new float[samples];
            for (int i = 0; i < samples; i++) tone[i] = Mathf.Sin(Tau * Mathf.Repeat(tones[p] * i / Rate, 1f) + 0.4f * Mathf.Sin(Tau * 3.3f * i / Rate));
            Envelope(tone, t => Smooth(t / 3.5f) * Smooth((seconds - t - 1f) / 5f));
            bus.Add(tone, 0, 0.25f / (1f + p * 0.3f), -0.6f + p * 0.4f);
        }

        var low = new float[samples];
        for (int i = 0; i < samples; i++) low[i] = Sine(E1, i);
        Envelope(low, t => Smooth((t - 2f) / 3f) * Smooth((seconds - t - 1f) / 4f));
        bus.Add(low, 0, 0.5f, 0f);

        var mix = new Stereo(samples);
        mix.Add(bus, 0.7f);
        mix.Add(Reverb(bus, 4.5f, 0.4f), 1f);
        mix.Normalize(-15f);
        return mix;
    }

    // A long low swell, as if the whole station had leaned in.
    static Stereo Low()
    {
        const float seconds = 9f;
        int samples = Samples(seconds);
        var rng = new System.Random(78);

        var bus = new Stereo(samples);
        float[] body = Sub(seconds, E1 * 0.75f, 0.9f, 1f);
        Envelope(body, t => Smooth(t / 3f) * Smooth((seconds - t) / 4f));
        bus.Add(body, 0, 0.6f, 0f);

        float[] dust = BandPass(Pink(rng, samples), 240f, 0.8f);
        Envelope(dust, t => Smooth(t / 4f) * Smooth((seconds - t) / 3f));
        bus.Add(dust, 0, 1.4f, 0.2f);

        float[] metal = BowedMetal(rng, 131f, seconds);
        Normalize(metal, 0f);
        bus.Add(metal, 0, 0.12f, -0.3f);

        var mix = new Stereo(samples);
        mix.Add(bus, 0.8f);
        mix.Add(Reverb(bus, 3.5f, 0.5f), 0.9f);
        mix.Normalize(-11f);
        return mix;
    }
}
