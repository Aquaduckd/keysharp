using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public abstract class Panel : Components.UIElement
    {
        protected Font Font { get; }

        protected Panel(Font font, string name) : base(name)
        {
            Font = font;
            
            // Panels are not directly clickable or hoverable
            // Their children handle interactions
            IsClickable = false;
            IsHoverable = false;
        }

        protected override void DrawSelf()
        {
            // Draw panel background and borders
            DrawPanelContent(Bounds);
        }

        protected abstract void DrawPanelContent(Rectangle bounds);

        // Override to update bounds when panel is drawn
        public virtual void UpdateBounds(Rectangle bounds)
        {
            Bounds = bounds;
        }
    }
}

