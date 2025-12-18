using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class MenuItem : Components.UIElement
    {
        public string Text { get; set; }
        public System.Action? OnClick { get; set; }
        public bool IsOpen { get; set; }

        private Font font;
        private int fontSize;

        public void UpdateFont(Font newFont)
        {
            font = newFont;
        }

        public MenuItem(Font font, string text, int fontSize = 14) : base($"MenuItem_{text}")
        {
            this.font = font;
            this.Text = text;
            this.fontSize = fontSize;
        }

        public override void Update()
        {
            base.Update();

            // Handle clicks
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                int mouseX = Raylib.GetMouseX();
                int mouseY = Raylib.GetMouseY();

                if (IsHovering(mouseX, mouseY))
                {
                    OnClick?.Invoke();
                }
            }
        }

        protected override void DrawSelf()
        {
            // Draw menu item background if open or hovered
            if (IsOpen)
            {
                Raylib.DrawRectangleRec(Bounds, UITheme.MainPanelColor);
            }

            // Draw menu text
            Color textColor = IsOpen ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, Text, (int)Bounds.X + 12, 8, fontSize, textColor);
        }
    }
}

