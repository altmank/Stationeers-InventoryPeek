using System.Collections.Generic;
using UnityEngine;

namespace InventoryPeek;

/// <summary>
/// The eye drawn on the game's own title-bar button tiles. The game bakes tile and icon into one sprite per state
/// (inv-sort_normal at rest, inv-sort under the pointer), so the tile is lifted out of the sort button's sprites: the
/// rim (outline, bevel, rounded corners, drop shadow) is kept, the inside where the arrows are is refilled with the
/// tile's own fill, and the eye is drawn in that sprite's icon colour with a drop shadow like the game's icons.
/// Faint is the untagged eye, drawn at a third of the strength.
/// </summary>
internal sealed class EyeSprites
{
    private const string RestSuffix = "_normal";
    private const float FaintStrength = 0.35f;
    private static readonly Dictionary<Sprite, EyeSprites> Cache = new Dictionary<Sprite, EyeSprites>();

    private readonly Sprite _rest;
    private readonly Sprite _restFaint;
    private readonly Sprite _hover;
    private readonly Sprite _hoverFaint;

    private EyeSprites(Tile rest, Tile hover)
    {
        _rest = Draw(rest, 1f);
        _restFaint = Draw(rest, FaintStrength);
        _hover = Draw(hover, 1f);
        _hoverFaint = Draw(hover, FaintStrength);
    }

    internal Sprite For(bool tagged, bool hovered) => (tagged, hovered) switch
    {
        (true, true) => _hover,
        (true, false) => _rest,
        (false, true) => _hoverFaint,
        (false, false) => _restFaint,
    };

    /// <summary>One set per game button sprite, shared by every window.</summary>
    internal static EyeSprites Of(Sprite rest)
    {
        if (!Cache.TryGetValue(rest, out EyeSprites sprites))
        {
            sprites = new EyeSprites(Tile.Of(rest), Tile.Of(HoverOf(rest)));
            Cache[rest] = sprites;
        }

        return sprites;
    }

    // Only the animator clips reference the hover sprite, so it is found by name among the loaded sprites.
    private static Sprite HoverOf(Sprite rest)
    {
        if (rest.name.EndsWith(RestSuffix, System.StringComparison.Ordinal))
        {
            string name = rest.name.Substring(0, rest.name.Length - RestSuffix.Length);
            foreach (Sprite sprite in Resources.FindObjectsOfTypeAll<Sprite>())
            {
                if (sprite.name == name && sprite.texture != null)
                {
                    return sprite;
                }
            }
        }

        InventoryPeekPlugin.Instance?.LogWarning($"No hover sprite for '{rest.name}'; the eye keeps its rest tile on hover.");
        return rest;
    }

