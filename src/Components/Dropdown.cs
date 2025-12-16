using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class Dropdown : UIElement
    {
        private const int ItemHeight = 25;
        private const int Padding = 5;
        private const int MinWidth = 200;

        private Font font;
        private List<string> items;
        private int selectedIndex = -1;
        private bool isOpen = false;
        private int fontSize;
        private string? customDisplayText = null;

        public string? SelectedItem => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;
        public Action<string>? OnSelectionChanged { get; set; }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            bool hoveringButton = mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                                 mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
            bool hoveringDropdown = isOpen && mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                                   mouseY >= Bounds.Y + Bounds.Height && 
                                   mouseY <= Bounds.Y + Bounds.Height + items.Count * ItemHeight + Padding * 2;
            return hoveringButton || hoveringDropdown;
        }

        public void SetCustomDisplayText(string? text)
        {
            customDisplayText = text;
            if (text != null)
            {
                selectedIndex = -1; // Clear selection when showing custom
            }
        }

        public Dropdown(Font font, List<string> items, int fontSize = 14) : base("Dropdown")
        {
            this.font = font;
            this.items = items;
            this.fontSize = fontSize;
            
            // Set flags
            IsClickable = true;
            IsHoverable = true;
        }

        public void SetItems(List<string> newItems)
        {
            items = newItems;
            if (selectedIndex >= items.Count)
            {
                selectedIndex = -1;
            }
        }

        public void SetBounds(Rectangle bounds)
        {
            this.Bounds = bounds;
        }

        public override void Update()
        {
            base.Update(); // Update children if any

            // Only process input if visible, enabled, clickable, and bounds are valid
            // Also check if any parent is hidden
            if (!IsVisible || !IsEnabled || !IsClickable || Bounds.Width <= 0 || Bounds.Height <= 0 || IsAnyParentHidden())
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if clicking on dropdown
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                // Check if clicking on the dropdown button
                if (mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                    mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height)
                {
                    isOpen = !isOpen;
                }
                // Check if clicking on dropdown items
                else if (isOpen)
                {
                    int dropdownY = (int)(Bounds.Y + Bounds.Height);
                    int dropdownHeight = items.Count * ItemHeight + Padding * 2;

                    if (mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                        mouseY >= dropdownY && mouseY <= dropdownY + dropdownHeight)
                    {
                        int itemIndex = (mouseY - dropdownY - Padding) / ItemHeight;
                        if (itemIndex >= 0 && itemIndex < items.Count)
                        {
                            selectedIndex = itemIndex;
                            OnSelectionChanged?.Invoke(items[itemIndex]);
                            isOpen = false;
                        }
                    }
                    else
                    {
                        // Clicked outside, close dropdown
                        isOpen = false;
                    }
                }
            }

            // Note: Cursor is set centrally in Program.cs based on hover state
        }

        protected override void DrawSelf()
        {
            // Only draw if bounds are valid
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            // Draw dropdown button only (dropdown list drawn separately via DrawDropdown)
            DrawButton();
        }

        public void DrawButton()
        {
            // Draw dropdown button
            Color bgColor = isOpen ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(Bounds, bgColor);
            Raylib.DrawRectangleLinesEx(Bounds, 1, UITheme.BorderColor);

            // Draw selected item, custom text, or placeholder
            string displayText;
            if (customDisplayText != null)
            {
                displayText = customDisplayText;
            }
            else
            {
                displayText = SelectedItem ?? "Select corpus...";
            }
            Color textColor = (SelectedItem != null || customDisplayText != null) ? UITheme.TextColor : UITheme.TextSecondaryColor;
            
            // Create text bounds (left-aligned with padding, reserve space for arrow)
            Rectangle textBounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width - 20, Bounds.Height);
            TextContainer.DrawLeftAlignedText(font, displayText, textBounds, fontSize, textColor, Padding);

            // Draw dropdown arrow
            int arrowX = (int)(Bounds.X + Bounds.Width - 20);
            int arrowY = (int)(Bounds.Y + Bounds.Height / 2);
            string arrow = isOpen ? "▲" : "▼";
            FontManager.DrawText(font, arrow, arrowX, arrowY - fontSize / 2, fontSize - 2, UITheme.TextSecondaryColor);
        }

        public void DrawDropdown()
        {
            // Draw dropdown list if open (called separately to render on top)
            if (isOpen && items.Count > 0)
            {
                // Calculate dropdown width
                int dropdownWidth = MinWidth;
                foreach (var item in items)
                {
                    int textWidth = (int)FontManager.MeasureText(font, item, fontSize);
                    if (textWidth + Padding * 2 > dropdownWidth)
                    {
                        dropdownWidth = textWidth + Padding * 2;
                    }
                }
                if (dropdownWidth < (int)Bounds.Width)
                {
                    dropdownWidth = (int)Bounds.Width;
                }

                int dropdownY = (int)(Bounds.Y + Bounds.Height);
                int dropdownHeight = items.Count * ItemHeight + Padding * 2;

                // Draw dropdown background
                Rectangle dropdownRect = new Rectangle(Bounds.X, dropdownY, dropdownWidth, dropdownHeight);
                Raylib.DrawRectangleRec(dropdownRect, UITheme.SidePanelColor);
                Raylib.DrawRectangleLinesEx(dropdownRect, 1, UITheme.BorderColor);

                // Draw items
                int itemY = dropdownY + Padding;
                for (int i = 0; i < items.Count; i++)
                {
                    // Check if hovering
                    int mouseX = Raylib.GetMouseX();
                    int mouseY = Raylib.GetMouseY();
                    bool isHovered = mouseX >= Bounds.X && mouseX <= Bounds.X + dropdownWidth &&
                                   mouseY >= itemY && mouseY <= itemY + ItemHeight;
                    bool isSelected = i == selectedIndex;

                    if (isHovered || isSelected)
                    {
                        Raylib.DrawRectangle((int)Bounds.X + 1, itemY, dropdownWidth - 2, ItemHeight, UITheme.MainPanelColor);
                    }

                    // Draw item text
                    Color itemTextColor = isSelected ? UITheme.TextColor : UITheme.TextSecondaryColor;
                    FontManager.DrawText(font, items[i], (int)Bounds.X + Padding, itemY + 2, fontSize - 1, itemTextColor);

                    itemY += ItemHeight;
                }
            }
        }
    }
}

