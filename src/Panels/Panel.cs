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

        public abstract void Draw(Rectangle bounds);

        // Override to update bounds when panel is drawn
        public virtual void UpdateBounds(Rectangle bounds)
        {
            Bounds = bounds;
        }
    }
}

