using Raylib_cs;
using Keysharp.Panels;
using Keysharp.UI;

namespace Keysharp
{
    public class DebugOverlay
    {
        private bool isEnabled = false;

        public void Update()
        {
            // Toggle on F3 press
            if (Raylib.IsKeyPressed(KeyboardKey.KEY_F3))
            {
                isEnabled = !isEnabled;
            }
        }

        public void Draw(LayoutManager layout, MenuBar menuBar, SidePanel sidePanel, MainPanel mainPanel, BottomPanel bottomPanel, PanelLayout panelLayout)
        {
            if (!isEnabled)
                return;

            // Draw debug rectangles for splitters (not UI elements)
            DrawDebugRect(panelLayout.VerticalSplitter, "VerticalSplitter");
            DrawDebugRect(panelLayout.HorizontalSplitter, "HorizontalSplitter");

            // Update panel bounds before drawing
            sidePanel.UpdateBounds(panelLayout.SidePanel);
            mainPanel.UpdateBounds(panelLayout.MainPanel);
            bottomPanel.UpdateBounds(panelLayout.BottomPanel);

            // Recursively draw all UI elements
            DrawUIElement(sidePanel);
            DrawUIElement(mainPanel);
            DrawUIElement(bottomPanel);

            // Draw menu bar (not a UIElement yet, so handle separately)
            Rectangle menuBarRect = new Rectangle(0, 0, Raylib.GetScreenWidth(), menuBar.Height);
            DrawDebugRect(menuBarRect, "MenuBar");
            DrawMenuBarItems(menuBar);

            // Draw tab bounds (special case for MainPanel)
            var tabBounds = mainPanel.GetTabBounds(panelLayout.MainPanel);
            for (int i = 0; i < tabBounds.Count; i++)
            {
                DrawDebugRect(tabBounds[i], $"Tab{i}");
            }

            // Draw info text bounds (special case)
            var infoTextBounds = mainPanel.GetInfoTextBounds(panelLayout.MainPanel);
            if (infoTextBounds.HasValue)
            {
                DrawDebugRect(infoTextBounds.Value, "InfoText");
            }
        }

        private void DrawUIElement(UI.UIElement element)
        {
            // Draw this element's bounds
            DrawDebugRect(element.Bounds, element.Name);

            // Recursively draw all children
            foreach (var child in element.Children)
            {
                DrawUIElement(child);
            }
        }

        private void DrawDebugRect(Rectangle bounds, string label)
        {
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            
            // Draw rectangle outline
            Raylib.DrawRectangleLinesEx(bounds, 2, new Color(255, 0, 0, 200)); // Red outline

            // Only show label when hovering over the bounding box
            if (mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                mouseY >= bounds.Y && mouseY <= bounds.Y + bounds.Height)
            {
                // Draw label at top-left corner
                int labelX = (int)bounds.X + 2;
                int labelY = (int)bounds.Y + 2;
                Raylib.DrawRectangle(labelX - 1, labelY - 1, 100, 16, new Color(0, 0, 0, 180)); // Semi-transparent background
                Raylib.DrawText(label, labelX, labelY, 10, Color.WHITE);
            }
        }

        private void DrawMenuBarItems(MenuBar menuBar)
        {
            var menuItemBounds = menuBar.GetMenuItemBounds();
            for (int i = 0; i < menuItemBounds.Count; i++)
            {
                DrawDebugRect(menuItemBounds[i], $"Menu{i}");
            }
        }
    }
}

