using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.UI;
using System.IO;
using System.Diagnostics;

namespace Keysharp.Panels
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
        private Button? loadCorpusButton;
        private Dropdown? corpusDropdown;
        private InfoText? infoText;
        private string? loadedCorpusPath = null;

        // Tab elements
        private List<Tab> tabElements = new List<Tab>();

        public MainPanel(Font font) : base(font, "MainPanel")
        {
            // All tabs visible by default
            foreach (var tab in tabs)
            {
                visibleTabs.Add(tab);
            }

            // Create tab elements
            for (int i = 0; i < tabs.Count; i++)
            {
                var tabElement = new Tab(font, tabs[i]);
                int tabIndex = i; // Capture for closure
                tabElement.OnClick = () => { activeTabIndex = tabIndex; };
                tabElements.Add(tabElement);
                AddChild(tabElement);
            }

            // Initialize corpus button
            loadCorpusButton = new Button(font, "Load Corpus", 14);
            loadCorpusButton.OnClick = LoadCorpusFromFile;
            AddChild(loadCorpusButton);

            // Initialize corpus dropdown
            List<string> corpusFiles = GetCorpusFiles();
            corpusDropdown = new Dropdown(font, corpusFiles, 14);
            corpusDropdown.OnSelectionChanged = OnCorpusSelected;
            AddChild(corpusDropdown);

            // Initialize info text
            infoText = new InfoText(font, "", 14);
            AddChild(infoText);
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
        }

        public void Update(Rectangle bounds)
        {
            // Update panel bounds
            UpdateBounds(bounds);

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Update tab elements
            int currentX = (int)bounds.X;
            int tabY = (int)bounds.Y;
            for (int i = 0; i < tabElements.Count; i++)
            {
                var tabElement = tabElements[i];
                bool isVisible = visibleTabs.Contains(tabs[i]);
                
                if (isVisible)
                {
                    int textWidth = (int)FontManager.MeasureText(Font, tabs[i], 14);
                    int tabWidth = TabPadding * 2 + textWidth;
                    
                    tabElement.Bounds = new Rectangle(currentX, tabY, tabWidth, TabHeight);
                    tabElement.IsActive = (i == activeTabIndex);
                    
                    currentX += tabWidth + TabSpacing;
                }
                else
                {
                    // Hide invisible tabs
                    tabElement.Bounds = new Rectangle(-1000, -1000, 0, 0);
                }
            }

            // Update corpus button and dropdown if corpus tab is active
            if (tabs[activeTabIndex] == "corpus")
            {
                Rectangle contentArea = new Rectangle(
                    bounds.X,
                    bounds.Y + TabHeight,
                    bounds.Width,
                    bounds.Height - TabHeight
                );
                
                // Position button and dropdown on the same line
                const int lineY = 60;
                const int elementHeight = 35;
                const int spacing = 10; // Space between button and dropdown
                
                if (loadCorpusButton != null)
                {
                    loadCorpusButton.Bounds = new Rectangle(
                        (int)contentArea.X + 20,
                        (int)contentArea.Y + lineY,
                        150,
                        elementHeight
                    );
                }

                if (corpusDropdown != null)
                {
                    int dropdownX = loadCorpusButton != null 
                        ? (int)(loadCorpusButton.Bounds.X + loadCorpusButton.Bounds.Width + spacing)
                        : (int)contentArea.X + 20;
                    corpusDropdown.SetBounds(new Rectangle(
                        dropdownX,
                        (int)contentArea.Y + lineY,
                        250,
                        elementHeight
                    ));
                }

                // Update info text
                if (infoText != null)
                {
                    if (!string.IsNullOrEmpty(loadedCorpusPath))
                    {
                        string fileName = Path.GetFileName(loadedCorpusPath);
                        infoText.SetText($"Loaded: {fileName}");
                        infoText.Bounds = new Rectangle(
                            contentArea.X,
                            contentArea.Y + lineY,
                            contentArea.Width - 20,
                            elementHeight
                        );
                    }
                    else
                    {
                        infoText.SetText("");
                        infoText.Bounds = new Rectangle(0, 0, 0, 0);
                    }
                }
            }
            else
            {
                // Hide button, dropdown, and info text when not in corpus tab
                if (loadCorpusButton != null)
                {
                    loadCorpusButton.Bounds = new Rectangle(-1000, -1000, 0, 0);
                }
                if (corpusDropdown != null)
                {
                    corpusDropdown.SetBounds(new Rectangle(-1000, -1000, 0, 0));
                }
                if (infoText != null)
                {
                    infoText.Bounds = new Rectangle(0, 0, 0, 0);
                }
            }

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

            DrawTabContent(contentArea, activeTabIndex);
        }


        private void DrawTabContent(Rectangle contentArea, int tabIndex)
        {
            // Make sure we have a valid visible tab
            if (tabIndex < 0 || tabIndex >= tabs.Count || !visibleTabs.Contains(tabs[tabIndex]))
            {
                // Find first visible tab
                for (int i = 0; i < tabs.Count; i++)
                {
                    if (visibleTabs.Contains(tabs[i]))
                    {
                        activeTabIndex = i;
                        tabIndex = i;
                        break;
                    }
                }
            }

            string tabName = tabs[tabIndex];
            int contentX = (int)contentArea.X + 20;
            int contentY = (int)contentArea.Y + 20;

            switch (tabName)
            {
                case "layout":
                    FontManager.DrawText(Font, "Layout", contentX, contentY, 24, UITheme.TextColor);
                    FontManager.DrawText(Font, "Layout configuration will appear here.", 
                        contentX, contentY + 40, 16, UITheme.TextSecondaryColor);
                    break;

                case "corpus":
                    DrawCorpusTab(contentArea, contentX, contentY);
                    break;

                case "settings":
                    FontManager.DrawText(Font, "Settings", contentX, contentY, 24, UITheme.TextColor);
                    FontManager.DrawText(Font, "Application settings and preferences", 
                        contentX, contentY + 40, 16, UITheme.TextSecondaryColor);
                    break;
            }
        }

        private void DrawCorpusTab(Rectangle contentArea, int contentX, int contentY)
        {
            FontManager.DrawText(Font, "Corpus", contentX, contentY, 24, UITheme.TextColor);
            
            // Button, dropdown, and info text are drawn as children via base.Draw()
            // No need to draw them manually here
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

