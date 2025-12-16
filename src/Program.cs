using Raylib_cs;
using System;
using Keysharp.Panels;

namespace Keysharp
{
    class Program
    {
        public static void Main(string[] args)
        {
            const int screenWidth = 1000;
            const int screenHeight = 700;

            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(screenWidth, screenHeight, "Keysharp");
            Raylib.SetTargetFPS(60);

            // Load font
            Font font = FontManager.LoadFont();

            // Create panels
            SidePanel sidePanel = new SidePanel(font);
            MainPanel mainPanel = new MainPanel(font);
            BottomPanel bottomPanel = new BottomPanel(font);

            // Create menu bar (pass mainPanel so it can access tabs)
            MenuBar menuBar = new MenuBar(font, mainPanel);

            // Create layout manager
            LayoutManager layout = new LayoutManager();
            layout.MenuBarHeight = menuBar.Height;

            while (!Raylib.WindowShouldClose())
            {
                int windowWidth = Raylib.GetScreenWidth();
                int windowHeight = Raylib.GetScreenHeight();

                // Update layout (handles resizing)
                layout.Update(windowWidth, windowHeight);

                // Calculate current layout
                var layoutRect = layout.CalculateLayout(windowWidth, windowHeight);

                // Update menu bar and panels (handle input)
                menuBar.Update();
                mainPanel.Update(layoutRect.MainPanel);
                
                // Resolve cursor with proper priority (only set once per frame)
                ResolveCursor(layout, menuBar, mainPanel, layoutRect.MainPanel);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(UITheme.BackgroundColor);

                // Draw menu bar (bar only, not dropdowns)
                menuBar.Draw();

                // Draw panels (each panel draws itself)
                sidePanel.Draw(layoutRect.SidePanel);
                mainPanel.Draw(layoutRect.MainPanel);
                bottomPanel.Draw(layoutRect.BottomPanel);

                // Draw splitters
                layout.DrawSplitters();

                // Draw menu dropdowns on top of everything
                menuBar.DrawDropdowns();

                // Draw panel dropdowns on top of everything
                mainPanel.DrawDropdowns();

                Raylib.EndDrawing();
            }

            // Unload font if it's not the default
            if (font.Texture.Id != 0)
            {
                Raylib.UnloadFont(font);
            }

            Raylib.CloseWindow();
        }

        private static void ResolveCursor(LayoutManager layout, MenuBar menuBar, MainPanel mainPanel, Rectangle mainPanelBounds)
        {
            // Priority order:
            // 1. Splitters (highest priority) - already handled in LayoutManager.Update()
            // 2. Interactive elements (buttons, dropdowns, tabs, menus)
            // 3. Default (lowest priority)

            // Check splitters first (highest priority)
            if (layout.IsHoveringSplitter())
            {
                // Cursor already set by LayoutManager.Update()
                return;
            }

            // Check interactive elements
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check menu bar (includes dropdowns)
            if (menuBar.IsHovering(mouseX, mouseY))
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
                return;
            }

            // Check tabs
            if (mainPanel.IsHoveringTab(mainPanelBounds, mouseX, mouseY))
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
                return;
            }

            // Check buttons and dropdowns in main panel
            if (mainPanel.IsHoveringInteractiveElement(mainPanelBounds, mouseX, mouseY))
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
                return;
            }

            // Default cursor
            Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_DEFAULT);
        }
    }
}
