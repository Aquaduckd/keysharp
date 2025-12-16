using Raylib_cs;
using System;

namespace Keysharp
{
    public class LayoutManager
    {
        // Panel dimensions (stored as state so they can be modified)
        public float SidePanelWidth { get; set; } = 200;
        public float BottomPanelHeight { get; set; } = 100;
        public const int SplitterWidth = 4;
        public const int MinPanelSize = 50; // Minimum size for panels
        public int MenuBarHeight { get; set; } = 0; // Set by MenuBar

        // Splitter state
        private bool isDraggingVertical = false;
        private bool isDraggingHorizontal = false;
        private bool isHoveringVertical = false;
        private bool isHoveringHorizontal = false;

        public bool IsHoveringSplitter()
        {
            return isHoveringVertical || isDraggingVertical || isHoveringHorizontal || isDraggingHorizontal;
        }

        public void Update(int windowWidth, int windowHeight)
        {
            // Clamp panel sizes to window bounds
            SidePanelWidth = Math.Clamp(SidePanelWidth, MinPanelSize, windowWidth - MinPanelSize - SplitterWidth);
            BottomPanelHeight = Math.Clamp(BottomPanelHeight, MinPanelSize, windowHeight - MinPanelSize - SplitterWidth);

            // Calculate splitter positions
            int verticalSplitterX = (int)SidePanelWidth;
            int horizontalSplitterY = windowHeight - (int)BottomPanelHeight - SplitterWidth;

            // Get mouse position
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if mouse is over vertical splitter
            isHoveringVertical = mouseX >= verticalSplitterX && 
                                mouseX <= verticalSplitterX + SplitterWidth &&
                                mouseY >= MenuBarHeight && 
                                mouseY <= windowHeight;

            // Check if mouse is over horizontal splitter
            isHoveringHorizontal = mouseY >= horizontalSplitterY && 
                                  mouseY <= horizontalSplitterY + SplitterWidth &&
                                  mouseX >= verticalSplitterX + SplitterWidth && 
                                  mouseX <= windowWidth;

            // Handle mouse input
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                if (isHoveringVertical)
                {
                    isDraggingVertical = true;
                }
                else if (isHoveringHorizontal)
                {
                    isDraggingHorizontal = true;
                }
            }

            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                isDraggingVertical = false;
                isDraggingHorizontal = false;
            }

            // Update panel sizes while dragging
            if (isDraggingVertical)
            {
                SidePanelWidth = mouseX;
                SidePanelWidth = Math.Clamp(SidePanelWidth, MinPanelSize, windowWidth - MinPanelSize - SplitterWidth);
            }

            if (isDraggingHorizontal)
            {
                BottomPanelHeight = windowHeight - mouseY;
                BottomPanelHeight = Math.Clamp(BottomPanelHeight, MinPanelSize, windowHeight - MinPanelSize - SplitterWidth);
            }

            // Change cursor when hovering over splitters
            // This has priority over other cursor changes
            if (isHoveringVertical || isDraggingVertical)
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_EW);
            }
            else if (isHoveringHorizontal || isDraggingHorizontal)
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_RESIZE_NS);
            }
        }

        public PanelLayout CalculateLayout(int windowWidth, int windowHeight)
        {
            int sidePanelX = 0;
            int sidePanelY = MenuBarHeight;
            int sidePanelWidth = (int)SidePanelWidth;
            int sidePanelHeight = windowHeight - MenuBarHeight;

            int splitterX = sidePanelWidth;
            int splitterY = MenuBarHeight;
            int splitterWidth = SplitterWidth;
            int splitterHeight = windowHeight - MenuBarHeight;

            int mainPanelX = sidePanelWidth + SplitterWidth;
            int mainPanelY = MenuBarHeight;
            int mainPanelWidth = windowWidth - sidePanelWidth - SplitterWidth;
            int mainPanelHeight = windowHeight - MenuBarHeight - (int)BottomPanelHeight - SplitterWidth;

            int bottomSplitterX = sidePanelWidth + SplitterWidth;
            int bottomSplitterY = mainPanelY + mainPanelHeight;
            int bottomSplitterWidth = mainPanelWidth;
            int bottomSplitterHeight = SplitterWidth;

            int bottomPanelX = sidePanelWidth + SplitterWidth;
            int bottomPanelY = bottomSplitterY + SplitterWidth;
            int bottomPanelWidth = mainPanelWidth;
            int bottomPanelHeight = (int)BottomPanelHeight;

            return new PanelLayout
            {
                SidePanel = new Rectangle(sidePanelX, sidePanelY, sidePanelWidth, sidePanelHeight),
                VerticalSplitter = new Rectangle(splitterX, splitterY, splitterWidth, splitterHeight),
                MainPanel = new Rectangle(mainPanelX, mainPanelY, mainPanelWidth, mainPanelHeight),
                HorizontalSplitter = new Rectangle(bottomSplitterX, bottomSplitterY, bottomSplitterWidth, bottomSplitterHeight),
                BottomPanel = new Rectangle(bottomPanelX, bottomPanelY, bottomPanelWidth, bottomPanelHeight)
            };
        }

        public void DrawSplitters()
        {
            var layoutRect = CalculateLayout(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
            
            // Draw vertical splitter (with hover effect)
            Color splitterColor = (isHoveringVertical || isDraggingVertical) 
                ? UITheme.SplitterHoverColor 
                : UITheme.SplitterColor;
            Raylib.DrawRectangleRec(layoutRect.VerticalSplitter, splitterColor);

            // Draw horizontal splitter (with hover effect)
            splitterColor = (isHoveringHorizontal || isDraggingHorizontal) 
                ? UITheme.SplitterHoverColor 
                : UITheme.SplitterColor;
            Raylib.DrawRectangleRec(layoutRect.HorizontalSplitter, splitterColor);
        }
    }

    public struct PanelLayout
    {
        public Rectangle SidePanel;
        public Rectangle VerticalSplitter;
        public Rectangle MainPanel;
        public Rectangle HorizontalSplitter;
        public Rectangle BottomPanel;
    }
}