    private static Sprite Draw(Tile tile, float strength)
    {
        Texture2D texture = new Texture2D(tile.Width, tile.Height, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp,
        };
        texture.SetPixels32(Eye.DrawOnto(tile.Pixels, tile.Width, tile.Height, tile.IconColour, strength));
        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, tile.Width, tile.Height), new Vector2(0.5f, 0.5f),
            tile.PixelsPerUnit, 0, SpriteMeshType.FullRect);
        sprite.name = "InventoryPeekEye " + tile.Name;
        return sprite;
    }

    /// <summary>A game button sprite with its icon taken out, and the colour that icon was drawn in.</summary>
    private sealed class Tile
    {
        // Depth of the rim in the game's 48 px button sprites (tile edge at 4 px, bevel to 7 px); the icons start
        // further in. Rows and columns this deep from the top-left corner are free of icon pixels.
        private const int Rim = 8;
        private const byte Opaque = 255;

        private Tile(string name, int width, int height, Color32[] pixels, Color32 iconColour, float pixelsPerUnit)
        {
            Name = name;
            Width = width;
            Height = height;
            Pixels = pixels;
            IconColour = iconColour;
            PixelsPerUnit = pixelsPerUnit;
        }

        internal string Name { get; }
        internal int Width { get; }
        internal int Height { get; }
        internal Color32[] Pixels { get; }
        internal Color32 IconColour { get; }
        internal float PixelsPerUnit { get; }

        internal static Tile Of(Sprite sprite)
        {
            int width = Mathf.Max((int)sprite.rect.width, 1);
            int height = Mathf.Max((int)sprite.rect.height, 1);
            Color32 white = new Color32(Opaque, Opaque, Opaque, Opaque);
            Color32[] pixels;
            try
            {
                pixels = Copy(sprite, width, height);
            }
            catch (System.Exception e)
            {
                InventoryPeekPlugin.Instance?.LogWarning($"Cannot copy button sprite '{sprite.name}': {e.Message}");
                return new Tile(sprite.name, width, height, new Color32[width * height], white, sprite.pixelsPerUnit);
            }

            return new Tile(sprite.name, width, height, WithoutIcon(pixels, width, height),
                BrightestInside(pixels, width, height), sprite.pixelsPerUnit);
        }

        private static Color32[] Copy(Sprite sprite, int width, int height)
        {
            if (sprite.packed || width <= 2 * Rim || height <= 2 * Rim)
            {
                throw new System.InvalidOperationException($"unexpected layout {width}x{height}, packed {sprite.packed}");
            }

            Texture2D texture = GpuPixels.Read(sprite.texture, sprite.rect);
            try
            {
                return texture.GetPixels32();
            }
            finally
            {
                Object.Destroy(texture);
            }
        }

        // Corners are kept as they are, the side rims repeat one icon-free row or column, the inside is the fill.
        private static Color32[] WithoutIcon(Color32[] pixels, int width, int height)
        {
            int referenceRow = height - 1 - Rim;
            int referenceColumn = Rim;
            Color32 fill = pixels[referenceRow * width + referenceColumn];
            Color32[] tile = new Color32[pixels.Length];
            for (int y = 0; y < height; y++)
            {
                bool rimRow = y < Rim || y >= height - Rim;
                for (int x = 0; x < width; x++)
                {
                    bool rimColumn = x < Rim || x >= width - Rim;
                    tile[y * width + x] = (rimRow, rimColumn) switch
                    {
                        (true, true) => pixels[y * width + x],
                        (false, true) => pixels[referenceRow * width + x],
                        (true, false) => pixels[y * width + referenceColumn],
                        (false, false) => fill,
                    };
                }
            }

            return tile;
        }

        private static Color32 BrightestInside(Color32[] pixels, int width, int height)
        {
            Color32 brightest = new Color32(Opaque, Opaque, Opaque, Opaque);
            int best = -1;
            for (int y = Rim; y < height - Rim; y++)
            {
                for (int x = Rim; x < width - Rim; x++)
                {
                    Color32 pixel = pixels[y * width + x];
                    int brightness = pixel.r + pixel.g + pixel.b;
                    if (pixel.a == Opaque && brightness > best)
                    {
                        best = brightness;
                        brightest = pixel;
                    }
                }
            }

            return brightest;
        }
    }

    /// <summary>An almond outline with a round pupil, anti-aliased, sized like the game's 48 px icons.</summary>
    private static class Eye
    {
        private const float DesignSize = 48f;
        private const float HalfWidth = 16f;
        private const float LidHeight = 10f;
        private const float HalfStroke = 2.25f;
        private const float PupilRadius = 5.5f;
        private const float ShadowOffset = 1.5f;
        private const float ShadowAlpha = 0.5f;

        internal static Color32[] DrawOnto(Color32[] tile, int width, int height, Color32 colour, float strength)
        {
            float scale = Mathf.Min(width, height) / DesignSize;
            Vector2 centre = new Vector2((width - 1) / 2f, (height - 1) / 2f);
            Vector2 shadow = new Vector2(ShadowOffset, -ShadowOffset) * scale;
            Color32 black = new Color32(0, 0, 0, 0);
            Color32[] pixels = new Color32[tile.Length];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2 point = new Vector2(x, y);
                    float shade = Coverage(point - shadow - centre, scale) * ShadowAlpha * strength;
                    float ink = Coverage(point - centre, scale) * strength;
                    int index = y * width + x;
                    pixels[index] = Over(Over(tile[index], black, shade), colour, ink);
                }
            }

            return pixels;
        }

        // Point relative to the eye's centre, in texture pixels.
        private static float Coverage(Vector2 point, float scale)
        {
            float halfWidth = HalfWidth * scale;
            float lidHeight = LidHeight * scale;
            float halfStroke = HalfStroke * scale;
            float u = point.x / halfWidth;
            float lidDistance;
            if (Mathf.Abs(u) <= 1f)
            {
                float lid = lidHeight * (1f - u * u);
                float slope = 2f * lidHeight * u / halfWidth;
                lidDistance = Mathf.Abs(Mathf.Abs(point.y) - lid) / Mathf.Sqrt(1f + slope * slope);
            }
            else
            {
                lidDistance = Vector2.Distance(point, new Vector2(Mathf.Sign(u) * halfWidth, 0f));
            }

            float outline = Mathf.Clamp01(halfStroke + 0.5f - lidDistance);
            float pupil = Mathf.Clamp01(PupilRadius * scale + 0.5f - point.magnitude);
            return Mathf.Max(outline, pupil);
        }

        private static Color32 Over(Color32 below, Color32 colour, float alpha)
        {
            if (alpha <= 0f)
            {
                return below;
            }

            float belowAlpha = below.a / 255f;
            float outAlpha = alpha + belowAlpha * (1f - alpha);
            float Mix(byte top, byte bottom) => (top * alpha + bottom * belowAlpha * (1f - alpha)) / outAlpha;
            return new Color32((byte)Mathf.RoundToInt(Mix(colour.r, below.r)), (byte)Mathf.RoundToInt(Mix(colour.g, below.g)),
                (byte)Mathf.RoundToInt(Mix(colour.b, below.b)), (byte)Mathf.RoundToInt(outAlpha * 255f));
        }
    }
}
