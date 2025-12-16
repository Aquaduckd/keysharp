using Raylib_cs;

namespace Keysharp.UI
{
    public class InfoText : UIElement
    {
        private Font font;
        private string text;
        private int fontSize;
        private Color color;

        public InfoText(Font font, string text, int fontSize = 14, Color? color = null) 
            : base("InfoText")
        {
            this.font = font;
            this.text = text;
            this.fontSize = fontSize;
            this.color = color ?? UITheme.TextSecondaryColor;
            
            // Info text is not interactive
            IsClickable = false;
            IsHoverable = false;
        }

        public void SetText(string newText)
        {
            text = newText;
        }

        public override void Draw()
        {
            if (!IsVisible)
                return;

            if (!string.IsNullOrEmpty(text))
            {
                TextContainer.DrawRightAlignedText(font, text, Bounds, fontSize, color, 20);
            }

            base.Draw();
        }
    }
}

