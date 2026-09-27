using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace InventoryPeek;

/// <summary>
/// Makes one inventory window's background solid. The window prefab draws its background with an Image whose colour
/// alpha is 0.75 and whose sprite (inv-window) fills the inside of its frame at alpha 0.75 too, so about 56% of it is
/// window and the rest is the world behind. Raising the colour alpha alone leaves the sprite's 0.75, so the sprite is
/// swapped for a copy whose inside is opaque. Game code never writes this Image (only the prefab sets it), so it is
/// set once and restored once: nothing is re-applied per frame.
/// </summary>
internal sealed class OpaqueBackground
{
    private readonly Image _image;
    private Sprite _gameSprite;
    private Sprite _opaqueSprite;
    private float _gameAlpha;

    internal OpaqueBackground(Image image)
    {
        _image = image;
    }

    internal bool Applied { get; private set; }

    internal void Apply()
    {
        if (Applied || _image == null || _image.sprite == null)
        {
            return;
        }

        _gameSprite = _image.sprite;
        _gameAlpha = _image.color.a;
        _opaqueSprite = OpaqueSprites.Of(_gameSprite);
        _image.sprite = _opaqueSprite;
        _image.color = WithAlpha(_image.color, 1f);
        Applied = true;
    }

    internal void Restore()
    {
        if (!Applied)
        {
            return;
        }

        Applied = false;
        if (_image == null)
        {
            return;
        }

        // Put back only what is still ours, in case something else has set the background since.
        if (_image.sprite == _opaqueSprite)
        {
            _image.sprite = _gameSprite;
        }

        if (Mathf.Approximately(_image.color.a, 1f))
        {
            _image.color = WithAlpha(_image.color, _gameAlpha);
        }
    }

    private static Color WithAlpha(Color colour, float alpha)
    {
        colour.a = alpha;
        return colour;
    }

    /// <summary>One opaque copy per game sprite, shared by every window that uses it.</summary>
    private static class OpaqueSprites
    {
        private const byte Opaque = 255;
        private static readonly Dictionary<Sprite, Sprite> Copies = new Dictionary<Sprite, Sprite>();

        internal static Sprite Of(Sprite sprite)
        {
            if (!Copies.TryGetValue(sprite, out Sprite copy))
            {
                copy = Create(sprite);
                Copies[sprite] = copy;
            }

            return copy;
        }

        // The texture is not CPU-readable, so it is copied through the GPU. Everything reachable from the centre
        // without crossing the opaque frame or the transparent outside becomes opaque; the frame and the
        // anti-aliased rounded corners keep their pixels. A sprite that cannot be copied is used as it is, which
        // still leaves the colour alpha raised.
        private static Sprite Create(Sprite sprite)
        {
            Texture2D pixels;
            try
            {
                pixels = GpuPixels.Read(sprite.texture, sprite.textureRect);
            }
            catch (System.Exception e)
            {
                InventoryPeekPlugin.Instance?.LogWarning($"Cannot copy window background '{sprite.name}': {e.Message}");
                return sprite;
            }

            if (!FillInside(pixels))
            {
                Object.Destroy(pixels);
                return sprite;
            }

            Vector2 pivot = new Vector2(sprite.pivot.x / sprite.rect.width, sprite.pivot.y / sprite.rect.height);
            Sprite opaque = Sprite.Create(pixels, new Rect(0f, 0f, pixels.width, pixels.height), pivot,
                sprite.pixelsPerUnit, 0, SpriteMeshType.FullRect, sprite.border);
            opaque.name = sprite.name + " (opaque)";
            return opaque;
        }

        /// <summary>Flood-fills the see-through inside to opaque. False when the centre is not see-through.</summary>
        private static bool FillInside(Texture2D texture)
        {
            int width = texture.width;
            int height = texture.height;
            Color32[] pixels = texture.GetPixels32();
            int centre = height / 2 * width + width / 2;
            if (!SeeThrough(pixels[centre]))
            {
                return false;
            }

            // A List used as a stack: Stack<T> is ambiguous between the game's System and mscorlib.
            List<int> pending = new List<int> { centre };
            while (pending.Count > 0)
            {
                int index = pending[pending.Count - 1];
                pending.RemoveAt(pending.Count - 1);
                if (!SeeThrough(pixels[index]))
                {
                    continue;
                }

                pixels[index].a = Opaque;
                int x = index % width;
                int y = index / width;
                if (x > 0) pending.Add(index - 1);
                if (x < width - 1) pending.Add(index + 1);
                if (y > 0) pending.Add(index - width);
                if (y < height - 1) pending.Add(index + width);
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return true;
        }

        private static bool SeeThrough(Color32 pixel) => pixel.a > 0 && pixel.a < Opaque;
    }
}
