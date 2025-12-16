using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Keysharp.UI
{
    public class Dropdown
    {
        private const int ItemHeight = 25;
        private const int Padding = 5;
        private const int MinWidth = 200;

        private Font font;
        private List<string> items;
        private int selectedIndex = -1;
        private bool isOpen = false;
        private Rectangle bounds;
        private int fontSize;
        private string? customDisplayText = null;

        public string? SelectedItem => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;
        public Action<string>? OnSelectionChanged { get; set; }

        public bool IsHovering(int mouseX, int mouseY)
        {
            bool hoveringButton = mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                                 mouseY >= bounds.Y && mouseY <= bounds.Y + bounds.Height;
            bool hoveringDropdown = isOpen && mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                                   mouseY >= bounds.Y + bounds.Height && 
                                   mouseY <= bounds.Y + bounds.Height + items.Count * ItemHeight + Padding * 2;
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

        public Dropdown(Font font, List<string> items, int fontSize = 14)
        {
            this.font = font;
            this.items = items;
            this.fontSize = fontSize;
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
            this.bounds = bounds;
        }

        public void Update()
        {
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if clicking on dropdown
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                // Check if clicking on the dropdown button
                if (mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                    mouseY >= bounds.Y && mouseY <= bounds.Y + bounds.Height)
                {
                    isOpen = !isOpen;
                }
                // Check if clicking on dropdown items
                else if (isOpen)
                {
                    int dropdownY = (int)(bounds.Y + bounds.Height);
                    int dropdownHeight = items.Count * ItemHeight + Padding * 2;

                    if (mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
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

        public void Draw()
        {
            // Draw dropdown button only
            DrawButton();
        }

        public void DrawButton()
        {
            // Draw dropdown button
            Color bgColor = isOpen ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(bounds, bgColor);
            Raylib.DrawRectangleLinesEx(bounds, 1, UITheme.BorderColor);

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
            int textX = (int)bounds.X + Padding;
            int textY = (int)bounds.Y + (int)((bounds.Height - fontSize) / 2);
            Color textColor = (SelectedItem != null || customDisplayText != null) ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, displayText, textX, textY, fontSize, textColor);

            // Draw dropdown arrow
            int arrowX = (int)(bounds.X + bounds.Width - 20);
            int arrowY = (int)(bounds.Y + bounds.Height / 2);
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
                if (dropdownWidth < (int)bounds.Width)
                {
                    dropdownWidth = (int)bounds.Width;
                }

                int dropdownY = (int)(bounds.Y + bounds.Height);
                int dropdownHeight = items.Count * ItemHeight + Padding * 2;

                // Draw dropdown background
                Rectangle dropdownRect = new Rectangle(bounds.X, dropdownY, dropdownWidth, dropdownHeight);
                Raylib.DrawRectangleRec(dropdownRect, UITheme.SidePanelColor);
                Raylib.DrawRectangleLinesEx(dropdownRect, 1, UITheme.BorderColor);

                // Draw items
                int itemY = dropdownY + Padding;
                for (int i = 0; i < items.Count; i++)
                {
                    // Check if hovering
                    int mouseX = Raylib.GetMouseX();
                    int mouseY = Raylib.GetMouseY();
                    bool isHovered = mouseX >= bounds.X && mouseX <= bounds.X + dropdownWidth &&
                                   mouseY >= itemY && mouseY <= itemY + ItemHeight;
                    bool isSelected = i == selectedIndex;

                    if (isHovered || isSelected)
                    {
                        Raylib.DrawRectangle((int)bounds.X + 1, itemY, dropdownWidth - 2, ItemHeight, UITheme.MainPanelColor);
                    }

                    // Draw item text
                    Color itemTextColor = isSelected ? UITheme.TextColor : UITheme.TextSecondaryColor;
                    FontManager.DrawText(font, items[i], (int)bounds.X + Padding, itemY + 2, fontSize - 1, itemTextColor);

                    itemY += ItemHeight;
                }
            }
        }
    }
}

