using Raylib_cs;
using System;

namespace Keysharp.UI
{
    public class Button : UIElement
    {
        public string Text { get; set; }
        public Action? OnClick { get; set; }
        public bool IsHovered { get; private set; }
        public bool IsPressed { get; private set; }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        private Font font;
        private int fontSize;

        public Button(Font font, string text, int fontSize = 14) : base($"Button_{text}")
        {
            this.font = font;
            this.Text = text;
            this.fontSize = fontSize;
            
            // Set flags
            IsClickable = true;
            IsHoverable = true;
        }

        public override void Update()
        {
            base.Update(); // Update children if any

            // Only process input if visible, enabled, clickable, and bounds are valid
            // Also check if any parent is hidden
            if (!IsVisible || !IsEnabled || !IsClickable || Bounds.Width <= 0 || Bounds.Height <= 0 || IsAnyParentHidden())
            {
                IsHovered = false;
                IsPressed = false;
                return;
            }

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if hovering
            IsHovered = IsHovering(mouseX, mouseY);

            // Check if clicking
            if (IsHovered && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                IsPressed = true;
                OnClick?.Invoke();
            }
            else if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                IsPressed = false;
            }
        }

        public override void Draw()
        {
            // Only draw if visible and bounds are valid
            if (!IsVisible || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            {
                // Button background
                Color bgColor = IsPressed ? UITheme.MainPanelColor : 
                               IsHovered ? UITheme.SplitterHoverColor : 
                               UITheme.SidePanelColor;
                
                Raylib.DrawRectangleRec(Bounds, bgColor);
                Raylib.DrawRectangleLinesEx(Bounds, 1, UITheme.BorderColor);

                // Button text (centered)
                Color textColor = IsHovered ? UITheme.TextColor : UITheme.TextSecondaryColor;
                TextContainer.DrawCenteredText(font, Text, Bounds, fontSize, textColor);
            }

            base.Draw(); // Draw children if any
        }
    }
}

