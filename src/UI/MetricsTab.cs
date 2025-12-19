using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class MetricsTab
    {
        private Components.TabContent tabContent;
        private Font font;

        // Metrics tab containers
        private Components.Container? metricsMainContainer;
        private Components.Container? metricsHeaderContainer;
        private Components.Container? metricsControlsContainer;
        private Components.Container? metricsContentContainer;
        private Components.Label? metricsHeaderLabel;
        private Components.Table? metricsTable;
        private Components.TextInput? ngramSearchInput;
        private Components.TextInput? metricSearchInput;

        // Data references
        private Layout? layout = null;
        private Corpus? corpus = null;

        // Search state
        private string ngramSearchText = "";
        private string metricSearchText = "";

        public Components.TabContent TabContent => tabContent;

        /// <summary>
        /// Sets the layout reference for key sequence conversion.
        /// </summary>
        public void SetLayout(Layout? layout)
        {
            this.layout = layout;
            UpdateMetricsTable();
        }

        /// <summary>
        /// Sets the corpus reference for bigram data.
        /// </summary>
        public void SetCorpus(Corpus? corpus)
        {
            this.corpus = corpus;
            UpdateMetricsTable();
        }

        public MetricsTab(Font font)
        {
            this.font = font;
            tabContent = new Components.TabContent(font, "", null); // Empty title - we'll use our own header
            tabContent.PositionMode = Components.PositionMode.Relative;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create main vertical container that wraps header, controls, and content
            metricsMainContainer = new Components.Container("MetricsMain");
            metricsMainContainer.AutoLayoutChildren = true;
            metricsMainContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            metricsMainContainer.AutoSize = false; // Size will be set explicitly in Update method
            metricsMainContainer.PositionMode = Components.PositionMode.Relative;
            metricsMainContainer.ChildPadding = 20;
            metricsMainContainer.ChildGap = 10;
            tabContent.AddChild(metricsMainContainer);

            // Create header container for metrics tab (will display centered text)
            metricsHeaderContainer = new Components.Container("MetricsHeader");
            metricsHeaderContainer.AutoSize = false; // Size will be set explicitly in Update
            metricsHeaderContainer.PositionMode = Components.PositionMode.Relative;
            metricsHeaderContainer.ChildPadding = 0; // No padding needed since label fills it
            metricsMainContainer.AddChild(metricsHeaderContainer);

            // Create header label with centered text
            metricsHeaderLabel = new Components.Label(font, "Metrics", 24);
            metricsHeaderLabel.Bounds = new Rectangle(0, 0, 0, 40); // Height for header text
            metricsHeaderContainer.AddChild(metricsHeaderLabel);

            // Create container for metrics controls bar
            metricsControlsContainer = new Components.Container("MetricsControls");
            metricsControlsContainer.AutoLayoutChildren = true;
            metricsControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            metricsControlsContainer.AutoSize = true; // Auto-size based on children + padding
            metricsControlsContainer.ChildJustification = Components.ChildJustification.Left;
            metricsControlsContainer.ChildGap = 10;
            metricsControlsContainer.ChildPadding = 0;
            metricsMainContainer.AddChild(metricsControlsContainer);

            // Create ngram search label
            var ngramSearchLabel = new Components.Label(font, "Ngram Search:", 14);
            ngramSearchLabel.Bounds = new Rectangle(0, 0, 120, 35);
            ngramSearchLabel.PositionMode = Components.PositionMode.Absolute;
            metricsControlsContainer.AddChild(ngramSearchLabel);

            // Create ngram search input
            ngramSearchInput = new Components.TextInput(font, "Enter ngram search term...", 14);
            ngramSearchInput.Bounds = new Rectangle(0, 0, 300, 35);
            ngramSearchInput.PositionMode = Components.PositionMode.Absolute;
            ngramSearchInput.OnTextChanged = OnNgramSearchTextChanged;
            metricsControlsContainer.AddChild(ngramSearchInput);

            // Create metric search label
            var metricSearchLabel = new Components.Label(font, "Metric Search:", 14);
            metricSearchLabel.Bounds = new Rectangle(0, 0, 120, 35);
            metricSearchLabel.PositionMode = Components.PositionMode.Absolute;
            metricsControlsContainer.AddChild(metricSearchLabel);

            // Create metric search input
            metricSearchInput = new Components.TextInput(font, "Enter metric search (e.g., SFB)...", 14);
            metricSearchInput.Bounds = new Rectangle(0, 0, 300, 35);
            metricSearchInput.PositionMode = Components.PositionMode.Absolute;
            metricSearchInput.OnTextChanged = OnMetricSearchTextChanged;
            metricsControlsContainer.AddChild(metricSearchInput);

            // Create content container for the rest of the metrics tab content
            metricsContentContainer = new Components.Container("MetricsContent");
            metricsContentContainer.AutoSize = false; // Size will be set by parent's fill-remaining logic
            metricsContentContainer.PositionMode = Components.PositionMode.Relative;
            metricsContentContainer.AutoLayoutChildren = true; // Enable auto-layout for table
            metricsContentContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            metricsContentContainer.ChildGap = 10;
            metricsContentContainer.ChildPadding = 0;
            metricsContentContainer.FillRemaining = true; // Fill remaining space in parent container
            metricsMainContainer.AddChild(metricsContentContainer);

            // Create metrics table (Bigram, Frequency, Key Sequence, Finger Sequence, Metric Matches)
            metricsTable = new Components.Table(font, 14, "Bigram", "Frequency", "Key Sequence", "Finger Sequence", "Metric Matches");
            metricsTable.Bounds = new Rectangle(0, 0, 0, 0); // Will be set by Update
            metricsTable.AutoSize = false;
            metricsTable.FillRemaining = true;
            metricsTable.PositionMode = Components.PositionMode.Relative;
            metricsContentContainer.AddChild(metricsTable);
        }

        public void Update(Rectangle contentArea, bool isActive)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);

            if (isActive)
            {
                // Update metrics containers if metrics tab is active
                const int headerHeight = 40; // Height for header text

                // Set main container bounds (fills parent, uses auto-layout)
                if (metricsMainContainer != null)
                {
                    metricsMainContainer.Bounds = new Rectangle(
                        0, 0,
                        (int)contentArea.Width,
                        (int)contentArea.Height
                    );
                    // Set target height so fill-remaining children can calculate their size
                    metricsMainContainer.TargetHeight = contentArea.Height;
                    metricsMainContainer.IsVisible = true;
                }

                // Calculate available width accounting for parent padding
                int availableWidth = (int)contentArea.Width;
                if (metricsMainContainer != null)
                {
                    availableWidth = (int)contentArea.Width - (int)(metricsMainContainer.ChildPadding * 2);
                }

                // Set header container bounds (width accounts for parent padding, fixed height for label)
                // Position will be handled by auto-layout
                if (metricsHeaderContainer != null)
                {
                    metricsHeaderContainer.Bounds = new Rectangle(0, 0, availableWidth, headerHeight);
                    metricsHeaderContainer.IsVisible = true;
                }

                // Update header label bounds (fills header container)
                if (metricsHeaderLabel != null)
                {
                    metricsHeaderLabel.Bounds = new Rectangle(
                        0, 0,
                        availableWidth,
                        headerHeight
                    );
                    metricsHeaderLabel.IsVisible = true;
                }

                // Set controls container bounds (width accounts for parent padding, fixed height)
                if (metricsControlsContainer != null)
                {
                    metricsControlsContainer.Bounds = new Rectangle(0, 0, availableWidth, 35);
                    metricsControlsContainer.IsVisible = true;
                }

                // Set content container width and target height for auto-layout
                if (metricsContentContainer != null && metricsMainContainer != null)
                {
                    // Calculate available height for the content container
                    // Account for: top padding, header, gap, controls, gap, and bottom padding
                    int actualHeaderHeight = metricsHeaderContainer != null && metricsHeaderContainer.Bounds.Height > 0 ? (int)metricsHeaderContainer.Bounds.Height : headerHeight;
                    int actualControlsHeight = metricsControlsContainer != null && metricsControlsContainer.Bounds.Height > 0 ? (int)metricsControlsContainer.Bounds.Height : 35;
                    int gaps = (int)metricsMainContainer.ChildGap * 2; // Gap between header->controls and controls->content
                    int availableHeight = (int)contentArea.Height - (int)(metricsMainContainer.ChildPadding * 2) - actualHeaderHeight - actualControlsHeight - gaps;

                    metricsContentContainer.Bounds = new Rectangle(
                        0, 0,
                        availableWidth, // Width accounts for parent padding
                        availableHeight // Set initial height
                    );
                    metricsContentContainer.TargetHeight = (float)availableHeight; // Set target for FillRemaining children
                    metricsContentContainer.IsVisible = true;
                }

                // Table bounds will be set automatically by container's auto-layout since it has FillRemaining = true
                if (metricsTable != null)
                {
                    // Only set width - height and position will be handled by auto-layout
                    metricsTable.Bounds = new Rectangle(
                        metricsTable.Bounds.X,
                        metricsTable.Bounds.Y,
                        availableWidth,
                        metricsTable.Bounds.Height // Height will be set by FillRemaining logic
                    );
                    metricsTable.IsVisible = true;
                }
            }
            else
            {
                // Hide containers when tab is not active
                if (metricsMainContainer != null)
                    metricsMainContainer.IsVisible = false;
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update fonts in components if needed
        }

        private void UpdateMetricsTable()
        {
            if (metricsTable == null)
                return;

            metricsTable.Rows.Clear();

            if (corpus == null || layout == null || !corpus.IsLoaded)
                return;

            // Get bigrams from corpus
            var bigrams = corpus.GetBigrams();
            var allBigrams = bigrams.GetAllSorted();

            foreach (var (sequence, count, frequency) in allBigrams)
            {
                // Convert bigram to key sequence
                var keySequence = layout.ConvertNgramToKeySequence(sequence);
                if (keySequence == null)
                    continue; // Skip if can't be mapped

                // Format key sequence
                string keySequenceStr = FormatKeySequence(keySequence);

                // Format finger sequence
                string fingerSequenceStr = FormatFingerSequence(keySequence);

                // Check if it's an SFB (Same Finger Bigram)
                bool isSFB = IsSameFingerBigram(keySequence);
                
                // Check if it's an LSB (Lateral Stretch Bigram)
                bool isLSB = IsLateralStretchBigram(keySequence);
                
                // Build metric matches string
                var metrics = new List<string>();
                if (isSFB) metrics.Add("SFB");
                if (isLSB) metrics.Add("LSB");
                string metricMatches = string.Join(" ", metrics);

                // Apply ngram search filter
                if (!string.IsNullOrEmpty(ngramSearchText))
                {
                    if (!sequence.Contains(ngramSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Apply metric search filter
                if (!string.IsNullOrEmpty(metricSearchText))
                {
                    if (!metricMatches.Contains(metricSearchText, StringComparison.OrdinalIgnoreCase))
                        continue;
                }

                // Format bigram with escape characters (like corpus tab)
                string bigramText = $"\"{EscapeSpecialChars(sequence)}\"";
                
                // Format frequency as percentage with 3 decimal places
                string frequencyText = $"{frequency * 100:F3}%";
                
                metricsTable.Rows.Add(new List<string> { bigramText, frequencyText, keySequenceStr, fingerSequenceStr, metricMatches });
            }
        }

        /// <summary>
        /// Formats a finger sequence as a readable string with shortened finger names (e.g., "LP+LI", "LM+RM").
        /// </summary>
        private string FormatFingerSequence(List<PhysicalKey> sequence)
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
        /// Formats a key sequence as a readable string (e.g., "A", "Shift+A", "Space").
        /// </summary>
        private string FormatKeySequence(List<PhysicalKey> sequence)
        {
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

            return string.Join("+", parts);
        }

        /// <summary>
        /// Determines if a key sequence represents a Same Finger Bigram (SFB).
        /// For sequences longer than 2 keys, splits into overlapping bigrams and checks if ANY bigram is an SFB.
        /// An SFB occurs when two keys are:
        /// 1. On the same finger
        /// 2. Not the same key
        /// </summary>
        private bool IsSameFingerBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Need at least 2 keys
            if (sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                // Check if this bigram is an SFB
                if (IsSFBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an SFB, return true
                }
            }

            return false; // No SFB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Same Finger Bigram (SFB).
        /// Returns true if the keys are on the same finger but are different keys.
        /// </summary>
        private bool IsSFBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if same finger
            if (firstKey.Finger != secondKey.Finger)
                return false;

            // Check if not the same key (compare by identifier if available, otherwise by position)
            bool isSameKey;
            if (!string.IsNullOrEmpty(firstKey.Identifier) && !string.IsNullOrEmpty(secondKey.Identifier))
            {
                isSameKey = firstKey.Identifier == secondKey.Identifier;
            }
            else
            {
                // Compare by position (X, Y, Width, Height)
                isSameKey = firstKey.X == secondKey.X &&
                           firstKey.Y == secondKey.Y &&
                           firstKey.Width == secondKey.Width &&
                           firstKey.Height == secondKey.Height;
            }

            // SFB if same finger but different key
            return !isSameKey;
        }

        /// <summary>
        /// Determines if a key sequence represents a Lateral Stretch Bigram (LSB).
        /// An LSB occurs when two keys are:
        /// 1. On the same hand
        /// 2. On adjacent fingers (excluding thumb)
        /// 3. Have a horizontal distance of 2U or greater
        /// </summary>
        private bool IsLateralStretchBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                if (IsLSBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an LSB, return true
                }
            }

            return false; // No LSB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Lateral Stretch Bigram (LSB).
        /// Returns true if they are on the same hand, adjacent fingers (excluding thumb), and 2U+ apart horizontally.
        /// </summary>
        private bool IsLSBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if on same hand
            if (firstKey.HandIndex != secondKey.HandIndex)
                return false;

            // Check if adjacent fingers (excluding thumb)
            // Adjacent means finger index difference is 1
            int fingerIndexDiff = Math.Abs(firstKey.FingerIndex - secondKey.FingerIndex);
            if (fingerIndexDiff != 1)
                return false;

            // Exclude thumb: index 0 on right hand, index 4 on left hand
            // Since both keys are on the same hand (already checked), we can use either key's HandIndex
            if (firstKey.HandIndex == 0)
            {
                // Left hand: exclude thumb at index 4
                if (firstKey.FingerIndex == 4 || secondKey.FingerIndex == 4)
                    return false;
            }
            else
            {
                // Right hand: exclude thumb at index 0
                if (firstKey.FingerIndex == 0 || secondKey.FingerIndex == 0)
                    return false;
            }

            // Check horizontal distance >= 2U
            float horizontalDistance = CalculateHorizontalDistance(firstKey, secondKey);
            if (horizontalDistance < 2.0f)
                return false;

            return true;
        }

        /// <summary>
        /// Calculates the horizontal distance between two keys in U units.
        /// Uses center-to-center distance.
        /// </summary>
        private float CalculateHorizontalDistance(PhysicalKey key1, PhysicalKey key2)
        {
            float center1X = key1.X + key1.Width / 2.0f;
            float center2X = key2.X + key2.Width / 2.0f;
            return Math.Abs(center2X - center1X);
        }

        /// <summary>
        /// Handles ngram search text changes.
        /// </summary>
        private void OnNgramSearchTextChanged(string text)
        {
            ngramSearchText = text;
            // Reset scroll when search changes
            if (metricsTable != null)
            {
                metricsTable.ResetScroll();
            }
            UpdateMetricsTable();
        }

        /// <summary>
        /// Handles metric search text changes.
        /// </summary>
        private void OnMetricSearchTextChanged(string text)
        {
            metricSearchText = text;
            // Reset scroll when search changes
            if (metricsTable != null)
            {
                metricsTable.ResetScroll();
            }
            UpdateMetricsTable();
        }

        /// <summary>
        /// Escapes special characters for display (like newline, tab, etc.).
        /// </summary>
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
    }
}

