using Raylib_cs;
using Keysharp.UI;

namespace Keysharp.Panels
{
    public abstract class Panel : UIElement
    {
        protected Font Font { get; }

        protected Panel(Font font, string name) : base(name)
        {
            Font = font;
        }

        public override void Draw()
        {
            // Draw panel background and borders
            DrawPanelContent(Bounds);

            // Draw all children recursively
            base.Draw();
        }

        protected abstract void DrawPanelContent(Rectangle bounds);

        // Override to update bounds when panel is drawn
        public virtual void UpdateBounds(Rectangle bounds)
        {
            Bounds = bounds;
        }
    }
}

