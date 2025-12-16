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

        public void Draw(RootUI rootUI, PanelLayout panelLayout)
        {
            if (!isEnabled)
                return;

            // Recursively draw all UI elements starting from root
            // This includes panels, tabs, buttons, dropdowns, info text, splitters, and menu bar
            DrawUIElement(rootUI);
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

    }
}

