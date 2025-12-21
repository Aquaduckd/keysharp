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

            // Phase 2: Layout and input handling
            base.Update();
        }

        private void UpdateTabContentBounds(Rectangle contentArea)
        {
            // For now, just update visibility - actual content updates will be handled by tab instances
            // This will be expanded when we implement tab transfer
        }

        private void UpdateTabVisibility()
        {
            // Update tab content visibility based on active tab
            // This will be expanded when we implement tab instances
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

        public void RemoveTab(string tabName)
        {
            int index = tabs.IndexOf(tabName);
            if (index == -1)
                return;

            // Remove from lists
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

