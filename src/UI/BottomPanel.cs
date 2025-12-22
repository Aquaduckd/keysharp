using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class BottomPanel : Panel
    {
        private const int TabHeight = 35;
        private const int TabPadding = 15;
        private const int TabSpacing = 2;

        private List<string> tabs = new List<string>();
        private HashSet<string> visibleTabs = new HashSet<string>();
        private int activeTabIndex = -1; // -1 means no active tab

        // Tab elements
        private List<Components.Tab> tabElements = new List<Components.Tab>();
        
        // Containers for layout
        private Components.Container? tabsContainer;
        private Components.Container? tabContentContainer;
        
        // Tab instances (using dictionary for flexibility)
        private Dictionary<string, object> tabInstances = new Dictionary<string, object>();

        public string Status { get; set; } = "Ready";
        public string Version { get; set; } = "1.0.0";

        public BottomPanel(Font font) : base(font, "BottomPanel")
        {
            // Create container for tabs
            tabsContainer = new Container("BottomTabsContainer");
            tabsContainer.AutoLayoutChildren = true;
            tabsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            tabsContainer.ChildJustification = Components.ChildJustification.Left;
            tabsContainer.ChildGap = TabSpacing;
            tabsContainer.ChildPadding = 0;
            tabsContainer.AutoSize = false; // Width/height set manually
            AddChild(tabsContainer);
            
            // Create container for tab content
            tabContentContainer = new Components.Container("BottomTabContentContainer");
            tabContentContainer.AutoLayoutChildren = false; // Content positioned manually
            tabContentContainer.AutoSize = false; // Size set manually
            AddChild(tabContentContainer);
        }

        public void Update(Rectangle bounds)
        {
            // Update panel bounds
            UpdateBounds(bounds);

            // Update tabs container bounds (only if we have tabs)
            if (tabsContainer != null && tabs.Count > 0)
            {
                tabsContainer.Bounds = new Rectangle(
                    bounds.X,
                    bounds.Y,
                    bounds.Width,
                    TabHeight
                );
            }
            else if (tabsContainer != null)
            {
                // Hide tabs container if no tabs
                tabsContainer.Bounds = new Rectangle(0, 0, 0, 0);
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
            
            // Update tab element visibility and active state
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                bool isVisible = visibleTabs.Contains(tabs[i]);
                tabElement.IsVisible = isVisible;
                tabElement.IsActive = (i == activeTabIndex);
            }

            // Calculate content area (account for tab bar if tabs exist)
            float contentY = tabs.Count > 0 ? bounds.Y + TabHeight : bounds.Y;
            float contentHeight = tabs.Count > 0 ? bounds.Height - TabHeight : bounds.Height;
            Rectangle contentArea = new Rectangle(
                bounds.X,
                contentY,
                bounds.Width,
                contentHeight
            );

            // Set tab content container bounds
            if (tabContentContainer != null)
            {
                tabContentContainer.Bounds = contentArea;
            }

            // Update tab content visibility and bounds
            UpdateTabContentBounds(contentArea);

            // Phase 1: Resolve all bounds
            ResolveBounds();

            // Call CheckAndUpdateStats on StatsTab if present (similar to MainPanel)
            CheckAndUpdateStatsForActiveTab();

            // Handle tab drag-and-drop (after bounds are resolved)
            HandleTabDragAndDrop(bounds);

            // Phase 2: Layout and input handling
            // Note: The mouse-over-BottomPanel flag is set in RootUI.Update() before panels are updated
            // This ensures MainPanel can check the flag before processing input
            base.Update();
        }

        private void UpdateTabContentBounds(Rectangle contentArea)
        {
            // Update tab content bounds using reflection to call Update methods
            if (activeTabIndex >= 0 && activeTabIndex < tabs.Count)
            {
                string activeTabName = tabs[activeTabIndex];
                if (tabInstances.TryGetValue(activeTabName, out var tabInstance))
                {
                    var type = tabInstance.GetType();
                    
                    // Try Update(Rectangle, bool) first (for CorpusTab)
                    var updateMethodWithBool = type.GetMethod("Update", new Type[] { typeof(Rectangle), typeof(bool) });
                    if (updateMethodWithBool != null)
                    {
                        // For tabs with bool parameter, pass true (they're active if we're updating them)
                        updateMethodWithBool.Invoke(tabInstance, new object[] { contentArea, true });
                        return;
                    }
                    
                    // Fall back to Update(Rectangle) for other tabs
                    var updateMethod = type.GetMethod("Update", new[] { typeof(Rectangle) });
                    if (updateMethod != null)
                    {
                        updateMethod.Invoke(tabInstance, new object[] { contentArea });
                    }
                }
            }
        }

        private void CheckAndUpdateStatsForActiveTab()
        {
            // Call CheckAndUpdateStats on StatsTab if the active tab is a StatsTab
            if (activeTabIndex >= 0 && activeTabIndex < tabs.Count)
            {
                string activeTabName = tabs[activeTabIndex];
                if (tabInstances.TryGetValue(activeTabName, out var tabInstance))
                {
                    var type = tabInstance.GetType();
                    
                    // Check if this tab has a CheckAndUpdateStats method (StatsTab)
                    var checkAndUpdateMethod = type.GetMethod("CheckAndUpdateStats", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance, null, new Type[0], null);
                    if (checkAndUpdateMethod != null)
                    {
                        checkAndUpdateMethod.Invoke(tabInstance, null);
                        
                        // After updating stats, resolve bounds for the tab content (similar to MainPanel)
                        var tabContentProperty = type.GetProperty("TabContent");
                        if (tabContentProperty != null)
                        {
                            var tabContent = tabContentProperty.GetValue(tabInstance) as Components.TabContent;
                            tabContent?.ResolveBounds();
                        }
                    }
                }
            }
        }

        private void UpdateTabVisibility()
        {
            // Update tab content visibility based on active tab
            foreach (var kvp in tabInstances)
            {
                string tabName = kvp.Key;
                object tabInstance = kvp.Value;
                bool isActive = tabs.IndexOf(tabName) == activeTabIndex && visibleTabs.Contains(tabName);

                // Try to call SetVisible method on the tab instance
                var setVisibleMethod = tabInstance.GetType().GetMethod("SetVisible", new[] { typeof(bool) });
                if (setVisibleMethod != null)
                {
                    setVisibleMethod.Invoke(tabInstance, new object[] { isActive });
                }
            }
        }

        // Tab management methods
        public void AddTab(string tabName, object tabInstance)
        {
            if (tabs.Contains(tabName))
                return; // Tab already exists

            tabs.Add(tabName);
            visibleTabs.Add(tabName);
            tabInstances[tabName] = tabInstance;

            // Create tab element
            var tabElement = new Components.Tab(Font, tabName);
            int tabIndex = tabs.Count - 1;
            tabElement.OnClick = () => { 
                activeTabIndex = tabIndex; 
                UpdateTabVisibility(); 
            };
            tabElements.Add(tabElement);
            
            if (tabsContainer != null)
            {
                tabsContainer.AddChild(tabElement);
            }

            // Set as active if it's the first tab
            if (activeTabIndex == -1)
            {
                activeTabIndex = 0;
            }
        }

        /// <summary>
        /// Removes a tab (does not return transfer data). Use RemoveTab(string) that returns TabTransferData for transfers.
        /// </summary>
        public void RemoveTabInternal(string tabName)
        {
            var transferData = RemoveTab(tabName);
            // This method exists for backward compatibility but should not be used for transfers
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
            if (!tabInstances.TryGetValue(tabName, out var tabInstance))
                return null;

            // Try to get TabContent using reflection
            var tabContentProperty = tabInstance.GetType().GetProperty("TabContent");
            if (tabContentProperty == null)
                return null;

            var tabContent = tabContentProperty.GetValue(tabInstance) as Components.TabContent;
            if (tabContent == null)
                return null;

            // Remove from lists and dictionary
            tabs.RemoveAt(index);
            visibleTabs.Remove(tabName);
            tabInstances.Remove(tabName);

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
                tabElements[i].OnClick = () => { 
                    activeTabIndex = tabIndex; 
                    UpdateTabVisibility(); 
                };
            }
        }

        public bool HasTab(string tabName)
        {
            return tabs.Contains(tabName);
        }

        public List<string> GetTabs()
        {
            return new List<string>(tabs);
        }

        /// <summary>
        /// Gets the bounds of the tab area (for drag-and-drop detection).
        /// Returns the tab area bounds even if there are no tabs (for drop detection).
        /// </summary>
        public Rectangle? GetTabAreaBounds()
        {
            if (tabsContainer == null)
            {
                System.Console.WriteLine($"BottomPanel.GetTabAreaBounds: tabsContainer is null");
                return null;
            }
            
            // If we have tabs, return the actual container bounds
            if (tabs.Count > 0)
            {
                var bounds = tabsContainer.Bounds;
                System.Console.WriteLine($"BottomPanel.GetTabAreaBounds: Returning actual bounds({bounds.X}, {bounds.Y}, {bounds.Width}, {bounds.Height}), tabCount={tabs.Count}");
                return bounds;
            }
            
            // If no tabs, return where the tab area would be (top of panel, TabHeight high)
            // This allows dropping tabs into an empty bottom panel
            var panelBounds = Bounds;
            var tabAreaBounds = new Rectangle(panelBounds.X, panelBounds.Y, panelBounds.Width, TabHeight);
            System.Console.WriteLine($"BottomPanel.GetTabAreaBounds: No tabs, returning calculated tab area bounds({tabAreaBounds.X}, {tabAreaBounds.Y}, {tabAreaBounds.Width}, {tabAreaBounds.Height})");
            return tabAreaBounds;
        }

        /// <summary>
        /// Calculates the drop index for a tab being dragged over the tab area.
        /// </summary>
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
        /// Adds a tab with its instance and content at the specified index.
        /// </summary>
        public void AddTabWithContent(string tabName, object tabInstance, Components.TabContent tabContent, int index)
        {
            // Insert into tabs list
            if (index < 0 || index > tabs.Count)
                index = tabs.Count;
            
            tabs.Insert(index, tabName);
            visibleTabs.Add(tabName);
            tabInstances[tabName] = tabInstance;

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

        /// <summary>
        /// Sets the tab transfer manager for handling cross-panel transfers.
        /// </summary>
        public void SetTabTransferManager(TabTransferManager manager)
        {
            tabTransferManager = manager;
        }

        /// <summary>
        /// Handles drag-and-drop for tabs within this panel and cross-panel transfers.
        /// </summary>
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

                    // Check if dropping over MainPanel's tab area
                    System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: Checking drop for '{draggedTabName}' at ({mouseX}, {mouseY})");
                    if (tabTransferManager != null)
                    {
                        bool isOverMain = tabTransferManager.IsOverMainPanelTabArea(mouseX, mouseY);
                        System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: IsOverMainPanelTabArea = {isOverMain}");
                        
                        if (isOverMain && mainPanel != null)
                        {
                            var mainTabAreaBounds = mainPanel.GetTabAreaBounds();
                            System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: MainPanel tab area bounds: {mainTabAreaBounds.HasValue}");
                            
                            if (mainTabAreaBounds.HasValue)
                            {
                                // When dropping into an empty panel, use index 0
                                int dropIndex;
                                if (mainPanel.GetTabs().Count == 0)
                                {
                                    dropIndex = 0;
                                    System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: MainPanel has no tabs, using dropIndex=0");
                                }
                                else
                                {
                                    dropIndex = mainPanel.CalculateDropIndex(mouseX, mainTabAreaBounds.Value, -1);
                                    System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: Calculated dropIndex={dropIndex}");
                                }
                                
                                int mainTabCount = mainPanel.GetTabs().Count;
                                System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: dropIndex={dropIndex}, mainTabCount={mainTabCount}");
                                
                                if (dropIndex >= 0 && dropIndex <= mainTabCount)
                                {
                                    System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: Calling TransferTab...");
                                    tabTransferManager.TransferTab(draggedTabName, fromMainPanel: false, dropIndex);
                                }
                                else
                                {
                                    System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: Drop index invalid: {dropIndex} (must be 0-{mainTabCount})");
                                }
                            }
                            draggedTabName = null;
                            return;
                        }
                    }
                    else
                    {
                        System.Console.WriteLine($"BottomPanel.HandleTabDragAndDrop: tabTransferManager is null");
                    }

                    // Calculate drop position within this panel
                    var tabAreaBounds = GetTabAreaBounds();
                    if (tabAreaBounds.HasValue)
                    {
                        int dropIndex = CalculateDropIndex(mouseX, tabAreaBounds.Value, currentIndex);
                        
                        // Only move if drop index is valid and different from current position
                        if (dropIndex >= 0 && dropIndex < tabs.Count && dropIndex != currentIndex)
                        {
                            MoveTab(draggedTabName, dropIndex);
                        }
                    }
                    draggedTabName = null;
                }
            }
        }

        // Reference to MainPanel for cross-panel transfers (set by TabTransferManager or RootUI)
        private MainPanel? mainPanel;
        public void SetMainPanel(MainPanel panel)
        {
            mainPanel = panel;
        }

        /// <summary>
        /// Draws dropdowns from all tab instances on top of everything.
        /// Uses reflection to find dropdown properties in tab instances.
        /// </summary>
        public void DrawDropdowns()
        {
            // Iterate through all tab instances and draw their dropdowns
            foreach (var kvp in tabInstances)
            {
                object tabInstance = kvp.Value;
                var type = tabInstance.GetType();
                
                // Find all properties that are Dropdown types
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    // Check if property type is Dropdown (handle both nullable and non-nullable)
                    var propType = prop.PropertyType;
                    bool isDropdown = propType == typeof(Components.Dropdown) || 
                                     (propType.IsGenericType && 
                                      propType.GetGenericTypeDefinition() == typeof(System.Nullable<>) && 
                                      propType.GetGenericArguments()[0] == typeof(Components.Dropdown));
                    
                    if (isDropdown)
                    {
                        var dropdown = prop.GetValue(tabInstance) as Components.Dropdown;
                        dropdown?.DrawDropdown();
                    }
                }
            }
        }

        /// <summary>
        /// Draws help screens from all tab instances on top of everything.
        /// Uses reflection to find help screen properties in tab instances.
        /// </summary>
        public void DrawHelpScreen()
        {
            // Iterate through all tab instances and draw their help screens
            foreach (var kvp in tabInstances)
            {
                object tabInstance = kvp.Value;
                var type = tabInstance.GetType();
                
                // Find properties that might be help screens (e.g., RegexHelpScreen)
                var properties = type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var prop in properties)
                {
                    var propType = prop.PropertyType;
                    
                    // Check if property name contains "HelpScreen" and has IsVisible and Draw methods
                    if (prop.Name.Contains("HelpScreen"))
                    {
                        var helpScreen = prop.GetValue(tabInstance);
                        if (helpScreen != null)
                        {
                            // Check if it has an IsVisible property
                            var isVisibleProp = propType.GetProperty("IsVisible");
                            if (isVisibleProp != null)
                            {
                                var isVisible = (bool)isVisibleProp.GetValue(helpScreen);
                                if (isVisible)
                                {
                                    // Check if it has a Draw method
                                    var drawMethod = propType.GetMethod("Draw", new Type[0]);
                                    if (drawMethod != null)
                                    {
                                        drawMethod.Invoke(helpScreen, null);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        protected override void DrawPanelContent(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.BottomPanelColor);
            
            // Draw borders only on outer edges (left, bottom, right)
            // Top edge is handled by the horizontal splitter
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Left
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Bottom
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Right
        }

        public override void UpdateFont(Font newFont)
        {
            base.UpdateFont(newFont);
            // Update fonts in tab elements
            foreach (var tabElement in tabElements)
            {
                tabElement.UpdateFont(newFont);
            }
        }
    }
}

