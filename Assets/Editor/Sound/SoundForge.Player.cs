using UnityEngine;
using static Dsp;

// Everything the player does: their feet, and the things they touch.
public static partial class SoundForge
{
    public const int WalkSteps = 8, RunSteps = 6, CabSteps = 4;

    static void ForgePlayer()
    {
        var rng = new System.Random(3107);

        for (int i = 0; i < WalkSteps; i++) Write($"F_Walk{i}", Footstep(rng, 0.6f, 0.07f + (float)rng.NextDouble() * 0.03f, 0.05f));
        for (int i = 0; i < RunSteps; i++) Write($"F_Run{i}", Footstep(rng, 1f, 0.035f + (float)rng.NextDouble() * 0.02f, 0.12f));
        for (int i = 0; i < CabSteps; i++) Write($"F_Cab{i}", CabStep(rng));

        Write("I_Gate", Gate(rng));
        Write("I_CabDoor", CabDoor(rng));
        Write("I_Key", Key(rng));
        Write("I_Pen", Pen(rng));
        Write("I_Radio", RadioClick(rng));
        Write("I_Seat", Seat(rng));
        Write("I_Call", IntercomCall());
        Write("U_Move", MenuMove(rng));
        Write("U_Press", MenuPress(rng));
    }

    // A hard shoe on stone: the click of the heel, a short knock from the body of the shoe, the sole
    // slapping down after it, and grit. It's mostly click: the room's reverb supplies the size.
    static float[] Footstep(System.Random rng, float weight, float toeDelay, float grit)
    {
        var x = new float[Samples(0.32f)];
        float body = 165f + (float)rng.NextDouble() * 70f;

        Burst(rng, x, 0, 650f, 6500f, 0.0035f + (float)rng.NextDouble() * 0.0015f, 2.2f + weight * 1.2f);
        Knock(x, 0, body, body * 2.2f, 0.008f, 0.016f + weight * 0.01f, 0.9f);
        Knock(x, 0, body * 3.1f, body * 4f, 0.005f, 0.008f, 0.5f);

        int toe = Samples(toeDelay);
        Burst(rng, x, toe, 500f, 3800f, 0.005f, 0.9f);
        Knock(x, toe, body * 1.3f, body * 2.4f, 0.008f, 0.012f, 0.3f);

        // The scuff of the sole leaving the floor, a touch of dirt under it.
        Burst(rng, x, toe + Samples(0.012f), 2200f, 7500f, 0.018f, grit * 2f);
        return Finish(x, -7f + weight * 3f, 0.05f);
    }

    // Boots on the rubber floor of the cab, over a hollow steel deck.
    static float[] CabStep(System.Random rng)
    {
        var x = new float[Samples(0.28f)];
        float deck = 170f + (float)rng.NextDouble() * 50f;
        Knock(x, 0, deck, deck * 1.8f, 0.01f, 0.05f, 1f);
        Knock(x, 0, deck * 2.63f, deck * 2.9f, 0.01f, 0.02f, 0.3f);
        Burst(rng, x, 0, 300f, 2000f, 0.006f, 0.3f);
        return Finish(x, -9f, 0.05f);
    }

    // A folding lattice gate: a long rattle of steel strips closing up on each other.
    static float[] Gate(System.Random rng)
    {
        const float seconds = 2.1f;
        var x = new float[Samples(seconds)];

        float[] slide = BandPass(White(rng, x.Length), 1600f, 0.7f);
        Envelope(slide, t => Smooth(t / 0.2f) * Smooth((seconds - 0.3f - t) / 0.5f));
        AddInto(x, slide, 0, 0.07f);

        for (float t = 0.05f; t < seconds - 0.35f; t += 0.07f + (float)rng.NextDouble() * 0.09f)
            Ping(x, Samples(t), 700f + (float)rng.NextDouble() * 1300f, 0.05f, 0.25f + (float)rng.NextDouble() * 0.3f);

        Ping(x, Samples(seconds - 0.32f), 420f, 0.12f, 1f);
        Knock(x, Samples(seconds - 0.32f), 90f, 160f, 0.01f, 0.06f, 0.8f);
        return Finish(x, -5f, 0.1f);
    }

