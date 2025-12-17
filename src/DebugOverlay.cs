using Raylib_cs;
using System.Reflection;
using Keysharp.Components;
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

            // Also draw help screen if it's visible (it's not in the normal hierarchy)
            try
            {
                var helpScreen = GetHelpScreenFromMainPanel(rootUI.MainPanel);
                if (helpScreen != null && helpScreen.IsVisible)
                {
                    DrawUIElement(helpScreen);
                }
            }
            catch
            {
                // If reflection fails, just skip drawing the help screen
                // Don't let it prevent the debug overlay from working
            }
        }

        private void DrawUIElement(Components.UIElement element)
        {
            // Skip hidden elements
            if (!element.IsVisible)
                return;

            // Draw this element's bounds
            DrawDebugRect(element.Bounds, element.Name);

            // Recursively draw all children (they will check their own visibility)
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

        private Components.UIElement? GetHelpScreenFromMainPanel(MainPanel mainPanel)
        {
            try
            {
                // Use reflection to access the private corpusTab field
                var corpusTabField = typeof(MainPanel).GetField("corpusTab", BindingFlags.NonPublic | BindingFlags.Instance);
                if (corpusTabField == null)
                    return null;
                
                var corpusTab = corpusTabField.GetValue(mainPanel);
                if (corpusTab == null)
                    return null;
                
                var regexHelpScreenProperty = corpusTab.GetType().GetProperty("RegexHelpScreen");
                if (regexHelpScreenProperty == null)
                    return null;
                
                return regexHelpScreenProperty.GetValue(corpusTab) as Components.UIElement;
            }
            catch
            {
                return null;
            }
        }
    }
}
