using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static Dsp;

// Generates all of the game's sound from nothing but maths, so there are no licences to clear.
// The results land in Resources, where the Score can load them by name and scenes can point at them.
public static partial class SoundForge
{
    public const string SoundDir = "Assets/Resources/Sound";
    const float VorbisQuality = 0.6f;

    static readonly List<string> written = new List<string>();

    [MenuItem("Metropoliten/Forge Sounds")]
    public static void ForgeAll()
    {
        Directory.CreateDirectory(SoundDir);
        written.Clear();

        ForgeScore();
        ForgeStation();
        ForgePlayer();
        ForgeTrain();

        AssetDatabase.Refresh();
        foreach (string name in written) ConfigureImport(name);
        Debug.Log($"Forged {written.Count} sounds.");
    }

    static string PathOf(string name) => $"{SoundDir}/{name}.wav";

    static void ConfigureImport(string name)
    {
        var importer = (AudioImporter)AssetImporter.GetAtPath(PathOf(name));
        AudioImporterSampleSettings settings = importer.defaultSampleSettings;
        settings.loadType = name.StartsWith("F_") || name.StartsWith("I_") ? AudioClipLoadType.DecompressOnLoad : AudioClipLoadType.CompressedInMemory;
        settings.compressionFormat = AudioCompressionFormat.Vorbis;
        settings.quality = VorbisQuality;
        settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
        importer.defaultSampleSettings = settings;
        importer.SaveAndReimport();
    }

    // Rewrites a file only when it has changed, so that an unchanged sound isn't imported again.
    static void Save(string name, byte[] wav)
    {
        string path = PathOf(name);
        if (!File.Exists(path) || !File.ReadAllBytes(path).SequenceEqual(wav)) File.WriteAllBytes(path, wav);
        written.Add(name);
    }

    static void Write(string name, float[] mono)
    {
        Save(name, Encode(new[] { mono }));
    }

    static void Write(string name, Stereo stereo)
    {
        Save(name, Encode(new[] { stereo.Left, stereo.Right }));
    }

    static byte[] Encode(float[][] channels)
    {
        // A bad value would otherwise be written out as a file of solid full-scale noise.
        foreach (float[] channel in channels)
        {
            foreach (float sample in channel)
            {
                if (float.IsNaN(sample) || float.IsInfinity(sample)) throw new InvalidOperationException("A sound came out with invalid samples.");
            }
        }

        int frames = channels[0].Length, count = channels.Length, dataSize = frames * count * 2;
        using (var stream = new MemoryStream(44 + dataSize))
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)count);
            writer.Write(Rate);
            writer.Write(Rate * count * 2);
            writer.Write((short)(count * 2));
            writer.Write((short)16);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < frames; i++)
                for (int c = 0; c < count; c++)
                    writer.Write((short)Mathf.Clamp(Mathf.RoundToInt(channels[c][i] * 32767f), -32768, 32767));
            return stream.ToArray();
        }
    }

    // A short fade at each end, so that a one-shot never starts or stops with a click.
    static float[] Finish(float[] x, float peakDb, float fadeOutSeconds = 0.02f)
    {
        int fadeIn = Samples(0.001f), fadeOut = Samples(fadeOutSeconds);
        for (int i = 0; i < fadeIn && i < x.Length; i++) x[i] *= (float)i / fadeIn;
        for (int i = 0; i < fadeOut && i < x.Length; i++) x[x.Length - 1 - i] *= (float)i / fadeOut;
        Normalize(x, peakDb);
        return x;
    }

    // A decaying sine, with its pitch falling from some way above where it settles.
    static void Knock(float[] into, int at, float hz, float fromHz, float sweepSeconds, float decaySeconds, float gain)
    {
        float phase = 0f;
        for (int i = 0; at + i < into.Length; i++)
        {
            float t = (float)i / Rate;
            float amplitude = Mathf.Exp(-t / decaySeconds);
            if (amplitude < 0.0005f) break;
            phase += Tau * Mathf.Lerp(hz, fromHz, Mathf.Exp(-t / sweepSeconds)) / Rate;
            into[at + i] += Mathf.Sin(phase) * amplitude * gain;
        }
    }

    // A short burst of filtered noise.
    static void Burst(System.Random rng, float[] into, int at, float lowHz, float highHz, float decaySeconds, float gain)
    {
        int samples = Samples(decaySeconds * 6f);
        float[] noise = HighPass(LowPass(White(rng, samples), highHz), lowHz);
        Envelope(noise, t => Mathf.Exp(-t / decaySeconds));
        AddInto(into, noise, at, gain);
    }

    // A struck piece of metal: a handful of partials that don't line up into a note.
    static void Ping(float[] into, int at, float hz, float decaySeconds, float gain)
    {
        float[] ratios = { 1f, 2.76f, 5.4f, 8.93f };
        float[] levels = { 1f, 0.5f, 0.25f, 0.1f };
        for (int p = 0; p < ratios.Length; p++)
            Knock(into, at, hz * ratios[p], hz * ratios[p], 1f, decaySeconds / (1f + p * 0.7f), gain * levels[p]);
    }
}