    // The cab door: air letting go, the slide, and the stop.
    static float[] CabDoor(System.Random rng)
    {
        const float seconds = 1.6f;
        var x = new float[Samples(seconds)];

        float[] hiss = BandPass(White(rng, x.Length), 4200f, 0.9f);
        Envelope(hiss, t => Mathf.Exp(-t / 0.3f));
        AddInto(x, hiss, 0, 0.5f);

        float[] roll = LowPass(Pink(rng, x.Length), 500f);
        Envelope(roll, t => Smooth((t - 0.1f) / 0.3f) * Smooth((1.35f - t) / 0.2f));
        AddInto(x, roll, 0, 0.5f);

        Knock(x, Samples(1.34f), 110f, 200f, 0.01f, 0.07f, 1f);
        Ping(x, Samples(1.34f), 880f, 0.05f, 0.25f);
        return Finish(x, -6f, 0.1f);
    }

    // A key lifted off its hook, against the others.
    static float[] Key(System.Random rng)
    {
        var x = new float[Samples(0.6f)];
        for (int i = 0; i < 6; i++) Ping(x, Samples(i * 0.045f + (float)rng.NextDouble() * 0.02f), 2600f + (float)rng.NextDouble() * 2400f, 0.04f, 0.4f + (float)rng.NextDouble() * 0.4f);
        return Finish(x, -8f, 0.1f);
    }

    // A signature: a few quick strokes of a ballpoint on a hard surface.
    static float[] Pen(System.Random rng)
    {
        const float seconds = 1.2f;
        var x = new float[Samples(seconds)];
        for (float t = 0.02f; t < seconds - 0.2f; t += 0.11f + (float)rng.NextDouble() * 0.12f)
        {
            float length = 0.06f + (float)rng.NextDouble() * 0.09f;
            float centre = 2800f + (float)rng.NextDouble() * 1800f;
            float[] stroke = BandPass(White(rng, Samples(length)), centre, 1.2f);
            Envelope(stroke, s => Mathf.Sin(Mathf.PI * s / length));
            AddInto(x, stroke, Samples(t), 0.7f);
        }
        return Finish(x, -14f, 0.05f);
    }

    // The radio opening: a click and a breath of squelch.
    static float[] RadioClick(System.Random rng)
    {
        var x = new float[Samples(0.28f)];
        Burst(rng, x, 0, 1200f, 5000f, 0.002f, 1f);
        float[] squelch = BandPass(White(rng, x.Length), 1900f, 0.9f);
        Envelope(squelch, t => Mathf.Exp(-t / 0.08f) * Smooth(t / 0.01f));
        AddInto(x, squelch, 0, 0.6f);
        return Finish(x, -12f);
    }

    // The menus: a small relay ticking over as the selection moves.
    static float[] MenuMove(System.Random rng)
    {
        var x = new float[Samples(0.08f)];
        Burst(rng, x, 0, 1800f, 6000f, 0.0012f, 1f);
        Knock(x, 0, 1450f, 2100f, 0.003f, 0.006f, 0.5f);
        return Finish(x, -21f, 0.01f);
    }

    // And the same relay pulling in.
    static float[] MenuPress(System.Random rng)
    {
        var x = new float[Samples(0.22f)];
        Knock(x, 0, 210f, 420f, 0.006f, 0.02f, 1f);
        Burst(rng, x, 0, 900f, 5000f, 0.002f, 1.2f);
        Ping(x, Samples(0.012f), 1240f, 0.02f, 0.35f);
        return Finish(x, -13f, 0.02f);
    }

    // The intercom wanting an answer: a flat two-tone buzz through a speaker the size of a coin.
    static float[] IntercomCall()
    {
        const float seconds = 0.55f;
        var x = new float[Samples(seconds)];
        for (int i = 0; i < x.Length; i++)
        {
            for (int h = 1; h < 12; h += 2) x[i] += (Sine(425f * h, i) + Sine(450f * h, i)) / h;
        }
        x = HighPass(LowPass(x, 2400f), 350f);
        Envelope(x, t => Smooth(t / 0.015f) * Smooth((seconds - t) / 0.02f));
        return Finish(x, -8f);
    }

    // A worn seat taking the weight.
    static float[] Seat(System.Random rng)
    {
        var x = new float[Samples(0.6f)];
        Knock(x, 0, 90f, 150f, 0.02f, 0.09f, 1f);
        float[] creak = Groan(rng, 0.45f);
        AddInto(x, creak, Samples(0.03f), 0.25f);
        return Finish(x, -9f, 0.05f);
    }
}
