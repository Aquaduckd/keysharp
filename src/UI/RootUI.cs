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

        public RootUI(SidePanel sidePanel, MainPanel mainPanel, BottomPanel bottomPanel, MenuBar menuBar, LayoutManager layout) 
            : base("RootUI")
        {
            this.sidePanel = sidePanel;
            this.mainPanel = mainPanel;
            this.bottomPanel = bottomPanel;
            this.menuBar = menuBar;
            this.layout = layout;
            
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

            // MainPanel has a special Update(Rectangle) method for tab-specific logic
            // This sets bounds and calls base.Update() internally, so we don't need to call base.Update() here
            mainPanel.Update(layoutRect.MainPanel);
            
            // Update other children (menu bar, side panel, bottom panel, splitters)
            // MainPanel is already updated above, so skip it
            menuBar.Update();
            sidePanel.Update();
            bottomPanel.Update();
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
        }

        public MenuBar MenuBar => menuBar;
        public MainPanel MainPanel => mainPanel;
        public LayoutManager Layout => layout;
    }
}

