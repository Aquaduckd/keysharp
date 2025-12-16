using Raylib_cs;

namespace Keysharp.UI
{
    public static class TextContainer
    {
        public static void DrawCenteredText(Font font, string text, Rectangle bounds, int fontSize, Color color)
        {
            float textWidth = FontManager.MeasureText(font, text, fontSize);
            float textHeight = fontSize;
            
            int textX = (int)(bounds.X + (bounds.Width - textWidth) / 2);
            int textY = (int)(bounds.Y + (bounds.Height - textHeight) / 2);
            
            FontManager.DrawText(font, text, textX, textY, fontSize, color);
        }

        public static void DrawLeftAlignedText(Font font, string text, Rectangle bounds, int fontSize, Color color, int padding = 0)
        {
            float textHeight = fontSize;
            
            int textX = (int)bounds.X + padding;
            int textY = (int)(bounds.Y + (bounds.Height - textHeight) / 2);
            
            FontManager.DrawText(font, text, textX, textY, fontSize, color);
        }

        public static void DrawRightAlignedText(Font font, string text, Rectangle bounds, int fontSize, Color color, int padding = 0)
        {
            float textWidth = FontManager.MeasureText(font, text, fontSize);
            float textHeight = fontSize;
            
            int textX = (int)(bounds.X + bounds.Width - textWidth - padding);
            int textY = (int)(bounds.Y + (bounds.Height - textHeight) / 2);
            
            FontManager.DrawText(font, text, textX, textY, fontSize, color);
        }
    }
}

