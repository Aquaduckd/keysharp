using Raylib_cs;

namespace Keysharp.Components
{
    public class Tab : UIElement
    {
        public string TabName { get; }
        public bool IsActive { get; set; }
        public System.Action? OnClick { get; set; }
        
        // Drag state
        public bool IsDragging { get; private set; }
        public int DragStartX { get; private set; }
        public int DragStartY { get; private set; }

        private Font font;
        private const int TabPadding = 15;
        private const int TabHeight = 35;
        private const int DragThreshold = 5; // Pixels to move before drag starts
        private bool isDragStarted = false;
        private bool wasClicked = false; // Track if this tab received the initial click

        public void UpdateFont(Font newFont)
        {
            font = newFont;
        }

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
            {
                // Reset drag state if not enabled
                if (IsDragging)
                {
                    IsDragging = false;
                    isDragStarted = false;
                    wasClicked = false;
                }
                return;
            }

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            bool isHovering = IsHovering(mouseX, mouseY);

            // Handle mouse button press
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (isHovering)
                {
                    DragStartX = mouseX;
                    DragStartY = mouseY;
                    isDragStarted = false;
                    wasClicked = true; // Mark that this tab was clicked
                }
                else
                {
                    wasClicked = false; // Another tab was clicked
                }
            }

            // Handle drag start - only if this tab was the one clicked
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (wasClicked && !IsDragging && !isDragStarted)
                {
                    int deltaX = System.Math.Abs(mouseX - DragStartX);
                    int deltaY = System.Math.Abs(mouseY - DragStartY);
                    
                    if (deltaX > DragThreshold || deltaY > DragThreshold)
                    {
                        IsDragging = true;
                        isDragStarted = true;
                    }
                }
                else if (!wasClicked && IsDragging)
                {
                    // If we didn't get the click but somehow are dragging, stop
                    IsDragging = false;
                    isDragStarted = false;
                }
            }

            // Handle mouse button release
            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (IsDragging)
                {
                    IsDragging = false;
                    isDragStarted = false;
                }
                else if (isHovering && wasClicked && !isDragStarted)
                {
                    // Only trigger click if we were the one clicked and didn't drag
                    OnClick?.Invoke();
                }
                wasClicked = false;
                isDragStarted = false;
            }
        }

        protected override void DrawSelf()
        {
            // Tab background (lighter when dragging)
            Color tabColor = IsActive ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            if (IsDragging)
            {
                // Make tab slightly transparent when dragging
                tabColor = new Color(tabColor.R, tabColor.G, tabColor.B, (byte)(tabColor.A * 0.7f));
            }
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
        }
    }
}

