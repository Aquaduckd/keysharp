using Raylib_cs;
using System;
using System.Collections.Generic;
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
            
            // Only apply custom widths if we have exactly 7 columns (the corpus table format)
            if (Columns.Count == 7)
            {
                bool showConditional = GetColumnVisibility(4) || GetColumnVisibility(5) || GetColumnVisibility(6); // Columns 5, 6, 7 (0-indexed: 4, 5, 6)
                
                if (showConditional)
                {
                    widths.Add((int)(tableWidth * 0.07f)); // Column 0 (Rank)
                    widths.Add((int)(tableWidth * 0.20f)); // Column 1 (N-gram)
                    widths.Add((int)(tableWidth * 0.10f)); // Column 2 (Frequency)
                    widths.Add((int)(tableWidth * 0.12f)); // Column 3 (Count)
                    widths.Add(GetColumnVisibility(4) ? (int)(tableWidth * 0.08f) : 0); // Column 4 (Global Rank)
                    widths.Add(GetColumnVisibility(5) ? (int)(tableWidth * 0.15f) : 0); // Column 5 (Relative Frequency)
                    widths.Add(GetColumnVisibility(6) ? (int)(tableWidth * 0.28f) : 0); // Column 6 (Key Sequence)
                }
                else
                {
                    widths.Add((int)(tableWidth * 0.10f)); // Column 0 (Rank)
                    widths.Add((int)(tableWidth * 0.50f)); // Column 1 (N-gram)
                    widths.Add((int)(tableWidth * 0.20f)); // Column 2 (Frequency)
                    widths.Add((int)(tableWidth * 0.20f)); // Column 3 (Count)
                    widths.Add(0); // Column 4
                    widths.Add(0); // Column 5
                    widths.Add(0); // Column 6
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
            
            // Add highlighting for column 2 (n-gram column)
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

