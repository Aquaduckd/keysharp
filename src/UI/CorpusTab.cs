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
        private Components.Container? corpusControlsContainer;
        private Components.Container? corpusRowContainer;
        private Components.Container? corpusHeaderContainer;
        private Components.Container? corpusContentContainer;
        private Components.Container? corpusMainContainer;
        private Components.Container? ngramSelectorContainer;
        private Components.Label? corpusHeaderLabel;
        private Components.Dropdown? ngramSizeDropdown;
        private Components.Table? ngramTable;
        private Components.TextInput? limitInput;
        private Components.TextInput? searchInput;
        private Components.Button? regexToggleButton;
        private Components.Label? totalCountLabel;
        private string selectedNgramSize = "bigram"; // "monogram", "bigram", "trigram", or "words"
        private string searchText = "";
        private int? resultLimit = null; // Null means no limit
        private bool useRegex = false;

        public Components.TabContent TabContent => tabContent;
        public Components.Dropdown? CorpusDropdown => corpusDropdown;
        public Components.Dropdown? NgramSizeDropdown => ngramSizeDropdown;

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

            // Create label for n-gram selector
            var ngramLabel = new Components.Label(font, "N-gram Size:", 14);
            ngramLabel.Bounds = new Rectangle(0, 0, 120, 35);
            ngramLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(ngramLabel);

            // Create n-gram size dropdown
            List<string> ngramSizes = new List<string> { "Monogram", "Bigram", "Trigram", "Words" };
            ngramSizeDropdown = new Components.Dropdown(font, ngramSizes, 14);
            ngramSizeDropdown.SetBounds(new Rectangle(0, 0, 200, 35));
            ngramSizeDropdown.PositionMode = Components.PositionMode.Absolute;
            ngramSizeDropdown.OnSelectionChanged = OnNgramSizeSelected;
            // Set default selection to "Bigram"
            ngramSizeDropdown.SetSelectedItem("Bigram");
            leftControlsContainer.AddChild(ngramSizeDropdown);

            // Create limit label
            var limitLabel = new Components.Label(font, "Limit:", 14);
            limitLabel.Bounds = new Rectangle(0, 0, 60, 35);
            limitLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(limitLabel);

            // Create limit input
            limitInput = new Components.TextInput(font, "All", 14);
            limitInput.Bounds = new Rectangle(0, 0, 80, 35);
            limitInput.PositionMode = Components.PositionMode.Absolute;
            limitInput.OnTextChanged = OnLimitTextChanged;
            leftControlsContainer.AddChild(limitInput);

            // Create search label
            var searchLabel = new Components.Label(font, "Search:", 14);
            searchLabel.Bounds = new Rectangle(0, 0, 80, 35);
            searchLabel.PositionMode = Components.PositionMode.Absolute;
            leftControlsContainer.AddChild(searchLabel);

            // Create search input
            searchInput = new Components.TextInput(font, "Enter search term...", 14);
            searchInput.Bounds = new Rectangle(0, 0, 300, 35);
            searchInput.PositionMode = Components.PositionMode.Absolute;
            searchInput.OnTextChanged = OnSearchTextChanged;
            leftControlsContainer.AddChild(searchInput);

            // Create regex toggle button
            regexToggleButton = new Components.Button(font, "Regex: Off", 12);
            regexToggleButton.Bounds = new Rectangle(0, 0, 100, 35);
            regexToggleButton.PositionMode = Components.PositionMode.Absolute;
            regexToggleButton.OnClick = ToggleRegexMode;
            leftControlsContainer.AddChild(regexToggleButton);

            // Create total count label (right-aligned)
            totalCountLabel = new Components.Label(font, "", 14, null, rightAlign: true);
            totalCountLabel.Bounds = new Rectangle(0, 0, 250, 35);
            totalCountLabel.PositionMode = Components.PositionMode.Absolute;
            ngramSelectorContainer.AddChild(totalCountLabel);

            // Create n-gram table (Rank, N-gram, Frequency, Count, Global Rank, Rel. Freq.)
            ngramTable = new Components.Table(font, "Rank", "N-gram", "Frequency", "Count", "Global Rank", "Rel. Freq.", 14);
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
                }
            }
        }

        private void OnNgramSizeSelected(string ngramSize)
        {
            selectedNgramSize = ngramSize.ToLower();
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

        private void ToggleRegexMode()
        {
            useRegex = !useRegex;
            if (regexToggleButton != null)
            {
                regexToggleButton.Text = useRegex ? "Regex: On" : "Regex: Off";
            }
            UpdateNgramTable();
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

            // Calculate filtered total for relative frequency (only when filtered)
            bool isFiltered = !string.IsNullOrEmpty(searchText);
            long filteredTotal = 0;
            if (isFiltered && filteredNgrams.Count > 0)
            {
                filteredTotal = filteredNgrams.Sum(ng => ng.count);
            }

            // Store counts before applying limit (for display in total label)
            int filteredCountBeforeLimit = filteredNgrams.Count;
            long filteredTotalBeforeLimit = filteredTotal;

            // Apply limit if specified
            if (resultLimit.HasValue && resultLimit.Value > 0)
            {
                filteredNgrams = filteredNgrams.Take(resultLimit.Value).ToList();
                // Recalculate filtered total based on limited results
                filteredTotal = filteredNgrams.Sum(ng => ng.count);
            }

            // Show/hide conditional columns based on whether we're filtered
            ngramTable.ShowColumn5 = isFiltered; // Global Rank
            ngramTable.ShowColumn6 = isFiltered; // Relative Frequency

            // Update table rows
            ngramTable.Rows.Clear();
            for (int i = 0; i < filteredNgrams.Count; i++)
            {
                var ngram = filteredNgrams[i];
                // Rank is relative rank within filtered results (1-based)
                string rank = (i + 1).ToString();
                string ngramText = $"\"{EscapeSpecialChars(ngram.sequence)}\""; // Add quotation marks around n-gram and escape special chars
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

                // Calculate relative frequency (count / filtered total) if filtered (conditional, last column)
                string relativeFreq = "";
                if (isFiltered && filteredTotal > 0)
                {
                    double relFreq = (double)ngram.count / filteredTotal;
                    relativeFreq = $"{relFreq * 100:F3}%";
                }

                // Column order: Rank, N-gram, Frequency, Count, Global Rank, Relative Frequency
                ngramTable.Rows.Add((rank, ngramText, frequency, count, globalRank, relativeFreq));
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
                            var headers = new List<string> { ngramTable.Column1Header, ngramTable.Column2Header, ngramTable.Column3Header, ngramTable.Column4Header };
                            if (ngramTable.ShowColumn5)
                            {
                                headers.Add(ngramTable.Column5Header);
                            }
                            if (ngramTable.ShowColumn6)
                            {
                                headers.Add(ngramTable.Column6Header);
                            }
                            writer.WriteLine(string.Join(",", headers.Select(h => EscapeCsvField(h))));

                            // Write data rows
                            foreach (var row in ngramTable.Rows)
                            {
                                var fields = new List<string> { row.column1, row.column2, row.column3, row.column4 };
                                if (ngramTable.ShowColumn5)
                                {
                                    fields.Add(row.column5);
                                }
                                if (ngramTable.ShowColumn6)
                                {
                                    fields.Add(row.column6);
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
    }
}

