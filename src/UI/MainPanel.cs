using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.UI;
using System.IO;
using System.Diagnostics;

namespace Keysharp.UI
{
    public class MainPanel : Panel
    {
        private const int TabHeight = 35;
        private const int TabPadding = 15;
        private const int TabSpacing = 2;

        private List<string> tabs = new List<string> { "layout", "corpus", "settings" };
        private HashSet<string> visibleTabs = new HashSet<string> { "layout", "corpus", "settings" };
        private int activeTabIndex = 0;

        // Corpus tab state
        private Components.Button? loadCorpusButton;
        private Components.Dropdown? corpusDropdown;
        private Components.InfoText? infoText;
        private string? loadedCorpusPath = null;
        private Components.Container? corpusControlsContainer;
        private Components.Container? corpusRowContainer;
        private Components.Container? infoTextContainer;

        // Tab elements
        private List<Components.Tab> tabElements = new List<Components.Tab>();
        
        // Containers for layout
        private Components.Container? tabsContainer;
        private Components.Container? tabContentContainer;
        
        // Tab content containers
        private Components.TabContent? layoutTabContent;
        private Components.TabContent? corpusTabContent;
        private Components.TabContent? settingsTabContent;

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

            // Create tab content containers
            layoutTabContent = new Components.TabContent(font, "Layout", "Layout configuration will appear here.");
            layoutTabContent.IsVisible = (activeTabIndex == 0);
            layoutTabContent.PositionMode = Components.PositionMode.Relative;
            tabContentContainer.AddChild(layoutTabContent);

            corpusTabContent = new Components.TabContent(font, "Corpus", null);
            corpusTabContent.IsVisible = (activeTabIndex == 1);
            corpusTabContent.PositionMode = Components.PositionMode.Relative;
            tabContentContainer.AddChild(corpusTabContent);

            settingsTabContent = new Components.TabContent(font, "Settings", "Application settings and preferences");
            settingsTabContent.IsVisible = (activeTabIndex == 2);
            settingsTabContent.PositionMode = Components.PositionMode.Relative;
            tabContentContainer.AddChild(settingsTabContent);

            // Create a container for the entire corpus controls row (button, dropdown, and info text)
            corpusRowContainer = new Components.Container("CorpusRow");
            corpusRowContainer.AutoLayoutChildren = true;
            corpusRowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            corpusRowContainer.ChildJustification = Components.ChildJustification.SpaceBetween;
            corpusRowContainer.ChildGap = 0; // Gap will be calculated by SpaceBetween
            corpusRowContainer.ChildPadding = 0;
            corpusTabContent.AddChild(corpusRowContainer);

            // Create container for left-aligned controls (button and dropdown)
            corpusControlsContainer = new Components.Container("CorpusControls");
            corpusControlsContainer.AutoLayoutChildren = true;
            corpusControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            corpusControlsContainer.ChildJustification = Components.ChildJustification.Left;
            corpusControlsContainer.ChildGap = 10;
            corpusControlsContainer.ChildPadding = 20;
            corpusControlsContainer.Bounds = new Rectangle(0, 0, 0, 35); // Width will be calculated by layout
            corpusRowContainer.AddChild(corpusControlsContainer);

            // Initialize corpus button
            loadCorpusButton = new Components.Button(font, "Load Corpus", 14);
            loadCorpusButton.Bounds = new Rectangle(0, 0, 150, 35); // Set initial size
            loadCorpusButton.PositionMode = Components.PositionMode.Absolute; // Will be positioned by container's auto-layout
            loadCorpusButton.OnClick = LoadCorpusFromFile;
            corpusControlsContainer.AddChild(loadCorpusButton);

            // Initialize corpus dropdown
            List<string> corpusFiles = GetCorpusFiles();
            corpusDropdown = new Components.Dropdown(font, corpusFiles, 14);
            corpusDropdown.SetBounds(new Rectangle(0, 0, 250, 35)); // Set initial size
            corpusDropdown.PositionMode = Components.PositionMode.Absolute; // Will be positioned by container's auto-layout
            corpusDropdown.OnSelectionChanged = OnCorpusSelected;
            corpusControlsContainer.AddChild(corpusDropdown);

