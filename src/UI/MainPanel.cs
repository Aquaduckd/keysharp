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
            bool layoutVisible = activeTabIndex == 0 && visibleTabs.Contains("layout");
            bool corpusVisible = activeTabIndex == 1 && visibleTabs.Contains("corpus");
            bool statsVisible = activeTabIndex == 2 && visibleTabs.Contains("stats");
            bool settingsVisible = activeTabIndex == 3 && visibleTabs.Contains("settings");
            
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

            // Phase 2: Layout and input handling
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


