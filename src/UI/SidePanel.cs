using Raylib_cs;

namespace Keysharp.UI
{
    public class SidePanel : Panel
    {
        public SidePanel(Font font) : base(font, "SidePanel")
        {
        }

        protected override void DrawPanelContent(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.SidePanelColor);
            
            // Draw borders only on outer edges (left, top, bottom)
            // Right edge is handled by the vertical splitter
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Left
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                1, UITheme.BorderColor); // Top
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Bottom
        }
    }
}

