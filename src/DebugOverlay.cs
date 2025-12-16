using Raylib_cs;
using Keysharp.Panels;

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

            // Draw debug rectangles for all panels and splitters
            DrawDebugRect(panelLayout.SidePanel, "SidePanel");
            DrawDebugRect(panelLayout.MainPanel, "MainPanel");
            DrawDebugRect(panelLayout.BottomPanel, "BottomPanel");
            DrawDebugRect(panelLayout.VerticalSplitter, "VerticalSplitter");
            DrawDebugRect(panelLayout.HorizontalSplitter, "HorizontalSplitter");

            // Draw menu bar
            Rectangle menuBarRect = new Rectangle(0, 0, Raylib.GetScreenWidth(), menuBar.Height);
            DrawDebugRect(menuBarRect, "MenuBar");

            // Draw menu items
            DrawMenuBarItems(menuBar);

            // Draw component bounds within panels
            DrawPanelComponentBounds(mainPanel, panelLayout.MainPanel);
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

        private void DrawPanelComponentBounds(MainPanel mainPanel, Rectangle panelBounds)
        {
            // Draw tab bounds
            var tabBounds = mainPanel.GetTabBounds(panelBounds);
            for (int i = 0; i < tabBounds.Count; i++)
            {
                DrawDebugRect(tabBounds[i], $"Tab{i}");
            }

            // Draw bounds for buttons and dropdowns in the active tab
            if (mainPanel.LoadCorpusButton != null)
            {
                DrawDebugRect(mainPanel.LoadCorpusButton.Bounds, "LoadButton");
            }

            if (mainPanel.CorpusDropdown != null)
            {
                DrawDebugRect(mainPanel.CorpusDropdown.Bounds, "CorpusDropdown");
            }

            // Draw info text bounds
            var infoTextBounds = mainPanel.GetInfoTextBounds(panelBounds);
            if (infoTextBounds.HasValue)
            {
                DrawDebugRect(infoTextBounds.Value, "InfoText");
            }
        }
    }
}

