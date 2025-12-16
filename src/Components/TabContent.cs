using Raylib_cs;

namespace Keysharp.Components
{
    /// <summary>
    /// A container for tab content that can be shown/hidden.
    /// </summary>
    public class TabContent : Container
    {
        private Font font;
        private string title;
        private string? subtitle;

        public TabContent(Font font, string title, string? subtitle = null) : base($"TabContent_{title}")
        {
            this.font = font;
            this.title = title;
            this.subtitle = subtitle;
        }

        protected override void DrawSelf()
        {
            // Draw title (relative to bounds)
            if (!string.IsNullOrEmpty(title))
            {
                FontManager.DrawText(font, title, (int)Bounds.X + 20, (int)Bounds.Y + 20, 24, UITheme.TextColor);
            }

            // Draw subtitle (relative to bounds)
            if (!string.IsNullOrEmpty(subtitle))
            {
                FontManager.DrawText(font, subtitle, (int)Bounds.X + 20, (int)Bounds.Y + 60, 16, UITheme.TextSecondaryColor);
            }
        }
    }
}

