using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class CorpusTable : Table
    {
        // Store match positions for highlighting in column 2 (n-gram column)
        // Maps row index to list of (start, length) match positions in the display text
        public Dictionary<int, List<(int start, int length)>> Column2MatchPositions { get; set; } = new Dictionary<int, List<(int, int)>>();
        
        // Callback to calculate match positions for a row (used for lazy calculation during drawing)
        public Func<int, string, List<(int start, int length)>>? CalculateMatchPositionsCallback { get; set; }

        public CorpusTable(Font font, int fontSize, params string[] columnHeaders) 
            : base(font, fontSize, columnHeaders)
        {
        }

        protected override List<int> CalculateColumnWidths(int tableWidth)
        {
            var widths = new List<int>();
            
            // Handle 9 columns: Rank, N-gram, Frequency, Count, Global Rank, Rel. Freq., Key Sequence, Finger Sequence, Metric Matches
            if (Columns.Count == 9)
            {
                // Match the 6-column style: Rank is a small fixed width on the left, rest distributed on the right
                bool col4Visible = GetColumnVisibility(4);
                bool col5Visible = GetColumnVisibility(5);
                bool col6Visible = GetColumnVisibility(6);
                bool col7Visible = GetColumnVisibility(7);
                bool col8Visible = GetColumnVisibility(8);
                
                // Rank gets fixed small width on the left (same as 6-column case)
                float rankRatio = 0.05f;
                float remainingRatio = 1.0f - rankRatio; // 95% for all other columns
                
                // Calculate ratios for remaining columns (N-gram, Frequency, Count, and conditionally visible columns)
                // Base ratios (will be scaled to fit remaining space)
                float ngramBaseRatio = 0.40f; // N-gram reduced to give more space to key/finger sequences
                float frequencyBaseRatio = 0.20f;
                float countBaseRatio = 0.25f;
                float globalRankBaseRatio = col4Visible ? 0.08f : 0f;
                float relativeFreqBaseRatio = col5Visible ? 0.10f : 0f;
                float keySeqBaseRatio = col6Visible ? 0.25f : 0f; // Increased from 0.20f
                float fingerSeqBaseRatio = col7Visible ? 0.17f : 0f; // Increased from 0.12f
                float metricMatchesBaseRatio = col8Visible ? 0.25f : 0f;
                
                // Sum of base ratios for remaining columns
                float totalBaseRatio = ngramBaseRatio + frequencyBaseRatio + countBaseRatio + 
                                      globalRankBaseRatio + relativeFreqBaseRatio + 
                                      keySeqBaseRatio + fingerSeqBaseRatio + metricMatchesBaseRatio;
                
                // Scale all remaining column ratios to fit the remaining space
                float scaleFactor = remainingRatio / totalBaseRatio;
                
                widths.Add((int)(tableWidth * rankRatio)); // Column 0 (Rank) - fixed small width on left
                widths.Add((int)(tableWidth * ngramBaseRatio * scaleFactor)); // Column 1 (N-gram)
                widths.Add((int)(tableWidth * frequencyBaseRatio * scaleFactor)); // Column 2 (Frequency)
                widths.Add((int)(tableWidth * countBaseRatio * scaleFactor)); // Column 3 (Count)
                widths.Add((int)(tableWidth * globalRankBaseRatio * scaleFactor)); // Column 4 (Global Rank)
                widths.Add((int)(tableWidth * relativeFreqBaseRatio * scaleFactor)); // Column 5 (Relative Frequency)
                widths.Add((int)(tableWidth * keySeqBaseRatio * scaleFactor)); // Column 6 (Key Sequence)
                widths.Add((int)(tableWidth * fingerSeqBaseRatio * scaleFactor)); // Column 7 (Finger Sequence)
                widths.Add((int)(tableWidth * metricMatchesBaseRatio * scaleFactor)); // Column 8 (Metric Matches)
                
                // Adjust last visible column to account for rounding errors to ensure total equals tableWidth
                int currentTotal = widths.Sum();
                if (currentTotal != tableWidth)
                {
                    // Find the last visible column and adjust its width
                    for (int i = widths.Count - 1; i >= 0; i--)
                    {
                        if (widths[i] > 0)
                        {
                            widths[i] += tableWidth - currentTotal;
                            break;
                        }
                    }
                }
            }
            // Handle 6 columns (legacy support): Rank, N-gram, Frequency, Count, Global Rank, Rel. Freq.
            else if (Columns.Count == 6)
            {
                bool showConditional = GetColumnVisibility(4) || GetColumnVisibility(5); // Columns 5, 6 (0-indexed: 4, 5)
                
                if (showConditional)
                {
                    widths.Add((int)(tableWidth * 0.035f)); // Column 0 (Rank) - halved from 0.07f
                    widths.Add((int)(tableWidth * 0.20f)); // Column 1 (N-gram)
                    widths.Add((int)(tableWidth * 0.10f)); // Column 2 (Frequency)
                    widths.Add((int)(tableWidth * 0.12f)); // Column 3 (Count)
                    widths.Add(GetColumnVisibility(4) ? (int)(tableWidth * 0.08f) : 0); // Column 4 (Global Rank)
                    widths.Add(GetColumnVisibility(5) ? (int)(tableWidth * 0.465f) : 0); // Column 5 (Relative Frequency) - adjusted to compensate for rank column reduction (0.43 + 0.035)
                }
                else
                {
                    widths.Add((int)(tableWidth * 0.05f)); // Column 0 (Rank) - halved from 0.10f
                    widths.Add((int)(tableWidth * 0.50f)); // Column 1 (N-gram)
                    widths.Add((int)(tableWidth * 0.20f)); // Column 2 (Frequency)
                    widths.Add((int)(tableWidth * 0.25f)); // Column 3 (Count) - adjusted to compensate for rank column reduction
                    widths.Add(0); // Column 4
                    widths.Add(0); // Column 5
                }
            }
            else
            {
                // For other column counts, use the base implementation
                return base.CalculateColumnWidths(tableWidth);
            }
            
            return widths;
        }

        protected override void DrawCell(int columnIndex, string cellText, Rectangle cellRect, int rowIndex, int padding)
        {
            // Draw the text first
            base.DrawCell(columnIndex, cellText, cellRect, rowIndex, padding);
            
            // Add highlighting for column 1 (n-gram column)
            // Note: DrawCell receives colIndex + 1, so column 1 (0-indexed) is passed as columnIndex 2
            if (columnIndex == 2)
            {
                // Get or calculate match positions for this row (lazy calculation)
                List<(int start, int length)>? matchPositions = null;
                if (Column2MatchPositions != null && Column2MatchPositions.TryGetValue(rowIndex, out var cachedPositions))
                {
                    matchPositions = cachedPositions;
                }
                else if (CalculateMatchPositionsCallback != null)
                {
                    // Calculate on-demand during drawing
                    matchPositions = CalculateMatchPositionsCallback(rowIndex, cellText);
                    if (matchPositions != null && matchPositions.Count > 0)
                    {
                        // Cache the result
                        if (Column2MatchPositions == null)
                            Column2MatchPositions = new Dictionary<int, List<(int, int)>>();
                        Column2MatchPositions[rowIndex] = matchPositions;
                    }
                }
                
                // Draw highlights if we have match positions
                if (matchPositions != null && matchPositions.Count > 0)
                {
                    DrawColumn2Highlights(cellText, cellRect, matchPositions);
                }
            }
        }

        private void DrawColumn2Highlights(string displayText, Rectangle cellRect, List<(int start, int length)> matchPositions)
        {
            if (string.IsNullOrEmpty(displayText) || matchPositions.Count == 0)
                return;

            // Calculate text position (right-aligned)
            float textWidth = FontManager.MeasureText(font, displayText, fontSize - 1);
            float textX = cellRect.X + cellRect.Width - textWidth - Padding;
            float textY = cellRect.Y + (cellRect.Height - (fontSize - 1)) / 2;

            // Draw highlighting rectangles for each match
            Color highlightColor = new Color(255, 165, 0, 50); // Semi-transparent yellow with reduced opacity

            foreach (var (start, length) in matchPositions)
            {
                if (start >= displayText.Length || start + length > displayText.Length)
                    continue;

                // Measure text before the match
                string beforeMatch = displayText.Substring(0, start);
                float beforeWidth = FontManager.MeasureText(font, beforeMatch, fontSize - 1);

                // Measure the matched text
                string matchedText = displayText.Substring(start, length);
                float matchWidth = FontManager.MeasureText(font, matchedText, fontSize - 1);

                // Draw highlight rectangle
                Rectangle highlightRect = new Rectangle(
                    textX + beforeWidth,
                    textY,
                    matchWidth,
                    fontSize - 1
                );
                Raylib.DrawRectangleRec(highlightRect, highlightColor);
            }
        }
    }
}

