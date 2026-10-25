using TMPro;
using UnityEngine;

// RUGA's look: warm parchment, paper cards, brown ink, terracotta and sage, from the botanical mockups.
// Change values here to restyle the whole app.
public static class UITheme
{
    public static readonly Color Background = Hex("#f3ece0");    // parchment
    public static readonly Color Card = Hex("#fbf7f0");          // paper
    public static readonly Color CardSelected = Hex("#f4e3d6");  // terracotta-tinted paper
    public static readonly Color Ink = Hex("#3a2a20");           // dark brown text
    public static readonly Color Muted = Hex("#7d6b5d");
    public static readonly Color Line = Hex("#ddd0bf");
    public static readonly Color ButtonBorder = Hex("#8c6b57");  // outlined buttons
    public static readonly Color Accent = Hex("#c4643f");        // terracotta
    public static readonly Color Deep = Hex("#fff8f0");          // text and icons on terracotta
    public static readonly Color Sage = Hex("#93a888");
    public static readonly Color Ochre = Hex("#d49a3a");
    public static readonly Color ToastBackground = Hex("#3a2a20");
    public static readonly Color Warning = Hex("#a8432a");
    public static readonly Color Overlay = new Color(0.23f, 0.16f, 0.12f, 0.45f);
    public static readonly Color PanelBackground = new Color(0.984f, 0.969f, 0.941f, 0.96f);
    public static readonly Color ShadowColour = new Color(0.23f, 0.16f, 0.12f, 0.12f);

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
    static Sprite paper;

    // Subtle paper grain, tiled behind the screens. Near-white, so it only slightly varies the colour it's tinted with.
    public static Sprite Paper => paper != null ? paper : paper = CreatePaperSprite(256);

    static Sprite CreatePaperSprite(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "RUGA Paper",
            wrapMode = TextureWrapMode.Repeat,
            filterMode = FilterMode.Bilinear
        };

        var random = new System.Random(7); // fixed seed: same grain every run
        var px = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                // Fine grain plus faint horizontal fibres; tileable because the fibres wrap around.
                float grain = (float)random.NextDouble() * 0.035f;
                float waviness = 3f * Mathf.Sin(x * 2f * Mathf.PI * 2f / size);
                float fibre = 0.012f * Mathf.Sin((y + waviness) * 2f * Mathf.PI * 37f / size);
                byte v = (byte)(255f * Mathf.Clamp01(1f - grain - Mathf.Abs(fibre)));
                px[y * size + x] = new Color32(v, v, v, 255);
            }
        }
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
    }

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
