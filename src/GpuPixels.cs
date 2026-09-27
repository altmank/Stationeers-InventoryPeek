using UnityEngine;

namespace InventoryPeek;

/// <summary>Copies of game textures, which are not CPU-readable, taken through the GPU.</summary>
internal static class GpuPixels
{
    internal static Texture2D Read(Texture2D source, Rect area)
    {
        RenderTexture target = RenderTexture.GetTemporary(source.width, source.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        RenderTexture previous = RenderTexture.active;
        try
        {
            Graphics.Blit(source, target);
            RenderTexture.active = target;
            Texture2D pixels = new Texture2D((int)area.width, (int)area.height, TextureFormat.RGBA32, false)
            {
                filterMode = source.filterMode,
                wrapMode = TextureWrapMode.Clamp,
            };
            pixels.ReadPixels(area, 0, 0);
            pixels.Apply();
            return pixels;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
        }
    }
}
