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
        private string? loadedCorpusPath = null;

        public MainPanel(Font font) : base(font)
        {
            // All tabs visible by default
            foreach (var tab in tabs)
            {
                visibleTabs.Add(tab);
            }

            // Initialize corpus button
            loadCorpusButton = new Button(font, "Load Corpus", 14);
            loadCorpusButton.OnClick = LoadCorpusFromFile;

            // Initialize corpus dropdown
            List<string> corpusFiles = GetCorpusFiles();
            corpusDropdown = new Dropdown(font, corpusFiles, 14);
            corpusDropdown.OnSelectionChanged = OnCorpusSelected;
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
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Update corpus button and dropdown if corpus tab is active
            if (tabs[activeTabIndex] == "corpus")
            {
                Rectangle contentArea = new Rectangle(
                    bounds.X,
                    bounds.Y + TabHeight,
                    bounds.Width,
                    bounds.Height - TabHeight
                );
                
                if (loadCorpusButton != null)
                {
                    loadCorpusButton.Bounds = new Rectangle(
                        (int)contentArea.X + 20,
                        (int)contentArea.Y + 60,
                        150,
                        35
                    );
                    loadCorpusButton.Update();
                }

                if (corpusDropdown != null)
                {
                    corpusDropdown.SetBounds(new Rectangle(
                        (int)contentArea.X + 20,
                        (int)contentArea.Y + 110,
                        250,
                        30
                    ));
                    corpusDropdown.Update();
                }
            }

            // Note: Cursor is set centrally in Program.cs based on hover state

            // Handle tab clicks
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                // Check if click is in tab area
                if (mouseY >= bounds.Y && mouseY <= bounds.Y + TabHeight)
                {
                    int currentX = (int)bounds.X;
                    for (int i = 0; i < tabs.Count; i++)
                    {
                        // Skip invisible tabs
                        if (!visibleTabs.Contains(tabs[i]))
                            continue;

                        // Estimate tab width (text width + padding)
                        int tabWidth = TabPadding * 2 + (int)FontManager.MeasureText(Font, tabs[i], 14);
                        
                        if (mouseX >= currentX && mouseX <= currentX + tabWidth)
                        {
                            activeTabIndex = i;
                            break;
                        }
                        
                        currentX += tabWidth + TabSpacing;
                    }
                }
            }
        }

        public override void Draw(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.MainPanelColor);

            // Draw border on right edge (window edge)
            // Left, top, and bottom edges are handled by splitters and menu bar
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor);

            // Draw tabs
            DrawTabs(bounds);

            // Draw tab content area
            Rectangle contentArea = new Rectangle(
                bounds.X,
                bounds.Y + TabHeight,
                bounds.Width,
                bounds.Height - TabHeight
            );

            DrawTabContent(contentArea, activeTabIndex);
        }

        private void DrawTabs(Rectangle bounds)
        {
            int currentX = (int)bounds.X;
            int tabY = (int)bounds.Y;

            for (int i = 0; i < tabs.Count; i++)
            {
                // Skip invisible tabs
                if (!visibleTabs.Contains(tabs[i]))
                    continue;

                bool isActive = i == activeTabIndex;
                
                // Calculate tab width
                int textWidth = (int)FontManager.MeasureText(Font, tabs[i], 14);
                int tabWidth = TabPadding * 2 + textWidth;

                // Tab background
                Color tabColor = isActive ? UITheme.MainPanelColor : UITheme.SidePanelColor;
                Rectangle tabRect = new Rectangle(currentX, tabY, tabWidth, TabHeight);
                Raylib.DrawRectangleRec(tabRect, tabColor);

                // Tab border (only bottom border for active, all borders for inactive)
                if (isActive)
                {
                    // Draw bottom border to separate from content
                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(currentX, tabY + TabHeight),
                        new System.Numerics.Vector2(currentX + tabWidth, tabY + TabHeight),
                        1,
                        UITheme.BorderColor
                    );
                }
                else
                {
                    // Draw right border
                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(currentX + tabWidth, tabY),
                        new System.Numerics.Vector2(currentX + tabWidth, tabY + TabHeight),
                        1,
                        UITheme.BorderColor
                    );
                }

                // Tab text
                Color textColor = isActive ? UITheme.TextColor : UITheme.TextSecondaryColor;
                FontManager.DrawText(Font, tabs[i], currentX + TabPadding, tabY + 10, 14, textColor);

                currentX += tabWidth + TabSpacing;
            }
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
            
            // Draw corpus dropdown button (dropdown list drawn separately)
            if (corpusDropdown != null)
            {
                corpusDropdown.DrawButton();
            }

            // Draw load button
            if (loadCorpusButton != null)
            {
                loadCorpusButton.Draw();
            }

            // Show loaded corpus info
            if (!string.IsNullOrEmpty(loadedCorpusPath))
            {
                string fileName = Path.GetFileName(loadedCorpusPath);
                FontManager.DrawText(Font, $"Loaded: {fileName}", 
                    contentX, contentY + 160, 14, UITheme.TextSecondaryColor);
                FontManager.DrawText(Font, loadedCorpusPath, 
                    contentX, contentY + 185, 12, UITheme.TextSecondaryColor);
            }
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
                                // Set dropdown to show "custom"
                                if (corpusDropdown != null)
                                {
                                    corpusDropdown.SetCustomDisplayText("custom");
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
    }
}

