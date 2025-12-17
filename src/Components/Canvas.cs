using Raylib_cs;

namespace Keysharp.Components
{
    /// <summary>
    /// A canvas component that provides a drawing area for custom content.
    /// Unlike Container, Canvas doesn't automatically layout children - it provides
    /// a fixed drawing area that can be used for custom rendering.
    /// </summary>
    public class Canvas : UIElement
    {
        public Canvas(string name = "Canvas") : base(name)
        {
            // Canvas is not interactive by default
            IsClickable = false;
            IsHoverable = false;
            
            // Canvas doesn't auto-layout children
            AutoLayoutChildren = false;
            
            // Canvas doesn't auto-size - size must be set explicitly
            AutoSize = false;
        }

        protected override void DrawSelf()
        {
            // Canvas is transparent by default - derived classes or children draw content
            // Optionally draw a background or border if needed
        }
    }
}

