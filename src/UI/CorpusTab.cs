using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Raylib_cs;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class CorpusTab
    {
        private Components.TabContent tabContent;
        private Font font;

        // Corpus tab state
        private Components.Button? loadCorpusButton;
        private Components.Dropdown? corpusDropdown;
        private Components.Button? saveCsvButton;
        private string? loadedCorpusPath = null;
        private Corpus? loadedCorpus = null;
        private Layout? layout = null; // Reference to layout for key sequence conversion
        private Components.Container? corpusControlsContainer;
        private Components.Container? corpusRowContainer;
        private Components.Container? corpusHeaderContainer;
        private Components.Container? corpusContentContainer;
        private Components.Container? corpusMainContainer;
        private Components.Container? ngramSelectorContainer;
        private Components.Label? corpusHeaderLabel;
        private Components.Dropdown? ngramSizeDropdown;
        private Components.CorpusTable? ngramTable;
        private Components.TextInput? limitInput;
        private Components.TextInput? searchInput;
        private Components.TextInput? metricSearchInput;
        private Components.Button? regexToggleButton;
        private Components.Button? regexHelpButton;
        private RegexHelpScreen? regexHelpScreen;
        private Components.Label? totalCountLabel;
        private Components.Checkbox? ignoreSpaceCheckbox;
        private Components.Checkbox? collapseCaseCheckbox;
        private Components.Checkbox? collapsePunctuationCheckbox;
        private Components.Checkbox? filterNoMetricsCheckbox;
        private Components.Label? metricSearchLabel;
        private string selectedNgramSize = "bigram"; // "monogram", "bigram", "trigram", or "words"
        private string searchText = "";
        private string metricSearchText = "";
        private int? resultLimit = null; // Null means no limit
        private bool useRegex = false;
        private bool ignoreSpace = true;
        private bool collapseCase = true;
        private bool collapsePunctuation = false;
        private bool filterNoMetrics = false;
        
        // Store filtered n-grams for lazy highlight calculation
        private List<(string displayText, string originalSequence)> storedNgramsForHighlighting = new List<(string, string)>();

        public Components.TabContent TabContent => tabContent;
        public Components.Dropdown? CorpusDropdown => corpusDropdown;
        public Components.Dropdown? NgramSizeDropdown => ngramSizeDropdown;
        public RegexHelpScreen? RegexHelpScreen => regexHelpScreen;

        /// <summary>
        /// Gets whether a corpus is currently loaded.
        /// </summary>
        public bool HasLoadedCorpus => loadedCorpus != null && loadedCorpus.IsLoaded;

        /// <summary>
        /// Gets the loaded corpus, or null if no corpus is loaded.
        /// </summary>
        public Core.Corpus? LoadedCorpus => loadedCorpus;

        /// <summary>
        /// Sets the layout reference for key sequence conversion.
        /// </summary>
        public void SetLayout(Layout? layout)
        {
            this.layout = layout;
            // Update table to show key sequences if layout is available
            UpdateNgramTable();
        }

        /// <summary>
        /// Sets a loaded corpus programmatically (e.g., during startup).
        /// </summary>
        public void SetLoadedCorpus(Core.Corpus corpus, string corpusPath)
        {
            loadedCorpus = corpus;
            loadedCorpusPath = corpusPath;

            // Update dropdown selection if available
            if (corpusDropdown != null)
            {
                string fileName = Path.GetFileName(corpusPath);
                // Temporarily remove callback to avoid triggering reload
                var oldCallback = corpusDropdown.OnSelectionChanged;
                corpusDropdown.OnSelectionChanged = null;
                corpusDropdown.SetSelectedItem(fileName);
                corpusDropdown.OnSelectionChanged = oldCallback;
            }

            // Update n-gram table with loaded corpus
            UpdateNgramTable();

            // Update save CSV button visibility
            if (saveCsvButton != null)
            {
                bool hasData = ngramTable != null && ngramTable.Rows.Count > 0;
                saveCsvButton.IsVisible = loadedCorpus != null && hasData;
            }

            // Notify layout tab that corpus changed (for heatmap updates)
            NotifyCorpusChanged();

            System.Console.WriteLine($"Corpus set: {corpus.FileName}");
        }

        private Action? onCorpusChanged;

        /// <summary>
        /// Sets a callback to be called when the corpus is loaded, unloaded, or changed.
        /// </summary>
        public void SetOnCorpusChanged(Action? callback)
        {
            onCorpusChanged = callback;
        }

        private void NotifyCorpusChanged()
        {
            onCorpusChanged?.Invoke();
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update fonts in key UI components (would need UpdateFont methods in components)
            // For now, just update the font reference - some components may need recreation
        }

        public CorpusTab(Font font)
        {
            this.font = font;
            tabContent = new Components.TabContent(font, "", null); // Empty title - we'll use our own header
            tabContent.PositionMode = Components.PositionMode.Relative;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create main vertical container that wraps header, controls, and content
            corpusMainContainer = new Components.Container("CorpusMain");
            corpusMainContainer.AutoLayoutChildren = true;
            corpusMainContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            corpusMainContainer.AutoSize = true; // Auto-size to fit children (accounts for padding)
            corpusMainContainer.ChildPadding = 20;
            corpusMainContainer.ChildGap = 10;
            tabContent.AddChild(corpusMainContainer);

            // Create header container for corpus tab (will display centered text)
            corpusHeaderContainer = new Components.Container("CorpusHeader");
            corpusHeaderContainer.AutoSize = true; // Auto-size based on label + padding
            corpusHeaderContainer.ChildPadding = 0; // No padding needed since label fills it
            corpusMainContainer.AddChild(corpusHeaderContainer);

            // Create header label with centered text
            corpusHeaderLabel = new Components.Label(font, "Corpus", 24);
            corpusHeaderLabel.Bounds = new Rectangle(0, 0, 0, 40); // Height for header text
            corpusHeaderContainer.AddChild(corpusHeaderLabel);

            // Create a container for the entire corpus controls row (button, dropdown, and info text)
            corpusRowContainer = new Components.Container("CorpusRow");
            corpusRowContainer.AutoLayoutChildren = true;
            corpusRowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            corpusRowContainer.AutoSize = true; // Auto-size based on children + padding
            corpusRowContainer.ChildJustification = Components.ChildJustification.SpaceBetween;
            corpusRowContainer.ChildGap = 0; // Gap will be calculated by SpaceBetween
            corpusRowContainer.ChildPadding = 0;
            corpusMainContainer.AddChild(corpusRowContainer);

            // Create content container for the rest of the corpus tab content
            corpusContentContainer = new Components.Container("CorpusContent");
            corpusContentContainer.AutoSize = false; // Size will be set by parent's fill-remaining logic
            corpusContentContainer.AutoLayoutChildren = true; // Enable auto-layout for n-gram selector and table
            corpusContentContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            corpusContentContainer.ChildGap = 10;
            corpusContentContainer.ChildPadding = 0;
            corpusContentContainer.FillRemaining = true; // Fill remaining space in parent container
            corpusMainContainer.AddChild(corpusContentContainer);

            // Create outer container for controls and total label (with SpaceBetween)
            ngramSelectorContainer = new Components.Container("NgramSelector");
            ngramSelectorContainer.AutoSize = false; // Width must be set explicitly for SpaceBetween to work
            ngramSelectorContainer.AutoLayoutChildren = true;
            ngramSelectorContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            ngramSelectorContainer.ChildJustification = Components.ChildJustification.SpaceBetween;
            ngramSelectorContainer.ChildGap = 10;
            ngramSelectorContainer.ChildPadding = 0;
            ngramSelectorContainer.Bounds = new Rectangle(0, 0, 0, 35);
            corpusContentContainer.AddChild(ngramSelectorContainer);

            // Create left container for n-gram size selector and search controls
            var leftControlsContainer = new Components.Container("LeftControls");
            leftControlsContainer.AutoSize = true;
            leftControlsContainer.AutoLayoutChildren = true;
            leftControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            leftControlsContainer.ChildJustification = Components.ChildJustification.Left;
            leftControlsContainer.ChildGap = 10;
            leftControlsContainer.ChildPadding = 0;
            leftControlsContainer.Bounds = new Rectangle(0, 0, 0, 35);
            ngramSelectorContainer.AddChild(leftControlsContainer);

            // Create limit label (first in bottom container)
            var limitLabel = new Components.Label(font, "Limit:", 14);
            limitLabel.Bounds = new Rectangle(0, 0, 60, 35);
            limitLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(limitLabel);

            // Create limit input (first in bottom container)
            limitInput = new Components.TextInput(font, "All", 14);
            limitInput.Bounds = new Rectangle(0, 0, 80, 35);
            limitInput.PositionMode = Components.PositionMode.Absolute;
            limitInput.OnTextChanged = OnLimitTextChanged;
            leftControlsContainer.AddChild(limitInput);

            // Create search label (ngram search - second in bottom container)
            var searchLabel = new Components.Label(font, "Search:", 14);
            searchLabel.Bounds = new Rectangle(0, 0, 80, 35);
            searchLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(searchLabel);

            // Create search input (ngram search - second in bottom container)
            searchInput = new Components.TextInput(font, "Enter search term...", 14);
            searchInput.Bounds = new Rectangle(0, 0, 300, 35);
            searchInput.PositionMode = Components.PositionMode.Absolute;
            searchInput.OnTextChanged = OnSearchTextChanged;
            leftControlsContainer.AddChild(searchInput);

            // Create regex toggle button (follows search bar)
            regexToggleButton = new Components.Button(font, "Regex: Off", 12);
            regexToggleButton.Bounds = new Rectangle(0, 0, 100, 35);
            regexToggleButton.PositionMode = Components.PositionMode.Absolute;
            regexToggleButton.OnClick = ToggleRegexMode;
            leftControlsContainer.AddChild(regexToggleButton);

            // Create regex help button (follows search bar)
            regexHelpButton = new Components.Button(font, "?", 12);
            regexHelpButton.Bounds = new Rectangle(0, 0, 35, 35);
            regexHelpButton.PositionMode = Components.PositionMode.Absolute;
            regexHelpButton.OnClick = ShowRegexHelp;
            leftControlsContainer.AddChild(regexHelpButton);

            // Create metric search label (third in bottom container)
            metricSearchLabel = new Components.Label(font, "Metric Search:", 14);
            metricSearchLabel.Bounds = new Rectangle(0, 0, 110, 35);
            metricSearchLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(metricSearchLabel);

            // Create metric search input (third in bottom container)
            metricSearchInput = new Components.TextInput(font, "Enter metric search (e.g., SFB)...", 14);
            metricSearchInput.Bounds = new Rectangle(0, 0, 250, 35);
            metricSearchInput.PositionMode = Components.PositionMode.Absolute;
            metricSearchInput.OnTextChanged = OnMetricSearchTextChanged;
            leftControlsContainer.AddChild(metricSearchInput);

            // Create filter no metrics checkbox (fourth in bottom container)
            filterNoMetricsCheckbox = new Components.Checkbox(font, "Filter No Metrics", 14);
            filterNoMetricsCheckbox.Bounds = new Rectangle(0, 0, 150, 35);
            filterNoMetricsCheckbox.PositionMode = Components.PositionMode.Absolute;
            filterNoMetricsCheckbox.IsChecked = false; // Unchecked by default
            filterNoMetricsCheckbox.OnCheckedChanged = (isChecked) => { filterNoMetrics = isChecked; UpdateNgramTable(); };
            leftControlsContainer.AddChild(filterNoMetricsCheckbox);

            // Set initial visibility of metric controls (hidden for monograms/words, shown for bigrams/trigrams)
            bool showMetricControls = (selectedNgramSize == "bigram" || selectedNgramSize == "trigram");
            metricSearchLabel.IsVisible = showMetricControls;
            metricSearchInput.IsVisible = showMetricControls;
            filterNoMetricsCheckbox.IsVisible = showMetricControls;

            // Create regex help screen
            // Note: Not added as a child to tabContent because it uses absolute screen coordinates
            // and is updated explicitly in Update() method
            regexHelpScreen = new RegexHelpScreen(font);
            regexHelpScreen.SetPatternSelectedCallback(OnRegexPatternSelected);

            // Create total count label (right-aligned)
            totalCountLabel = new Components.Label(font, "", 14, null, Components.Label.TextAlignment.Right);
            totalCountLabel.Bounds = new Rectangle(0, 0, 250, 35);
            totalCountLabel.PositionMode = Components.PositionMode.Absolute;
            ngramSelectorContainer.AddChild(totalCountLabel);

            // Create n-gram table (Rank, N-gram, Frequency, Count, Global Rank, Rel. Freq., Key Sequence, Finger Sequence, Metric Matches)
            ngramTable = new Components.CorpusTable(font, 14, "Rank", "N-gram", "Frequency", "Count", "Global Rank", "Rel. Freq.", "Key Sequence", "Finger Sequence", "Metric Matches");
            ngramTable.Bounds = new Rectangle(0, 0, 0, 0); // Will be set by Update
            ngramTable.AutoSize = false;
            ngramTable.FillRemaining = true; // Fill remaining space after selector
            ngramTable.PositionMode = Components.PositionMode.Absolute;
            corpusContentContainer.AddChild(ngramTable);

            // Create container for left-aligned controls (button and dropdown)
            corpusControlsContainer = new Components.Container("CorpusControls");
            corpusControlsContainer.AutoLayoutChildren = true;
            corpusControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            corpusControlsContainer.ChildJustification = Components.ChildJustification.Left;
            corpusControlsContainer.ChildGap = 10;
            corpusControlsContainer.ChildPadding = 0;
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

            // Create n-gram size dropdown (moved to top container)
            List<string> ngramSizes = new List<string> { "Monogram", "Bigram", "Trigram", "Words" };
            ngramSizeDropdown = new Components.Dropdown(font, ngramSizes, 14);
            ngramSizeDropdown.SetBounds(new Rectangle(0, 0, 200, 35));
            ngramSizeDropdown.PositionMode = Components.PositionMode.Absolute;
            ngramSizeDropdown.OnSelectionChanged = OnNgramSizeSelected;
            // Set default selection to "Bigram"
            ngramSizeDropdown.SetSelectedItem("Bigram");
            corpusControlsContainer.AddChild(ngramSizeDropdown);

            // Create checkboxes for filtering/transforming n-grams
            ignoreSpaceCheckbox = new Components.Checkbox(font, "Ignore Space", 14);
            ignoreSpaceCheckbox.Bounds = new Rectangle(0, 0, 120, 35);
            ignoreSpaceCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignoreSpaceCheckbox.IsChecked = true; // Checked by default
            ignoreSpaceCheckbox.OnCheckedChanged = (isChecked) => { ignoreSpace = isChecked; UpdateNgramTable(); };
            corpusControlsContainer.AddChild(ignoreSpaceCheckbox);

            collapseCaseCheckbox = new Components.Checkbox(font, "Collapse Case", 14);
            collapseCaseCheckbox.Bounds = new Rectangle(0, 0, 128, 35);
            collapseCaseCheckbox.PositionMode = Components.PositionMode.Absolute;
            collapseCaseCheckbox.IsChecked = true; // Checked by default
            collapseCaseCheckbox.OnCheckedChanged = (isChecked) => { collapseCase = isChecked; UpdateNgramTable(); };
            corpusControlsContainer.AddChild(collapseCaseCheckbox);

            collapsePunctuationCheckbox = new Components.Checkbox(font, "Collapse Punctuation", 14);
            collapsePunctuationCheckbox.Bounds = new Rectangle(0, 0, 160, 35);
            collapsePunctuationCheckbox.PositionMode = Components.PositionMode.Absolute;
            collapsePunctuationCheckbox.OnCheckedChanged = (isChecked) => { collapsePunctuation = isChecked; UpdateNgramTable(); };
            corpusControlsContainer.AddChild(collapsePunctuationCheckbox);

            // Create save CSV button
            saveCsvButton = new Components.Button(font, "Save to CSV", 14);
            saveCsvButton.Bounds = new Rectangle(0, 0, 150, 35);
            saveCsvButton.PositionMode = Components.PositionMode.Absolute;
            saveCsvButton.OnClick = SaveNgramsToCsv;
            saveCsvButton.IsVisible = false; // Hidden until corpus is loaded and has data
            corpusRowContainer.AddChild(saveCsvButton);
        }

        public void Update(Rectangle contentArea, bool isActive)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);

            // Update help screen explicitly (it uses absolute screen coordinates, not relative to parent)
            // Phase 1: Resolve bounds
            if (regexHelpScreen != null)
            {
                regexHelpScreen.ResolveBounds();
            }
            
            // Phase 2: Layout and input handling
            if (regexHelpScreen != null)
            {
                regexHelpScreen.Update();
            }

            if (isActive)
            {
                // Update corpus containers if corpus tab is active
                // With auto-layout, we only need to set sizes, not positions
                const int elementHeight = 35;
                const int headerHeight = 40; // Height for header text

                // Set main container bounds (fills parent, uses auto-layout)
                if (corpusMainContainer != null)
                {
                    corpusMainContainer.Bounds = new Rectangle(
                        0, 0,
                        (int)contentArea.Width,
                        (int)contentArea.Height
                    );
                    // Set target height so fill-remaining children can calculate their size
                    corpusMainContainer.TargetHeight = contentArea.Height;
                    corpusMainContainer.IsVisible = true;
                }

                // Calculate available width accounting for parent padding
                int availableWidth = (int)contentArea.Width;
                if (corpusMainContainer != null)
                {
                    availableWidth = (int)contentArea.Width - (int)(corpusMainContainer.ChildPadding * 2);
                }

                // Set header container bounds (width accounts for parent padding, fixed height for label)
                // Position will be handled by auto-layout
                if (corpusHeaderContainer != null)
                {
                    corpusHeaderContainer.Bounds = new Rectangle(
                        0, 0,
                        availableWidth,
                        headerHeight
                    );
                    corpusHeaderContainer.IsVisible = true;

                    // Update header label bounds (fills header container)
                    if (corpusHeaderLabel != null)
                    {
                        corpusHeaderLabel.Bounds = new Rectangle(
                            0, 0,
                            availableWidth,
                            headerHeight
                        );
                        corpusHeaderLabel.IsVisible = true;
                    }
                }

                // Update corpus row container bounds (width accounts for parent padding)
                if (corpusRowContainer != null)
                {
                    corpusRowContainer.Bounds = new Rectangle(
                        0, 0,
                        availableWidth,
                        elementHeight
                    );
                    corpusRowContainer.IsVisible = true;

                    // Update controls container bounds (will be left-aligned by parent auto-layout)
                    if (corpusControlsContainer != null)
                    {
                        // Width will be calculated by auto-layout based on children
                        corpusControlsContainer.Bounds = new Rectangle(
                            0, 0,
                            0, // Width calculated by layout
                            elementHeight
                        );
                        corpusControlsContainer.IsVisible = true;
                    }

                    // Update save CSV button
                    if (saveCsvButton != null)
                    {
                        saveCsvButton.Bounds = new Rectangle(
                            0, 0,
                            150,
                            elementHeight
                        );
                        // Show button only if corpus is loaded and table has data
                        bool hasData = ngramTable != null && ngramTable.Rows.Count > 0;
                        saveCsvButton.IsVisible = loadedCorpus != null && hasData;
                    }
                }

                // Set content container width and target height for auto-layout
                if (corpusContentContainer != null && corpusMainContainer != null)
                {
                    // Calculate available height for the content container
                    // Use actual heights from containers if available, otherwise use expected values
                    // We need to account for: top padding, header, gap, row, gap, and bottom padding
                    int actualHeaderHeight = corpusHeaderContainer != null && corpusHeaderContainer.Bounds.Height > 0 ? (int)corpusHeaderContainer.Bounds.Height : 40;
                    int actualRowHeight = corpusRowContainer != null && corpusRowContainer.Bounds.Height > 0 ? (int)corpusRowContainer.Bounds.Height : 75;
                    int gaps = (int)corpusMainContainer.ChildGap * 2; // Gap between header->row and row->content
                    int availableHeight = (int)contentArea.Height - (int)(corpusMainContainer.ChildPadding * 2) - actualHeaderHeight - actualRowHeight - gaps;

                    corpusContentContainer.Bounds = new Rectangle(
                        0, 0,
                        availableWidth, // Width accounts for parent padding
                        availableHeight // Set initial height
                    );
                    corpusContentContainer.TargetHeight = (float)availableHeight; // Set target for FillRemaining children
                    corpusContentContainer.IsVisible = true;
                }

                // Set ngramSelectorContainer width for SpaceBetween to work correctly
                if (ngramSelectorContainer != null)
                {
                    ngramSelectorContainer.Bounds = new Rectangle(
                        ngramSelectorContainer.Bounds.X,
                        ngramSelectorContainer.Bounds.Y,
                        availableWidth, // Set width explicitly for SpaceBetween
                        ngramSelectorContainer.Bounds.Height
                    );
                }

                // Table bounds will be set automatically by container's auto-layout since it has FillRemaining = true
                if (ngramTable != null)
                {
                    // Only set width - height and position will be handled by auto-layout
                    ngramTable.Bounds = new Rectangle(
                        ngramTable.Bounds.X,
                        ngramTable.Bounds.Y,
                        availableWidth,
                        ngramTable.Bounds.Height // Height will be set by FillRemaining logic
                    );
                    ngramTable.IsVisible = true; // Always show table, even if empty
                }
            }
            else
            {
                // Hide containers when not in corpus tab
                if (corpusMainContainer != null)
                {
                    corpusMainContainer.IsVisible = false;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
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

                // Load the corpus
                try
                {
                    loadedCorpus = new Corpus(fullPath);
                    loadedCorpus.Load();
                    System.Console.WriteLine($"Corpus loaded: {loadedCorpus.FileName}");
                    System.Console.WriteLine($"  Characters: {loadedCorpus.CharacterCount:N0}");
                    System.Console.WriteLine($"  Monograms: {loadedCorpus.GetMonograms().UniqueCount} unique, {loadedCorpus.GetMonograms().Total:N0} total");
                    System.Console.WriteLine($"  Bigrams: {loadedCorpus.GetBigrams().UniqueCount} unique, {loadedCorpus.GetBigrams().Total:N0} total");
                    System.Console.WriteLine($"  Trigrams: {loadedCorpus.GetTrigrams().UniqueCount} unique, {loadedCorpus.GetTrigrams().Total:N0} total");

                    // Update n-gram table with loaded corpus
                    UpdateNgramTable();

                    // Update save CSV button visibility
                    if (saveCsvButton != null)
                    {
                        bool hasData = ngramTable != null && ngramTable.Rows.Count > 0;
                        saveCsvButton.IsVisible = hasData;
                    }

                    // Notify layout tab that corpus changed (for heatmap updates)
                    NotifyCorpusChanged();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error loading corpus: {ex.Message}");
                    loadedCorpus = null;
                    UpdateNgramTable();

                    // Hide save CSV button on error
                    if (saveCsvButton != null)
                    {
                        saveCsvButton.IsVisible = false;
                    }

                    // Notify layout tab that corpus changed (for heatmap updates)
                    NotifyCorpusChanged();
                }
            }
        }

        private void OnNgramSizeSelected(string ngramSize)
        {
            selectedNgramSize = ngramSize.ToLower();
            
            // Show/hide metric controls based on ngram size (only show for bigrams and trigrams)
            bool showMetricControls = (selectedNgramSize == "bigram" || selectedNgramSize == "trigram");
            if (metricSearchLabel != null)
                metricSearchLabel.IsVisible = showMetricControls;
            if (metricSearchInput != null)
                metricSearchInput.IsVisible = showMetricControls;
            if (filterNoMetricsCheckbox != null)
                filterNoMetricsCheckbox.IsVisible = showMetricControls;
            
            UpdateNgramTable();
        }

        private void OnLimitTextChanged(string text)
        {
            // Parse the limit value
            if (string.IsNullOrWhiteSpace(text))
            {
                resultLimit = null; // No limit
            }
            else if (int.TryParse(text, out int limit) && limit > 0)
            {
                resultLimit = limit;
            }
            else
            {
                resultLimit = null; // Invalid input, no limit
            }

            // Reset scroll when limit changes
            if (ngramTable != null)
            {
                ngramTable.ResetScroll();
            }
            UpdateNgramTable();
        }

        private void OnSearchTextChanged(string text)
        {
            searchText = text;
            // Reset scroll when search changes
            if (ngramTable != null)
            {
                ngramTable.ResetScroll();
            }
            UpdateNgramTable();
        }

        private void OnMetricSearchTextChanged(string text)
        {
            metricSearchText = text;
            // Reset scroll when search changes
            if (ngramTable != null)
            {
                ngramTable.ResetScroll();
            }
            UpdateNgramTable();
        }

        private void ToggleRegexMode()
        {
            useRegex = !useRegex;
            if (regexToggleButton != null)
            {
                regexToggleButton.Text = useRegex ? "Regex: On" : "Regex: Off";
            }
            UpdateNgramTable();
        }

        private void ShowRegexHelp()
        {
            if (regexHelpScreen != null)
            {
                regexHelpScreen.Show();
            }
        }

        private void OnRegexPatternSelected(string pattern)
        {
            // Enable regex mode
            useRegex = true;
            if (regexToggleButton != null)
            {
                regexToggleButton.Text = "Regex: On";
            }

            // Set search text to the selected pattern
            searchText = pattern;
            if (searchInput != null)
            {
                searchInput.SetText(pattern);
            }

            // Update the table
            if (ngramTable != null)
            {
                ngramTable.ResetScroll();
            }
            UpdateNgramTable();
        }

        private Grams ApplyTransformations(Grams grams)
        {
            var resultCounts = new Dictionary<string, long>();

            foreach (var kvp in grams.Counts)
            {
                string sequence = kvp.Key;
                long count = kvp.Value;

                // Ignore space: filter out grams that contain space
                if (ignoreSpace && sequence.Contains(' '))
                {
                    continue;
                }

                // Apply transformations
                string transformedSequence = sequence;

                // Collapse case: lowercase all grams
                if (collapseCase)
                {
                    transformedSequence = transformedSequence.ToLowerInvariant();
                }

                // Collapse punctuation: remove punctuation characters
                if (collapsePunctuation)
                {
                    transformedSequence = new string(transformedSequence.Where(c => !char.IsPunctuation(c)).ToArray());
                }

                // Add or combine counts for transformed sequence
                if (resultCounts.ContainsKey(transformedSequence))
                {
                    resultCounts[transformedSequence] += count;
                }
                else
                {
                    resultCounts[transformedSequence] = count;
                }
            }

            // Create new Grams object from transformed counts
            return Grams.FromDictionary(resultCounts);
        }

        private bool MatchesSearch(string sequence)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            try
            {
                if (useRegex)
                {
                    // Validate and compile the regex pattern first
                    // This will throw ArgumentException if the pattern is invalid before we try to match
                    var regex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                    return regex.IsMatch(sequence);
                }
                else
                {
                    return sequence.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, return false (don't match anything)
                return false;
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex matching timed out (pattern too complex), return false
                return false;
            }
            catch
            {
                // Any other exception, return false
                return false;
            }
        }

        private void UpdateNgramTable()
        {
            if (ngramTable == null)
            {
                return;
            }

            if (loadedCorpus == null)
            {
                ngramTable.Rows.Clear();
                // Hide save CSV button when no corpus is loaded
                if (saveCsvButton != null)
                {
                    saveCsvButton.IsVisible = false;
                }
                return;
            }

            Grams grams;
            switch (selectedNgramSize)
            {
                case "bigram":
                    grams = loadedCorpus.GetBigrams();
                    break;
                case "trigram":
                    grams = loadedCorpus.GetTrigrams();
                    break;
                case "words":
                    grams = loadedCorpus.GetWords();
                    break;
                default:
                    grams = loadedCorpus.GetMonograms();
                    break;
            }

            // Apply transformations if any checkbox is checked
            grams = ApplyTransformations(grams);

            // Get all n-grams sorted by frequency
            var allNgrams = grams.GetAllSorted();

            // Create a dictionary mapping n-gram sequences to their actual rank (1-based)
            var actualRankMap = new Dictionary<string, int>();
            for (int i = 0; i < allNgrams.Count; i++)
            {
                actualRankMap[allNgrams[i].sequence] = i + 1;
            }

            // Filter n-grams based on search text
            // Pre-compile regex if in regex mode for better performance
            Regex? compiledRegex = null;
            if (useRegex && !string.IsNullOrEmpty(searchText))
            {
                try
                {
                    compiledRegex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                }
                catch
                {
                    // Invalid regex pattern - will filter out everything
                    compiledRegex = null;
                }
            }

            var filteredNgrams = allNgrams.Where(ngram =>
            {
                if (string.IsNullOrEmpty(searchText))
                    return true;

                if (useRegex)
                {
                    if (compiledRegex == null)
                        return false; // Invalid regex pattern
                    try
                    {
                        return compiledRegex.IsMatch(ngram.sequence);
                    }
                    catch
                    {
                        return false;
                    }
                }
                else
                {
                    return ngram.sequence.Contains(searchText, StringComparison.OrdinalIgnoreCase);
                }
            }).ToList();

            // Create a list to store rows with metrics before final filtering
            var rowsWithMetrics = new List<((string sequence, long count, double frequency) ngram, string keySequenceStr, string fingerSequenceStr, string metricMatches)>();

            // Calculate filtered total for relative frequency (only when filtered or limited)
            bool isFiltered = !string.IsNullOrEmpty(searchText);
            bool hasLimit = resultLimit.HasValue && resultLimit.Value > 0;
            bool showConditionalColumns = isFiltered || hasLimit; // Show when filtered or limited
            
            long filteredTotal = 0;
            if (showConditionalColumns && filteredNgrams.Count > 0)
            {
                filteredTotal = filteredNgrams.Sum(ng => ng.count);
            }

            // Store counts before applying limit (for display in total label)
            // Note: filteredCountBeforeLimit and filteredTotalBeforeLimit are calculated before limit,
            // but the actual limit will be applied after metric filtering
            int filteredCountBeforeLimit = filteredNgrams.Count;
            long filteredTotalBeforeLimit = filteredTotal;

            // Show/hide conditional columns (Global Rank and Relative Frequency)
            ngramTable.SetColumnVisibility(4, showConditionalColumns); // Column 5 (Global Rank) - 0-indexed
            ngramTable.SetColumnVisibility(5, showConditionalColumns); // Column 6 (Relative Frequency) - 0-indexed

            // Show/hide metric columns (Key Sequence, Finger Sequence, Metric Matches) only for bigrams and trigrams
            bool showMetricColumns = (selectedNgramSize == "bigram" || selectedNgramSize == "trigram");
            ngramTable.SetColumnVisibility(6, showMetricColumns); // Column 7 (Key Sequence) - 0-indexed
            ngramTable.SetColumnVisibility(7, showMetricColumns); // Column 8 (Finger Sequence) - 0-indexed
            ngramTable.SetColumnVisibility(8, showMetricColumns); // Column 9 (Metric Matches) - 0-indexed

            // Compute key sequences, finger sequences, and metric matches for all filtered ngrams
            // Only compute for bigrams and trigrams (skip monograms and words for now)
            bool computeMetrics = layout != null && (selectedNgramSize == "bigram" || selectedNgramSize == "trigram");
            
            foreach (var ngram in filteredNgrams)
            {
                string keySequenceStr = "";
                string fingerSequenceStr = "";
                string metricMatches = "";

                if (computeMetrics)
                {
                    // Convert ngram to key sequence
                    var keySequence = layout!.ConvertNgramToKeySequence(ngram.sequence);
                    if (keySequence != null)
                    {
                        // Format key sequence
                        keySequenceStr = FormatKeySequence(keySequence);

                        // Format finger sequence
                        fingerSequenceStr = FormatFingerSequence(keySequence);

                        // Build metric matches string based on ngram size
                        var metrics = new List<string>();
                        
                        if (selectedNgramSize == "bigram")
                        {
                            // Bigram metrics
                            bool isSFB = Core.Metrics.CheckAnyNgram(keySequence, 2, Core.Metrics.IsSFBPair);
                            bool isLSB = Core.Metrics.CheckAnyNgram(keySequence, 2, Core.Metrics.IsLSBPair);
                            bool isFSB = Core.Metrics.CheckAnyNgram(keySequence, 2, Core.Metrics.IsFSBPair);
                            bool isHSB = Core.Metrics.CheckAnyNgram(keySequence, 2, Core.Metrics.IsHSBPair);
                            
                            if (isSFB) metrics.Add("SFB");
                            if (isLSB) metrics.Add("LSB");
                            if (isFSB) metrics.Add("FSB");
                            if (isHSB) metrics.Add("HSB");
                        }
                        else if (selectedNgramSize == "trigram")
                        {
                            // Trigram metrics
                            bool isInHand = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsInHandTrigram);
                            bool isOutHand = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsOutHandTrigram);
                            bool isRedirect = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsRedirectTrigram);
                            bool isAlternate = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsAlternateTrigram);
                            bool isInRoll = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsInRollTrigram);
                            bool isOutRoll = Core.Metrics.CheckAnyNgram(keySequence, 3, Core.Metrics.IsOutRollTrigram);
                            
                            if (isInHand) metrics.Add("InHand");
                            if (isOutHand) metrics.Add("OutHand");
                            if (isRedirect) metrics.Add("Redirect");
                            if (isAlternate) metrics.Add("Alternate");
                            if (isInRoll) metrics.Add("InRoll");
                            if (isOutRoll) metrics.Add("OutRoll");
                        }
                        
                        metricMatches = string.Join(" ", metrics);
                    }
                }

                // Apply metric search filter (only if metrics are being computed)
                if (computeMetrics && !string.IsNullOrEmpty(metricSearchText))
                {
                    if (!metricMatches.Contains(metricSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Apply filter no metrics checkbox (only if metrics are being computed)
                if (computeMetrics && filterNoMetrics && string.IsNullOrEmpty(metricMatches))
                    continue;

                rowsWithMetrics.Add(((ngram.sequence, ngram.count, ngram.frequency), keySequenceStr, fingerSequenceStr, metricMatches));
            }

            // Apply limit AFTER all metric filtering (limit should be applied last)
            if (hasLimit && rowsWithMetrics.Count > resultLimit.Value)
            {
                rowsWithMetrics = rowsWithMetrics.Take(resultLimit.Value).ToList();
                // Recalculate filtered total based on limited results
                filteredTotal = rowsWithMetrics.Sum(row => row.ngram.count);
            }

            // Update table rows
            ngramTable.Rows.Clear();
            ngramTable.Column2MatchPositions.Clear(); // Clear previous match positions
            
            // Store filtered n-grams for lazy highlight calculation
            storedNgramsForHighlighting.Clear();
            
            for (int i = 0; i < rowsWithMetrics.Count; i++)
            {
                var row = rowsWithMetrics[i];
                var ngram = row.ngram;
                // Rank is relative rank within filtered results (1-based)
                string rank = (i + 1).ToString();
                string ngramText = $"\"{EscapeSpecialChars(ngram.sequence)}\""; // Add quotation marks around n-gram and escape special chars
                string originalSequence = ngram.sequence;
                
                // Store for lazy calculation
                storedNgramsForHighlighting.Add((ngramText, originalSequence));
                
                // Format frequency as percentage with 3 decimal places
                string frequency = $"{ngram.frequency * 100:F3}%";
                // Format count with thousand separators
                string count = ngram.count.ToString("N0");

                // Get global rank from all n-grams (conditional)
                string globalRank = "";
                if (isFiltered && actualRankMap.TryGetValue(ngram.sequence, out int actualRank))
                {
                    globalRank = actualRank.ToString();
                }

                // Calculate relative frequency (count / filtered total) if filtered or limited (conditional)
                string relativeFreq = "";
                if (showConditionalColumns && filteredTotal > 0)
                {
                    double relFreq = (double)ngram.count / filteredTotal;
                    relativeFreq = $"{relFreq * 100:F3}%";
                }

                // Column order: Rank, N-gram, Frequency, Count, Global Rank, Rel. Freq., Key Sequence, Finger Sequence, Metric Matches
                ngramTable.Rows.Add(new List<string> { rank, ngramText, frequency, count, globalRank, relativeFreq, row.keySequenceStr, row.fingerSequenceStr, row.metricMatches });
            }
            
            // Set up callback for lazy highlight calculation
            if (!string.IsNullOrEmpty(searchText))
            {
                ngramTable.CalculateMatchPositionsCallback = (rowIndex, displayText) =>
                {
                    if (rowIndex >= 0 && rowIndex < storedNgramsForHighlighting.Count)
                    {
                        var (_, originalSequence) = storedNgramsForHighlighting[rowIndex];
                        return CalculateMatchPositions(displayText, originalSequence);
                    }
                    return new List<(int, int)>();
                };
            }
            else
            {
                ngramTable.CalculateMatchPositionsCallback = null;
            }

            // Update total count label
            if (totalCountLabel != null)
            {
                long totalCount = grams.Total;
                string countText;

                // Calculate what we're actually showing (after limit)
                long showingTotal = filteredNgrams.Sum(ng => ng.count);

                if (showingTotal == totalCount && (string.IsNullOrEmpty(searchText) && !resultLimit.HasValue))
                {
                    // Showing everything, no filters or limits
                    countText = $"Total: {totalCount:N0}";
                }
                else
                {
                    // Show what's displayed vs total
                    double frequency = totalCount > 0 ? (double)showingTotal / totalCount : 0.0;
                    countText = $"Showing {showingTotal:N0} of {totalCount:N0} ({frequency * 100:F3}%)";
                }
                totalCountLabel.SetText(countText);
            }
        }

        private void SaveNgramsToCsv()
        {
            if (ngramTable == null || loadedCorpus == null || ngramTable.Rows.Count == 0)
                return;

            try
            {
                // Use zenity for file save dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Save CSV File\" --save --confirm-overwrite",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    string? output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        string filePath = output.Trim();
                        if (!filePath.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                        {
                            filePath += ".csv";
                        }

                        // Write CSV file
                        using (var writer = new StreamWriter(filePath))
                        {
                            // Write header row
                            var headers = new List<string>();
                            for (int colIndex = 0; colIndex < ngramTable.Columns.Count; colIndex++)
                            {
                                if (ngramTable.GetColumnVisibility(colIndex))
                                {
                                    headers.Add(ngramTable.Columns[colIndex].Header);
                                }
                            }
                            writer.WriteLine(string.Join(",", headers.Select(h => EscapeCsvField(h))));

                            // Write data rows
                            foreach (var row in ngramTable.Rows)
                            {
                                var fields = new List<string>();
                                for (int colIndex = 0; colIndex < ngramTable.Columns.Count && colIndex < row.Count; colIndex++)
                                {
                                    if (ngramTable.GetColumnVisibility(colIndex))
                                    {
                                        fields.Add(row[colIndex]);
                                    }
                                }
                                writer.WriteLine(string.Join(",", fields.Select(f => EscapeCsvField(f))));
                            }
                        }

                        System.Console.WriteLine($"CSV saved to: {filePath}");
                    }
                    else
                    {
                        System.Console.WriteLine("CSV save cancelled");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file save dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving CSV: {ex.Message}");
            }
        }

        private string EscapeCsvField(string field)
        {
            // Remove quotes from n-gram text if present, then properly escape for CSV
            if (field.StartsWith("\"") && field.EndsWith("\""))
            {
                field = field.Substring(1, field.Length - 2);
            }

            // Escape quotes and wrap in quotes if field contains comma, quote, or newline
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
            {
                field = "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            return field;
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

                                // Load the corpus
                                try
                                {
                                    loadedCorpus = new Corpus(filePath);
                                    loadedCorpus.Load();
                                    System.Console.WriteLine($"Corpus loaded: {loadedCorpus.FileName}");
                                    System.Console.WriteLine($"  Characters: {loadedCorpus.CharacterCount:N0}");
                                    System.Console.WriteLine($"  Monograms: {loadedCorpus.GetMonograms().UniqueCount} unique, {loadedCorpus.GetMonograms().Total:N0} total");
                                    System.Console.WriteLine($"  Bigrams: {loadedCorpus.GetBigrams().UniqueCount} unique, {loadedCorpus.GetBigrams().Total:N0} total");
                                    System.Console.WriteLine($"  Trigrams: {loadedCorpus.GetTrigrams().UniqueCount} unique, {loadedCorpus.GetTrigrams().Total:N0} total");
                                    System.Console.WriteLine($"  Words: {loadedCorpus.GetWords().UniqueCount} unique, {loadedCorpus.GetWords().Total:N0} total");

                                    // Update n-gram table with loaded corpus
                                    UpdateNgramTable();

                                    // Notify layout tab that corpus changed (for heatmap updates)
                                    NotifyCorpusChanged();
                                }
                                catch (Exception ex)
                                {
                                    System.Console.WriteLine($"Error loading corpus: {ex.Message}");
                                    loadedCorpus = null;
                                    UpdateNgramTable();

                                    // Notify layout tab that corpus changed (for heatmap updates)
                                    NotifyCorpusChanged();

                                    // Hide save CSV button on error
                                    if (saveCsvButton != null)
                                    {
                                        saveCsvButton.IsVisible = false;
                                    }
                                }
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

        private List<(int start, int length)> CalculateMatchPositions(string displayText, string originalSequence)
        {
            var matchPositions = new List<(int, int)>();
            
            if (string.IsNullOrEmpty(searchText) || string.IsNullOrEmpty(originalSequence))
                return matchPositions;

            try
            {
                if (useRegex)
                {
                    // For regex mode, search the original sequence to get match positions
                    var regex = new Regex(searchText, RegexOptions.Compiled | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100));
                    var matches = regex.Matches(originalSequence);
                    
                    // Map match positions from original sequence to display text
                    // Display text format: "escaped_sequence" (quotes + escaped chars)
                    string escapedSequence = EscapeSpecialChars(originalSequence);
                    
                    foreach (Match match in matches)
                    {
                        // Match is in originalSequence at (match.Index, match.Length)
                        // We need to find where this appears in the display text
                        // Display text: "\"escapedSequence\""
                        // Opening quote is at position 0, content starts at 1
                        
                        // Calculate start position in display text
                        // Start with 1 (after opening quote), then add length of escaped text before match
                        int displayStart = 1; // Skip opening quote
                        
                        // Calculate how many characters the prefix (before match) takes in escaped form
                        string prefix = originalSequence.Substring(0, match.Index);
                        string escapedPrefix = EscapeSpecialChars(prefix);
                        displayStart += escapedPrefix.Length;
                        
                        // Calculate match length in display text
                        string matchedText = originalSequence.Substring(match.Index, match.Length);
                        string escapedMatch = EscapeSpecialChars(matchedText);
                        int displayLength = escapedMatch.Length;
                        
                        matchPositions.Add((displayStart, displayLength));
                    }
                }
                else
                {
                    // For non-regex mode, find all occurrences in original sequence and map to display text
                    string searchLower = searchText.ToLowerInvariant();
                    string originalLower = originalSequence.ToLowerInvariant();
                    
                    int startIndex = 0;
                    while (true)
                    {
                        int index = originalLower.IndexOf(searchLower, startIndex, StringComparison.Ordinal);
                        if (index == -1)
                            break;
                        
                        // Map position from original to display text
                        int displayStart = 1; // Skip opening quote
                        string prefix = originalSequence.Substring(0, index);
                        string escapedPrefix = EscapeSpecialChars(prefix);
                        displayStart += escapedPrefix.Length;
                        
                        // Match length in display text (account for escaped chars)
                        string matchedText = originalSequence.Substring(index, searchText.Length);
                        string escapedMatch = EscapeSpecialChars(matchedText);
                        int displayLength = escapedMatch.Length;
                        
                        matchPositions.Add((displayStart, displayLength));
                        startIndex = index + 1;
                    }
                }
            }
            catch
            {
                // Invalid regex or other error - return empty list
            }
            
            return matchPositions;
        }

        private string EscapeSpecialChars(string text)
        {
            // Escape special characters for display
            return text
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t")
                .Replace("\0", "\\0")
                .Replace("\b", "\\b")
                .Replace("\f", "\\f")
                .Replace("\v", "\\v");
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

        /// <summary>
        /// Formats a finger sequence as a readable string with shortened finger names (e.g., "LP+LI", "LM+RM").
        /// </summary>
        private string FormatFingerSequence(List<Core.PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count == 0)
                return "";

            var fingerNames = sequence.Select(key => GetShortFingerName(key.Finger)).ToList();
            return string.Join("+", fingerNames);
        }

        /// <summary>
        /// Gets the shortened 2-character name for a finger.
        /// </summary>
        private string GetShortFingerName(Core.Finger finger)
        {
            return finger switch
            {
                Core.Finger.LeftPinky => "LP",
                Core.Finger.LeftRing => "LR",
                Core.Finger.LeftMiddle => "LM",
                Core.Finger.LeftIndex => "LI",
                Core.Finger.LeftThumb => "LT",
                Core.Finger.RightThumb => "RT",
                Core.Finger.RightIndex => "RI",
                Core.Finger.RightMiddle => "RM",
                Core.Finger.RightRing => "RR",
                Core.Finger.RightPinky => "RP",
                _ => "??"
            };
        }

        /// <summary>
        /// Formats a key sequence as a readable string (e.g., "A", "LShift+A", "Space").
        /// Truncates to a maximum length to prevent overflow in the table.
        /// </summary>
        private string FormatKeySequence(List<PhysicalKey> sequence)
        {
            const int MaxLength = 50; // Maximum characters to display before truncation

            if (sequence == null || sequence.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (var key in sequence)
            {
                // Check if it's a modifier key (LShift)
                if (key.Identifier == "LShift")
                {
                    parts.Add("Shift");
                }
                else
                {
                    // Use identifier if available, otherwise use primary character
                    string keyName = !string.IsNullOrEmpty(key.Identifier) ? key.Identifier : key.PrimaryCharacter ?? "?";
                    parts.Add(keyName);
                }
            }

            string result = string.Join("+", parts);
            
            // Truncate if too long
            if (result.Length > MaxLength)
            {
                result = result.Substring(0, MaxLength - 3) + "...";
            }

            return result;
        }
    }
}

