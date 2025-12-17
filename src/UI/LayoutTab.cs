using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class LayoutTab
    {
        private Components.TabContent tabContent;

        public Components.TabContent TabContent => tabContent;

        public LayoutTab(Font font)
        {
            tabContent = new Components.TabContent(font, "Layout", "Layout configuration will appear here.");
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

