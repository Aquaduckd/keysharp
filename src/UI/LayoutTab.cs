using Raylib_cs;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class LayoutTab
    {
        private Components.TabContent tabContent;
        private Components.Label titleLabel;
        private Components.Canvas keyboardCanvas;
        private Components.KeyboardLayoutView keyboardView;
        private Layout layout;
        private SidePanel? sidePanel;

        public Components.TabContent TabContent => tabContent;

        public SidePanel? SidePanel
        {
            get => sidePanel;
            set
            {
                sidePanel = value;
            }
        }

        public LayoutTab(Font font)
        {
            // Create the standard 60% QWERTY layout
            layout = Layout.CreateStandard60PercentQwerty();

            // Create tab content without title (we'll use a Label element instead)
            tabContent = new Components.TabContent(font, "", null);
            tabContent.PositionMode = Components.PositionMode.Relative;
            tabContent.AutoLayoutChildren = true;
            tabContent.LayoutDirection = Components.LayoutDirection.Vertical;
            tabContent.ChildJustification = Components.ChildJustification.Left;
            tabContent.ChildPadding = 20;
            tabContent.ChildGap = 0;

            // Create title label
            titleLabel = new Components.Label(font, "Layout", 24);
            titleLabel.AutoSize = false;
            titleLabel.Bounds = new Rectangle(0, 0, 0, 40); // Height for title
            titleLabel.PositionMode = Components.PositionMode.Relative;

            // Create canvas to hold the keyboard view
            keyboardCanvas = new Components.Canvas("KeyboardCanvas");
            keyboardCanvas.AutoSize = true; // Auto-size to fit keyboard
            keyboardCanvas.AutoLayoutChildren = true; // Enable layout to calculate size
            keyboardCanvas.PositionMode = Components.PositionMode.Relative;

            // Create keyboard layout view
            keyboardView = new Components.KeyboardLayoutView(font);
            keyboardView.Layout = layout;
            keyboardView.PositionMode = Components.PositionMode.Relative;
            keyboardView.OnSelectedKeyChanged = (key) => {
                // Use the property getter to get current value
                var currentSidePanel = SidePanel;
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetSelectedKey(key);
                }
            };
            
            keyboardCanvas.AddChild(keyboardView);
            tabContent.AddChild(titleLabel);
            tabContent.AddChild(keyboardCanvas);
        }

        public void Update(Rectangle contentArea)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
            tabContent.TargetHeight = contentArea.Height; // Set target height for vertical layout

            // Set title label width to fill available space
            titleLabel.Bounds = new Rectangle(0, 0, contentArea.Width, 40);

            // Update keyboard view to calculate its bounds if needed (this will set width/height)
            keyboardView.Update();
            
            // Canvas will auto-size to fit keyboard, but we need to ensure keyboard has proper bounds
            // The canvas's AutoSize will handle resizing based on keyboardView's bounds

            // Align the keyboard view to top-left within the canvas using relative position
            if (keyboardView.Bounds.Width > 0 && keyboardView.Bounds.Height > 0)
            {
                float keyboardX = 0; // Left align
                float keyboardY = 0; // Top align (title is handled by layout)
                keyboardView.RelativePosition = new System.Numerics.Vector2(keyboardX, keyboardY);
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }
    }
}

