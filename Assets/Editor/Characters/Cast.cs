using UnityEngine;

// Everyone in the game, and what they look like. Personalities are in the design notes; this is how they show.
public static class Cast
{
    static Color Rgb(int r, int g, int b) => new Color(r / 255f, g / 255f, b / 255f);

    static readonly Color FairSkin = Rgb(206, 164, 138), PaleSkin = Rgb(198, 166, 148), TannedSkin = Rgb(184, 136, 104), OldSkin = Rgb(190, 152, 130);

    // Thin and upright, and never quite looking at you. A long bloodless face, wire glasses, not a thread out of place.
    public static readonly Look Clerk = new Look
    {
        Name = "Clerk", TextureSize = 1024, Seed = 11,
        Height = 1.8f, Build = 0.84f, Shoulders = 0.92f,
        HeadSize = 1.2f, FaceLength = 1.14f, FaceWidth = 0.9f, Jaw = 0.72f, Nose = 1.1f, NoseWidth = 0.8f, Brow = 0.8f, Cheek = 1.3f, Gaunt = 0.8f,
        Age = 0.6f, Mouth = -0.3f, BrowTilt = 0.2f, Skin = PaleSkin, HairColor = Rgb(112, 108, 102), Eyes = Rgb(70, 76, 74), Glasses = true,
        Hair = HairStyle.Receding,
        Top = Top.Suit, TopColor = Rgb(52, 50, 52), TopTrim = Rgb(214, 210, 198), Under = Rgb(206, 204, 194), Tie = true, TieColor = Rgb(84, 40, 40),
        BottomColor = Rgb(44, 42, 44),
    };

    // Six years on the line and the only person who seems to be enjoying himself: round-faced, unshaven, hat pushed back.
    public static readonly Look Mitko = new Look
    {
        Name = "Mitko", TextureSize = 1024, Seed = 22,
        Height = 1.76f, Build = 1.14f, Shoulders = 1.08f, Belly = 0.012f,
        HeadSize = 1.2f, FaceLength = 0.92f, FaceWidth = 1.1f, Jaw = 0.92f, Nose = 0.86f, NoseWidth = 1.3f, Brow = 1.1f, Cheek = 1.5f,
        Age = 0.16f, Stubble = 0.8f, Mouth = 0.8f, BrowTilt = 0.6f, Skin = TannedSkin, HairColor = Rgb(34, 26, 22), Eyes = Rgb(58, 42, 28),
        Hat = Hat.Beanie, HatColor = Rgb(42, 44, 48),
        Top = Top.Hoodie, TopColor = Rgb(38, 46, 62), TopTrim = Rgb(30, 36, 50), Under = Rgb(38, 46, 62), HiVis = true,
        BottomColor = Rgb(34, 40, 54), ShoeColor = Rgb(44, 32, 24),
        Carried = Carried.ToolPouch, CarriedColor = Rgb(62, 46, 32),
    };

    // The same man, the day he says he wasn't there.
    public static readonly Look MitkoOffDuty = new Look
    {
        Name = "MitkoOffDuty", TextureSize = 1024, Seed = 22,
        Height = 1.76f, Build = 1.1f, Shoulders = 1.08f, Belly = 0.012f,
        HeadSize = 1.2f, FaceLength = 0.92f, FaceWidth = 1.1f, Jaw = 0.92f, Nose = 0.86f, NoseWidth = 1.3f, Brow = 1.1f, Cheek = 1.5f,
        Age = 0.16f, Stubble = 0.8f, Mouth = 0.2f, BrowTilt = 0.1f, Skin = TannedSkin, HairColor = Rgb(34, 26, 22), Eyes = Rgb(58, 42, 28),
        Hat = Hat.Beanie, HatColor = Rgb(42, 44, 48),
        Top = Top.Jacket, TopColor = Rgb(52, 56, 60), TopTrim = Rgb(44, 46, 48), Under = Rgb(140, 136, 126),
        BottomColor = Rgb(40, 40, 44), ShoeColor = Rgb(44, 32, 24),
    };

