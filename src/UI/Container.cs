using Raylib_cs;

namespace Keysharp.UI
{
    /// <summary>
    /// A generic container element that acts like a "div" - used for grouping and laying out child elements.
    /// </summary>
    public class Container : UIElement
    {
        public Container(string name = "Container") : base(name)
        {
            // Containers are not interactive by default
            IsClickable = false;
            IsHoverable = false;
            
            // Enable auto-layout by default for convenience
            AutoLayoutChildren = true;
            
            // Enable auto-sizing by default so containers fit their children
            AutoSize = true;
        }

        public override void Draw()
        {
            // Containers don't draw anything by default (transparent)
            // Override if you need a background or border
            base.Draw();
        }
    }
}

