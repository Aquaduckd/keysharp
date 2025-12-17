using Raylib_cs;
using Keysharp.UI;
using Keysharp;

namespace Keysharp.Components
{
    public class Label : UIElement
    {
        private Font font;
        private string text;
        private int fontSize;
        private Color color;
        private bool rightAlign;

        public Label(Font font, string text, int fontSize = 14, Color? color = null, bool rightAlign = false) 
            : base("Label")
        {
            this.font = font;
            this.text = text;
            this.fontSize = fontSize;
            this.color = color ?? UITheme.TextColor;
            this.rightAlign = rightAlign;
            
            // Labels are not interactive
            IsClickable = false;
            IsHoverable = false;
        }

        public void SetText(string newText)
        {
            text = newText;
        }

        public string GetText()
        {
            return text;
        }

        protected override void DrawSelf()
        {
            if (string.IsNullOrEmpty(text))
                return;
                
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            // Draw multiline text line by line
            if (text.Contains('\n'))
            {
                string[] lines = text.Split('\n');
                int lineHeight = fontSize + 4; // Add spacing between lines
                int startY = (int)Bounds.Y;
                
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        int y = startY;
                        if (rightAlign)
                        {
                            float textWidth = FontManager.MeasureText(font, line, fontSize);
                            int textX = (int)(Bounds.X + Bounds.Width - textWidth - 10);
                            FontManager.DrawText(font, line, textX, y, fontSize, color);
                        }
                        else
                        {
                            FontManager.DrawText(font, line, (int)Bounds.X, y, fontSize, color);
                        }
                    }
                    startY += lineHeight;
                }
            }
            else
            {
                // Single line text
                if (rightAlign)
                {
                    TextContainer.DrawRightAlignedText(font, text, Bounds, fontSize, color, 10);
                }
                else
                {
                    FontManager.DrawText(font, text, (int)Bounds.X, (int)Bounds.Y, fontSize, color);
                }
            }
        }
    }
}

