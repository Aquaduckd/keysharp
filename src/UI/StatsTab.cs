using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class StatsTab
    {
        private Components.TabContent tabContent;
        private Font font;

        private Components.Container? statsMainContainer;
        private Components.Container? statsHeaderContainer;
        private Components.Label? statsHeaderLabel;
        private Components.Container? statsControlsContainer;
        private Components.Container? statsContentContainer;
        
        private Components.Checkbox? ignoreWhitespaceCheckbox;
        private Components.Checkbox? ignoreModifiersCheckbox;
        private Components.Checkbox? ignoreNumbersSymbolsCheckbox;
        private Components.Checkbox? ignorePunctuationCheckbox;
        private Components.Checkbox? ignoreDisabledCheckbox;
        
        private bool ignoreWhitespace = false;
        private bool ignoreModifiers = false;
        private bool ignoreNumbersSymbols = false;
        private bool ignorePunctuation = false;
        private bool ignoreDisabled = false;
        
        private CorpusTab? corpusTab;
        private bool statsNeedUpdate = true; // Track if stats need recomputation
        private bool shouldUpdateBaseline = false; // Track if baseline should be updated after recomputation
        
        // Baseline metric counts for comparison (stored per ngram type)
        private Dictionary<string, long>? baselineMonogramCounts = null;
        private Dictionary<string, long>? baselineBigramCounts = null;
        private Dictionary<string, long>? baselineSkipgramCounts = null;
        private Dictionary<string, long>? baselineTrigramCounts = null;
        
        // Baseline totals for frequency calculation
        private long? baselineMonogramTotal = null;
        private long? baselineBigramTotal = null;
        private long? baselineSkipgramTotal = null;
        private long? baselineTrigramTotal = null;

        public Components.TabContent TabContent => tabContent;

        public CorpusTab? CorpusTab
        {
            get => corpusTab;
            set
            {
                // Unsubscribe from old corpus tab's callbacks
                if (corpusTab != null)
                {
                    corpusTab.OnMetricAnalyzerUpdated = null;
                }
                
                corpusTab = value;
                statsNeedUpdate = true;
                
                // Subscribe to new corpus tab's callbacks
                if (corpusTab != null)
                {
                    corpusTab.OnMetricAnalyzerUpdated = () => 
                    { 
                        // When layout changes, mark that we need to update the baseline
                        // This will be done after recomputing stats in UpdateStats
                        shouldUpdateBaseline = true;
                        statsNeedUpdate = true;
                    };
                }
            }
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update fonts in components (would need UpdateFont methods)
        }

        public StatsTab(Font font)
        {
            this.font = font;
            tabContent = new Components.TabContent(font, "", null);
            tabContent.PositionMode = Components.PositionMode.Relative;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create main vertical container
            statsMainContainer = new Components.Container("StatsMain");
            statsMainContainer.AutoLayoutChildren = true;
            statsMainContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            statsMainContainer.AutoSize = true;
            statsMainContainer.ChildPadding = 20;
            statsMainContainer.ChildGap = 15;
            tabContent.AddChild(statsMainContainer);

            // Create header container
            statsHeaderContainer = new Components.Container("StatsHeader");
            statsHeaderContainer.AutoSize = true;
            statsHeaderContainer.ChildPadding = 0;
            statsMainContainer.AddChild(statsHeaderContainer);

            // Create header label
            statsHeaderLabel = new Components.Label(font, "Stats", 24);
            statsHeaderLabel.Bounds = new Rectangle(0, 0, 0, 40);
            statsHeaderContainer.AddChild(statsHeaderLabel);

            // Create controls container
            statsControlsContainer = new Components.Container("StatsControls");
            statsControlsContainer.AutoLayoutChildren = true;
            statsControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            statsControlsContainer.ChildJustification = Components.ChildJustification.Left;
            statsControlsContainer.ChildGap = 10;
            statsControlsContainer.ChildPadding = 0;
            statsControlsContainer.Bounds = new Rectangle(0, 0, 0, 35);
            statsMainContainer.AddChild(statsControlsContainer);

            // Create checkboxes for filtering
            ignoreWhitespaceCheckbox = new Components.Checkbox(font, "Ignore Whitespace", 14);
            ignoreWhitespaceCheckbox.Bounds = new Rectangle(0, 0, 155, 35);
            ignoreWhitespaceCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignoreWhitespaceCheckbox.IsChecked = ignoreWhitespace;
            ignoreWhitespaceCheckbox.OnCheckedChanged = (isChecked) => { ignoreWhitespace = isChecked; statsNeedUpdate = true; };
            statsControlsContainer.AddChild(ignoreWhitespaceCheckbox);

            ignoreModifiersCheckbox = new Components.Checkbox(font, "Ignore Modifiers", 14);
            ignoreModifiersCheckbox.Bounds = new Rectangle(0, 0, 145, 35);
            ignoreModifiersCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignoreModifiersCheckbox.IsChecked = ignoreModifiers;
            ignoreModifiersCheckbox.OnCheckedChanged = (isChecked) => { ignoreModifiers = isChecked; statsNeedUpdate = true; };
            statsControlsContainer.AddChild(ignoreModifiersCheckbox);

            ignoreNumbersSymbolsCheckbox = new Components.Checkbox(font, "Ignore Numbers & Symbols", 14);
            ignoreNumbersSymbolsCheckbox.Bounds = new Rectangle(0, 0, 200, 35);
            ignoreNumbersSymbolsCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignoreNumbersSymbolsCheckbox.IsChecked = ignoreNumbersSymbols;
            ignoreNumbersSymbolsCheckbox.OnCheckedChanged = (isChecked) => { ignoreNumbersSymbols = isChecked; statsNeedUpdate = true; };
            statsControlsContainer.AddChild(ignoreNumbersSymbolsCheckbox);

            ignorePunctuationCheckbox = new Components.Checkbox(font, "Ignore Punctuation", 14);
            ignorePunctuationCheckbox.Bounds = new Rectangle(0, 0, 160, 35);
            ignorePunctuationCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignorePunctuationCheckbox.IsChecked = ignorePunctuation;
            ignorePunctuationCheckbox.OnCheckedChanged = (isChecked) => { ignorePunctuation = isChecked; statsNeedUpdate = true; };
            statsControlsContainer.AddChild(ignorePunctuationCheckbox);

            ignoreDisabledCheckbox = new Components.Checkbox(font, "Ignore Disabled", 14);
            ignoreDisabledCheckbox.Bounds = new Rectangle(0, 0, 140, 35);
            ignoreDisabledCheckbox.PositionMode = Components.PositionMode.Absolute;
            ignoreDisabledCheckbox.IsChecked = ignoreDisabled;
            ignoreDisabledCheckbox.OnCheckedChanged = (isChecked) => { ignoreDisabled = isChecked; statsNeedUpdate = true; };
            ignoreDisabledCheckbox.IsVisible = false; // Hidden by default, shown only if disabled keys exist
            statsControlsContainer.AddChild(ignoreDisabledCheckbox);

            // Create content container
            statsContentContainer = new Components.Container("StatsContent");
            statsContentContainer.AutoLayoutChildren = true;
            statsContentContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            statsContentContainer.AutoSize = true;
            statsContentContainer.ChildPadding = 0;
            statsContentContainer.ChildGap = 20;
            statsMainContainer.AddChild(statsContentContainer);
        }

        public void Update(Rectangle contentArea)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
            
            // Update main container bounds
            if (statsMainContainer != null)
            {
                statsMainContainer.Bounds = new Rectangle(0, 0, (int)contentArea.Width, (int)contentArea.Height);
                statsMainContainer.TargetHeight = contentArea.Height;
                
                // Calculate available width accounting for parent padding
                int availableWidth = (int)contentArea.Width - (int)(statsMainContainer.ChildPadding * 2.0f);
                
                // Update header container width
                if (statsHeaderContainer != null)
                {
                    int headerHeight = 40;
                    statsHeaderContainer.Bounds = new Rectangle(
                        statsHeaderContainer.Bounds.X,
                        statsHeaderContainer.Bounds.Y,
                        availableWidth,
                        headerHeight
                    );
                    statsHeaderContainer.IsVisible = true;
                    
                    // Update header label bounds
                    if (statsHeaderLabel != null)
                    {
                        statsHeaderLabel.Bounds = new Rectangle(
                            0, 0,
                            availableWidth,
                            headerHeight
                        );
                        statsHeaderLabel.IsVisible = true;
                    }
                }
                
                // Update controls container width
                if (statsControlsContainer != null)
                {
                    int controlsHeight = 35;
                    statsControlsContainer.Bounds = new Rectangle(
                        statsControlsContainer.Bounds.X,
                        statsControlsContainer.Bounds.Y,
                        availableWidth,
                        controlsHeight
                    );
                    statsControlsContainer.IsVisible = true;
                    
                    // Update ignore disabled checkbox visibility based on layout
                    var layout = corpusTab?.GetLayout();
                    if (ignoreDisabledCheckbox != null && layout != null)
                    {
                        bool hasDisabledKeys = layout.GetPhysicalKeys().Any(key => key.Disabled);
                        ignoreDisabledCheckbox.IsVisible = hasDisabledKeys;
                    }
                    else if (ignoreDisabledCheckbox != null)
                    {
                        ignoreDisabledCheckbox.IsVisible = false;
                    }
                }
                
                // Update content container width
                if (statsContentContainer != null)
                {
                    statsContentContainer.Bounds = new Rectangle(
                        statsContentContainer.Bounds.X,
                        statsContentContainer.Bounds.Y,
                        availableWidth,
                        statsContentContainer.Bounds.Height
                    );
                    statsContentContainer.IsVisible = true;
                    
                    // Calculate width per section container for horizontal layout
                    int sectionCount = statsContentContainer.Children.Count;
                    if (sectionCount > 0 && statsContentContainer.LayoutDirection == Components.LayoutDirection.Horizontal)
                    {
                        // Calculate width per section: (availableWidth - (gaps * (count-1)) - (padding * 2)) / count
                        float totalGaps = statsContentContainer.ChildGap * (sectionCount - 1);
                        float sectionWidth = (availableWidth - totalGaps - (statsContentContainer.ChildPadding * 2)) / sectionCount;
                        UpdateContainerAndTableWidths(statsContentContainer, (int)sectionWidth);
                    }
                    else
                    {
                        // Vertical layout: use full width
                        UpdateContainerAndTableWidths(statsContentContainer, availableWidth);
                    }
                }
            }
        }
        
        private void UpdateContainerAndTableWidths(Components.Container? container, int width)
        {
            if (container == null)
                return;
            
            foreach (var child in container.Children)
            {
                if (child is Components.Table table)
                {
                    // Update table width (preserve height)
                    // Account for section container padding
                    int tableWidth = width - (int)(container.ChildPadding * 2.0f);
                    table.Bounds = new Rectangle(table.Bounds.X, table.Bounds.Y, tableWidth, table.Bounds.Height);
                    table.IsVisible = true;
                }
                else if (child is Components.Container childContainer)
                {
                    // Update section container width (preserve height if it has one, otherwise calculate)
                    int containerHeight = childContainer.Bounds.Height > 0 ? (int)childContainer.Bounds.Height : 100; // Temporary height
                    childContainer.Bounds = new Rectangle(
                        childContainer.Bounds.X,
                        childContainer.Bounds.Y,
                        width,
                        containerHeight
                    );
                    childContainer.IsVisible = true;
                    
                    // Recursively update tables in child containers
                    // For horizontal layout (like statsContentContainer), childWidth = width
                    // For vertical layout (like section containers), childWidth = width - padding
                    int childWidth = width;
                    if (childContainer.LayoutDirection == Components.LayoutDirection.Vertical)
                    {
                        childWidth = width - (int)(childContainer.ChildPadding * 2.0f);
                    }
                    UpdateContainerAndTableWidths(childContainer, childWidth);
                }
                else
                {
                    child.IsVisible = true;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }

        public void UpdateStats()
        {
            if (statsContentContainer == null)
                return;

            // Clear existing stats content
            statsContentContainer.Children.Clear();

            // Get metric analyzer, corpus, and layout from corpus tab
            var metricAnalyzer = corpusTab?.GetMetricAnalyzer();
            var corpus = corpusTab?.LoadedCorpus;
            var layout = corpusTab?.GetLayout();
            
            if (metricAnalyzer == null || corpus == null || layout == null)
            {
                // Show message when no data available
                var noDataLabel = new Components.Label(font, "Load a corpus and layout to see statistics.", 16);
                noDataLabel.Bounds = new Rectangle(0, 0, 0, 30);
                statsContentContainer.AddChild(noDataLabel);
                return;
            }

            // Compute filtered metric counts based on filter settings
            // Use MetricAnalyzer like CorpusTab does, but filter ngrams first
            var monogramCounts = ComputeFilteredMetricCounts(corpus, layout, metricAnalyzer, "monogram");
            var bigramCounts = ComputeFilteredMetricCounts(corpus, layout, metricAnalyzer, "bigram");
            var skipgramCounts = ComputeFilteredMetricCounts(corpus, layout, metricAnalyzer, "skipgram");
            var trigramCounts = ComputeFilteredMetricCounts(corpus, layout, metricAnalyzer, "trigram");
            
            // Get current totals
            var currentMonogramTotal = GetFilteredTotal(corpus, "monogram");
            var currentBigramTotal = GetFilteredTotal(corpus, "bigram");
            var currentSkipgramTotal = GetFilteredTotal(corpus, "skipgram");
            var currentTrigramTotal = GetFilteredTotal(corpus, "trigram");
            
            // Set baseline on first computation if not set
            if (baselineMonogramCounts == null)
            {
                baselineMonogramCounts = new Dictionary<string, long>(monogramCounts);
                baselineBigramCounts = new Dictionary<string, long>(bigramCounts);
                baselineSkipgramCounts = new Dictionary<string, long>(skipgramCounts);
                baselineTrigramCounts = new Dictionary<string, long>(trigramCounts);
                baselineMonogramTotal = currentMonogramTotal;
                baselineBigramTotal = currentBigramTotal;
                baselineSkipgramTotal = currentSkipgramTotal;
                baselineTrigramTotal = currentTrigramTotal;
            }
            
            // If baseline should be updated (layout changed), save the OLD baseline temporarily
            // so we can show changes from it, then update baseline to new state after displaying
            Dictionary<string, long>? oldBaselineMonogram = null;
            Dictionary<string, long>? oldBaselineBigram = null;
            Dictionary<string, long>? oldBaselineSkipgram = null;
            Dictionary<string, long>? oldBaselineTrigram = null;
            long? oldBaselineMonogramTotal = null;
            long? oldBaselineBigramTotal = null;
            long? oldBaselineSkipgramTotal = null;
            long? oldBaselineTrigramTotal = null;
            
            if (shouldUpdateBaseline)
            {
                // Save old baseline for comparison (display changes from old baseline)
                oldBaselineMonogram = baselineMonogramCounts;
                oldBaselineBigram = baselineBigramCounts;
                oldBaselineSkipgram = baselineSkipgramCounts;
                oldBaselineTrigram = baselineTrigramCounts;
                oldBaselineMonogramTotal = baselineMonogramTotal;
                oldBaselineBigramTotal = baselineBigramTotal;
                oldBaselineSkipgramTotal = baselineSkipgramTotal;
                oldBaselineTrigramTotal = baselineTrigramTotal;
                
                // Update baseline to new state (for next change comparison)
                baselineMonogramCounts = new Dictionary<string, long>(monogramCounts);
                baselineBigramCounts = new Dictionary<string, long>(bigramCounts);
                baselineSkipgramCounts = new Dictionary<string, long>(skipgramCounts);
                baselineTrigramCounts = new Dictionary<string, long>(trigramCounts);
                baselineMonogramTotal = currentMonogramTotal;
                baselineBigramTotal = currentBigramTotal;
                baselineSkipgramTotal = currentSkipgramTotal;
                baselineTrigramTotal = currentTrigramTotal;
                shouldUpdateBaseline = false; // Reset flag
            }
            
            // Use old baseline for display if we just updated (to show changes from previous state)
            var displayBaselineMonogram = oldBaselineMonogram ?? baselineMonogramCounts;
            var displayBaselineBigram = oldBaselineBigram ?? baselineBigramCounts;
            var displayBaselineSkipgram = oldBaselineSkipgram ?? baselineSkipgramCounts;
            var displayBaselineTrigram = oldBaselineTrigram ?? baselineTrigramCounts;
            var displayBaselineMonogramTotal = oldBaselineMonogramTotal ?? baselineMonogramTotal;
            var displayBaselineBigramTotal = oldBaselineBigramTotal ?? baselineBigramTotal;
            var displayBaselineSkipgramTotal = oldBaselineSkipgramTotal ?? baselineSkipgramTotal;
            var displayBaselineTrigramTotal = oldBaselineTrigramTotal ?? baselineTrigramTotal;

            // Add monograms first (order: LP, LR, LM, LI, LT, RT, RI, RM, RR, RP)
            var monogramOrder = new List<string> { "LeftPinky", "LeftRing", "LeftMiddle", "LeftIndex", "LeftThumb", "RightThumb", "RightIndex", "RightMiddle", "RightRing", "RightPinky" };
            DisplayNgramMetrics("Monograms", monogramCounts, displayBaselineMonogram,
                currentMonogramTotal, displayBaselineMonogramTotal,
                statsContentContainer, monogramOrder);
            
            // Combine bigrams and skipgrams into one table
            // Order: SFB, LSB, HSB, FSB (bigrams), then SFS, LSS, HSS, FSS (skipgrams)
            DisplayCombinedBigramSkipgramMetrics("Bigrams & Skipgrams", bigramCounts, skipgramCounts,
                displayBaselineBigram, displayBaselineSkipgram,
                currentBigramTotal, currentSkipgramTotal,
                displayBaselineBigramTotal, displayBaselineSkipgramTotal,
                statsContentContainer);
            
            // Add trigrams third (order: InRoll, OutRoll, InHand, OutHand, Redirect, Alternate)
            var trigramOrder = new List<string> { "InRoll", "OutRoll", "InHand", "OutHand", "Redirect", "Alternate" };
            DisplayNgramMetrics("Trigrams", trigramCounts, displayBaselineTrigram,
                currentTrigramTotal, displayBaselineTrigramTotal,
                statsContentContainer, trigramOrder);
            
            // After creating all tables, we need to update their widths if main container is already sized
            if (statsMainContainer != null && statsMainContainer.Bounds.Width > 0)
            {
                int availableWidth = (int)statsMainContainer.Bounds.Width - (int)(statsMainContainer.ChildPadding * 2.0f);
                
                // Update content container width
                if (statsContentContainer != null)
                {
                    statsContentContainer.Bounds = new Rectangle(
                        statsContentContainer.Bounds.X,
                        statsContentContainer.Bounds.Y,
                        availableWidth,
                        statsContentContainer.Bounds.Height
                    );
                    
                    // Calculate width per section container for horizontal layout
                    // We need to distribute available width across all section containers
                    int sectionCount = statsContentContainer.Children.Count;
                    if (sectionCount > 0 && statsContentContainer.LayoutDirection == Components.LayoutDirection.Horizontal)
                    {
                        // Calculate width per section: (availableWidth - (gaps * (count-1)) - (padding * 2)) / count
                        float totalGaps = statsContentContainer.ChildGap * (sectionCount - 1);
                        float sectionWidth = (availableWidth - totalGaps - (statsContentContainer.ChildPadding * 2)) / sectionCount;
                        
                        // Update section container widths and table widths
                        UpdateContainerAndTableWidths(statsContentContainer, (int)sectionWidth);
                    }
                    else
                    {
                        // Vertical layout: use full width
                        UpdateContainerAndTableWidths(statsContentContainer, availableWidth);
                    }
                }
                
                // After setting widths, we need to trigger Update() which calls LayoutChildren()
                // This positions children correctly in containers with AutoLayoutChildren
                // Then ResolveBounds() to recalculate AutoSize heights
                if (statsContentContainer != null)
                {
                    statsContentContainer.Update();
                    statsContentContainer.ResolveBounds();
                }
            }
        }

        private void DisplayNgramMetrics(string title, IReadOnlyDictionary<string, long> metricCounts, 
            Dictionary<string, long>? baselineCounts,
            long currentTotal, long? baselineTotal,
            Components.Container parentContainer, List<string>? metricOrder = null)
        {
            // Create section container
            var sectionContainer = new Components.Container($"Section_{title}");
            sectionContainer.AutoLayoutChildren = true;
            sectionContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            sectionContainer.AutoSize = true;
            sectionContainer.ChildPadding = 0;
            sectionContainer.ChildGap = 8;
            sectionContainer.PositionMode = Components.PositionMode.Relative; // Let parent auto-layout handle positioning
            parentContainer.AddChild(sectionContainer);

            // Create section title
            var titleLabel = new Components.Label(font, title, 20);
            titleLabel.Bounds = new Rectangle(0, 0, 0, 30);
            titleLabel.IsVisible = true;
            titleLabel.PositionMode = Components.PositionMode.Relative;
            sectionContainer.AddChild(titleLabel);

            // Create table for metrics with Change column
            var table = new Components.Table(font, 14, "Metric", "Frequency", "Change");
            table.AutoSize = false; // Width and height will be set manually
            table.IsVisible = true;
            table.PositionMode = Components.PositionMode.Relative;
            
            // Initialize cell colors list (for Change column coloring)
            table.CellColors = new List<List<Raylib_cs.Color?>>();
            
            // If metricOrder is provided, use it; otherwise use the dictionary order
            if (metricOrder != null && metricOrder.Count > 0)
            {
                // Display metrics in the specified order, showing 0 for missing ones
                foreach (var metricKey in metricOrder)
                {
                    long count = metricCounts != null && metricCounts.TryGetValue(metricKey, out var value) ? value : 0;
                    double frequency = currentTotal > 0 ? (double)count / currentTotal * 100.0 : 0.0;
                    
                    // Format metric key for display (convert finger names to short form, and skipgram suffixes)
                    string displayKey = FormatMetricKey(metricKey, title == "Skipgrams");
                    
                    // Calculate frequency change from baseline
                    long baselineCount = baselineCounts != null && baselineCounts.TryGetValue(metricKey, out var baselineValue) ? baselineValue : 0;
                    double baselineFrequency = (baselineTotal.HasValue && baselineTotal.Value > 0) ? (double)baselineCount / baselineTotal.Value * 100.0 : 0.0;
                    double frequencyChange = frequency - baselineFrequency;
                    string changeStr = frequencyChange == 0 ? "0.000%" : (frequencyChange > 0 ? $"+{frequencyChange:F3}%" : $"{frequencyChange:F3}%");
                    
                    table.Rows.Add(new List<string>
                    {
                        displayKey,
                        $"{frequency:F3}%",
                        changeStr
                    });
                    
                    // Set color for Change column (column index 2, 0-based)
                    var rowColors = new List<Raylib_cs.Color?> { null, null, null };
                    if (frequencyChange > 0)
                    {
                        rowColors[2] = new Raylib_cs.Color(0, 255, 255, 255); // Cyan
                    }
                    else if (frequencyChange < 0)
                    {
                        rowColors[2] = new Raylib_cs.Color(255, 165, 0, 255); // Orange
                    }
                    table.CellColors.Add(rowColors);
                }
            }
            else
            {
                // Fallback to original behavior if no order specified
                if (metricCounts != null && metricCounts.Count > 0)
                {
                    foreach (var kvp in metricCounts.OrderByDescending(x => x.Value))
                    {
                        double frequency = currentTotal > 0 ? (double)kvp.Value / currentTotal * 100.0 : 0.0;
                        string displayKey = FormatMetricKey(kvp.Key, title == "Skipgrams");
                        
                        // Calculate frequency change from baseline
                        long baselineCount = baselineCounts != null && baselineCounts.TryGetValue(kvp.Key, out var baselineValue) ? baselineValue : 0;
                        double baselineFrequency = (baselineTotal.HasValue && baselineTotal.Value > 0) ? (double)baselineCount / baselineTotal.Value * 100.0 : 0.0;
                        double frequencyChange = frequency - baselineFrequency;
                        string changeStr = frequencyChange == 0 ? "0.000%" : (frequencyChange > 0 ? $"+{frequencyChange:F3}%" : $"{frequencyChange:F3}%");
                        
                        table.Rows.Add(new List<string>
                        {
                            displayKey,
                            $"{frequency:F3}%",
                            changeStr
                        });
                        
                        // Set color for Change column (column index 2, 0-based)
                        var rowColors = new List<Raylib_cs.Color?> { null, null, null };
                        if (frequencyChange > 0)
                        {
                            rowColors[2] = new Raylib_cs.Color(0, 255, 255, 255); // Cyan
                        }
                        else if (frequencyChange < 0)
                        {
                            rowColors[2] = new Raylib_cs.Color(255, 165, 0, 255); // Orange
                        }
                        table.CellColors.Add(rowColors);
                    }
                }
            }
            
            // Calculate table height: HeaderHeight (28) + (rows * RowHeight (22))
            // Add some extra padding at the bottom
            const int HeaderHeight = 28;
            const int RowHeight = 22;
            int rowCount = metricOrder != null ? metricOrder.Count : (metricCounts?.Count ?? 0);
            int tableHeight = HeaderHeight + (rowCount * RowHeight) + 2;
            table.Bounds = new Rectangle(0, 0, 0, tableHeight); // Width will be set by Update method
            
            sectionContainer.AddChild(table);
        }
        
        private void DisplayCombinedBigramSkipgramMetrics(string title, 
            IReadOnlyDictionary<string, long> bigramCounts,
            IReadOnlyDictionary<string, long> skipgramCounts,
            Dictionary<string, long>? baselineBigramCounts,
            Dictionary<string, long>? baselineSkipgramCounts,
            long currentBigramTotal, long currentSkipgramTotal,
            long? baselineBigramTotal, long? baselineSkipgramTotal,
            Components.Container parentContainer)
        {
            // Create section container
            var sectionContainer = new Components.Container($"Section_{title}");
            sectionContainer.AutoLayoutChildren = true;
            sectionContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            sectionContainer.AutoSize = true;
            sectionContainer.ChildPadding = 0;
            sectionContainer.ChildGap = 8;
            sectionContainer.PositionMode = Components.PositionMode.Relative;
            parentContainer.AddChild(sectionContainer);

            // Create section title
            var titleLabel = new Components.Label(font, title, 20);
            titleLabel.Bounds = new Rectangle(0, 0, 0, 30);
            titleLabel.IsVisible = true;
            titleLabel.PositionMode = Components.PositionMode.Relative;
            sectionContainer.AddChild(titleLabel);

            // Create table for metrics with Change column
            var table = new Components.Table(font, 14, "Metric", "Frequency", "Change");
            table.AutoSize = false;
            table.IsVisible = true;
            table.PositionMode = Components.PositionMode.Relative;
            
            // Initialize cell colors list (for Change column coloring)
            table.CellColors = new List<List<Raylib_cs.Color?>>();
            
            // Order: SFB, LSB, HSB, FSB (bigrams), then SFS, LSS, HSS, FSS (skipgrams)
            var bigramOrder = new List<string> { "SFB", "LSB", "HSB", "FSB" };
            
            // Add bigram metrics
            foreach (var metricKey in bigramOrder)
            {
                long count = bigramCounts != null && bigramCounts.TryGetValue(metricKey, out var value) ? value : 0;
                double frequency = currentBigramTotal > 0 ? (double)count / currentBigramTotal * 100.0 : 0.0;
                
                string displayKey = FormatMetricKey(metricKey, false); // false = bigram, use "B" suffix
                
                // Calculate frequency change from baseline
                long baselineCount = baselineBigramCounts != null && baselineBigramCounts.TryGetValue(metricKey, out var baselineValue) ? baselineValue : 0;
                double baselineFrequency = (baselineBigramTotal.HasValue && baselineBigramTotal.Value > 0) ? (double)baselineCount / baselineBigramTotal.Value * 100.0 : 0.0;
                double frequencyChange = frequency - baselineFrequency;
                string changeStr = frequencyChange == 0 ? "0.000%" : (frequencyChange > 0 ? $"+{frequencyChange:F3}%" : $"{frequencyChange:F3}%");
                
                table.Rows.Add(new List<string>
                {
                    displayKey,
                    $"{frequency:F3}%",
                    changeStr
                });
                
                // Set color for Change column (column index 2, 0-based)
                var rowColors = new List<Raylib_cs.Color?> { null, null, null };
                if (frequencyChange > 0)
                {
                    rowColors[2] = new Raylib_cs.Color(0, 255, 255, 255); // Cyan
                }
                else if (frequencyChange < 0)
                {
                    rowColors[2] = new Raylib_cs.Color(255, 165, 0, 255); // Orange
                }
                table.CellColors.Add(rowColors);
            }
            
            // Add skipgram metrics
            foreach (var metricKey in bigramOrder) // Same order, but will be displayed with "S" suffix
            {
                long count = skipgramCounts != null && skipgramCounts.TryGetValue(metricKey, out var value) ? value : 0;
                double frequency = currentSkipgramTotal > 0 ? (double)count / currentSkipgramTotal * 100.0 : 0.0;
                
                string displayKey = FormatMetricKey(metricKey, true); // true = skipgram, use "S" suffix
                
                // Calculate frequency change from baseline
                long baselineCount = baselineSkipgramCounts != null && baselineSkipgramCounts.TryGetValue(metricKey, out var baselineValue) ? baselineValue : 0;
                double baselineFrequency = (baselineSkipgramTotal.HasValue && baselineSkipgramTotal.Value > 0) ? (double)baselineCount / baselineSkipgramTotal.Value * 100.0 : 0.0;
                double frequencyChange = frequency - baselineFrequency;
                string changeStr = frequencyChange == 0 ? "0.000%" : (frequencyChange > 0 ? $"+{frequencyChange:F3}%" : $"{frequencyChange:F3}%");
                
                table.Rows.Add(new List<string>
                {
                    displayKey,
                    $"{frequency:F3}%",
                    changeStr
                });
                
                // Set color for Change column (column index 2, 0-based)
                var rowColors = new List<Raylib_cs.Color?> { null, null, null };
                if (frequencyChange > 0)
                {
                    rowColors[2] = new Raylib_cs.Color(0, 255, 255, 255); // Cyan
                }
                else if (frequencyChange < 0)
                {
                    rowColors[2] = new Raylib_cs.Color(255, 165, 0, 255); // Orange
                }
                table.CellColors.Add(rowColors);
            }
            
            // Calculate table height: HeaderHeight (28) + (rows * RowHeight (22))
            const int HeaderHeight = 28;
            const int RowHeight = 22;
            int tableHeight = HeaderHeight + (table.Rows.Count * RowHeight) + 2;
            table.Bounds = new Rectangle(0, 0, 0, tableHeight);
            
            sectionContainer.AddChild(table);
        }
        
        private string FormatMetricKey(string key, bool isSkipgram = false)
        {
            // Convert finger names to short form (LP, LR, LM, LI, LT, RT, RI, RM, RR, RP)
            if (key == "LeftPinky") return "LP";
            if (key == "LeftRing") return "LR";
            if (key == "LeftMiddle") return "LM";
            if (key == "LeftIndex") return "LI";
            if (key == "LeftThumb") return "LT";
            if (key == "RightThumb") return "RT";
            if (key == "RightIndex") return "RI";
            if (key == "RightMiddle") return "RM";
            if (key == "RightRing") return "RR";
            if (key == "RightPinky") return "RP";
            
            // For skipgrams, replace "B" suffix with "S" suffix
            if (isSkipgram)
            {
                if (key == "SFB") return "SFS";
                if (key == "LSB") return "LSS";
                if (key == "HSB") return "HSS";
                if (key == "FSB") return "FSS";
            }
            
            // Return as-is for other metrics
            return key;
        }
        
        private Dictionary<string, long> ComputeFilteredMetricCounts(Core.Corpus corpus, Core.Layout? layout, Core.MetricAnalyzer? metricAnalyzer, string ngramType)
        {
            var filteredCounts = new Dictionary<string, long>();
            
            if (layout == null || metricAnalyzer == null)
                return filteredCounts;
            
            // Get the appropriate ngrams based on type
            Core.Grams ngrams = ngramType switch
            {
                "monogram" => corpus.GetMonograms(),
                "bigram" => corpus.GetBigrams(),
                "skipgram" => corpus.GetSkipgrams(),
                "trigram" => corpus.GetTrigrams(),
                _ => corpus.GetMonograms()
            };
            
            // Iterate through ngrams and filter
            foreach (var kvp in ngrams.Counts)
            {
                string sequence = kvp.Key;
                long count = kvp.Value;
                
                // Apply filters
                if (!ShouldIncludeNgram(sequence, layout))
                    continue;
                
                // Get metric result from MetricAnalyzer (like CorpusTab does)
                var result = metricAnalyzer.GetNgramResult(sequence, ngramType);
                if (result == null || result.MetricMatches == null || result.MetricMatches.Count == 0)
                    continue;
                
                // Add metric matches from the analyzer result
                foreach (var metric in result.MetricMatches)
                {
                    IncrementMetricCount(filteredCounts, metric, count);
                }
            }
            
            return filteredCounts;
        }
        
        private bool ShouldIncludeNgram(string sequence, Core.Layout? layout)
        {
            if (layout == null)
                return false;
            
            // Ignore whitespace: filter out grams that contain any whitespace character
            if (ignoreWhitespace && sequence.Any(c => char.IsWhiteSpace(c)))
                return false;
            
            // Check the original sequence for numbers, symbols, and punctuation first
            // Ignore numbers & symbols
            if (ignoreNumbersSymbols && sequence.Any(c => char.IsDigit(c) || char.IsSymbol(c)))
                return false;
            
            // Ignore punctuation
            if (ignorePunctuation && sequence.Any(c => char.IsPunctuation(c)))
                return false;
            
            // Convert to key sequence to check for modifiers
            var keySequence = layout.ConvertNgramToKeySequence(sequence);
            if (keySequence == null || keySequence.Count == 0)
                return false;
            
            // Ignore modifiers: filter out sequences that contain modifier keys
            if (ignoreModifiers)
            {
                bool hasModifier = keySequence.Any(key => 
                    key.Identifier != null && (
                        key.Identifier.Contains("Shift", StringComparison.OrdinalIgnoreCase) ||
                        key.Identifier.Contains("Ctrl", StringComparison.OrdinalIgnoreCase) ||
                        key.Identifier.Contains("Alt", StringComparison.OrdinalIgnoreCase) ||
                        key.Identifier.Contains("Win", StringComparison.OrdinalIgnoreCase) ||
                        key.Identifier.Contains("Cmd", StringComparison.OrdinalIgnoreCase)));
                if (hasModifier)
                    return false;
            }
            
            // Ignore disabled: filter out sequences that contain disabled keys
            if (ignoreDisabled)
            {
                bool hasDisabled = keySequence.Any(key => key.Disabled);
                if (hasDisabled)
                    return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Marks that stats need to be recomputed. Called when filters or data changes.
        /// </summary>
        public void MarkStatsNeedUpdate()
        {
            statsNeedUpdate = true;
        }
        
        /// <summary>
        /// Checks if stats need updating and updates them if necessary.
        /// This should be called from Update() to ensure stats are only recomputed when needed.
        /// </summary>
        public void CheckAndUpdateStats()
        {
            if (statsNeedUpdate)
            {
                UpdateStats();
                statsNeedUpdate = false;
            }
        }
        
        private long GetFilteredTotal(Core.Corpus corpus, string ngramType)
        {
            // Get the appropriate ngrams based on type
            Core.Grams ngrams = ngramType switch
            {
                "monogram" => corpus.GetMonograms(),
                "bigram" => corpus.GetBigrams(),
                "skipgram" => corpus.GetSkipgrams(),
                "trigram" => corpus.GetTrigrams(),
                _ => corpus.GetMonograms()
            };
            
            var layout = corpusTab?.GetLayout();
            if (layout == null)
                return 0;
            
            // Count filtered ngrams
            long total = 0;
            foreach (var kvp in ngrams.Counts)
            {
                if (ShouldIncludeNgram(kvp.Key, layout))
                {
                    total += kvp.Value;
                }
            }
            
            return total;
        }
        
        private void IncrementMetricCount(Dictionary<string, long> counts, string metric, long amount)
        {
            if (counts.ContainsKey(metric))
            {
                counts[metric] += amount;
            }
            else
            {
                counts[metric] = amount;
            }
        }
    }
}