    // Past retirement and still in the booth. Small, bent and sharp, and she looks at you over her glasses as if she's read your file.
    public static readonly Look Vera = new Look
    {
        Name = "Vera", TextureSize = 1024, Seed = 33, Woman = true,
        Height = 1.6f, Build = 0.92f, Shoulders = 0.84f, Bust = 0.012f, Hips = 1.08f, Stoop = 0.8f,
        HeadSize = 1.2f, FaceLength = 1.04f, FaceWidth = 0.94f, Jaw = 0.66f, Nose = 1.34f, NoseWidth = 0.92f, Brow = 1.2f, Cheek = 1.4f, Gaunt = 1f,
        Age = 1f, Mouth = -0.7f, BrowTilt = -0.5f, Skin = OldSkin, HairColor = Rgb(150, 146, 140), Eyes = Rgb(58, 62, 54), Glasses = true,
        Hair = HairStyle.Bun,
        Top = Top.Uniform, TopColor = Rgb(40, 48, 66), TopTrim = Rgb(150, 124, 70), Under = Rgb(170, 166, 150),
        Bottom = Bottom.Skirt, BottomColor = Rgb(58, 58, 62), ShoeColor = Rgb(24, 20, 18),
    };

    // A heavy man who has been in a queue for most of his life.
    public static readonly Look WaitingMan = new Look
    {
        Name = "WaitingMan", Seed = 44,
        Height = 1.78f, Build = 1.18f, Shoulders = 1.04f, Belly = 0.03f,
        HeadSize = 1.2f, FaceLength = 1.02f, FaceWidth = 1.12f, Jaw = 0.98f, Nose = 1.1f, NoseWidth = 1.3f, Cheek = 1.1f,
        Age = 0.7f, Mouth = -0.5f, BrowTilt = 0.4f, Moustache = true, Skin = FairSkin, HairColor = Rgb(84, 76, 68), Eyes = Rgb(62, 50, 38),
        Top = Top.Coat, TopColor = Rgb(70, 58, 46), TopTrim = Rgb(40, 32, 26), Under = Rgb(110, 104, 92),
        BottomColor = Rgb(50, 48, 46), ShoeColor = Rgb(30, 24, 22),
    };

    public static readonly Look OldWoman = new Look
    {
        Name = "OldWoman", Seed = 51, Woman = true,
        Height = 1.56f, Build = 1.2f, Shoulders = 0.9f, Belly = 0.02f, Bust = 0.018f, Hips = 1.18f, Stoop = 0.7f,
        HeadSize = 1.2f, FaceLength = 0.94f, FaceWidth = 1.1f, Jaw = 0.9f, Nose = 1.1f, NoseWidth = 1.3f, Cheek = 1.6f,
        Age = 1f, Mouth = -0.4f, BrowTilt = 0.5f, Skin = OldSkin, HairColor = Rgb(170, 166, 160),
        Hat = Hat.Headscarf, HatColor = Rgb(96, 40, 44), HatPrint = Rgb(196, 176, 120),
        Top = Top.Coat, TopColor = Rgb(44, 42, 48), TopTrim = Rgb(30, 28, 30), Under = Rgb(60, 54, 56),
        Bottom = Bottom.Skirt, BottomColor = Rgb(50, 44, 44), ShoeColor = Rgb(36, 28, 24),
        Carried = Carried.ShoppingBag, CarriedColor = Rgb(120, 104, 78),
    };

    public static readonly Look Fitter = new Look
    {
        Name = "Fitter", Seed = 52,
        Height = 1.79f, Build = 1.08f, Shoulders = 1.12f,
        FaceLength = 1.06f, FaceWidth = 1.04f, Jaw = 0.96f, Nose = 1.2f, NoseWidth = 1.2f, Brow = 1.3f,
        Age = 0.62f, Moustache = true, Stubble = 0.3f, Mouth = -0.2f, Skin = TannedSkin, HairColor = Rgb(58, 50, 44), Eyes = Rgb(50, 44, 34),
        Hat = Hat.FlatCap, HatColor = Rgb(66, 64, 58),
        Top = Top.Quilted, TopColor = Rgb(78, 80, 74), TopTrim = Rgb(46, 46, 42), Under = Rgb(60, 60, 56),
        BottomColor = Rgb(46, 44, 42),
        Carried = Carried.Briefcase, CarriedColor = Rgb(58, 42, 30),
    };

