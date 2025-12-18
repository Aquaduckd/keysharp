using Raylib_cs;
using System;
using System.IO;
using System.Linq;
using Keysharp.Components;
using Keysharp.UI;

namespace Keysharp
{
    class Program
    {
        public static void Main(string[] args)
        {
            const int screenWidth = 1000;
            const int screenHeight = 700;

            Raylib.SetConfigFlags(ConfigFlags.FLAG_WINDOW_RESIZABLE);
            Raylib.InitWindow(screenWidth, screenHeight, "Keysharp");
            Raylib.SetTargetFPS(60);

            // Load font
            Font font = FontManager.LoadFont();

            // Show startup screen immediately
            StartupScreen startupScreen = new StartupScreen(font);
            bool showingStartup = true;
            int startupFrames = 0;
            const int startupFrameCount = 30; // Show startup screen for ~0.5 seconds at 60fps

            // Initialize UI components
            SidePanel? sidePanel = null;
            MainPanel? mainPanel = null;
            BottomPanel? bottomPanel = null;
            MenuBar? menuBar = null;
            LayoutManager? layout = null;
            RootUI? rootUI = null;
            DebugOverlay? debugOverlay = null;
            bool corpusLoaded = false;

            while (!Raylib.WindowShouldClose())
            {
                if (showingStartup)
                {
                    // Calculate loading progress (0.0 to 1.0)
                    float progress = System.Math.Clamp((float)startupFrames / startupFrameCount, 0.0f, 1.0f);
                    
                    // Show startup screen while initializing
                    startupScreen.Update(progress);
                    startupFrames++;

                    Raylib.BeginDrawing();
                    startupScreen.Draw(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                    Raylib.EndDrawing();

                    // Initialize UI components in background (do it after first frame so startup screen renders)
                    if (sidePanel == null && startupFrames > 1)
                    {
                        // Create panels
                        sidePanel = new SidePanel(font);
                        mainPanel = new MainPanel(font);
                        mainPanel.SidePanel = sidePanel; // Connect side panel for key info display
                        bottomPanel = new BottomPanel(font);

                        // Create menu bar (pass mainPanel so it can access tabs)
                        menuBar = new MenuBar(font, mainPanel);

                        // Create layout manager
                        layout = new LayoutManager();
                        layout.MenuBarHeight = menuBar.Height;

                        // Create root UI element (contains all panels)
                        rootUI = new RootUI(sidePanel, mainPanel, bottomPanel, menuBar, layout);

                        // Create debug overlay
                        debugOverlay = new DebugOverlay();
                    }

                    // Load default corpus after UI is initialized and startup screen has rendered a few times
                    if (!corpusLoaded && mainPanel != null && startupFrames > 3)
                    {
                        LoadDefaultCorpus(mainPanel);
                        corpusLoaded = true;
                    }

                    // Switch to main UI after showing startup screen for a brief moment
                    if (startupFrames >= startupFrameCount)
                    {
                        showingStartup = false;
                    }
                }
                else
                {
                    // Main UI loop
                    // Update root UI (recursively updates all children)
                    rootUI!.Update();

                    // Update debug overlay
                    debugOverlay!.Update();

                    // Cursor is handled by individual UI elements
                    var layoutRect = rootUI.Layout.CalculateLayout(Raylib.GetScreenWidth(), Raylib.GetScreenHeight());
                    ResolveCursor(rootUI, layoutRect.MainPanel);

                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(UITheme.BackgroundColor);

                    // Draw root UI (recursively draws all children)
                    rootUI.Draw();

                    // Draw debug overlay on top of everything
                    debugOverlay.Draw(rootUI, layoutRect);

                    Raylib.EndDrawing();
                }
            }

            // Unload font if it's not the default
            if (font.Texture.Id != 0)
            {
                Raylib.UnloadFont(font);
            }

            Raylib.CloseWindow();
        }

        private static void ResolveCursor(RootUI rootUI, Rectangle mainPanelBounds)
        {
            // Check all UI elements in priority order to determine cursor
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            
            // Priority 1: Splitters (highest priority)
            if (rootUI.Children.OfType<Splitter>().Any(s => s.IsHovering(mouseX, mouseY) || s.IsDragging))
            {
                var splitter = rootUI.Children.OfType<Splitter>().FirstOrDefault(s => s.IsHovering(mouseX, mouseY) || s.IsDragging);
                if (splitter != null)
                {
                    Raylib.SetMouseCursor(splitter.IsVertical 
                        ? MouseCursor.MOUSE_CURSOR_RESIZE_EW 
                        : MouseCursor.MOUSE_CURSOR_RESIZE_NS);
                    return;
                }
            }
            
            // Priority 2: Interactive elements (buttons, dropdowns, tabs, menu items)
            if (IsHoveringInteractiveElement(rootUI, mouseX, mouseY))
            {
                Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_POINTING_HAND);
                return;
            }
            
            // Default cursor
            Raylib.SetMouseCursor(MouseCursor.MOUSE_CURSOR_DEFAULT);
        }
        
        private static bool IsHoveringInteractiveElement(UIElement element, int mouseX, int mouseY)
        {
            // Check this element - use flags instead of type checking
            if (element.IsHoverable && element.IsEnabled && element.IsHovering(mouseX, mouseY))
            {
                return true;
            }
            
            // Check children recursively
            foreach (var child in element.Children)
            {
                if (IsHoveringInteractiveElement(child, mouseX, mouseY))
                {
                    return true;
                }
            }
            
            return false;
        }

        private static void LoadDefaultCorpus(MainPanel mainPanel)
        {
            try
            {
                string corpusDir = Path.Combine(Directory.GetCurrentDirectory(), "corpus");
                string mrPath = Path.Combine(corpusDir, "mr.txt");

                if (File.Exists(mrPath))
                {
                    System.Console.WriteLine("Loading default corpus: mr.txt");
                    var corpus = new Core.Corpus(mrPath);
                    corpus.Load();
                    System.Console.WriteLine($"Default corpus loaded: {corpus.FileName}");
                    System.Console.WriteLine($"  Characters: {corpus.CharacterCount:N0}");
                    System.Console.WriteLine($"  Monograms: {corpus.GetMonograms().UniqueCount} unique, {corpus.GetMonograms().Total:N0} total");

                    // Set the corpus in the CorpusTab
                    if (mainPanel.CorpusTab != null)
                    {
                        mainPanel.CorpusTab.SetLoadedCorpus(corpus, mrPath);
                    }
                }
                else
                {
                    System.Console.WriteLine($"Default corpus not found: {mrPath}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading default corpus: {ex.Message}");
            }
        }
    }
}
