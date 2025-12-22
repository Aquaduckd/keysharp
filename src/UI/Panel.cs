using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public abstract class Panel : Components.UIElement
    {
        // Static flag to track if mouse is over BottomPanel (to prevent click-through to MainPanel)
        private static bool isMouseOverBottomPanel = false;

        protected Font Font { get; private set; }

        protected Panel(Font font, string name) : base(name)
        {
            Font = font;
            
            // Panels are not directly clickable or hoverable
            // Their children handle interactions
            IsClickable = false;
            IsHoverable = false;
        }

        /// <summary>
        /// Checks if the mouse is currently over the BottomPanel.
        /// MainPanel and its children should check this before processing clicks.
        /// </summary>
        public static bool IsMouseOverBottomPanel()
        {
            return isMouseOverBottomPanel;
        }

        /// <summary>
        /// Checks if the given element is within the BottomPanel hierarchy.
        /// Used to determine if an element should be allowed to process clicks even when mouse is over bottom panel.
        /// </summary>
        public static bool IsElementInBottomPanel(Components.UIElement element)
        {
            Components.UIElement? current = element;
            while (current != null)
            {
                if (current is BottomPanel)
                    return true;
                current = current.Parent;
            }
            return false;
        }

        /// <summary>
        /// Sets whether the mouse is over the BottomPanel.
        /// Should be called by RootUI before updating panels.
        /// </summary>
        public static void SetMouseOverBottomPanel(bool value)
        {
            isMouseOverBottomPanel = value;
        }

        /// <summary>
        /// Resets the flag. Should be called once per frame before processing UI updates.
        /// </summary>
        public static void ResetMouseOverBottomPanel()
        {
            isMouseOverBottomPanel = false;
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

        // Update font (for debug font cycling)
        public virtual void UpdateFont(Font newFont)
        {
            Font = newFont;
        }
    }
}

