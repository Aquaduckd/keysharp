using Raylib_cs;
using System;
using System.Collections.Generic;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class Table : UIElement
    {
        protected Font font;
        protected int fontSize;
        protected const int Padding = 10;
        private const int RowHeight = 22; // Increased for larger font
        private const int HeaderHeight = 28; // Increased for larger font
        private const int ColumnGap = 20;
        private const int ScrollbarWidth = 16;

        private int scrollOffset = 0; // Number of rows scrolled
        private bool isDraggingScrollbar = false;
        private int scrollbarDragStartY = 0;
        private int scrollbarDragStartOffset = 0;

        public int ScrollOffset => scrollOffset;

        public void ResetScroll()
        {
            scrollOffset = 0;
        }

        public List<List<string>> Rows { get; set; }
        public List<TableColumn> Columns { get; set; }

        public Table(Font font, params string[] columnHeaders) 
            : base("Table")
        {
            this.font = font;
            this.fontSize = 12;
            
            // Initialize columns
            Columns = new List<TableColumn>();
            foreach (var header in columnHeaders)
            {
                Columns.Add(new TableColumn(header, isVisible: true));
            }
            
            this.Rows = new List<List<string>>();

            // Tables are interactive for scrolling
            IsClickable = true;
            IsHoverable = true;
        }

        public Table(Font font, int fontSize, params string[] columnHeaders) 
            : base("Table")
        {
            this.font = font;
            this.fontSize = fontSize;
            
            // Initialize columns
            Columns = new List<TableColumn>();
            foreach (var header in columnHeaders)
            {
                Columns.Add(new TableColumn(header, isVisible: true));
            }
            
            this.Rows = new List<List<string>>();

            // Tables are interactive for scrolling
            IsClickable = true;
            IsHoverable = true;
        }

        /// <summary>
        /// Sets the visibility of a column by index.
        /// </summary>
        public void SetColumnVisibility(int columnIndex, bool isVisible)
        {
            if (columnIndex >= 0 && columnIndex < Columns.Count)
            {
                Columns[columnIndex].IsVisible = isVisible;
            }
        }

        /// <summary>
        /// Gets the visibility of a column by index.
        /// </summary>
        public bool GetColumnVisibility(int columnIndex)
        {
            if (columnIndex >= 0 && columnIndex < Columns.Count)
            {
                return Columns[columnIndex].IsVisible;
            }
            return false;
        }

        protected override void DrawSelf()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            
            if (Rows == null)
                Rows = new List<List<string>>();
            if (Columns == null || Columns.Count == 0)
                return;

            const int FirstColumnPadding = 5;

            int x = (int)Bounds.X;
            int y = (int)Bounds.Y;
            
            // Calculate how many rows can fit in the available height
            int availableHeight = (int)Bounds.Height - HeaderHeight;
            int visibleRows = Math.Max(0, availableHeight / RowHeight);
            
            // Calculate column widths (account for scrollbar if needed)
            int tableWidth = (int)Bounds.Width;
            bool needsScrollbar = Rows.Count > visibleRows;
            if (needsScrollbar)
            {
                tableWidth -= ScrollbarWidth;
            }
            
            // Calculate column widths
            var columnWidths = CalculateColumnWidths(tableWidth);

            // Draw header background
            Rectangle headerRect = new Rectangle(x, y, Bounds.Width, HeaderHeight);
            Raylib.DrawRectangleRec(headerRect, UITheme.SidePanelColor);
            Raylib.DrawRectangleLinesEx(headerRect, 1, UITheme.BorderColor);

            // Draw header text (all right-aligned)
            float currentX = x;
            for (int colIndex = 0; colIndex < Columns.Count; colIndex++)
            {
                if (!Columns[colIndex].IsVisible)
                    continue;
                    
                int colWidth = columnWidths[colIndex];
                int padding = colIndex == 0 ? FirstColumnPadding : Padding;
                TextContainer.DrawRightAlignedText(font, Columns[colIndex].Header, new Rectangle(currentX, y, colWidth, HeaderHeight), fontSize, UITheme.TextColor, padding);
                currentX += colWidth;
            }

            // Draw rows
            int rowY = y + HeaderHeight;
            
            // Clamp scroll offset
            int maxScroll = Math.Max(0, Rows.Count - visibleRows);
            scrollOffset = Math.Clamp(scrollOffset, 0, maxScroll);
            
            // Draw visible rows starting from scrollOffset
            int rowsToDraw = Math.Min(Rows.Count - scrollOffset, visibleRows);

            for (int i = 0; i < rowsToDraw; i++)
            {
                int rowIndex = scrollOffset + i;
                var row = Rows[rowIndex];
                bool isEven = i % 2 == 0;
                Color rowColor = isEven ? UITheme.MainPanelColor : new Color(
                    (byte)(UITheme.MainPanelColor.R * 0.95f),
                    (byte)(UITheme.MainPanelColor.G * 0.95f),
                    (byte)(UITheme.MainPanelColor.B * 0.95f),
                    UITheme.MainPanelColor.A
                );

                Rectangle rowRect = new Rectangle(x, rowY, tableWidth, RowHeight);
                Raylib.DrawRectangleRec(rowRect, rowColor);

                // Draw row text (all right-aligned)
                float rowCurrentX = x;
                for (int colIndex = 0; colIndex < Columns.Count && colIndex < row.Count; colIndex++)
                {
                    if (!Columns[colIndex].IsVisible)
                        continue;
                        
                    int colWidth = columnWidths[colIndex];
                    int padding = colIndex == 0 ? FirstColumnPadding : Padding;
                    string cellText = row[colIndex];
                    DrawCell(colIndex + 1, cellText, new Rectangle(rowCurrentX, rowY, colWidth, RowHeight), rowIndex, padding);
                    rowCurrentX += colWidth;
                }

                rowY += RowHeight;
            }

            // Draw bottom border
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(x, rowY),
                new System.Numerics.Vector2(x + tableWidth, rowY),
                1, UITheme.BorderColor);

            // Draw scrollbar if needed
            if (needsScrollbar && Rows.Count > 0)
            {
                DrawScrollbar(x + tableWidth, y + HeaderHeight, availableHeight, Rows.Count, visibleRows);
            }
        }

        /// <summary>
        /// Overridable method to draw a cell. Base implementation draws right-aligned text.
        /// </summary>
        protected virtual void DrawCell(int columnIndex, string cellText, Rectangle cellRect, int rowIndex, int padding)
        {
            TextContainer.DrawRightAlignedText(font, cellText, cellRect, fontSize - 1, UITheme.TextColor, padding);
        }

        /// <summary>
        /// Overridable method to calculate column widths. Returns a list of widths, one per column.
        /// </summary>
        protected virtual List<int> CalculateColumnWidths(int tableWidth)
        {
            var widths = new List<int>();
            int visibleColumnCount = 0;
            
            // Count visible columns
            foreach (var col in Columns)
            {
                if (col.IsVisible)
                    visibleColumnCount++;
            }
            
            if (visibleColumnCount == 0)
                return widths;
            
            // Default: distribute width evenly among visible columns
            int baseWidth = tableWidth / visibleColumnCount;
            int remainder = tableWidth % visibleColumnCount;
            
            for (int i = 0; i < Columns.Count; i++)
            {
                if (Columns[i].IsVisible)
                {
                    int width = baseWidth;
                    if (remainder > 0)
                    {
                        width++;
                        remainder--;
                    }
                    widths.Add(width);
                }
                else
                {
                    widths.Add(0);
                }
            }
            
            return widths;
        }

        private void DrawScrollbar(int x, int y, int height, int totalRows, int visibleRows)
        {
            // Scrollbar track
            Rectangle trackRect = new Rectangle(x, y, ScrollbarWidth, height);
            Raylib.DrawRectangleRec(trackRect, UITheme.SidePanelColor);
            Raylib.DrawRectangleLinesEx(trackRect, 1, UITheme.BorderColor);

            // Calculate scrollbar thumb size and position
            float thumbHeight = (float)visibleRows / totalRows * height;
            float thumbY = y + ((float)scrollOffset / (totalRows - visibleRows)) * (height - thumbHeight);
            
            Rectangle thumbRect = new Rectangle(x + 1, (int)thumbY, ScrollbarWidth - 2, (int)thumbHeight);
            Color thumbColor = isDraggingScrollbar ? UITheme.MainPanelColor : new Color(
                (byte)(UITheme.MainPanelColor.R * 0.8f),
                (byte)(UITheme.MainPanelColor.G * 0.8f),
                (byte)(UITheme.MainPanelColor.B * 0.8f),
                UITheme.MainPanelColor.A
            );
            Raylib.DrawRectangleRec(thumbRect, thumbColor);
            Raylib.DrawRectangleLinesEx(thumbRect, 1, UITheme.BorderColor);
        }

        public override void Update()
        {
            base.Update();

            if (!IsVisible || !IsEnabled || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            int availableHeight = (int)Bounds.Height - HeaderHeight;
            int visibleRows = Math.Max(0, availableHeight / RowHeight);
            bool needsScrollbar = Rows.Count > visibleRows;

            // Handle mouse wheel scrolling
            float wheelMove = Raylib.GetMouseWheelMove();
            if (wheelMove != 0 && IsHovering(mouseX, mouseY))
            {
                int maxScroll = Math.Max(0, Rows.Count - visibleRows);
                scrollOffset = Math.Clamp(scrollOffset - (int)wheelMove, 0, maxScroll);
            }

            // Handle scrollbar dragging
            if (needsScrollbar)
            {
                int tableWidth = (int)Bounds.Width - ScrollbarWidth;
                int scrollbarX = (int)Bounds.X + tableWidth;
                int scrollbarY = (int)Bounds.Y + HeaderHeight;
                
                Rectangle scrollbarArea = new Rectangle(scrollbarX, scrollbarY, ScrollbarWidth, availableHeight);
                bool hoveringScrollbar = mouseX >= scrollbarArea.X && mouseX <= scrollbarArea.X + scrollbarArea.Width &&
                                        mouseY >= scrollbarArea.Y && mouseY <= scrollbarArea.Y + scrollbarArea.Height;

                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) && hoveringScrollbar)
                {
                    isDraggingScrollbar = true;
                    scrollbarDragStartY = mouseY;
                    scrollbarDragStartOffset = scrollOffset;
                }

                if (isDraggingScrollbar)
                {
                    if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                    {
                        float deltaY = mouseY - scrollbarDragStartY;
                        float scrollRatio = deltaY / availableHeight;
                        int deltaRows = (int)(scrollRatio * Rows.Count);
                        int maxScroll = Math.Max(0, Rows.Count - visibleRows);
                        scrollOffset = Math.Clamp(scrollbarDragStartOffset + deltaRows, 0, maxScroll);
                    }
                    else
                    {
                        isDraggingScrollbar = false;
                    }
                }
            }
        }
    }
}

