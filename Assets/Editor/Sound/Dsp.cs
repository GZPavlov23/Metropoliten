using System;
using UnityEngine;

// A stereo buffer being built up.
public class Stereo
{
    public readonly float[] Left, Right;

    public Stereo(int samples)
    {
        Left = new float[samples];
        Right = new float[samples];
    }

    public int Length => Left.Length;

    // Equal-power pan: -1 is hard left, 1 is hard right.
    public void Add(float[] mono, int at, float gain, float pan = 0f)
    {
        float angle = (pan + 1f) * Mathf.PI / 4f;
        float left = Mathf.Cos(angle) * gain, right = Mathf.Sin(angle) * gain;
        for (int i = 0; i < mono.Length; i++)
        {
            int to = at + i;
            if (to < 0 || to >= Length) continue;
            Left[to] += mono[i] * left;
            Right[to] += mono[i] * right;
        }
    }

    public void Add(Stereo other, float gain)
    {
        for (int i = 0; i < Mathf.Min(Length, other.Length); i++)
        {
            Left[i] += other.Left[i] * gain;
            Right[i] += other.Right[i] * gain;
        }
    }

    public float Rms()
    {
        double sum = 0.0;
        for (int i = 0; i < Length; i++) sum += Left[i] * Left[i] + Right[i] * Right[i];
        return Mathf.Sqrt((float)(sum / (2 * Length)));
    }

    public void ScaleToRms(float db)
    {
        Scale(Dsp.Db(db) / Mathf.Max(Rms(), 1e-9f));
    }

    public void Normalize(float peakDb)
    {
        Scale(Dsp.Db(peakDb) / Mathf.Max(Mathf.Max(Dsp.Peak(Left), Dsp.Peak(Right)), 1e-9f));
    }

    void Scale(float gain)
    {
        for (int i = 0; i < Length; i++)
        {
            Left[i] *= gain;
            Right[i] *= gain;
        }
    }
}

// Offline audio building blocks for SoundForge. Everything works on plain float arrays at one sample rate.
public static class Dsp
{
    public const int Rate = 44100;
    public const float Tau = Mathf.PI * 2f;

    public static int Samples(float seconds) => Mathf.RoundToInt(seconds * Rate);
    public static float Db(float db) => Mathf.Pow(10f, db / 20f);
    public static float Smooth(float t) => Mathf.SmoothStep(0f, 1f, t);

    // Worked out in full precision, because tones that must land exactly on a loop point can't afford to drift.
    public static float Sine(double hz, int sample) => (float)Math.Sin(2.0 * Math.PI * hz * sample / Rate);

    // The nearest frequency that fits a whole number of cycles into a loop.
    public static float Fit(float hz, float loopSeconds) => Mathf.Round(hz * loopSeconds) / loopSeconds;

    public static float[] White(System.Random rng, int samples, float gain = 1f)
    {
        var x = new float[samples];
        for (int i = 0; i < samples; i++) x[i] = ((float)rng.NextDouble() * 2f - 1f) * gain;
        return x;
    }

