using Raylib_cs;
using System;
using System.Collections.Generic;
using Keysharp.Components;

namespace Keysharp.UI
{
    /// <summary>
    /// Manages tab transfers between MainPanel and BottomPanel.
    /// Handles drag-and-drop detection across panels and coordinates tab transfers.
    /// </summary>
    public class TabTransferManager
    {
        private const int TabHeight = 35;
        
        private MainPanel mainPanel;
        private BottomPanel bottomPanel;

        public TabTransferManager(MainPanel mainPanel, BottomPanel bottomPanel)
        {
            this.mainPanel = mainPanel;
            this.bottomPanel = bottomPanel;
        }

        /// <summary>
        /// Checks if the mouse is over a panel's tab area (for drop detection).
        /// Returns the panel's bounds if the mouse is over the tab area, null otherwise.
        /// </summary>
        public Rectangle? GetDropTargetBounds(int mouseX, int mouseY)
        {
            // Check if over MainPanel's tab area
            var mainBounds = mainPanel.GetTabAreaBounds();
            if (mainBounds.HasValue)
            {
                var bounds = mainBounds.Value;
                if (mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                    mouseY >= bounds.Y && mouseY <= bounds.Y + bounds.Height)
                {
                    return bounds;
                }
            }

            // Check if over BottomPanel's tab area
            var bottomBounds = bottomPanel.GetTabAreaBounds();
            if (bottomBounds.HasValue)
            {
                var bounds = bottomBounds.Value;
                if (mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width &&
                    mouseY >= bounds.Y && mouseY <= bounds.Y + bounds.Height)
                {
                    return bounds;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if the mouse is over the MainPanel's tab area.
        /// </summary>
        public bool IsOverMainPanelTabArea(int mouseX, int mouseY)
        {
            var bounds = mainPanel.GetTabAreaBounds();
            if (!bounds.HasValue)
            {
                System.Console.WriteLine($"IsOverMainPanelTabArea: No tab area bounds (no tabs?)");
                return false;
            }
            
            var b = bounds.Value;
            bool isOver = mouseX >= b.X && mouseX <= b.X + b.Width &&
                   mouseY >= b.Y && mouseY <= b.Y + b.Height;
            if (isOver)
            {
                System.Console.WriteLine($"IsOverMainPanelTabArea: YES - mouse({mouseX}, {mouseY}) in bounds({b.X}, {b.Y}, {b.Width}, {b.Height})");
            }
            return isOver;
        }

        /// <summary>
        /// Checks if the mouse is over the BottomPanel's tab area.
        /// </summary>
        public bool IsOverBottomPanelTabArea(int mouseX, int mouseY)
        {
            var bounds = bottomPanel.GetTabAreaBounds();
            if (!bounds.HasValue)
            {
                System.Console.WriteLine($"IsOverBottomPanelTabArea: No tab area bounds (no tabs?)");
                return false;
            }
            
            var b = bounds.Value;
            bool isOver = mouseX >= b.X && mouseX <= b.X + b.Width &&
                   mouseY >= b.Y && mouseY <= b.Y + b.Height;
            if (isOver)
            {
                System.Console.WriteLine($"IsOverBottomPanelTabArea: YES - mouse({mouseX}, {mouseY}) in bounds({b.X}, {b.Y}, {b.Width}, {b.Height})");
            }
            return isOver;
        }


        /// <summary>
        /// Transfers a tab from MainPanel to BottomPanel.
        /// </summary>
        public bool TransferTabFromMainToBottom(string tabName, int dropIndex)
        {
            System.Console.WriteLine($"TransferTabFromMainToBottom: Transferring '{tabName}' from MainPanel to BottomPanel at index {dropIndex}");
            
            // Get tab instance and content from MainPanel
            var tabData = mainPanel.RemoveTab(tabName);
            if (tabData == null)
            {
                System.Console.WriteLine($"TransferTabFromMainToBottom: FAILED - Could not remove tab '{tabName}' from MainPanel");
                return false;
            }

            System.Console.WriteLine($"TransferTabFromMainToBottom: Successfully removed tab from MainPanel, adding to BottomPanel...");

            // Add to BottomPanel
            bottomPanel.AddTabWithContent(tabName, tabData.TabInstance, tabData.TabContent, dropIndex);

            System.Console.WriteLine($"TransferTabFromMainToBottom: SUCCESS - Tab '{tabName}' transferred to BottomPanel");
            return true;
        }

        /// <summary>
        /// Transfers a tab from BottomPanel to MainPanel.
        /// </summary>
        public bool TransferTabFromBottomToMain(string tabName, int dropIndex)
        {
            System.Console.WriteLine($"TransferTabFromBottomToMain: Transferring '{tabName}' from BottomPanel to MainPanel at index {dropIndex}");
            
            // Get tab instance and content from BottomPanel
            var tabData = bottomPanel.RemoveTab(tabName);
            if (tabData == null)
            {
                System.Console.WriteLine($"TransferTabFromBottomToMain: FAILED - Could not remove tab '{tabName}' from BottomPanel");
                return false;
            }

            System.Console.WriteLine($"TransferTabFromBottomToMain: Successfully removed tab from BottomPanel, adding to MainPanel...");

            // Add to MainPanel
            mainPanel.AddTabWithContent(tabName, tabData.TabInstance, tabData.TabContent, dropIndex);

            System.Console.WriteLine($"TransferTabFromBottomToMain: SUCCESS - Tab '{tabName}' transferred to MainPanel");
            return true;
        }

        /// <summary>
        /// Transfers a tab from one panel to another based on source and target.
        /// </summary>
        public bool TransferTab(string tabName, bool fromMainPanel, int dropIndex)
        {
            if (fromMainPanel)
            {
                return TransferTabFromMainToBottom(tabName, dropIndex);
            }
            else
            {
                return TransferTabFromBottomToMain(tabName, dropIndex);
            }
        }
    }

    /// <summary>
    /// Data structure containing tab instance and content for transfers.
    /// </summary>
    public class TabTransferData
    {
        public object TabInstance { get; set; }
        public Components.TabContent TabContent { get; set; }

        public TabTransferData(object tabInstance, Components.TabContent tabContent)
        {
            TabInstance = tabInstance;
            TabContent = tabContent;
        }
    }
}

