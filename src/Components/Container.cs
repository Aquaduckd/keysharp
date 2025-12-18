using Raylib_cs;

namespace Keysharp.Components
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

        #region Helper Methods for Common Container Patterns

        /// <summary>
        /// Creates a container configured for vertical layout with relative positioning.
        /// Use this helper to avoid common mistakes when setting up vertical containers.
        /// </summary>
        /// <param name="name">Container name for debugging</param>
        /// <param name="padding">Padding around children (default: 0)</param>
        /// <param name="gap">Gap between children (default: 0)</param>
        /// <param name="autoSize">Whether to auto-size based on children (default: true)</param>
        /// <returns>A Container configured for vertical layout</returns>
        public static Container CreateVertical(string name, float padding = 0, float gap = 0, bool autoSize = true)
        {
            var container = new Container(name);
            container.AutoLayoutChildren = true;
            container.LayoutDirection = LayoutDirection.Vertical;
            container.ChildPadding = padding;
            container.ChildGap = gap;
            container.AutoSize = autoSize;
            container.PositionMode = PositionMode.Relative;
            container.RelativePosition = new System.Numerics.Vector2(0, 0);
            return container;
        }

        /// <summary>
        /// Creates a container configured for horizontal layout with relative positioning.
        /// Use this helper to avoid common mistakes when setting up horizontal containers.
        /// </summary>
        /// <param name="name">Container name for debugging</param>
        /// <param name="padding">Padding around children (default: 0)</param>
        /// <param name="gap">Gap between children (default: 0)</param>
        /// <param name="autoSize">Whether to auto-size based on children (default: true)</param>
        /// <returns>A Container configured for horizontal layout</returns>
        public static Container CreateHorizontal(string name, float padding = 0, float gap = 0, bool autoSize = true)
        {
            var container = new Container(name);
            container.AutoLayoutChildren = true;
            container.LayoutDirection = LayoutDirection.Horizontal;
            container.ChildPadding = padding;
            container.ChildGap = gap;
            container.AutoSize = autoSize;
            container.PositionMode = PositionMode.Relative;
            container.RelativePosition = new System.Numerics.Vector2(0, 0);
            return container;
        }

        #endregion
    }
}

