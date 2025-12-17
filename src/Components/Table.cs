using Raylib_cs;
using System;
using System.Collections.Generic;
using Keysharp.UI;

namespace Keysharp.Components
{
    public class Table : UIElement
    {
        private Font font;
        private int fontSize;
        private const int RowHeight = 22; // Increased for larger font
        private const int HeaderHeight = 28; // Increased for larger font
        private const int Padding = 10;
        private const int ColumnGap = 20;
        private const int ScrollbarWidth = 16;

        private int scrollOffset = 0; // Number of rows scrolled
        private bool isDraggingScrollbar = false;
        private int scrollbarDragStartY = 0;
        private int scrollbarDragStartOffset = 0;

        public void ResetScroll()
        {
            scrollOffset = 0;
        }

        public List<(string column1, string column2, string column3, string column4)> Rows { get; set; }
        public string Column1Header { get; set; }
        public string Column2Header { get; set; }
        public string Column3Header { get; set; }
        public string Column4Header { get; set; }

        public Table(Font font, string column1Header, string column2Header, string column3Header, string column4Header, int fontSize = 12) 
            : base("Table")
        {
            this.font = font;
            this.fontSize = fontSize;
            this.Column1Header = column1Header;
            this.Column2Header = column2Header;
            this.Column3Header = column3Header;
            this.Column4Header = column4Header;
            this.Rows = new List<(string, string, string, string)>();

            // Tables are interactive for scrolling
            IsClickable = true;
            IsHoverable = true;
        }

        protected override void DrawSelf()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            
            if (Rows == null)
                Rows = new List<(string, string, string, string)>();

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
            
            int col1Width = (int)(tableWidth * 0.10f); // Rank
            int col2Width = (int)(tableWidth * 0.50f); // N-gram
            int col3Width = (int)(tableWidth * 0.20f); // Frequency
            int col4Width = (int)(tableWidth * 0.20f); // Count

            // Draw header background
            Rectangle headerRect = new Rectangle(x, y, Bounds.Width, HeaderHeight);
            Raylib.DrawRectangleRec(headerRect, UITheme.SidePanelColor);
            Raylib.DrawRectangleLinesEx(headerRect, 1, UITheme.BorderColor);

            // Draw header text (all right-aligned)
            Rectangle col1HeaderRect = new Rectangle(x, y, col1Width, HeaderHeight);
            Rectangle col2HeaderRect = new Rectangle(x + col1Width, y, col2Width, HeaderHeight);
            Rectangle col3HeaderRect = new Rectangle(x + col1Width + col2Width, y, col3Width, HeaderHeight);
            Rectangle col4HeaderRect = new Rectangle(x + col1Width + col2Width + col3Width, y, col4Width, HeaderHeight);

            TextContainer.DrawRightAlignedText(font, Column1Header, col1HeaderRect, fontSize, UITheme.TextColor, Padding);
            TextContainer.DrawRightAlignedText(font, Column2Header, col2HeaderRect, fontSize, UITheme.TextColor, Padding);
            TextContainer.DrawRightAlignedText(font, Column3Header, col3HeaderRect, fontSize, UITheme.TextColor, Padding);
            TextContainer.DrawRightAlignedText(font, Column4Header, col4HeaderRect, fontSize, UITheme.TextColor, Padding);

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
                Rectangle col1Rect = new Rectangle(x, rowY, col1Width, RowHeight);
                Rectangle col2Rect = new Rectangle(x + col1Width, rowY, col2Width, RowHeight);
                Rectangle col3Rect = new Rectangle(x + col1Width + col2Width, rowY, col3Width, RowHeight);
                Rectangle col4Rect = new Rectangle(x + col1Width + col2Width + col3Width, rowY, col4Width, RowHeight);

                TextContainer.DrawRightAlignedText(font, row.column1, col1Rect, fontSize - 1, UITheme.TextColor, Padding);
                TextContainer.DrawRightAlignedText(font, row.column2, col2Rect, fontSize - 1, UITheme.TextColor, Padding);
                TextContainer.DrawRightAlignedText(font, row.column3, col3Rect, fontSize - 1, UITheme.TextColor, Padding);
                TextContainer.DrawRightAlignedText(font, row.column4, col4Rect, fontSize - 1, UITheme.TextColor, Padding);

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

