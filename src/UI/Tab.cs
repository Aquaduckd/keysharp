using Raylib_cs;

namespace Keysharp.UI
{
    public class Tab : UIElement
    {
        public string TabName { get; }
        public bool IsActive { get; set; }
        public System.Action? OnClick { get; set; }

        private Font font;
        private const int TabPadding = 15;
        private const int TabHeight = 35;

        public Tab(Font font, string tabName) : base($"Tab_{tabName}")
        {
            this.font = font;
            TabName = tabName;
            
            // Set flags
            IsClickable = true;
            IsHoverable = true;
        }

        public override void Update()
        {
            base.Update();

            // Only process input if enabled and clickable
            if (!IsVisible || !IsEnabled || !IsClickable || IsAnyParentHidden())
                return;

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

        public override void Draw()
        {
            // Only draw if visible
            if (!IsVisible)
                return;
            
            // Tab background
            Color tabColor = IsActive ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(Bounds, tabColor);

            // Tab border
            if (IsActive)
            {
                // Draw bottom border to separate from content
                Raylib.DrawLineEx(
                    new System.Numerics.Vector2(Bounds.X, Bounds.Y + Bounds.Height),
                    new System.Numerics.Vector2(Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height),
                    1,
                    UITheme.BorderColor
                );
            }
            else
            {
                // Draw right border
                Raylib.DrawLineEx(
                    new System.Numerics.Vector2(Bounds.X + Bounds.Width, Bounds.Y),
                    new System.Numerics.Vector2(Bounds.X + Bounds.Width, Bounds.Y + Bounds.Height),
                    1,
                    UITheme.BorderColor
                );
            }

            // Tab text
            Color textColor = IsActive ? UITheme.TextColor : UITheme.TextSecondaryColor;
            FontManager.DrawText(font, TabName, (int)Bounds.X + TabPadding, (int)Bounds.Y + 10, 14, textColor);

            base.Draw();
        }
    }
}