    // Falls away at 3 dB an octave: the sound of air.
    public static float[] Pink(System.Random rng, int samples)
    {
        var x = new float[samples];
        float b0 = 0f, b1 = 0f, b2 = 0f, b3 = 0f, b4 = 0f, b5 = 0f, b6 = 0f;
        for (int i = 0; i < samples; i++)
        {
            float white = (float)rng.NextDouble() * 2f - 1f;
            b0 = 0.99886f * b0 + white * 0.0555179f;
            b1 = 0.99332f * b1 + white * 0.0750759f;
            b2 = 0.969f * b2 + white * 0.153852f;
            b3 = 0.8665f * b3 + white * 0.3104856f;
            b4 = 0.55f * b4 + white * 0.5329522f;
            b5 = -0.7616f * b5 - white * 0.016898f;
            x[i] = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f) * 0.11f;
            b6 = white * 0.115926f;
        }
        return x;
    }

    // Falls away at 6 dB an octave: rumble.
    public static float[] Brown(System.Random rng, int samples)
    {
        var x = new float[samples];
        float last = 0f;
        for (int i = 0; i < samples; i++)
        {
            float white = (float)rng.NextDouble() * 2f - 1f;
            last = (last + 0.02f * white) / 1.02f;
            x[i] = last * 3.5f;
        }
        return x;
    }

    // A second-order filter. Set it, then run samples through it one at a time.
    public class Biquad
    {
        float b0, b1, b2, a1, a2, x1, x2, y1, y2;

        void Set(float nb0, float nb1, float nb2, float a0, float na1, float na2)
        {
            b0 = nb0 / a0;
            b1 = nb1 / a0;
            b2 = nb2 / a0;
            a1 = na1 / a0;
            a2 = na2 / a0;
        }

        public void LowPass(float hz, float q = 0.707f)
        {
            float w = Tau * Mathf.Min(hz, Rate * 0.45f) / Rate, c = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q);
            Set((1f - c) / 2f, 1f - c, (1f - c) / 2f, 1f + alpha, -2f * c, 1f - alpha);
        }

        public void HighPass(float hz, float q = 0.707f)
        {
            float w = Tau * Mathf.Min(hz, Rate * 0.45f) / Rate, c = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q);
            Set((1f + c) / 2f, -(1f + c), (1f + c) / 2f, 1f + alpha, -2f * c, 1f - alpha);
        }

        public void BandPass(float hz, float q)
        {
            float w = Tau * Mathf.Min(hz, Rate * 0.45f) / Rate, c = Mathf.Cos(w), alpha = Mathf.Sin(w) / (2f * q);
            Set(alpha, 0f, -alpha, 1f + alpha, -2f * c, 1f - alpha);
        }

        public float Next(float x)
        {
            float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
            x2 = x1;
            x1 = x;
            y2 = y1;
            y1 = y;
            return y;
        }
    }

    public static float[] LowPass(float[] x, float hz, float q = 0.707f)
    {
        var filter = new Biquad();
        filter.LowPass(hz, q);
        return Run(filter, x);
    }

    public static float[] HighPass(float[] x, float hz, float q = 0.707f)
    {
        var filter = new Biquad();
        filter.HighPass(hz, q);
        return Run(filter, x);
    }

    public static float[] BandPass(float[] x, float hz, float q)
    {
        var filter = new Biquad();
        filter.BandPass(hz, q);
        return Run(filter, x);
    }

    static float[] Run(Biquad filter, float[] x)
    {
        var y = new float[x.Length];
        for (int i = 0; i < x.Length; i++) y[i] = filter.Next(x[i]);
        return y;
    }

    // A filter whose frequency moves over time. The shape is asked for every few milliseconds.
    public static float[] Sweep(float[] x, Action<Biquad, float> set)
    {
        const int block = 64;
        var filter = new Biquad();
        var y = new float[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            if (i % block == 0) set(filter, (float)i / Rate);
            y[i] = filter.Next(x[i]);
        }
        return y;
    }

    public static void Gain(float[] x, float gain)
    {
        for (int i = 0; i < x.Length; i++) x[i] *= gain;
    }

    // Multiplies by an envelope that's worked out every few milliseconds, which is plenty for anything slow.
    public static void Envelope(float[] x, Func<float, float> shape)
    {
        const int block = 32;
        float last = shape(0f);
        for (int i = 0; i < x.Length; i += block)
        {
            float next = shape((float)(i + block) / Rate);
            for (int j = 0; j < block && i + j < x.Length; j++) x[i + j] *= Mathf.Lerp(last, next, (float)j / block);
            last = next;
        }
    }

    public static void AddInto(float[] into, float[] x, int at, float gain)
    {
        for (int i = 0; i < x.Length; i++)
        {
            int to = at + i;
            if (to >= 0 && to < into.Length) into[to] += x[i] * gain;
        }
    }

    public static float Peak(float[] x)
    {
        float peak = 0f;
        foreach (float v in x) peak = Mathf.Max(peak, Mathf.Abs(v));
        return peak;
    }

    public static float Rms(float[] x)
    {
        double sum = 0.0;
        foreach (float v in x) sum += v * v;
        return Mathf.Sqrt((float)(sum / x.Length));
    }

    public static void Normalize(float[] x, float peakDb)
    {
        Gain(x, Db(peakDb) / Mathf.Max(Peak(x), 1e-9f));
    }

    // Rounds off anything over the ceiling instead of clipping it.
    public static void SoftLimit(float[] x, float ceilingDb)
    {
        float ceiling = Db(ceilingDb);
        for (int i = 0; i < x.Length; i++) x[i] = (float)Math.Tanh(x[i] / ceiling) * ceiling;
    }

    // Folds the last part of a take over its first part, so that it can repeat with no seam.
    public static float[] Loop(float[] x, int length)
    {
        int overlap = x.Length - length;
        var y = new float[length];
        Array.Copy(x, y, length);
        for (int i = 0; i < overlap; i++)
        {
            float t = (float)i / overlap;
            y[i] = x[i] * Mathf.Sin(t * Mathf.PI / 2f) + x[length + i] * Mathf.Cos(t * Mathf.PI / 2f);
        }
        return y;
    }

    public static Stereo Loop(Stereo x, int length)
    {
        var y = new Stereo(length);
        Array.Copy(Loop(x.Left, length), y.Left, length);
        Array.Copy(Loop(x.Right, length), y.Right, length);
        return y;
    }

    // The end of a periodic signal, once any filter it went through has settled down.
    public static float[] Settled(float[] x, int length)
    {
        var y = new float[length];
        Array.Copy(x, x.Length - length, y, 0, length);
        return y;
    }

    // One cycle of a saw wave with its harmonics limited, read with a phase from 0 to 1.
    static readonly float[] SawTable = MakeSaw(4096, 28);

    static float[] MakeSaw(int size, int harmonics)
    {
        var table = new float[size + 1];
        for (int i = 0; i <= size; i++)
        {
            float sum = 0f;
            for (int h = 1; h <= harmonics; h++) sum += Mathf.Sin(Tau * h * i / size) / h;
            table[i] = sum * 0.55f;
        }
        return table;
    }

    public static float Saw(float phase)
    {
        float at = phase * (SawTable.Length - 1);
        int i = (int)at;
        return Mathf.Lerp(SawTable[i], SawTable[Mathf.Min(i + 1, SawTable.Length - 1)], at - i);
    }

    // A delay-network reverb: eight lines feeding back into each other, losing their top end as they go.
    // Returns only the reverberation, so it can be mixed in at any level.
    public static Stereo Reverb(Stereo dry, float decaySeconds, float damping = 0.3f)
    {
        float[] lengths = { 0.0297f, 0.0371f, 0.0411f, 0.0437f, 0.0533f, 0.0599f, 0.0671f, 0.0733f };
        int lines = lengths.Length, preDelay = Samples(0.02f);
        var buffers = new float[lines][];
        var gains = new float[lines];
        var tone = new float[lines];
        var write = new int[lines];
        for (int l = 0; l < lines; l++)
        {
            buffers[l] = new float[Samples(lengths[l])];
            gains[l] = Mathf.Pow(10f, -3f * lengths[l] / decaySeconds);
        }

        var wet = new Stereo(dry.Length);
        for (int i = 0; i < dry.Length; i++)
        {
            int from = i - preDelay;
            float input = from >= 0 ? (dry.Left[from] + dry.Right[from]) * 0.5f : 0f;

            // Damped before they're mixed: the mix only keeps its energy if it's taken from the very signals
            // it feeds back into, and at the top end anything else makes the whole thing run away.
            float sum = 0f, left = 0f, right = 0f;
            for (int l = 0; l < lines; l++)
            {
                float line = buffers[l][write[l]];
                if (l % 2 == 0) left += line;
                else right += line;

                tone[l] += (line - tone[l]) * (1f - damping);
                sum += tone[l];
            }
            sum *= 2f / lines;

            for (int l = 0; l < lines; l++)
            {
                buffers[l][write[l]] = (tone[l] - sum) * gains[l] + input * (l % 2 == 0 ? 0.5f : -0.5f);
                write[l] = (write[l] + 1) % buffers[l].Length;
            }
            wet.Left[i] = left * 0.25f;
            wet.Right[i] = right * 0.25f;
        }
        return wet;
    }
}
