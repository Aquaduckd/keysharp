using Raylib_cs;
using System.Collections.Generic;
using Keysharp.Components;
using Keysharp.UI;

namespace Keysharp.UI
{
    public class MenuBar : Components.UIElement
    {
        private const int MenuBarHeight = 30;

        private Font font;
        private MainPanel? mainPanel;
        private List<Menu> menuElements = new List<Menu>();
        private Container menusContainer;
        private bool needsViewMenuRebuild = false;

        public MenuBar(Font font, MainPanel? mainPanel = null) : base("MenuBar")
        {
            this.font = font;
            this.mainPanel = mainPanel;
            
            // Set initial bounds
            Bounds = new Rectangle(0, 0, Raylib.GetScreenWidth(), MenuBarHeight);
            
            // Menu bar itself is not directly clickable (menu items are)
            IsClickable = false;
            IsHoverable = true; // For cursor changes on menu items
            
            // Create container for menus
            menusContainer = new Container("MenusContainer");
            menusContainer.AutoLayoutChildren = true;
            menusContainer.LayoutDirection = LayoutDirection.Horizontal;
            menusContainer.ChildJustification = ChildJustification.Left;
            menusContainer.ChildGap = 0;
            menusContainer.ChildPadding = 0;
            menusContainer.AutoSize = false; // Size set manually
            AddChild(menusContainer);
            
            // Initialize menus
            CreateFileMenu();
            CreateEditMenu();
            CreateViewMenu();
        }
        
        private void CreateFileMenu()
        {
            var fileMenu = new Menu(font, "File");
            fileMenu.AddItem("New", () => System.Console.WriteLine("New"));
            fileMenu.AddItem("Open", () => System.Console.WriteLine("Open"));
            fileMenu.AddItem("Save", () => System.Console.WriteLine("Save"));
            fileMenu.AddItem("Save As...", () => System.Console.WriteLine("Save As"));
            fileMenu.AddSeparator();
            fileMenu.AddItem("Exit", () => System.Console.WriteLine("Exit"));
            fileMenu.SetOnMenuOpened(OnMenuOpened);
            menuElements.Add(fileMenu);
            menusContainer.AddChild(fileMenu);
        }
        
        private void CreateEditMenu()
        {
            var editMenu = new Menu(font, "Edit");
            editMenu.AddItem("Undo", () => System.Console.WriteLine("Undo"));
            editMenu.AddItem("Redo", () => System.Console.WriteLine("Redo"));
            editMenu.AddSeparator();
            editMenu.AddItem("Cut", () => System.Console.WriteLine("Cut"));
            editMenu.AddItem("Copy", () => System.Console.WriteLine("Copy"));
            editMenu.AddItem("Paste", () => System.Console.WriteLine("Paste"));
            editMenu.SetOnMenuOpened(OnMenuOpened);
            menuElements.Add(editMenu);
            menusContainer.AddChild(editMenu);
        }
        
        private void CreateViewMenu()
        {
            var viewMenu = new Menu(font, "View");
            
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
                        // Menu will be rebuilt automatically when opened next time to show updated checkmarks
                    });
                }
                viewMenu.AddSeparator();
            }
            
            viewMenu.AddItem("Zoom In", () => System.Console.WriteLine("Zoom In"));
            viewMenu.AddItem("Zoom Out", () => System.Console.WriteLine("Zoom Out"));
            viewMenu.AddItem("Reset Zoom", () => System.Console.WriteLine("Reset Zoom"));
            viewMenu.AddSeparator();
            viewMenu.AddItem("Full Screen", () => System.Console.WriteLine("Full Screen"));
            
            viewMenu.SetOnMenuOpened(OnMenuOpened);
            menuElements.Add(viewMenu);
            menusContainer.AddChild(viewMenu);
        }
        
        private void RebuildViewMenu()
        {
            // Remove existing View menu
            bool wasOpen = false;
            if (menuElements.Count > 2 && menusContainer != null)
            {
                var oldViewMenu = menuElements[2];
                wasOpen = oldViewMenu.IsOpen; // Preserve open state
                menusContainer.RemoveChild(oldViewMenu);
                menuElements.RemoveAt(2);
            }
            
            // Create new View menu
            CreateViewMenu();
            
            // Restore open state if it was open
            if (wasOpen && menuElements.Count > 2)
            {
                menuElements[2].IsOpen = true;
            }
        }
        
        private void OnMenuOpened(Menu openedMenu)
        {
            // Close all other menus when one opens
            foreach (var menu in menuElements)
            {
                if (menu != openedMenu)
                {
                    menu.Close();
                }
            }
            
            // Flag that View menu needs to be rebuilt (defer to avoid collection modification during update)
            if (openedMenu.MenuName == "View" && mainPanel != null)
            {
                needsViewMenuRebuild = true;
            }
        }

        public void SetMainPanel(MainPanel mainPanel)
        {
            this.mainPanel = mainPanel;
            RebuildViewMenu();
        }

        public int Height => MenuBarHeight;

        public override void Update()
        {
            // Rebuild View menu if needed (before base.Update to avoid collection modification during iteration)
            if (needsViewMenuRebuild)
            {
                needsViewMenuRebuild = false;
                RebuildViewMenu();
            }
            
            base.Update();
            
            // Update bounds
            Bounds = new Rectangle(0, 0, Raylib.GetScreenWidth(), MenuBarHeight);
            
            // Update menus container bounds
            menusContainer.Bounds = new Rectangle(0, 0, Bounds.Width, MenuBarHeight);
            
            // Update menu element bounds based on their text width
            // Note: We set bounds manually instead of using auto-layout to control exact positioning
            float currentX = 0;
            foreach (var menu in menuElements)
            {
                int textWidth = (int)FontManager.MeasureText(font, menu.MenuName, 14);
                int menuWidth = 12 * 2 + textWidth; // MenuItemPadding * 2
                menu.Bounds = new Rectangle(currentX, 0, menuWidth, MenuBarHeight);
                currentX += menuWidth;
            }
        }

        protected override void DrawSelf()
        {
            // Draw menu bar background
            Raylib.DrawRectangleRec(Bounds, UITheme.SidePanelColor);
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(Bounds.X, Bounds.Y + Bounds.Height),
                new System.Numerics.Vector2(Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height),
                1,
                UITheme.BorderColor
            );
        }

        public void DrawDropdowns()
        {
            // Draw dropdowns separately so they appear on top of panels
            // Create a copy of the list to avoid modification during enumeration
            var menusToDraw = new List<Menu>(menuElements);
            foreach (var menu in menusToDraw)
            {
                // Only draw if menu is still in the current list (hasn't been replaced)
                if (menu.IsOpen && menuElements.Contains(menu))
                {
                    menu.DrawDropdown();
                }
            }
        }
    }
}

