using Raylib_cs;
using System;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class RadioButton : UIElement
    {
        public string Text { get; set; }
        public bool IsSelected { get; set; }
        public Action<bool>? OnSelectedChanged { get; set; }
        public bool IsHovered { get; private set; }
        public string GroupName { get; set; } // Group name to ensure only one is selected per group
        public Action<RadioButton>? OnSelectedInGroup { get; set; } // Callback when this radio button is selected (to deselect others)

        private Font font;
        private int fontSize;
        private const int RADIO_SIZE = 16;
        private const int TEXT_SPACING = 8;

        public RadioButton(Font font, string text, string groupName, int fontSize = 14) : base($"RadioButton_{text}")
        {
            this.font = font;
            this.Text = text;
            this.fontSize = fontSize;
            this.GroupName = groupName;
            this.IsSelected = false;
            
            IsClickable = true;
            IsHoverable = true;
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        public override void Update()
        {
            base.Update();

            if (!IsVisible || !IsEnabled || !IsClickable || Bounds.Width <= 0 || Bounds.Height <= 0 || IsAnyParentHidden())
            {
                IsHovered = false;
                return;
            }

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            IsHovered = IsHovering(mouseX, mouseY); // Check hover for entire component

            // Allow clicking anywhere on the radio button component (not just the circle)
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) && IsHovered)
            {
                // Don't process if click was consumed by a dropdown
                if (Dropdown.WasClickConsumed())
                {
                    return;
                }

                // Don't process if mouse is over BottomPanel AND this element is NOT in the BottomPanel (to prevent click-through)
                if (UI.Panel.IsMouseOverBottomPanel() && !UI.Panel.IsElementInBottomPanel(this))
                {
                    return;
                }

                // If not already selected, select this radio button
                if (!IsSelected)
                {
                    // Notify parent/group to deselect others
                    OnSelectedInGroup?.Invoke(this);
                    
                    IsSelected = true;
                    OnSelectedChanged?.Invoke(true);
                }
            }
        }

        protected override void DrawSelf()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int radioX = (int)Bounds.X + (int)ChildPadding;
            int radioY = (int)(Bounds.Y + (Bounds.Height - RADIO_SIZE) / 2);
            Rectangle radioRect = new Rectangle(radioX, radioY, RADIO_SIZE, RADIO_SIZE);

            // Draw radio button background (circle)
            Color bgColor = IsHovered ? UITheme.SplitterHoverColor : UITheme.SidePanelColor;
            Raylib.DrawCircle((int)(radioRect.X + RADIO_SIZE / 2), (int)(radioRect.Y + RADIO_SIZE / 2), RADIO_SIZE / 2, bgColor);
            Raylib.DrawCircleLines((int)(radioRect.X + RADIO_SIZE / 2), (int)(radioRect.Y + RADIO_SIZE / 2), RADIO_SIZE / 2, UITheme.BorderColor);

            // Draw inner filled circle if selected
            if (IsSelected)
            {
                int innerRadius = 5;
                Raylib.DrawCircle((int)(radioRect.X + RADIO_SIZE / 2), (int)(radioRect.Y + RADIO_SIZE / 2), innerRadius, UITheme.TextColor);
            }

            // Draw label text
            int textX = radioX + RADIO_SIZE + TEXT_SPACING;
            int textY = (int)(Bounds.Y + (Bounds.Height - fontSize) / 2);
            Color textColor = IsHovered ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, Text, textX, textY, fontSize, textColor);
        }
    }
}

