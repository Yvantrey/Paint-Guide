using TMPro;
using UnityEngine;

// RUGA's look, taken from the clickable prototype. Change values here to restyle the whole app.
public static class UITheme
{
    public static readonly Color Background = Hex("#141918");
    public static readonly Color Card = Hex("#242b28");
    public static readonly Color CardSelected = Hex("#303a2d");
    public static readonly Color Ink = Hex("#f0f1e9");
    public static readonly Color Muted = Hex("#a7b1aa");
    public static readonly Color Line = Hex("#3a443e");
    public static readonly Color Accent = Hex("#cfdfa1");
    public static readonly Color Deep = Hex("#1b2619");
    public static readonly Color ToastBackground = Hex("#e2edcf");
    public static readonly Color Warning = Hex("#ffe07a");
    public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.6f);
    public static readonly Color PanelBackground = new Color(0.078f, 0.098f, 0.094f, 0.94f);

    // Same proportions as the prototype's 390 pt wide phone.
    public static readonly Vector2 ReferenceResolution = new Vector2(390f, 844f);
    public const float Gutter = 22f;
    public const float ButtonHeight = 52f;
    public const float SmallButtonHeight = 46f;

    // Set by UIManager from its Inspector fields; null means TextMeshPro's default font.
    public static TMP_FontAsset TitleFont;
    public static TMP_FontAsset BodyFont;
    // Phosphor icon font; null means buttons show their text only.
    public static TMP_FontAsset IconFont;

    static Sprite rounded;
    static Sprite circle;

    // Rounded rectangle, 9-sliced so it stretches to any size with 16 px corners.
    public static Sprite Rounded => rounded != null ? rounded : rounded = CreateRoundedSprite(64, 16);
    public static Sprite Circle => circle != null ? circle : circle = CreateRoundedSprite(64, 32);

    static Sprite CreateRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RUGA Rounded",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Distance from the nearest corner circle's centre, for anti-aliased corners.
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                byte a = (byte)(Mathf.Clamp01(radius - d + 0.5f) * 255f);
                px[y * size + x] = new Color32(255, 255, 255, a);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);

        var border = radius < size / 2 ? new Vector4(radius, radius, radius, radius) : Vector4.zero;
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
    }

    static Color Hex(string hex) => ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
}
