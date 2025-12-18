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
        private TextAlignment alignment;

        public enum TextAlignment
        {
            Left,
            Center,
            Right
        }

        public Label(Font font, string text, int fontSize = 14, Color? color = null, TextAlignment alignment = TextAlignment.Left) 
            : base("Label")
        {
            this.font = font;
            this.text = text;
            this.fontSize = fontSize;
            this.color = color ?? UITheme.TextColor;
            this.alignment = alignment;
            
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
                int startY = (int)Bounds.Y + (int)ChildPadding;
                
                foreach (string line in lines)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        int y = startY;
                        float textWidth = FontManager.MeasureText(font, line, fontSize);
                        int textX = alignment switch
                        {
                            TextAlignment.Center => (int)(Bounds.X + (Bounds.Width - textWidth) / 2),
                            TextAlignment.Right => (int)(Bounds.X + Bounds.Width - textWidth - 10),
                            _ => (int)Bounds.X + (int)ChildPadding
                        };
                        FontManager.DrawText(font, line, textX, y, fontSize, color);
                    }
                    startY += lineHeight;
                }
            }
            else
            {
                // Single line text - center vertically and align horizontally
                int textY = (int)(Bounds.Y + (Bounds.Height - fontSize) / 2);
                
                switch (alignment)
                {
                    case TextAlignment.Center:
                        TextContainer.DrawCenteredText(font, text, Bounds, fontSize, color);
                        break;
                    case TextAlignment.Right:
                        TextContainer.DrawRightAlignedText(font, text, Bounds, fontSize, color, 10);
                        break;
                    default:
                        FontManager.DrawText(font, text, (int)Bounds.X + (int)ChildPadding, textY, fontSize, color);
                        break;
                }
            }
        }
    }
}

