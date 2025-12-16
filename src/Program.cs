using Raylib_cs;
using System;
using System.Linq;
using Keysharp.Panels;
using Keysharp.UI;

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

            // Create root UI element (contains all panels)
            RootUI rootUI = new RootUI(sidePanel, mainPanel, bottomPanel, menuBar, layout);

            // Create debug overlay
            DebugOverlay debugOverlay = new DebugOverlay();

            while (!Raylib.WindowShouldClose())
            {
                // Update root UI (recursively updates all children)
                rootUI.Update();

                // Update debug overlay
                debugOverlay.Update();

                // Cursor is handled by individual UI elements
                var layoutRect = rootUI.Layout.CalculateLayout(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                ResolveCursor(rootUI, layoutRect.MainPanel);

                Raylib.BeginDrawing();
                Raylib.ClearBackground(UITheme.BackgroundColor);

                // Draw root UI (recursively draws all children)
                rootUI.Draw();

                // Draw debug overlay on top of everything
                debugOverlay.Draw(rootUI, layoutRect);

                Raylib.EndDrawing();
            }

            // Unload font if it's not the default
            if (font.Texture.Id != 0)
            {
                Raylib.UnloadFont(font);
            }

            Raylib.CloseWindow();
        }

        private static void ResolveCursor(RootUI rootUI, Rectangle mainPanelBounds)
        {
            // Check all UI elements in priority order to determine cursor
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            
            // Priority 1: Splitters (highest priority)
            if (rootUI.Children.OfType<Splitter>().Any(s => s.IsHovering(mouseX, mouseY) || s.IsDragging))
            {
                var splitter = rootUI.Children.OfType<Splitter>().FirstOrDefault(s => s.IsHovering(mouseX, mouseY) || s.IsDragging);
                if (splitter != null)
                {
                    Raylib.SetMouseCursor(splitter.IsVertical 
                        ? MouseCursor.MOUSE_CURSOR_RESIZE_EW 
                        : MouseCursor.MOUSE_CURSOR_RESIZE_NS);
                    return;
                }
            }
            
            // Priority 2: Interactive elements (buttons, dropdowns, tabs, menu items)
            if (IsHoveringInteractiveElement(rootUI, mouseX, mouseY))
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
                return;
            }
            
            // Default cursor
            Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_DEFAULT);
        }
        
        private static bool IsHoveringInteractiveElement(UIElement element, int mouseX, int mouseY)
        {
            // Check this element - use flags instead of type checking
            if (element.IsHoverable && element.IsEnabled && element.IsHovering(mouseX, mouseY))
            {
                return true;
            }
            
            // Check children recursively
            foreach (var child in element.Children)
            {
                if (IsHoveringInteractiveElement(child, mouseX, mouseY))
                {
                    return true;
                }
            }
            
            return false;
        }
    }
}
