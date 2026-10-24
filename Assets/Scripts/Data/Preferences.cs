using UnityEngine;

public enum GuideColour
{
    Cyan,
    Yellow,
    White
}

public class Preferences
{
    public const float MinOpacity = 0.1f;
    public const float MaxOpacity = 1f;

    public GuideColour GuideColour = GuideColour.Cyan;
    public float DefaultOpacity = 0.5f;
}

public static class GuideColours
{
    // Same colours as the prototype.
    static readonly Color Cyan = new Color32(0x67, 0xe8, 0xf9, 0xff);
    static readonly Color Yellow = new Color32(0xff, 0xe0, 0x7a, 0xff);

    public static Color ToColor(GuideColour colour)
    {
        switch (colour)
        {
            case GuideColour.Yellow: return Yellow;
            case GuideColour.White: return Color.white;
            default: return Cyan;
        }
    }
}
