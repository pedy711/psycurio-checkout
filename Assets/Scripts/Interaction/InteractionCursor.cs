using UnityEngine;

namespace PsyCurio.Shop.Interaction
{
    /// <summary>
    /// Swaps the OS cursor for a generated ring-and-dot pointer over clickable
    /// objects. The texture is drawn in code — no licensed art anywhere in the
    /// project — with a dark outline so it reads on light and dark surfaces.
    /// </summary>
    public static class InteractionCursor
    {
        private const int Size = 32;
        private static Texture2D pointerTexture;
        private static bool pointerShown;

        public static void ShowPointer(bool show)
        {
            if (show == pointerShown)
            {
                return;
            }
            pointerShown = show;

            if (show)
            {
                Cursor.SetCursor(PointerTexture(), new Vector2(Size / 2f, Size / 2f), CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        private static Texture2D PointerTexture()
        {
            if (pointerTexture != null)
            {
                return pointerTexture;
            }

            pointerTexture = new Texture2D(Size, Size, TextureFormat.RGBA32, false);
            var center = (Size - 1) / 2f;
            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(center, center));
                    Color color;
                    if (distance < 3.5f)
                    {
                        color = Color.white;                      // centre dot
                    }
                    else if (distance > 9f && distance < 12f)
                    {
                        color = Color.white;                      // ring
                    }
                    else if (distance < 4.5f || (distance > 8f && distance < 13f))
                    {
                        color = new Color(0.1f, 0.1f, 0.1f, 0.9f); // outline
                    }
                    else
                    {
                        color = Color.clear;
                    }
                    pointerTexture.SetPixel(x, y, color);
                }
            }
            pointerTexture.Apply();
            return pointerTexture;
        }
    }
}
