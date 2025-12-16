using Raylib_cs;
using System.Collections.Generic;
using Keysharp.Panels;
using Keysharp.UI;

namespace Keysharp
{
    public class MenuBar : UIElement
    {
        private const int MenuBarHeight = 30;
        private const int MenuItemPadding = 12;
        private const int DropdownItemHeight = 25;
        private const int DropdownPadding = 5;

        private Font font;
        private MainPanel? mainPanel;
        private List<Menu> menus = new List<Menu>();
        private int? openMenuIndex = null;

        public MenuBar(Font font, MainPanel? mainPanel = null) : base("MenuBar")
        {
            this.font = font;
            this.mainPanel = mainPanel;
            
            // Set initial bounds
            Bounds = new Rectangle(0, 0, Raylib.GetScreenWidth(), MenuBarHeight);
            
            // Initialize menus
            var fileMenu = new Menu("File");
            fileMenu.AddItem("New", () => System.Console.WriteLine("New"));
            fileMenu.AddItem("Open", () => System.Console.WriteLine("Open"));
            fileMenu.AddItem("Save", () => System.Console.WriteLine("Save"));
            fileMenu.AddItem("Save As...", () => System.Console.WriteLine("Save As"));
            fileMenu.AddSeparator();
            fileMenu.AddItem("Exit", () => System.Console.WriteLine("Exit"));
            menus.Add(fileMenu);

            var editMenu = new Menu("Edit");
            editMenu.AddItem("Undo", () => System.Console.WriteLine("Undo"));
            editMenu.AddItem("Redo", () => System.Console.WriteLine("Redo"));
            editMenu.AddSeparator();
            editMenu.AddItem("Cut", () => System.Console.WriteLine("Cut"));
            editMenu.AddItem("Copy", () => System.Console.WriteLine("Copy"));
            editMenu.AddItem("Paste", () => System.Console.WriteLine("Paste"));
            menus.Add(editMenu);

            BuildViewMenu();
        }

        private void BuildViewMenu()
        {
            var viewMenu = new Menu("View");
            
            // Add tab visibility toggles if MainPanel is available
            if (mainPanel != null)
            {
                var tabs = mainPanel.GetTabs();
                foreach (var tabName in tabs)
                {
                    bool isVisible = mainPanel.IsTabVisible(tabName);
                    string menuText = isVisible ? $"✓ {tabName}" : $"  {tabName}";
                    string tabNameCopy = tabName; // Capture for closure
                    viewMenu.AddItem(menuText, () => {
                        mainPanel.ToggleTab(tabNameCopy);
                        // Rebuild menu to update checkmarks
                        BuildViewMenu();
                    });
                }
                viewMenu.AddSeparator();
            }
            
            viewMenu.AddItem("Zoom In", () => System.Console.WriteLine("Zoom In"));
            viewMenu.AddItem("Zoom Out", () => System.Console.WriteLine("Zoom Out"));
            viewMenu.AddItem("Reset Zoom", () => System.Console.WriteLine("Reset Zoom"));
            viewMenu.AddSeparator();
            viewMenu.AddItem("Full Screen", () => System.Console.WriteLine("Full Screen"));
            
            // Replace View menu if it already exists
            if (menus.Count > 2)
            {
                menus[2] = viewMenu;
            }
            else
            {
                menus.Add(viewMenu);
            }
        }

        public void SetMainPanel(MainPanel mainPanel)
        {
            this.mainPanel = mainPanel;
            BuildViewMenu();
        }

        public int Height => MenuBarHeight;

