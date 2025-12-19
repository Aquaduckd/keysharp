using Raylib_cs;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace Keysharp
{
    public static class FontManager
    {
        public static Font LoadFont()
        {
            // Build list of codepoints we need
            // ASCII range (0-255) plus Unicode characters we use
            List<int> codepoints = new List<int>();
            
            // Add ASCII range (printable characters)
            for (int i = 32; i <= 126; i++) // Space to tilde (printable ASCII)
            {
                codepoints.Add(i);
            }
            
            // Add extended ASCII/Latin-1 Supplement (128-255)
            for (int i = 128; i <= 255; i++)
            {
                codepoints.Add(i);
            }
            
            // Add specific Unicode characters we use:
            // ▲ U+25B2 (Black Up-Pointing Triangle)
            // ▼ U+25BC (Black Down-Pointing Triangle)
            // • U+2022 (Bullet - used in regex help tips)
            // ✓ U+2713 (Check Mark - used in View menu)
            codepoints.Add(0x25B2); // ▲
            codepoints.Add(0x25BC); // ▼
            codepoints.Add(0x2022); // •
            codepoints.Add(0x2713); // ✓

            int[] codepointArray = codepoints.ToArray();
            int fontSize = 32; // Base font size for loading

            // Try DejaVu Sans first (has better Unicode coverage, especially geometric shapes)
            string[] dejavuPaths = new[]
            {
                "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf",
                "/usr/share/fonts/TTF/DejaVuSans.ttf",
                "/usr/share/fonts/dejavu/DejaVuSans.ttf",
                "/usr/local/share/fonts/dejavu/DejaVuSans.ttf",
                "/opt/local/share/fonts/dejavu/DejaVuSans.ttf",
                "~/.fonts/DejaVuSans.ttf",
                "~/.local/share/fonts/DejaVuSans.ttf",
            };

            foreach (string fontPath in dejavuPaths)
            {
                string expandedPath = fontPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if (File.Exists(expandedPath))
                {
                    try
                    {
                        Font font = Raylib.LoadFontEx(expandedPath, fontSize, codepointArray, codepointArray.Length);
                        if (font.Texture.Id != 0)
                        {
                            Raylib.SetTextureFilter(font.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
                            System.Console.WriteLine($"Loaded DejaVu Sans from: {expandedPath}");
                            return font;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error loading DejaVu Sans from {expandedPath}: {ex.Message}");
                    }
                }
            }

            // Fallback to Ubuntu if DejaVu Sans not found
            string[] ubuntuPaths = new[]
            {
                "/usr/share/fonts/truetype/ubuntu/Ubuntu-R.ttf",
                "/usr/share/fonts/TTF/Ubuntu-R.ttf",
                "/usr/local/share/fonts/ubuntu/Ubuntu-R.ttf",
                "~/.fonts/Ubuntu-R.ttf",
                "~/.local/share/fonts/Ubuntu-R.ttf",
            };

            foreach (string fontPath in ubuntuPaths)
            {
                string expandedPath = fontPath.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
                if (File.Exists(expandedPath))
                {
                    try
                    {
                        Font font = Raylib.LoadFontEx(expandedPath, fontSize, codepointArray, codepointArray.Length);
                        if (font.Texture.Id != 0)
                        {
                            Raylib.SetTextureFilter(font.Texture, TextureFilter.TEXTURE_FILTER_BILINEAR);
                            System.Console.WriteLine($"Loaded Ubuntu from: {expandedPath}");
                            return font;
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Console.WriteLine($"Error loading Ubuntu from {expandedPath}: {ex.Message}");
                    }
                }
            }

            System.Console.WriteLine("Warning: Neither DejaVu Sans nor Ubuntu font found, using default font");
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

