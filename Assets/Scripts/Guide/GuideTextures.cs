using UnityEngine;

// Textures RUGA draws itself: the alignment grid, and plain test patterns that stand in
// for artwork that hasn't been added yet. The test patterns are deliberately not art.
public static class GuideTextures
{
    // 3:4 portrait, the same shape as the artwork spec.
    const int Width = 384;
    const int Height = 512;

    static readonly Color32 White = new Color32(255, 255, 255, 255);
    static readonly Color32 Clear = new Color32(255, 255, 255, 0);

    static Texture2D grid;
    static Texture2D finalPlaceholder;

    // Border plus lines at the thirds, like the prototype's grid.
    public static Texture2D Grid
    {
        get
        {
            if (grid == null)
            {
                var px = NewCanvas(Clear);
                DrawBorder(px, 4, White);
                for (int i = 1; i < 3; i++)
                {
                    FillRect(px, Width * i / 3 - 1, 0, Width * i / 3 + 2, Height, White);
                    FillRect(px, 0, Height * i / 3 - 1, Width, Height * i / 3 + 2, White);
                }
                grid = ToTexture(px, "RUGA Grid");
            }
            return grid;
        }
    }

    // Test pattern: border, a 4 x 4 grid, an arrow marking the top edge, and one dot per stage number.
    public static Texture2D Placeholder(int stageIndex, int stageCount)
    {
        var px = NewCanvas(Clear);
        DrawBorder(px, 6, White);
        for (int i = 1; i < 4; i++)
        {
            FillRect(px, Width * i / 4 - 1, 0, Width * i / 4 + 1, Height, White);
            FillRect(px, 0, Height * i / 4 - 1, Width, Height * i / 4 + 1, White);
        }

        FillTriangleUp(px, Width / 2, Height - 40, 36, White);

        const int dotRadius = 10;
        int spacing = dotRadius * 3;
        int startX = Width / 2 - (stageIndex * spacing) / 2;
        for (int i = 0; i <= stageIndex; i++)
            FillCircle(px, startX + i * spacing, 48, dotRadius, White);

        return ToTexture(px, $"RUGA Placeholder {stageIndex + 1}/{stageCount}");
    }

    // A plain card shown in the catalog and summary until the finished picture exists.
    public static Texture2D FinalPlaceholder()
    {
        if (finalPlaceholder == null)
        {
            var px = NewCanvas(new Color32(0xea, 0xdf, 0xcf, 0xff));                                 // parchment
            DrawBorder(px, 6, new Color32(0xc4, 0x64, 0x3f, 0xff));                                 // terracotta
            FillTriangleUp(px, Width / 2, Height / 2 + 40, 60, new Color32(0x93, 0xa8, 0x88, 0xff)); // sage
            finalPlaceholder = ToTexture(px, "RUGA Final Placeholder");
        }
        return finalPlaceholder;
    }

    static Color32[] NewCanvas(Color32 fill)
    {
        var px = new Color32[Width * Height];
        for (int i = 0; i < px.Length; i++)
            px[i] = fill;
        return px;
    }

    static void DrawBorder(Color32[] px, int thickness, Color32 c)
    {
        FillRect(px, 0, 0, Width, thickness, c);
        FillRect(px, 0, Height - thickness, Width, Height, c);
        FillRect(px, 0, 0, thickness, Height, c);
        FillRect(px, Width - thickness, 0, Width, Height, c);
    }

    // Fills x in [x0, x1) and y in [y0, y1). y = 0 is the bottom row.
    static void FillRect(Color32[] px, int x0, int y0, int x1, int y1, Color32 c)
    {
        x0 = Mathf.Clamp(x0, 0, Width);
        x1 = Mathf.Clamp(x1, 0, Width);
        y0 = Mathf.Clamp(y0, 0, Height);
        y1 = Mathf.Clamp(y1, 0, Height);
        for (int y = y0; y < y1; y++)
            for (int x = x0; x < x1; x++)
                px[y * Width + x] = c;
    }

    static void FillTriangleUp(Color32[] px, int tipX, int tipY, int size, Color32 c)
    {
        for (int row = 0; row < size; row++)
            FillRect(px, tipX - row, tipY - row, tipX + row + 1, tipY - row + 1, c);
    }

    static void FillCircle(Color32[] px, int cx, int cy, int r, Color32 c)
    {
        for (int y = -r; y <= r; y++)
            for (int x = -r; x <= r; x++)
                if (x * x + y * y <= r * r)
                    FillRect(px, cx + x, cy + y, cx + x + 1, cy + y + 1, c);
    }

    static Texture2D ToTexture(Color32[] px, string name)
    {
        var tex = new Texture2D(Width, Height, TextureFormat.RGBA32, true)
        {
            name = name,
            wrapMode = TextureWrapMode.Clamp
        };
        tex.SetPixels32(px);
        tex.Apply(true, true); // build mipmaps, then free the CPU copy
        return tex;
    }
}
