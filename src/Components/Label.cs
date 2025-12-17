using Raylib_cs;
using Keysharp.UI;

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
            if (!string.IsNullOrEmpty(text) && Bounds.Width > 0 && Bounds.Height > 0)
            {
                if (rightAlign)
                {
                    // Draw right-aligned text within the bounds
                    TextContainer.DrawRightAlignedText(font, text, Bounds, fontSize, color, 10);
                }
                else
                {
                    // Draw left-aligned text within the bounds
                    TextContainer.DrawLeftAlignedText(font, text, Bounds, fontSize, color, 0);
                }
            }
        }
    }
}

