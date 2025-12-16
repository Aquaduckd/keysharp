using Raylib_cs;
using System;

namespace Keysharp.UI
{
    public class Button
    {
        public string Text { get; set; }
        public Rectangle Bounds { get; set; }
        public Action? OnClick { get; set; }
        public bool IsHovered { get; private set; }
        public bool IsPressed { get; private set; }

        public bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        private Font font;
        private int fontSize;

        public Button(Font font, string text, int fontSize = 14)
        {
            this.font = font;
            this.Text = text;
            this.fontSize = fontSize;
        }

        public void Update()
        {
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if hovering
            IsHovered = mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                       mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;

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

        public void Draw()
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
    }
}