            // Create container for right-aligned info text
            infoTextContainer = new Components.Container("InfoTextContainer");
            infoTextContainer.AutoLayoutChildren = true;
            infoTextContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            infoTextContainer.ChildJustification = Components.ChildJustification.Right;
            infoTextContainer.ChildGap = 0;
            infoTextContainer.ChildPadding = 20; // Right padding
            infoTextContainer.Bounds = new Rectangle(0, 0, 0, 35); // Width will be calculated by layout
            corpusRowContainer.AddChild(infoTextContainer);

            // Initialize info text
            infoText = new Components.InfoText(font, "", 14);
            infoText.Bounds = new Rectangle(0, 0, 0, 35); // Width will be set based on content
            infoTextContainer.AddChild(infoText);
        }

        public List<string> GetTabs()
        {
            return new List<string>(tabs);
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
            // Update tab elements visibility
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                bool isVisible = visibleTabs.Contains(tabs[i]);
                tabElement.IsVisible = isVisible;
                tabElement.IsActive = (i == activeTabIndex);
            }
            
            // Update tab content visibility based on active tab and visibility state
            if (layoutTabContent != null)
            {
                layoutTabContent.IsVisible = (activeTabIndex == 0 && visibleTabs.Contains("layout"));
            }
            if (corpusTabContent != null)
            {
                bool isVisible = (activeTabIndex == 1 && visibleTabs.Contains("corpus"));
                corpusTabContent.IsVisible = isVisible;
                // Also update corpus controls visibility
                if (corpusControlsContainer != null)
                {
                    corpusControlsContainer.IsVisible = isVisible;
                }
            }
            if (settingsTabContent != null)
            {
                settingsTabContent.IsVisible = (activeTabIndex == 2 && visibleTabs.Contains("settings"));
            }
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
            
            // Update tab visibility (this will update both tab elements and tab content)
            UpdateTabVisibility();

            // Update tab content container bounds
            Rectangle contentArea = new Rectangle(
                bounds.X,
                bounds.Y + TabHeight,
                bounds.Width,
                bounds.Height - TabHeight
            );

            if (tabContentContainer != null)
            {
                tabContentContainer.Bounds = contentArea;
            }

            if (layoutTabContent != null && tabContentContainer != null)
            {
                layoutTabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
                layoutTabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
            }
            if (corpusTabContent != null && tabContentContainer != null)
            {
                corpusTabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
                corpusTabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
                
                // Update corpus row container if corpus tab is active
                if (tabs[activeTabIndex] == "corpus" && corpusRowContainer != null)
                {
                    // Position container on the same line as the title
                    const int lineY = 60;
                    const int elementHeight = 35;
                    
                    // Update row container bounds (relative to corpusTabContent)
                    corpusRowContainer.Bounds = new Rectangle(
                        0, // Relative to corpusTabContent
                        lineY,
                        contentArea.Width,
                        elementHeight
                    );
                    corpusRowContainer.IsVisible = true;

                    // Update controls container bounds (will be left-aligned by parent)
                    if (corpusControlsContainer != null)
                    {
                        // Width will be calculated by auto-layout based on children
                        corpusControlsContainer.Bounds = new Rectangle(
                            0,
                            0,
                            0, // Width calculated by layout
                            elementHeight
                        );
                        corpusControlsContainer.IsVisible = true;
                    }

                    // Update info text container bounds (will be right-aligned by parent)
                    if (infoTextContainer != null)
                    {
                        // Calculate width needed for info text if there's content
                        float infoTextWidth = 0;
                        if (infoText != null && !string.IsNullOrEmpty(loadedCorpusPath))
                        {
                            string fileName = Path.GetFileName(loadedCorpusPath);
                            string infoTextContent = $"Loaded: {fileName}";
                            float textWidth = FontManager.MeasureText(Font, infoTextContent, 14);
                            infoTextWidth = textWidth + 40; // Add some padding
                            infoText.SetText(infoTextContent);
                            infoText.Bounds = new Rectangle(0, 0, infoTextWidth, elementHeight);
                            infoText.IsVisible = true;
                        }
                        else if (infoText != null)
                        {
                            infoText.SetText("");
                            infoText.Bounds = new Rectangle(0, 0, 0, 0);
                            infoText.IsVisible = false;
                        }
                        
                        infoTextContainer.Bounds = new Rectangle(
                            0,
                            0,
                            infoTextWidth, // Width based on content
                            elementHeight
                        );
                        infoTextContainer.IsVisible = !string.IsNullOrEmpty(loadedCorpusPath);
                    }
                }
                else
                {
                    // Hide row container when not in corpus tab
                    if (corpusRowContainer != null)
                    {
                        corpusRowContainer.IsVisible = false;
                    }
                }
            }
            else
            {
                // Hide row container when corpus tab content is not visible
                if (corpusRowContainer != null)
                {
                    corpusRowContainer.IsVisible = false;
                }
            }
            if (settingsTabContent != null)
            {
                settingsTabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
                settingsTabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
            }
            