    public static readonly Look Nurse = new Look
    {
        Name = "Nurse", Seed = 53, Woman = true,
        Height = 1.67f, Build = 0.92f, Shoulders = 0.86f, Bust = 0.02f, Hips = 1.12f,
        FaceLength = 1f, FaceWidth = 0.96f, Jaw = 0.72f, Nose = 0.86f, NoseWidth = 0.86f, Cheek = 1.2f,
        Age = 0.34f, Mouth = -0.1f, BrowTilt = 0.5f, Skin = FairSkin, HairColor = Rgb(52, 36, 28), Eyes = Rgb(70, 60, 44),
        Hair = HairStyle.Bob,
        Top = Top.Nurse, TopColor = Rgb(196, 198, 192), TopTrim = Rgb(86, 110, 130), Under = Rgb(196, 198, 192),
        Bottom = Bottom.Skirt, BottomColor = Rgb(196, 198, 192), ShoeColor = Rgb(180, 178, 170),
    };

    public static readonly Look Student = new Look
    {
        Name = "Student", Seed = 54,
        Height = 1.81f, Build = 0.82f, Shoulders = 0.94f,
        HeadSize = 1.16f, FaceLength = 1.06f, FaceWidth = 0.9f, Jaw = 0.76f, Nose = 1f, NoseWidth = 0.9f, Brow = 0.8f,
        Age = 0.04f, Stubble = 0.2f, Mouth = 0f, BrowTilt = 0.3f, Skin = FairSkin, HairColor = Rgb(92, 66, 42), Eyes = Rgb(62, 74, 80), Glasses = true,
        Top = Top.Hoodie, TopColor = Rgb(62, 80, 62), TopTrim = Rgb(50, 66, 50), Under = Rgb(62, 80, 62),
        BottomColor = Rgb(58, 70, 92), ShoeColor = Rgb(150, 146, 136),
        Carried = Carried.Backpack, CarriedColor = Rgb(120, 62, 40),
    };

    public static readonly Look Drunk = new Look
    {
        Name = "Drunk", Seed = 55,
        Height = 1.72f, Build = 1.02f, Shoulders = 0.98f, Belly = 0.034f, Stoop = 0.4f,
        FaceLength = 0.98f, FaceWidth = 1.08f, Jaw = 0.9f, Nose = 1.2f, NoseWidth = 1.4f, Cheek = 1.2f,
        Age = 0.56f, Stubble = 1f, Mouth = -0.2f, BrowTilt = 0.7f, RedNose = true, Skin = FairSkin, HairColor = Rgb(44, 40, 36), Eyes = Rgb(56, 46, 36),
        Top = Top.Tracksuit, TopColor = Rgb(36, 40, 74), TopTrim = Rgb(196, 192, 180), Under = Rgb(150, 144, 130),
        BottomColor = Rgb(36, 40, 74), ShoeColor = Rgb(170, 166, 156),
    };

    public static readonly Look LongCoat = new Look
    {
        Name = "LongCoat", Seed = 56,
        Height = 1.84f, Build = 0.92f, Shoulders = 1.02f,
        FaceLength = 1.1f, FaceWidth = 0.92f, Jaw = 0.82f, Nose = 1.06f, NoseWidth = 0.9f, Brow = 1.3f, Gaunt = 0.6f,
        Age = 0.5f, Mouth = -0.1f, BrowTilt = -0.5f, Skin = PaleSkin, HairColor = Rgb(30, 28, 26), Eyes = Rgb(40, 40, 40),
        Hat = Hat.Trilby, HatColor = Rgb(36, 34, 34),
        Top = Top.Coat, TopColor = Rgb(32, 32, 36), TopTrim = Rgb(20, 20, 22), Under = Rgb(120, 116, 106),
        BottomColor = Rgb(32, 32, 34), ShoeColor = Rgb(18, 16, 16),
    };

