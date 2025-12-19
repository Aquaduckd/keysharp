using Raylib_cs;
using System;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class Checkbox : UIElement
    {
        public string Text { get; set; }
        public bool IsChecked { get; set; }
        public Action<bool>? OnCheckedChanged { get; set; }
        public bool IsHovered { get; private set; }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        private Font font;
        private int fontSize;
        private const int CHECKBOX_SIZE = 16;
        private const int TEXT_SPACING = 8;

        public Checkbox(Font font, string text, int fontSize = 14) : base($"Checkbox_{text}")
        {
            this.font = font;
            this.Text = text;
            this.fontSize = fontSize;
            this.IsChecked = false;
            
            // Set flags
            IsClickable = true;
            IsHoverable = true;
        }

        public override void Update()
        {
            base.Update(); // Update children if any

            // Only process input if visible, enabled, clickable, and bounds are valid
            if (!IsVisible || !IsEnabled || !IsClickable || Bounds.Width <= 0 || Bounds.Height <= 0 || IsAnyParentHidden())
            {
                IsHovered = false;
                return;
            }

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if hovering
            IsHovered = IsHovering(mouseX, mouseY);

            // Check if clicking
            if (IsHovered && Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                // Don't process if click was consumed by a dropdown
                if (Dropdown.WasClickConsumed())
                {
                    return;
                }

                IsChecked = !IsChecked;
                OnCheckedChanged?.Invoke(IsChecked);
            }
        }

        protected override void DrawSelf()
        {
            // Only draw if bounds are valid
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int checkboxX = (int)Bounds.X;
            int checkboxY = (int)(Bounds.Y + (Bounds.Height - CHECKBOX_SIZE) / 2);
            Rectangle checkboxRect = new Rectangle(checkboxX, checkboxY, CHECKBOX_SIZE, CHECKBOX_SIZE);

            // Draw checkbox background
            Color bgColor = IsHovered ? UITheme.SplitterHoverColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(checkboxRect, bgColor);
            Raylib.DrawRectangleLinesEx(checkboxRect, 1, UITheme.BorderColor);

            // Draw filled square if checked
            if (IsChecked)
            {
                int padding = 3;
                Rectangle filledRect = new Rectangle(
                    checkboxRect.X + padding,
                    checkboxRect.Y + padding,
                    checkboxRect.Width - (padding * 2),
                    checkboxRect.Height - (padding * 2)
                );
                Raylib.DrawRectangleRec(filledRect, UITheme.TextColor);
            }

            // Draw label text
            int textX = checkboxX + CHECKBOX_SIZE + TEXT_SPACING;
            int textY = (int)(Bounds.Y + (Bounds.Height - fontSize) / 2);
            Color textColor = IsHovered ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, Text, textX, textY, fontSize, textColor);
        }
    }
}

