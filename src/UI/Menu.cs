using Raylib_cs;
using System;
using System.Collections.Generic;

namespace Keysharp.UI
{
    /// <summary>
    /// A menu component that handles its own dropdown logic.
    /// </summary>
    public class Menu : UIElement
    {
        private const int MenuItemPadding = 12;
        private const int DropdownItemHeight = 25;
        private const int DropdownPadding = 5;

        private Font font;
        private string menuName;
        private List<MenuItemData> items = new List<MenuItemData>();
        private bool isOpen = false;
        private System.Action<Menu>? onMenuOpened; // Callback when this menu opens (to close others)

        public Menu(Font font, string name) : base($"Menu_{name}")
        {
            this.font = font;
            this.menuName = name;
            
            IsClickable = true;
            IsHoverable = true;
        }

        public string MenuName => menuName;
        public bool IsOpen 
        { 
            get => isOpen; 
            set => isOpen = value; 
        }

        /// <summary>
        /// Sets a callback that will be called when this menu opens, allowing MenuBar to close other menus.
        /// </summary>
        public void SetOnMenuOpened(System.Action<Menu> callback)
        {
            onMenuOpened = callback;
        }

        public void AddItem(string text, Action action)
        {
            items.Add(new MenuItemData { Text = text, Action = action, IsSeparator = false });
        }

        public void AddSeparator()
        {
            items.Add(new MenuItemData { IsSeparator = true });
        }

        public void Close()
        {
            isOpen = false;
        }

        public override void Update()
        {
            base.Update();

            if (!IsVisible || !IsEnabled || !IsClickable || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Handle clicks on menu button
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (IsHovering(mouseX, mouseY))
                {
                    // Toggle menu
                    isOpen = !isOpen;
                    if (isOpen)
                    {
                        onMenuOpened?.Invoke(this);
                    }
                }
                // Handle clicks on dropdown items
                else if (isOpen)
                {
                    int dropdownY = (int)(Bounds.Y + Bounds.Height);
                    int dropdownHeight = GetDropdownHeight();
                    int dropdownWidth = GetDropdownWidth();

                    if (mouseX >= Bounds.X && mouseX <= Bounds.X + dropdownWidth &&
                        mouseY >= dropdownY && mouseY <= dropdownY + dropdownHeight)
                    {
                        int itemIndex = (mouseY - dropdownY - DropdownPadding) / DropdownItemHeight;
                        if (itemIndex >= 0 && itemIndex < items.Count)
                        {
                            var item = items[itemIndex];
                            if (!item.IsSeparator)
                            {
                                // Close menu before invoking action to avoid collection modification issues
                                isOpen = false;
                                item.Action?.Invoke();
                            }
                        }
                    }
                    else
                    {
                        // Clicked outside dropdown, close it
                        isOpen = false;
                    }
                }
                else
                {
                    // Clicked outside any menu, close this one if it was open
                    if (isOpen)
                    {
                        isOpen = false;
                    }
                }
            }
        }

        public override void Draw()
        {
            if (!IsVisible)
                return;

            // Draw menu button background if open
            if (isOpen)
            {
                Raylib.DrawRectangleRec(Bounds, UITheme.MainPanelColor);
            }

            // Draw menu text
            Color textColor = isOpen ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, menuName, (int)Bounds.X + MenuItemPadding, (int)Bounds.Y + 8, 14, textColor);

            base.Draw();
        }

        public void DrawDropdown()
        {
            // Draw dropdown separately so it appears on top
            // Only draw if menu is still open and visible
            if (!isOpen || !IsVisible)
                return;

            int dropdownWidth = GetDropdownWidth();
            int dropdownHeight = GetDropdownHeight();
            int dropdownY = (int)(Bounds.Y + Bounds.Height);

            // Draw dropdown background
            Rectangle dropdownRect = new Rectangle(Bounds.X, dropdownY, dropdownWidth, dropdownHeight);
            Raylib.DrawRectangleRec(dropdownRect, UITheme.SidePanelColor);
            Raylib.DrawRectangleLinesEx(dropdownRect, 1, UITheme.BorderColor);

            // Draw menu items
            int itemY = dropdownY + DropdownPadding;
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                
                if (item.IsSeparator)
                {
                    // Draw separator line
                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(Bounds.X + DropdownPadding, itemY + DropdownItemHeight / 2),
                        new System.Numerics.Vector2(Bounds.X + dropdownWidth - DropdownPadding, itemY + DropdownItemHeight / 2),
                        1,
                        UITheme.BorderColor
                    );
                }
                else
                {
                    // Check if hovering
                    bool isHovered = mouseX >= Bounds.X && mouseX <= Bounds.X + dropdownWidth &&
                                    mouseY >= itemY && mouseY <= itemY + DropdownItemHeight;

                    if (isHovered)
                    {
                        Raylib.DrawRectangle((int)Bounds.X + 1, itemY, dropdownWidth - 2, DropdownItemHeight, UITheme.MainPanelColor);
                    }

                    // Draw item text
                    FontManager.DrawText(font, item.Text, (int)Bounds.X + DropdownPadding, itemY + 5, 13, UITheme.TextColor);
                }

                itemY += DropdownItemHeight;
            }
        }

        private int GetDropdownWidth()
        {
            int maxWidth = 150; // Minimum width
            foreach (var item in items)
            {
                if (!item.IsSeparator)
                {
                    int textWidth = (int)FontManager.MeasureText(font, item.Text, 13);
                    if (textWidth + DropdownPadding * 2 > maxWidth)
                    {
                        maxWidth = textWidth + DropdownPadding * 2;
                    }
                }
            }
            return maxWidth;
        }

        private int GetDropdownHeight()
        {
            return DropdownPadding * 2 + items.Count * DropdownItemHeight;
        }

        private class MenuItemData
        {
            public string Text { get; set; } = string.Empty;
            public Action? Action { get; set; }
            public bool IsSeparator { get; set; }
        }
    }
}

