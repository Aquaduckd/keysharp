using Raylib_cs;

namespace Keysharp.Panels
{
    public class BottomPanel : Panel
    {
        public string Status { get; set; } = "Ready";
        public string Version { get; set; } = "1.0.0";

        public BottomPanel(Font font) : base(font, "BottomPanel")
        {
        }

        public override void Draw(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.BottomPanelColor);
            
            // Draw borders only on outer edges (left, bottom, right)
            // Top edge is handled by the horizontal splitter
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Left
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Bottom
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Right
        }
    }
}

