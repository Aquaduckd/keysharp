using Raylib_cs;
using System;
using System.IO;

namespace Keysharp
{
    public static class FontManager
    {
        public static Font LoadFont()
        {
            string[] fontPaths = new[]
            {
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/TTF/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/usr/local/share/fonts/dejavu/DejaVuSans.ttf",
                "/opt/local/share/fonts/dejavu/DejaVuSans.ttf",
                "~/.fonts/DejaVuSans.ttf",
                "~/.local/share/fonts/DejaVuSans.ttf",
            };

            foreach (string fontPath in fontPaths)
            {
                string expandedPath = fontPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if (File.Exists(expandedPath))
                {
                    try
                    {
                        Font font = Raylib.LoadFont(expandedPath);
                        if (font.Texture.Id != 0)
                        {
                            Raylib.SetTextureFilter(font.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
                            System.Console.WriteLine($"Loaded font from: {expandedPath}");
                            return font;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error loading font from {expandedPath}: {ex.Message}");
                    }
                }
            }

            System.Console.WriteLine("Warning: DejaVu Sans not found, using default font");
            return Raylib.GetFontDefault();
        }

        public static void DrawText(Font font, string text, int x, int y, int fontSize, Color color)
        {
            if (font.Texture.Id == 0)
            {
                Raylib.DrawText(text, x, y, fontSize, color);
            }
            else
            {
                Raylib.DrawTextEx(font, text, new System.Numerics.Vector2(x, y), fontSize, 0f, color);
            }
        }

        public static float MeasureText(Font font, string text, int fontSize)
        {
            if (font.Texture.Id == 0)
            {
                return Raylib.MeasureText(text, fontSize);
            }
            else
            {
                return Raylib.MeasureTextEx(font, text, fontSize, 0).X;
            }
        }
    }
}