        public List<Rectangle> GetMenuItemBounds()
        {
            List<Rectangle> bounds = new List<Rectangle>();
            int currentX = 0;
            
            for (int i = 0; i < menus.Count; i++)
            {
                var menu = menus[i];
                int menuWidth = MenuItemPadding * 2 + (int)FontManager.MeasureText(font, menu.Name, 14);
                bounds.Add(new Rectangle(currentX, 0, menuWidth, MenuBarHeight));
                currentX += menuWidth;
            }
            
            return bounds;
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            // Check if hovering over menu bar items
            if (mouseY >= 0 && mouseY <= MenuBarHeight)
            {
                int currentX = 0;
                for (int i = 0; i < menus.Count; i++)
                {
                    int menuWidth = MenuItemPadding * 2 + (int)FontManager.MeasureText(font, menus[i].Name, 14);
                    if (mouseX >= currentX && mouseX <= currentX + menuWidth)
                    {
                        return true;
                    }
                    currentX += menuWidth;
                }
            }

            // Check if hovering over dropdown items
            if (openMenuIndex.HasValue)
            {
                var menu = menus[openMenuIndex.Value];
                int dropdownX = GetMenuX(openMenuIndex.Value);
                int dropdownY = MenuBarHeight;
                int dropdownWidth = GetDropdownWidth(menu);

                if (mouseX >= dropdownX && mouseX <= dropdownX + dropdownWidth &&
                    mouseY >= dropdownY && mouseY <= dropdownY + GetDropdownHeight(menu))
                {
                    int itemIndex = (mouseY - dropdownY - DropdownPadding) / DropdownItemHeight;
                    if (itemIndex >= 0 && itemIndex < menu.Items.Count && !menu.Items[itemIndex].IsSeparator)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Update()
        {
            base.Update();
            
            // Update bounds
            Bounds = new Rectangle(0, 0, Raylib.GetScreenWidth(), MenuBarHeight);
            
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            bool hoveringOverInteractive = false;

            // Check if hovering over menu bar items
            int menuBarX = 0;
            for (int i = 0; i < menus.Count; i++)
            {
                int menuWidth = MenuItemPadding * 2 + (int)FontManager.MeasureText(font, menus[i].Name, 14);
                
                if (mouseY >= 0 && mouseY <= MenuBarHeight &&
                    mouseX >= menuBarX && mouseX <= menuBarX + menuWidth)
                {
                    hoveringOverInteractive = true;
                    break;
                }
                menuBarX += menuWidth;
            }

            // Check if hovering over dropdown items
            if (!hoveringOverInteractive && openMenuIndex.HasValue)
            {
                var menu = menus[openMenuIndex.Value];
                int dropdownX = GetMenuX(openMenuIndex.Value);
                int dropdownY = MenuBarHeight;
                int dropdownWidth = GetDropdownWidth(menu);

                if (mouseX >= dropdownX && mouseX <= dropdownX + dropdownWidth &&
                    mouseY >= dropdownY && mouseY <= dropdownY + GetDropdownHeight(menu))
                {
                    int itemIndex = (mouseY - dropdownY - DropdownPadding) / DropdownItemHeight;
                    if (itemIndex >= 0 && itemIndex < menu.Items.Count && !menu.Items[itemIndex].IsSeparator)
                    {
                        hoveringOverInteractive = true;
                    }
                }
            }

            // Note: Cursor is set centrally in Program.cs based on hover state

            // Check if clicking outside menu area
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                bool clickedOnMenu = false;

                // Check menu bar items
                int currentX = 0;
                for (int i = 0; i < menus.Count; i++)
                {
                    int menuWidth = MenuItemPadding * 2 + (int)FontManager.MeasureText(font, menus[i].Name, 14);
                    
                    if (mouseY >= 0 && mouseY <= MenuBarHeight &&
                        mouseX >= currentX && mouseX <= currentX + menuWidth)
                    {
                        // Toggle menu
                        openMenuIndex = (openMenuIndex == i) ? null : i;
                        clickedOnMenu = true;
                        System.Console.WriteLine($"Menu {menus[i].Name} clicked, openMenuIndex: {openMenuIndex}");
                        break;
                    }
                    currentX += menuWidth;
                }

                // Check dropdown items
                if (!clickedOnMenu && openMenuIndex.HasValue)
                {
                    var menu = menus[openMenuIndex.Value];
                    int dropdownX = GetMenuX(openMenuIndex.Value);
                    int dropdownY = MenuBarHeight;
                    int dropdownWidth = GetDropdownWidth(menu);

                    if (mouseX >= dropdownX && mouseX <= dropdownX + dropdownWidth &&
                        mouseY >= dropdownY && mouseY <= dropdownY + GetDropdownHeight(menu))
                    {
                        int itemIndex = (mouseY - dropdownY - DropdownPadding) / DropdownItemHeight;
                        if (itemIndex >= 0 && itemIndex < menu.Items.Count)
                        {
                            var item = menu.Items[itemIndex];
                            if (!item.IsSeparator)
                            {
                                item.Action?.Invoke();
                                openMenuIndex = null; // Close menu after selection
                            }
                        }
                    }
                    else
                    {
                        // Clicked outside dropdown, close it
                        openMenuIndex = null;
                    }
                }
            }
        }

        public override void Draw()
        {
            // Draw menu bar background
            Raylib.DrawRectangleRec(Bounds, UITheme.SidePanelColor);
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(Bounds.X, Bounds.Y + Bounds.Height),
                new System.Numerics.Vector2(Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height),
                1,
                UITheme.BorderColor
            );

            // Draw menu items
            int currentX = 0;
            for (int i = 0; i < menus.Count; i++)
            {
                var menu = menus[i];
                bool isOpen = openMenuIndex == i;
                
                // Rebuild View menu when it's opened to ensure checkmarks are current
                if (isOpen && menu.Name == "View" && mainPanel != null)
                {
                    BuildViewMenu();
                    menu = menus[i]; // Get updated menu
                }
                
                int menuWidth = MenuItemPadding * 2 + (int)FontManager.MeasureText(font, menu.Name, 14);
                
                // Highlight if open or hovered
                if (isOpen)
                {
                    Raylib.DrawRectangle(currentX, 0, menuWidth, MenuBarHeight, UITheme.MainPanelColor);
                }

                // Draw menu text
                Color textColor = isOpen ? UITheme.TextColor : UITheme.TextSecondaryColor;
                FontManager.DrawText(font, menu.Name, currentX + MenuItemPadding, 8, 14, textColor);

                currentX += menuWidth;
            }
        }

        public void DrawDropdowns()
        {
            // Draw dropdowns separately so they appear on top of panels
            if (openMenuIndex.HasValue)
            {
                int currentX = 0;
                for (int i = 0; i < menus.Count; i++)
                {
                    if (i == openMenuIndex.Value)
                    {
                        DrawDropdown(menus[i], currentX);
                        break;
                    }
                    int menuWidth = MenuItemPadding * 2 + (int)Raylib.MeasureTextEx(font, menus[i].Name, 14, 0).X;
                    currentX += menuWidth;
                }
            }
        }

        private void DrawDropdown(Menu menu, int x)
        {
            int dropdownWidth = GetDropdownWidth(menu);
            int dropdownHeight = GetDropdownHeight(menu);
            int dropdownY = MenuBarHeight;

            // Draw dropdown background
            Raylib.DrawRectangle(x, dropdownY, dropdownWidth, dropdownHeight, UITheme.SidePanelColor);
            Raylib.DrawRectangleLinesEx(
                new Rectangle(x, dropdownY, dropdownWidth, dropdownHeight),
                1,
                UITheme.BorderColor
            );

            // Draw menu items
            int itemY = dropdownY + DropdownPadding;
            for (int i = 0; i < menu.Items.Count; i++)
            {
                var item = menu.Items[i];
                
                if (item.IsSeparator)
                {
                    // Draw separator line
                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(x + DropdownPadding, itemY + DropdownItemHeight / 2),
                        new System.Numerics.Vector2(x + dropdownWidth - DropdownPadding, itemY + DropdownItemHeight / 2),
                        1,
                        UITheme.BorderColor
                    );
                }
                else
                {
                    // Check if hovering
                    int mouseX = Raylib.GetMouseX();
                    int mouseY = Raylib.GetMouseY();
                    bool isHovered = mouseX >= x && mouseX <= x + dropdownWidth &&
                                    mouseY >= itemY && mouseY <= itemY + DropdownItemHeight;

                    if (isHovered)
                    {
                        Raylib.DrawRectangle(x + 1, itemY, dropdownWidth - 2, DropdownItemHeight, UITheme.MainPanelColor);
                    }

                    // Draw item text
                    FontManager.DrawText(font, item.Text, x + DropdownPadding, itemY + 5, 13, UITheme.TextColor);
                }

                itemY += DropdownItemHeight;
            }
        }

        private int GetMenuX(int menuIndex)
        {
            int x = 0;
            for (int i = 0; i < menuIndex; i++)
            {
                int menuWidth = MenuItemPadding * 2 + (int)Raylib.MeasureTextEx(font, menus[i].Name, 14, 0).X;
                x += menuWidth;
            }
            return x;
        }

        private int GetDropdownWidth(Menu menu)
        {
            int maxWidth = 150; // Minimum width
            foreach (var item in menu.Items)
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

        private int GetDropdownHeight(Menu menu)
        {
            return DropdownPadding * 2 + menu.Items.Count * DropdownItemHeight;
        }
    }

    public class Menu
    {
        public string Name { get; }
        public List<MenuItem> Items { get; } = new List<MenuItem>();

        public Menu(string name)
        {
            Name = name;
        }

        public void AddItem(string text, System.Action action)
        {
            Items.Add(new MenuItem { Text = text, Action = action, IsSeparator = false });
        }

        public void AddSeparator()
        {
            Items.Add(new MenuItem { IsSeparator = true });
        }
    }

    public class MenuItem
    {
        public string Text { get; set; } = string.Empty;
        public System.Action? Action { get; set; }
        public bool IsSeparator { get; set; }
    }
}

