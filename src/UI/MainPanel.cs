using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class MainPanel : Panel
    {
        private const int TabHeight = 35;
        private const int TabPadding = 15;
        private const int TabSpacing = 2;

        private List<string> tabs = new List<string> { "layout", "corpus", "stats", "settings" };
        private HashSet<string> visibleTabs = new HashSet<string> { "layout", "corpus", "metrics", "settings" };
        private int activeTabIndex = 0;

        // Tab elements
        private List<Components.Tab> tabElements = new List<Components.Tab>();
        
        // Containers for layout
        private Components.Container? tabsContainer;
        private Components.Container? tabContentContainer;
        
        // Tab classes
        private LayoutTab? layoutTab;
        private CorpusTab? corpusTab;
        private StatsTab? statsTab;
        private SettingsTab? settingsTab;

        // Reference to side panel for key info display
        private SidePanel? sidePanel;
        public CorpusTab? CorpusTab => corpusTab;

        public SidePanel? SidePanel
        {
            get => sidePanel;
            set
            {
                sidePanel = value;
                // Propagate to layout tab if it exists
                if (layoutTab != null)
                {
                    layoutTab.SidePanel = value;
                    // Also connect layout tab back to side panel for disabled key changes
                    if (value != null)
                    {
                        value.LayoutTab = layoutTab;
                    }
                }
            }
        }

        public MainPanel(Font font) : base(font, "MainPanel")
        {
            // All tabs visible by default
            foreach (var tab in tabs)
            {
                visibleTabs.Add(tab);
            }

            // Create container for tabs
            tabsContainer = new Container("TabsContainer");
            tabsContainer.AutoLayoutChildren = true;
            tabsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            tabsContainer.ChildJustification = Components.ChildJustification.Left;
            tabsContainer.ChildGap = TabSpacing;
            tabsContainer.ChildPadding = 0;
            tabsContainer.AutoSize = false; // Width/height set manually
            AddChild(tabsContainer);

            // Create tab elements
            for (int i = 0; i < tabs.Count; i++)
            {
                var tabElement = new Components.Tab(font, tabs[i]);
                int tabIndex = i; // Capture for closure
                tabElement.OnClick = () => { activeTabIndex = tabIndex; UpdateTabVisibility(); };
                tabElements.Add(tabElement);
                tabsContainer.AddChild(tabElement);
            }
            
            // Create container for tab content
            tabContentContainer = new Components.Container("TabContentContainer");
            tabContentContainer.AutoLayoutChildren = false; // Content positioned manually
            tabContentContainer.AutoSize = false; // Size set manually
            AddChild(tabContentContainer);

            // Create tab classes
            corpusTab = new CorpusTab(font);
            
            layoutTab = new LayoutTab(font);
            layoutTab.SidePanel = sidePanel;
            // Initialize metadata in side panel
            sidePanel?.SetLayoutMetadata(layoutTab.Metadata);
            // Connect layout tab to side panel for disabled key changes
            if (sidePanel != null)
            {
                sidePanel.LayoutTab = layoutTab;
            } // Connect to side panel for key info display (may be null initially)
            layoutTab.CorpusTab = corpusTab; // Connect to corpus tab for heatmap data
            
            // Notify layout tab when corpus changes
            corpusTab.SetOnCorpusChanged(() => {
                layoutTab?.OnCorpusChanged();
            });

            // Connect layout to corpus tab for key sequence display
            corpusTab.SetLayout(layoutTab.Layout);
            
            // Notify stats tab when metric analyzer is updated
            // Note: StatsTab sets its own callback, so we don't need to set it here
            // The StatsTab callback handles both baseline reset and marking for update
            
            layoutTab.SetVisible(activeTabIndex == 0);
            tabContentContainer.AddChild(layoutTab.TabContent);
            corpusTab.SetVisible(activeTabIndex == 1);
            tabContentContainer.AddChild(corpusTab.TabContent);

            statsTab = new StatsTab(font);
            statsTab.CorpusTab = corpusTab;
            statsTab.SetVisible(activeTabIndex == 2);
            tabContentContainer.AddChild(statsTab.TabContent);

            settingsTab = new SettingsTab(font);
            settingsTab.SetVisible(activeTabIndex == 3);
            tabContentContainer.AddChild(settingsTab.TabContent);
        }

        public List<string> GetTabs()
        {
            return new List<string>(tabs);
        }

        public void MoveTab(string tabName, int newIndex)
        {
            int oldIndex = tabs.IndexOf(tabName);
            if (oldIndex == -1 || newIndex < 0 || newIndex >= tabs.Count || oldIndex == newIndex)
                return;

            // Reorder lists
            tabs.RemoveAt(oldIndex);
            tabs.Insert(newIndex, tabName);

            var tabElement = tabElements[oldIndex];
            tabElements.RemoveAt(oldIndex);
            tabElements.Insert(newIndex, tabElement);

            // Reorder in container (manipulate Children list directly)
            if (tabsContainer != null)
            {
                tabsContainer.Children.Remove(tabElement);
                tabsContainer.Children.Insert(newIndex, tabElement);
            }

            // Update active tab index
            if (activeTabIndex == oldIndex)
            {
                activeTabIndex = newIndex;
            }
            else if (activeTabIndex > oldIndex && activeTabIndex <= newIndex)
            {
                activeTabIndex--;
            }
            else if (activeTabIndex < oldIndex && activeTabIndex >= newIndex)
            {
                activeTabIndex++;
            }

            // Rebuild click handlers
            RebuildTabClickHandlers();
        }

        private void RebuildTabClickHandlers()
        {
            for (int i = 0; i < tabElements.Count; i++)
            {
                int tabIndex = i; // Capture for closure
                tabElements[i].OnClick = () => { activeTabIndex = tabIndex; UpdateTabVisibility(); };
            }
        }

        public bool HasTab(string tabName)
        {
            return tabs.Contains(tabName);
        }

        /// <summary>
        /// Gets the bounds of the tab area (for drag-and-drop detection).
        /// </summary>
        public Rectangle? GetTabAreaBounds()
        {
            if (tabsContainer == null)
            {
                System.Console.WriteLine($"MainPanel.GetTabAreaBounds: tabsContainer is null");
                return null;
            }
            
            if (tabs.Count == 0)
            {
                System.Console.WriteLine($"MainPanel.GetTabAreaBounds: No tabs (count=0), returning null");
                return null;
            }
            
            var bounds = tabsContainer.Bounds;
            System.Console.WriteLine($"MainPanel.GetTabAreaBounds: Returning bounds({bounds.X}, {bounds.Y}, {bounds.Width}, {bounds.Height}), tabCount={tabs.Count}");
            return bounds;
        }

        /// <summary>
        /// Removes a tab and returns its instance and content for transfer.
        /// </summary>
        public TabTransferData? RemoveTab(string tabName)
        {
            int index = tabs.IndexOf(tabName);
            if (index == -1)
                return null;

            // Get tab instance and content
            object? tabInstance = null;
            Components.TabContent? tabContent = null;

            switch (tabName)
            {
                case "layout":
                    tabInstance = layoutTab;
                    tabContent = layoutTab?.TabContent;
                    layoutTab = null;
                    break;
                case "corpus":
                    tabInstance = corpusTab;
                    tabContent = corpusTab?.TabContent;
                    corpusTab = null;
                    break;
                case "stats":
                    tabInstance = statsTab;
                    tabContent = statsTab?.TabContent;
                    statsTab = null;
                    break;
                case "settings":
                    tabInstance = settingsTab;
                    tabContent = settingsTab?.TabContent;
                    settingsTab = null;
                    break;
            }

            if (tabInstance == null || tabContent == null)
                return null;

            // Remove from lists
            tabs.RemoveAt(index);
            visibleTabs.Remove(tabName);

            // Remove tab element
            if (index < tabElements.Count)
            {
                var tabElement = tabElements[index];
                if (tabsContainer != null)
                {
                    tabsContainer.RemoveChild(tabElement);
                }
                tabElements.RemoveAt(index);
            }

            // Remove tab content from container
            if (tabContentContainer != null)
            {
                tabContentContainer.RemoveChild(tabContent);
            }

            // Update active tab index
            if (activeTabIndex == index)
            {
                // Switch to first available tab, or -1 if none
                activeTabIndex = tabs.Count > 0 ? 0 : -1;
            }
            else if (activeTabIndex > index)
            {
                activeTabIndex--; // Adjust index if removed tab was before active tab
            }

            // Rebuild click handlers for remaining tabs
            RebuildTabClickHandlers();
            UpdateTabVisibility();

            return new TabTransferData(tabInstance, tabContent);
        }

        /// <summary>
        /// Adds a tab with its instance and content at the specified index.
        /// </summary>
        public void AddTabWithContent(string tabName, object tabInstance, Components.TabContent tabContent, int index)
        {
            // Insert into tabs list
            if (index < 0 || index > tabs.Count)
                index = tabs.Count;
            
            tabs.Insert(index, tabName);
            visibleTabs.Add(tabName);

            // Create tab element
            var tabElement = new Components.Tab(Font, tabName);
            tabElements.Insert(index, tabElement);
            
            if (tabsContainer != null)
            {
                // Insert at correct position in container
                if (index < tabsContainer.Children.Count)
                {
                    tabsContainer.Children.Insert(index, tabElement);
                }
                else
                {
                    tabsContainer.AddChild(tabElement);
                }
            }

            // Store tab instance
            switch (tabName)
            {
                case "layout":
                    layoutTab = tabInstance as LayoutTab;
                    break;
                case "corpus":
                    corpusTab = tabInstance as CorpusTab;
                    break;
                case "stats":
                    statsTab = tabInstance as StatsTab;
                    break;
                case "settings":
                    settingsTab = tabInstance as SettingsTab;
                    break;
            }

            // Add tab content to container
            if (tabContentContainer != null)
            {
                tabContentContainer.AddChild(tabContent);
            }

            // Update active tab index if needed
            if (activeTabIndex == -1)
            {
                activeTabIndex = index;
            }
            else if (activeTabIndex >= index)
            {
                activeTabIndex++; // Shift active index if inserting before it
            }

            // Rebuild click handlers
            RebuildTabClickHandlers();
            UpdateTabVisibility();
        }

        private string? draggedTabName = null;
        private TabTransferManager? tabTransferManager;
        private BottomPanel? bottomPanel;

        /// <summary>
        /// Sets the tab transfer manager for handling cross-panel transfers.
        /// </summary>
        public void SetTabTransferManager(TabTransferManager manager)
        {
            tabTransferManager = manager;
        }

        /// <summary>
        /// Sets the bottom panel reference for cross-panel transfers.
        /// </summary>
        public void SetBottomPanel(BottomPanel panel)
        {
            bottomPanel = panel;
        }

        private void HandleTabDragAndDrop(Rectangle bounds)
        {
            if (tabsContainer == null)
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Check if we're dragging a tab (only one should be dragging at a time)
            Components.Tab? draggedTab = null;
            string? newDraggedTabName = null;
            foreach (var tabElement in tabElements)
            {
                if (tabElement.IsDragging)
                {
                    draggedTab = tabElement;
                    newDraggedTabName = tabElement.TabName;
                    break; // Only one tab should be dragging
                }
            }

            // Update draggedTabName only when a drag starts/stops
            if (newDraggedTabName != draggedTabName)
            {
                draggedTabName = newDraggedTabName;
            }

            // Handle drop
            if (draggedTab != null && draggedTabName != null)
            {
                if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    int currentIndex = tabs.IndexOf(draggedTabName);
                    if (currentIndex == -1)
                    {
                        draggedTabName = null;
                        return;
                    }

                    // Check if dropping over BottomPanel's tab area
                    System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: Checking drop for '{draggedTabName}' at ({mouseX}, {mouseY})");
                    if (tabTransferManager != null && bottomPanel != null)
                    {
                        bool isOverBottom = tabTransferManager.IsOverBottomPanelTabArea(mouseX, mouseY);
                        System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: IsOverBottomPanelTabArea = {isOverBottom}");
                        
                        if (isOverBottom)
                        {
                            var bottomTabAreaBounds = bottomPanel.GetTabAreaBounds();
                            System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: BottomPanel tab area bounds: {bottomTabAreaBounds.HasValue}");
                            
                            if (bottomTabAreaBounds.HasValue)
                            {
                                // When dropping into an empty panel, use index 0
                                int bottomDropIndex;
                                if (bottomPanel.GetTabs().Count == 0)
                                {
                                    bottomDropIndex = 0;
                                    System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: BottomPanel has no tabs, using dropIndex=0");
                                }
                                else
                                {
                                    bottomDropIndex = bottomPanel.CalculateDropIndex(mouseX, bottomTabAreaBounds.Value, -1);
                                    System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: Calculated dropIndex={bottomDropIndex}");
                                }
                                
                                int bottomTabCount = bottomPanel.GetTabs().Count;
                                System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: dropIndex={bottomDropIndex}, bottomTabCount={bottomTabCount}");
                                
                                if (bottomDropIndex >= 0 && bottomDropIndex <= bottomTabCount)
                                {
                                    System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: Calling TransferTab...");
                                    tabTransferManager.TransferTab(draggedTabName, fromMainPanel: true, bottomDropIndex);
                                }
                                else
                                {
                                    System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: Drop index invalid: {bottomDropIndex} (must be 0-{bottomTabCount})");
                                }
                            }
                            draggedTabName = null;
                            return;
                        }
                    }
                    else
                    {
                        if (tabTransferManager == null) System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: tabTransferManager is null");
                        if (bottomPanel == null) System.Console.WriteLine($"MainPanel.HandleTabDragAndDrop: bottomPanel is null");
                    }

                    // Calculate drop position within this panel
                    int dropIndex = CalculateDropIndex(mouseX, bounds, currentIndex);
                    
                    // Only move if drop index is valid and different from current position
                    if (dropIndex >= 0 && dropIndex < tabs.Count && dropIndex != currentIndex)
                    {
                        MoveTab(draggedTabName, dropIndex);
                    }
                    draggedTabName = null;
                }
            }
        }

        public int CalculateDropIndex(int mouseX, Rectangle bounds, int draggedIndex)
        {
            // Allow draggedIndex == -1 for inter-panel transfers
            bool isInterPanelTransfer = (draggedIndex == -1);
            
            if (tabsContainer == null || tabElements.Count == 0)
                return -1;
            
            // Only validate draggedIndex bounds for intra-panel transfers
            if (!isInterPanelTransfer && (draggedIndex < 0 || draggedIndex >= tabs.Count))
                return -1;

            int mouseY = Raylib.GetMouseY();
            if (mouseY < bounds.Y || mouseY > bounds.Y + TabHeight)
                return -1;

            // Tabs use auto-layout, so we need to calculate cumulative positions
            float containerX = tabsContainer.Bounds.X;
            float currentX = containerX;

            // Find which tab position the mouse is over, excluding the dragged tab (if intra-panel)
            for (int i = 0; i < tabElements.Count; i++)
            {
                if (!visibleTabs.Contains(tabs[i]))
                    continue;

                var tabElement = tabElements[i];
                if (tabElement.Bounds.Width <= 0)
                    continue;

                // Calculate absolute position based on cumulative width
                float left = currentX;
                float right = left + tabElement.Bounds.Width;
                
                // Always advance currentX for next tab (add width + spacing)
                currentX += tabElement.Bounds.Width + TabSpacing;

                // Skip the dragged tab for comparison logic (only for intra-panel transfers)
                if (!isInterPanelTransfer && i == draggedIndex)
                    continue;

                // Check if mouse is before this tab
                if (mouseX < left)
                {
                    // Insert before this tab
                    if (isInterPanelTransfer)
                        return i; // For inter-panel, just insert at position i
                    return i < draggedIndex ? i : i - 1;
                }

                // Check if mouse is within this tab - insert AT this tab's position
                if (mouseX < right)
                {
                    // Insert at this tab's position (replacing/pushing it right)
                    // For inter-panel transfers, insert at i (pushing this tab right)
                    if (isInterPanelTransfer)
                        return i;
                    // For intra-panel, always insert at i
                    return i;
                }
            }

            // Mouse is past all tabs - insert at end
            if (isInterPanelTransfer)
            {
                // For inter-panel transfers, insert at the end
                return tabs.Count;
            }
            
            // For intra-panel transfers, find the last visible tab (excluding dragged tab)
            int lastVisibleIndex = -1;
            for (int i = tabElements.Count - 1; i >= 0; i--)
            {
                if (visibleTabs.Contains(tabs[i]) && i != draggedIndex)
                {
                    lastVisibleIndex = i;
                    break;
                }
            }
            
            if (lastVisibleIndex == -1)
                return draggedIndex; // No other visible tabs
            
            return lastVisibleIndex < draggedIndex ? lastVisibleIndex + 1 : lastVisibleIndex;
        }

        /// <summary>
        /// Gets the currently loaded corpus, or null if no corpus is loaded.
        /// </summary>
        public Corpus? GetLoadedCorpus()
        {
            // This method is kept for compatibility, but corpus management is now in CorpusTab
            // We could expose a method on CorpusTab if needed, but for now return null
            return null;
        }

        public bool IsTabVisible(string tabName)
        {
            return visibleTabs.Contains(tabName);
        }

        public void ToggleTab(string tabName)
        {
            if (visibleTabs.Contains(tabName))
            {
                visibleTabs.Remove(tabName);
                // If we're hiding the active tab, switch to first visible tab
                if (tabs[activeTabIndex] == tabName)
                {
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        if (visibleTabs.Contains(tabs[i]))
                        {
                            activeTabIndex = i;
                            break;
                        }
                    }
                }
            }
            else
            {
                visibleTabs.Add(tabName);
            }
            
            UpdateTabVisibility();
        }

        private void UpdateTabVisibility()
        {
            // Update tab content visibility based on active tab and visibility state
            // Note: Tab element visibility is updated separately earlier in Update()
            
            // Get the active tab name if we have a valid active index
            string? activeTabName = (activeTabIndex >= 0 && activeTabIndex < tabs.Count) 
                ? tabs[activeTabIndex] 
                : null;
            
            // Check visibility for each tab based on whether it exists, is visible, and matches active tab
            bool layoutVisible = layoutTab != null && visibleTabs.Contains("layout") && activeTabName == "layout";
            bool corpusVisible = corpusTab != null && visibleTabs.Contains("corpus") && activeTabName == "corpus";
            bool statsVisible = statsTab != null && visibleTabs.Contains("stats") && activeTabName == "stats";
            bool settingsVisible = settingsTab != null && visibleTabs.Contains("settings") && activeTabName == "settings";
            
            layoutTab?.SetVisible(layoutVisible);
            corpusTab?.SetVisible(corpusVisible);
            statsTab?.SetVisible(statsVisible);
            settingsTab?.SetVisible(settingsVisible);
        }

        public void Update(Rectangle bounds)
        {
            // Update panel bounds
            UpdateBounds(bounds);

            // Update tabs container bounds
            if (tabsContainer != null)
            {
                tabsContainer.Bounds = new Rectangle(
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    TabHeight
                );
            }

            // Update tab elements bounds and visibility
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                bool isVisible = visibleTabs.Contains(tabs[i]);
                
                // Calculate tab width
                if (isVisible)
                {
                    int textWidth = (int)FontManager.MeasureText(Font, tabs[i], 14);
                    int tabWidth = TabPadding * 2 + textWidth;
                    tabElement.Bounds = new Rectangle(0, 0, tabWidth, TabHeight);
                }
            }
            
            // Update tab element visibility only (not tab content - that happens after bounds are resolved)
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                bool isVisible = visibleTabs.Contains(tabs[i]);
                tabElement.IsVisible = isVisible;
                tabElement.IsActive = (i == activeTabIndex);
            }

            // Calculate content area
            Rectangle contentArea = new Rectangle(
                bounds.X,
                bounds.Y + TabHeight,
                bounds.Width,
                bounds.Height - TabHeight
            );

            // Set tab content container bounds
            if (tabContentContainer != null)
            {
                tabContentContainer.Bounds = contentArea;
            }

            // Set tab content bounds (external bounds setting)
            bool isLayoutActive = tabs[activeTabIndex] == "layout";
            bool isCorpusActive = tabs[activeTabIndex] == "corpus";
            bool isStatsActive = tabs[activeTabIndex] == "stats";
            bool isSettingsActive = tabs[activeTabIndex] == "settings";

            layoutTab?.Update(contentArea);
            corpusTab?.Update(contentArea, isCorpusActive);
            statsTab?.Update(contentArea);
            settingsTab?.Update(contentArea);
            
            // Temporarily make all tabs visible so ResolveBounds() can process them
            // (ResolveBounds() skips invisible elements, so we need them visible to resolve bounds)
            // We'll set correct visibility after bounds are resolved to prevent flicker
            if (layoutTab?.TabContent != null) layoutTab.TabContent.IsVisible = true;
            if (corpusTab?.TabContent != null) corpusTab.TabContent.IsVisible = true;
            if (statsTab?.TabContent != null) statsTab.TabContent.IsVisible = true;
            if (settingsTab?.TabContent != null) settingsTab.TabContent.IsVisible = true;
            
            // Resolve bounds for tab contents immediately after updating them
            // This prevents flicker when switching tabs - ensures bounds are resolved
            // before the element is drawn, especially important for newly visible tabs
            if (layoutTab?.TabContent != null)
            {
                layoutTab.TabContent.ResolveBounds();
            }
            if (corpusTab?.TabContent != null)
            {
                corpusTab.TabContent.ResolveBounds();
            }
            if (statsTab?.TabContent != null)
            {
                statsTab.TabContent.ResolveBounds();
            }
            if (settingsTab?.TabContent != null)
            {
                settingsTab.TabContent.ResolveBounds();
            }
            
            // Check and update stats only when needed (not every frame)
            // Call this even when stats tab is not active, so stats update when layout changes
            statsTab?.CheckAndUpdateStats();
            
            // After UpdateStats creates new elements, we need to resolve their bounds
            // Resolve bounds for stats tab content so new elements get proper positions
            if (isStatsActive && statsTab != null && statsTab.TabContent != null)
            {
                statsTab.TabContent.ResolveBounds();
            }

            // Phase 1: Resolve all bounds (converts relative to absolute, calculates AutoSize)
            ResolveBounds();

            // Constrain canvas width after bounds resolution (for layout tab)
            layoutTab?.ConstrainCanvasWidth(contentArea);

            // Set the correct visibility AFTER bounds are fully resolved but BEFORE input handling
            // This ensures tabs only become visible when their bounds are already correct
            // and prevents inactive tabs from receiving input events
            UpdateTabVisibility();

            // Handle tab drag and drop for reordering (after bounds are resolved)
            HandleTabDragAndDrop(bounds);

            // Phase 2: Layout and input handling
            // Note: Individual UI elements should check Panel.IsMouseOverBottomPanel() before processing clicks
            // to prevent clicks from passing through to MainPanel when mouse is over BottomPanel
            base.Update();

        }

        protected override void DrawPanelContent(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.MainPanelColor);

            // Draw border on right edge (window edge)
            // Left, top, and bottom edges are handled by splitters and menu bar
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor);

            // Draw tab content area (tabs are drawn by their UI elements)
            Rectangle contentArea = new Rectangle(
                bounds.X,
                bounds.Y + TabHeight,
                bounds.Width,
                bounds.Height - TabHeight
            );

            // Tab content is drawn by their container children
        }



        public override void UpdateFont(Font newFont)
        {
            base.UpdateFont(newFont);
            // Update fonts in tabs
            layoutTab?.UpdateFont(newFont);
            corpusTab?.UpdateFont(newFont);
            settingsTab?.UpdateFont(newFont);
            // Update fonts in tab elements
            foreach (var tabElement in tabElements)
            {
                tabElement.UpdateFont(newFont);
            }
        }

        public void DrawDropdowns()
        {
            // Draw dropdown lists on top of everything
            corpusTab?.CorpusDropdown?.DrawDropdown();
            corpusTab?.NgramSizeDropdown?.DrawDropdown();
            layoutTab?.LayoutsDropdown?.DrawDropdown();
            layoutTab?.LayoutsDropdown2?.DrawDropdown();
        }

        public void DrawHelpScreen()
        {
            // Draw help screen on top of everything
            if (corpusTab?.RegexHelpScreen != null && corpusTab.RegexHelpScreen.IsVisible)
            {
                corpusTab.RegexHelpScreen.Draw();
            }
        }

        public bool IsHoveringTab(Rectangle bounds, int mouseX, int mouseY)
        {
            return mouseY >= bounds.Y && mouseY <= bounds.Y + TabHeight &&
                   mouseX >= bounds.X && mouseX <= bounds.X + bounds.Width;
        }

        public bool IsHoveringInteractiveElement(Rectangle bounds, int mouseX, int mouseY)
        {
            // Check if hovering over buttons or dropdowns in the active tab
            if (tabs[activeTabIndex] == "corpus")
            {
                if (corpusTab?.CorpusDropdown != null && corpusTab.CorpusDropdown.IsHovering(mouseX, mouseY))
                {
                    return true;
                }

                if (corpusTab?.NgramSizeDropdown != null && corpusTab.NgramSizeDropdown.IsHovering(mouseX, mouseY))
                {
                    return true;
                }
            }

            return false;
        }

    }
}


