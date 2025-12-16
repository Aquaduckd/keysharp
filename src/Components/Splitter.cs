using Raylib_cs;
using System;

namespace Keysharp.Components
{
    public class Splitter : UIElement
    {
        public bool IsVertical { get; }
        public bool IsHovered { get; private set; }
        public bool IsDragging { get; private set; }
        public System.Action<float>? OnDrag { get; set; }

        public Splitter(string name, bool isVertical) : base(name)
        {
            IsVertical = isVertical;
            
            // Set flags
            IsClickable = true;
            IsHoverable = true;
        }

        public override void Update()
        {
            base.Update();

            // Only process input if visible, enabled, and clickable
            // Also check if any parent is hidden
            if (!IsVisible || !IsEnabled || !IsClickable || IsAnyParentHidden())
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if hovering
            IsHovered = IsHovering(mouseX, mouseY);

            // Handle mouse input
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (IsHovered)
                {
                    IsDragging = true;
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (IsDragging)
                {
                    IsDragging = false;
                }
            }

            // Update position while dragging
            if (IsDragging && OnDrag != null)
            {
                if (IsVertical)
                {
                    float clampedX = Math.Clamp(mouseX, LayoutManager.MinPanelSize, Raylib.GetScreenWidth() - LayoutManager.MinPanelSize - LayoutManager.SplitterWidth);
                    OnDrag(clampedX);
                }
                else
                {
                    int windowHeight = Raylib.GetScreenHeight();
                    // Menu bar is typically 30px, use that as minimum
                    const int menuBarHeight = 30;
                    float clampedY = Math.Clamp(mouseY, menuBarHeight + LayoutManager.MinPanelSize, windowHeight - LayoutManager.MinPanelSize - LayoutManager.SplitterWidth);
                    float bottomPanelHeight = windowHeight - clampedY - LayoutManager.SplitterWidth;
                    OnDrag(bottomPanelHeight);
                }
            }

            // Cursor is set centrally in Program.cs based on priority
        }

        protected override void DrawSelf()
        {
            // Draw splitter background
            Color splitterColor = (IsHovered || IsDragging) 
                ? UITheme.SplitterHoverColor 
                : UITheme.SplitterColor;
            Raylib.DrawRectangleRec(Bounds, splitterColor);
        }
    }
}

