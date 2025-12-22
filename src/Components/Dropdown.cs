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
        private const int DefaultMaxVisibleRows = 10;

        // Static flag to track if a click was consumed by a dropdown this frame
        private static bool clickConsumedThisFrame = false;

        private Font font;
        private List<string> items;
        private int selectedIndex = -1;
        private bool isOpen = false;
        private int fontSize;
        private string? customDisplayText = null;
        private int maxVisibleRows = DefaultMaxVisibleRows;
        private int scrollOffset = 0; // Index of first visible item when scrolling

        public string? SelectedItem => selectedIndex >= 0 && selectedIndex < items.Count ? items[selectedIndex] : null;
        public Action<string>? OnSelectionChanged { get; set; }
        public bool IsOpen => isOpen;
        
        /// <summary>
        /// Maximum number of rows to display in the dropdown before scrolling is needed.
        /// Default is 10.
        /// </summary>
        public int MaxVisibleRows
        {
            get => maxVisibleRows;
            set
            {
                maxVisibleRows = Math.Max(1, value); // At least 1 row must be visible
                // Clamp scroll offset to valid range when max rows changes
                int maxScroll = Math.Max(0, items.Count - maxVisibleRows);
                scrollOffset = Math.Min(scrollOffset, maxScroll);
            }
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            bool hoveringButton = mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                                 mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
            
            if (isOpen)
            {
                int dropdownY = (int)(Bounds.Y + Bounds.Height);
                int visibleRows = Math.Min(maxVisibleRows, items.Count);
                int dropdownHeight = visibleRows * ItemHeight + Padding * 2;
                bool hoveringDropdown = mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                                       mouseY >= dropdownY && mouseY <= dropdownY + dropdownHeight;
                return hoveringButton || hoveringDropdown;
            }
            
            return hoveringButton;
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
            // Reset scroll offset and clamp it to valid range
            int maxScroll = Math.Max(0, items.Count - maxVisibleRows);
            scrollOffset = Math.Min(scrollOffset, maxScroll);
        }

        public void SetSelectedItem(string item)
        {
            int index = items.IndexOf(item);
            if (index >= 0)
            {
                selectedIndex = index;
                OnSelectionChanged?.Invoke(item);
            }
        }

        public void SetSelectedIndex(int index, bool triggerCallback = true)
        {
            System.Console.WriteLine($"[Dropdown.SetSelectedIndex] Called with index: {index}, triggerCallback: {triggerCallback}, items.Count: {items.Count}, current selectedIndex: {selectedIndex}");
            if (index >= 0 && index < items.Count)
            {
                selectedIndex = index;
                System.Console.WriteLine($"[Dropdown.SetSelectedIndex] Set selectedIndex to {index}, selectedItem: {SelectedItem ?? "null"}");
                if (triggerCallback)
                {
                    System.Console.WriteLine($"[Dropdown.SetSelectedIndex] Invoking OnSelectionChanged with: {items[index]}");
                    OnSelectionChanged?.Invoke(items[index]);
                }
            }
            else
            {
                System.Console.WriteLine($"[Dropdown.SetSelectedIndex] Index {index} out of range (0-{items.Count - 1})");
            }
        }

        public int SelectedIndex => selectedIndex;

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
                // Don't process if another dropdown already consumed the click
                if (clickConsumedThisFrame)
                {
                    return;
                }

                // Don't process if mouse is over BottomPanel AND this element is NOT in the BottomPanel (to prevent click-through)
                if (UI.Panel.IsMouseOverBottomPanel() && !UI.Panel.IsElementInBottomPanel(this))
                {
                    return;
                }

                bool handledClick = false;

                // Check if clicking on the dropdown button
                if (mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                    mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height)
                {
                    bool wasOpen = isOpen;
                    isOpen = !isOpen;
                    
                    // When opening, scroll to show selected item if it exists
                    if (!wasOpen && isOpen && selectedIndex >= 0 && items.Count > maxVisibleRows)
                    {
                        // Ensure selected item is visible
                        if (selectedIndex < scrollOffset)
                        {
                            scrollOffset = selectedIndex;
                        }
                        else if (selectedIndex >= scrollOffset + maxVisibleRows)
                        {
                            scrollOffset = selectedIndex - maxVisibleRows + 1;
                        }
                    }
                    
                    handledClick = true;
                }
                // Check if clicking on dropdown items or anywhere within dropdown bounds when open
                else if (isOpen)
                {
                    // If dropdown is open and click is within the dropdown's expanded bounds, consume the click
                    if (ContainsPoint(mouseX, mouseY))
                    {
                        int dropdownY = (int)(Bounds.Y + Bounds.Height);
                        int visibleRows = Math.Min(maxVisibleRows, items.Count);
                        int dropdownHeight = visibleRows * ItemHeight + Padding * 2;

                        // Check if clicking on a specific item
                        if (mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                            mouseY >= dropdownY && mouseY <= dropdownY + dropdownHeight)
                        {
                            int itemIndex = (mouseY - dropdownY - Padding) / ItemHeight;
                            if (itemIndex >= 0 && itemIndex < visibleRows)
                            {
                                // Map visible item index to actual item index accounting for scroll
                                int actualIndex = scrollOffset + itemIndex;
                                if (actualIndex >= 0 && actualIndex < items.Count)
                                {
                                    selectedIndex = actualIndex;
                                    OnSelectionChanged?.Invoke(items[actualIndex]);
                                    isOpen = false;
                                    handledClick = true;
                                }
                            }
                        }
                        
                        // If we didn't handle it as an item click, but we're within bounds, close the dropdown
                        // This handles clicks in padding areas or outside specific items but still within dropdown bounds
                        if (!handledClick)
                        {
                            isOpen = false;
                            handledClick = true;
                        }
                    }
                }

                // Mark click as consumed if we handled it
                // Also, if dropdown is open and click is within its bounds, consume it to prevent click-through
                if (handledClick || (isOpen && ContainsPoint(mouseX, mouseY)))
                {
                    clickConsumedThisFrame = true;
                }
            }

            // Handle mouse wheel scrolling when dropdown is open
            if (isOpen && items.Count > maxVisibleRows)
            {
                float wheelMove = Raylib.GetMouseWheelMove();
                if (wheelMove != 0)
                {
                    // Only scroll if mouse is over the dropdown (reuse mouseX and mouseY from above)
                    if (ContainsPoint(mouseX, mouseY))
                    {
                        int maxScroll = items.Count - maxVisibleRows;
                        scrollOffset = (int)Math.Clamp(scrollOffset - wheelMove, 0, maxScroll);
                    }
                }
            }

            // Note: Cursor is set centrally in Program.cs based on hover state
        }

        /// <summary>
        /// Checks if a click was consumed by any dropdown this frame.
        /// Components should check this before processing clicks to prevent click-through.
        /// This should be called at the start of each frame before processing any clicks.
        /// </summary>
        public static bool WasClickConsumed()
        {
            return clickConsumedThisFrame;
        }

        /// <summary>
        /// Resets the consumed flag. Should be called once per frame before processing UI updates.
        /// </summary>
        public static void ResetClickConsumed()
        {
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                clickConsumedThisFrame = false;
            }
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
                displayText = SelectedItem ?? "Select...";
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

                // Calculate visible rows and scroll bounds
                int visibleRows = Math.Min(maxVisibleRows, items.Count);
                int dropdownY = (int)(Bounds.Y + Bounds.Height);
                int dropdownHeight = visibleRows * ItemHeight + Padding * 2;
                
                // Ensure scroll offset is within valid range
                int maxScroll = Math.Max(0, items.Count - maxVisibleRows);
                scrollOffset = Math.Clamp(scrollOffset, 0, maxScroll);

                // Draw dropdown background
                Rectangle dropdownRect = new Rectangle(Bounds.X, dropdownY, dropdownWidth, dropdownHeight);
                Raylib.DrawRectangleRec(dropdownRect, UITheme.SidePanelColor);
                Raylib.DrawRectangleLinesEx(dropdownRect, 1, UITheme.BorderColor);

                // Draw visible items only
                int currentMouseX = Raylib.GetMouseX();
                int currentMouseY = Raylib.GetMouseY();
                int itemY = dropdownY + Padding;
                
                for (int i = 0; i < visibleRows; i++)
                {
                    int actualIndex = scrollOffset + i;
                    if (actualIndex >= items.Count)
                        break;
                    
                    // Check if hovering
                    bool isHovered = currentMouseX >= Bounds.X && currentMouseX <= Bounds.X + dropdownWidth &&
                                   currentMouseY >= itemY && currentMouseY <= itemY + ItemHeight;
                    bool isSelected = actualIndex == selectedIndex;

                    if (isHovered || isSelected)
                    {
                        Raylib.DrawRectangle((int)Bounds.X + 1, itemY, dropdownWidth - 2, ItemHeight, UITheme.MainPanelColor);
                    }

                    // Draw item text
                    Color itemTextColor = isSelected ? UITheme.TextColor : UITheme.TextSecondaryColor;
                    FontManager.DrawText(font, items[actualIndex], (int)Bounds.X + Padding, itemY + 2, fontSize - 1, itemTextColor);

                    itemY += ItemHeight;
                }
            }
            
            // Reset scroll offset when dropdown closes
            if (!isOpen)
            {
                scrollOffset = 0;
            }
        }

        /// <summary>
        /// Checks if a point is over this dropdown (button or open dropdown list).
        /// </summary>
        public bool ContainsPoint(int x, int y)
        {
            // Check if point is over the button
            if (x >= Bounds.X && x <= Bounds.X + Bounds.Width &&
                y >= Bounds.Y && y <= Bounds.Y + Bounds.Height)
            {
                return true;
            }

            // Check if point is over the open dropdown list
            if (isOpen)
            {
                int dropdownY = (int)(Bounds.Y + Bounds.Height);
                int visibleRows = Math.Min(maxVisibleRows, items.Count);
                int dropdownHeight = visibleRows * ItemHeight + Padding * 2;
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

                if (x >= Bounds.X && x <= Bounds.X + dropdownWidth &&
                    y >= dropdownY && y <= dropdownY + dropdownHeight)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

