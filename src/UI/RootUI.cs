using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class RootUI : Components.UIElement
    {
        private SidePanel sidePanel;
        private MainPanel mainPanel;
        private BottomPanel bottomPanel;
        private MenuBar menuBar;
        private LayoutManager layout;
        private Splitter verticalSplitter;
        private Splitter horizontalSplitter;
        private TabTransferManager tabTransferManager;

        public RootUI(SidePanel sidePanel, MainPanel mainPanel, BottomPanel bottomPanel, MenuBar menuBar, LayoutManager layout) 
            : base("RootUI")
        {
            this.sidePanel = sidePanel;
            this.mainPanel = mainPanel;
            this.bottomPanel = bottomPanel;
            this.menuBar = menuBar;
            this.layout = layout;

            // Create and configure tab transfer manager
            tabTransferManager = new TabTransferManager(mainPanel, bottomPanel);
            mainPanel.SetTabTransferManager(tabTransferManager);
            mainPanel.SetBottomPanel(bottomPanel);
            bottomPanel.SetTabTransferManager(tabTransferManager);
            bottomPanel.SetMainPanel(mainPanel);
            
            // Root UI is not directly interactive
            IsClickable = false;
            IsHoverable = false;

            // Create splitters
            verticalSplitter = new Splitter("VerticalSplitter", true);
            verticalSplitter.OnDrag = (x) => { layout.SidePanelWidth = x; };
            horizontalSplitter = new Splitter("HorizontalSplitter", false);
            horizontalSplitter.OnDrag = (height) => { layout.BottomPanelHeight = height; };

            // Add all UI elements as children (order matters for drawing)
            AddChild(menuBar);
            AddChild(sidePanel);
            AddChild(verticalSplitter);
            AddChild(mainPanel);
            AddChild(horizontalSplitter);
            AddChild(bottomPanel);
        }

        public override void Update()
        {
            // Set root bounds to full window
            int windowWidth = Raylib.GetScreenWidth();
            int windowHeight = Raylib.GetScreenHeight();
            Bounds = new Rectangle(0, 0, windowWidth, windowHeight);

            // Update layout (handles splitter dragging logic)
            layout.Update(windowWidth, windowHeight);

            // Calculate current layout
            var layoutRect = layout.CalculateLayout(windowWidth, windowHeight);

            // Update menu bar bounds
            menuBar.Bounds = new Rectangle(0, 0, windowWidth, menuBar.Height);

            // Update panel bounds using calculated layout
            sidePanel.UpdateBounds(layoutRect.SidePanel);
            mainPanel.UpdateBounds(layoutRect.MainPanel);
            bottomPanel.UpdateBounds(layoutRect.BottomPanel);

            // Update splitter bounds
            verticalSplitter.Bounds = layoutRect.VerticalSplitter;
            horizontalSplitter.Bounds = layoutRect.HorizontalSplitter;

            // Phase 1: Resolve all bounds (converts relative to absolute, calculates AutoSize)
            // Resolve bounds for all children (MainPanel will handle its own ResolveBounds in its Update method)
            // For other children, we need to call ResolveBounds explicitly
            menuBar.ResolveBounds();
            sidePanel.ResolveBounds();
            bottomPanel.ResolveBounds();
            verticalSplitter.ResolveBounds();
            horizontalSplitter.ResolveBounds();
            
            // Phase 2: Layout and input handling
            // Reset dropdown click consumed flag at the start of each frame
            Components.Dropdown.ResetClickConsumed();
            // Reset panel mouse-over flag at the start of each frame
            Panel.ResetMouseOverBottomPanel();
            
            // Check if mouse is over BottomPanel and set the flag before MainPanel processes input
            // (BottomPanel is drawn on top, so clicks should go to it, not MainPanel)
            // Use resolved bounds (after ResolveBounds was called above)
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            var bottomBounds = bottomPanel.Bounds;
            bool mouseOverBottom = bottomPanel.IsVisible && // Panel must be visible
                                   bottomBounds.Width > 0 && bottomBounds.Height > 0 && // Panel must have valid bounds
                                   mouseX >= bottomBounds.X && mouseX <= bottomBounds.X + bottomBounds.Width &&
                                   mouseY >= bottomBounds.Y && mouseY <= bottomBounds.Y + bottomBounds.Height;
            Panel.SetMouseOverBottomPanel(mouseOverBottom);
            
            // MainPanel and BottomPanel have special Update(Rectangle) methods that handle ResolveBounds internally
            mainPanel.Update(layoutRect.MainPanel);
            bottomPanel.Update(layoutRect.BottomPanel);
            
            menuBar.Update();
            sidePanel.Update();
            verticalSplitter.Update();
            horizontalSplitter.Update();
        }

        public override void Draw()
        {
            // Draw all UI elements recursively (menu bar, panels, splitters, and their children)
            base.Draw();

            // Draw dropdowns on top (menu bar and panel dropdowns)
            menuBar.DrawDropdowns();
            mainPanel.DrawDropdowns();
            sidePanel.DrawDropdowns();
            bottomPanel.DrawDropdowns();
            
            // Draw help screens on top of everything
            mainPanel.DrawHelpScreen();
            bottomPanel.DrawHelpScreen();
        }

        public MenuBar MenuBar => menuBar;
        public MainPanel MainPanel => mainPanel;
        public LayoutManager Layout => layout;
    }
}