            // Update tab visibility
            UpdateTabVisibility();

            // Recursively update all children
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



        public void DrawDropdowns()
        {
            // Draw dropdown lists on top of everything
            if (corpusDropdown != null)
            {
                corpusDropdown.DrawDropdown();
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
                Rectangle contentArea = new Rectangle(
                    bounds.X,
                    bounds.Y + TabHeight,
                    bounds.Width,
                    bounds.Height - TabHeight
                );

                if (loadCorpusButton != null && loadCorpusButton.IsHovering(mouseX, mouseY))
                {
                    return true;
                }

                if (corpusDropdown != null && corpusDropdown.IsHovering(mouseX, mouseY))
                {
                    return true;
                }
            }

            return false;
        }

        private List<string> GetCorpusFiles()
        {
            List<string> files = new List<string>();
            string corpusDir = Path.Combine(Directory.GetCurrentDirectory(), "corpus");
            
            if (Directory.Exists(corpusDir))
            {
                var txtFiles = Directory.GetFiles(corpusDir, "*.txt")
                    .Select(f => Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToList();
                files.AddRange(txtFiles);
            }

            return files;
        }

        private void OnCorpusSelected(string corpusFile)
        {
            string corpusDir = Path.Combine(Directory.GetCurrentDirectory(), "corpus");
            string fullPath = Path.Combine(corpusDir, corpusFile);
            
            if (File.Exists(fullPath))
            {
                loadedCorpusPath = fullPath;
                // Clear custom display text when selecting from dropdown
                if (corpusDropdown != null)
                {
                    corpusDropdown.SetCustomDisplayText(null);
                }
                System.Console.WriteLine($"Corpus selected: {loadedCorpusPath}");
            }
        }

        private void LoadCorpusFromFile()
        {
            try
            {
                // Use zenity for file selection on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Select Corpus File\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (Process? process = Process.Start(startInfo))
                {
                    if (process != null)
                    {
                        string? output = process.StandardOutput.ReadLine()?.Trim();
                        process.WaitForExit();

                        if (process.ExitCode == 0 && !string.IsNullOrEmpty(output))
                        {
                            string filePath = output;
                            if (File.Exists(filePath))
                            {
                                loadedCorpusPath = filePath;
                                // Set dropdown to show truncated file path
                                if (corpusDropdown != null)
                                {
                                    string truncatedPath = TruncatePath(filePath, 40); // Max 40 characters
                                    corpusDropdown.SetCustomDisplayText(truncatedPath);
                                }
                                System.Console.WriteLine($"Corpus loaded from: {loadedCorpusPath}");
                            }
                            else
                            {
                                System.Console.WriteLine($"File not found: {filePath}");
                            }
                        }
                        else
                        {
                            // User cancelled or zenity not available
                            System.Console.WriteLine("File selection cancelled or zenity not available");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error opening file dialog: {ex.Message}");
                // Fallback: try using a simple text input or other method
            }
        }

        private string TruncatePath(string path, int maxLength)
        {
            if (path.Length <= maxLength)
            {
                return path;
            }

            // Try to show the end of the path (filename) with ellipsis
            string fileName = Path.GetFileName(path);
            string directory = Path.GetDirectoryName(path) ?? "";
            
            // If filename alone is too long, truncate it
            if (fileName.Length > maxLength - 3)
            {
                return "..." + fileName.Substring(fileName.Length - (maxLength - 3));
            }

            // Try to show directory + filename, truncating directory if needed
            int availableForDir = maxLength - fileName.Length - 3; // 3 for "..."
            if (availableForDir > 0 && directory.Length > availableForDir)
            {
                return "..." + directory.Substring(directory.Length - availableForDir) + Path.DirectorySeparatorChar + fileName;
            }

            // Fallback: just show end of path
            return "..." + path.Substring(path.Length - (maxLength - 3));
        }
    }
}