    public static readonly Look RedCoat = new Look
    {
        Name = "RedCoat", Seed = 57, Woman = true,
        Height = 1.7f, Build = 0.88f, Shoulders = 0.84f, Bust = 0.016f, Hips = 1.08f,
        FaceLength = 1.04f, FaceWidth = 0.92f, Jaw = 0.68f, Nose = 0.84f, NoseWidth = 0.8f, Cheek = 1.3f,
        Age = 0.22f, Mouth = 0f, Skin = PaleSkin, HairColor = Rgb(30, 22, 20), Eyes = Rgb(44, 36, 30),
        Hair = HairStyle.Long,
        Top = Top.Coat, TopColor = Rgb(118, 34, 30), TopTrim = Rgb(30, 22, 20), Under = Rgb(120, 40, 36),
        Bottom = Bottom.Skirt, BottomColor = Rgb(30, 28, 30), ShoeColor = Rgb(20, 16, 16),
    };

    public static readonly Look OldMan = new Look
    {
        Name = "OldMan", Seed = 58,
        Height = 1.68f, Build = 0.9f, Shoulders = 0.92f, Stoop = 0.9f,
        FaceLength = 1f, FaceWidth = 0.98f, Jaw = 0.76f, Nose = 1.3f, NoseWidth = 1.1f, Brow = 1.4f, Gaunt = 0.7f,
        Age = 1f, Beard = true, Mouth = -0.3f, Skin = OldSkin, HairColor = Rgb(176, 172, 164), Eyes = Rgb(66, 70, 70),
        Hat = Hat.Ushanka, HatColor = Rgb(58, 50, 44),
        Top = Top.Coat, TopColor = Rgb(66, 68, 62), TopTrim = Rgb(40, 40, 36), Under = Rgb(80, 74, 66),
        BottomColor = Rgb(46, 44, 42),
    };

    // The last trainee, in the jacket they issue you on the first night. What was done to the photograph on his badge was done to all of him.
    public static readonly Look Stoyan = new Look
    {
        Name = "Stoyan", TextureSize = 2048, Seed = 77,
        Height = 1.8f, Build = 0.84f, Shoulders = 0.98f, Stoop = 0.3f,
        HeadSize = 1.2f, FaceLength = 1.08f, FaceWidth = 0.94f, Jaw = 0.8f, Gaunt = 1f, Scratched = true,
        Age = 0.2f, Skin = Rgb(170, 150, 136), HairColor = Rgb(26, 24, 22),
        Top = Top.Uniform, TopColor = Rgb(30, 34, 46), TopTrim = Rgb(58, 52, 44), Under = Rgb(92, 88, 80),
        BottomColor = Rgb(26, 28, 34), ShoeColor = Rgb(20, 18, 16),
    };

    // Nikola, the one time anybody sees him: a tired man in a cheap jacket, from behind, going in to visit his mother.
    public static readonly Look Nikola = new Look
    {
        Name = "Nikola", Seed = 61,
        Height = 1.77f, Build = 0.94f, Shoulders = 1f, Stoop = 0.3f,
        FaceLength = 1.04f, FaceWidth = 0.96f, Jaw = 0.84f, Nose = 1.06f,
        Age = 0.34f, Stubble = 0.7f, Mouth = -0.2f, BrowTilt = 0.4f, Skin = FairSkin, HairColor = Rgb(40, 32, 28), Eyes = Rgb(50, 44, 38),
        Top = Top.Jacket, TopColor = Rgb(48, 50, 48), TopTrim = Rgb(36, 36, 36), Under = Rgb(120, 116, 106),
        BottomColor = Rgb(44, 48, 58), ShoeColor = Rgb(40, 30, 24),
        Carried = Carried.ShoppingBag, CarriedColor = Rgb(176, 172, 160),
    };

    public static readonly Look[] Passengers = { OldWoman, Fitter, Nurse, Student, Drunk, LongCoat, RedCoat, OldMan };

    public static readonly Look[] Everyone = { Clerk, Mitko, MitkoOffDuty, Vera, WaitingMan, OldWoman, Fitter, Nurse, Student, Drunk, LongCoat, RedCoat, OldMan, Stoyan, Nikola };
}
