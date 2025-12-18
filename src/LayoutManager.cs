using Raylib_cs;
using System;

namespace Keysharp
{
    public class LayoutManager
    {
        // Panel dimensions (stored as state so they can be modified)
        public float SidePanelWidth { get; set; } = 280;
        public float BottomPanelHeight { get; set; } = 100;
        public static int SplitterWidth => 4;
        public const int MinPanelSize = 50; // Minimum size for panels
        public int MenuBarHeight { get; set; } = 0; // Set by MenuBar

        public void Update(int windowWidth, int windowHeight)
        {
            // Clamp panel sizes to window bounds
            SidePanelWidth = Math.Clamp(SidePanelWidth, MinPanelSize, windowWidth - MinPanelSize - SplitterWidth);
            BottomPanelHeight = Math.Clamp(BottomPanelHeight, MinPanelSize, windowHeight - MinPanelSize - SplitterWidth);
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

