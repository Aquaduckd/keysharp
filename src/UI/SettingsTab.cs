using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class SettingsTab
    {
        private Components.TabContent tabContent;

        public Components.TabContent TabContent => tabContent;

        private Font font;

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update fonts in components (would need UpdateFont methods)
        }

        public SettingsTab(Font font)
        {
            this.font = font;
            tabContent = new Components.TabContent(font, "Settings", "Application settings and preferences");
            tabContent.PositionMode = Components.PositionMode.Relative;
        }

        public void Update(Rectangle contentArea)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }
    }
}

