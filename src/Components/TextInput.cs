using Raylib_cs;
using System;
using System.Text;
using Keysharp.UI;

namespace Keysharp.Components
{
    public enum InputType
    {
        Any,        // Allow any string (default)
        Integer,    // Only allow integer values
        Decimal     // Only allow decimal/float values
    }

    public class TextInput : UIElement
    {
        private Font font;
        private int fontSize;
        private const int Padding = 5;
        
        public string Text { get; set; }
        public string Placeholder { get; set; }
        public bool IsFocused { get; private set; }
        public Action<string>? OnTextChanged { get; set; }
        public bool IsInvalid { get; set; } = false;
        public InputType InputConstraint { get; set; } = InputType.Any; // Constraint on input type
        public bool EnableScrollIncrement { get; set; } = false; // Enable scroll wheel increment/decrement for numeric values
        public float ScrollIncrementAmount { get; set; } = 0.25f; // Amount to increment/decrement when scrolling
        public float? MinValue { get; set; } = null; // Minimum allowed value (null = no minimum)
        public float? MaxValue { get; set; } = null; // Maximum allowed value (null = no maximum)
        private bool backspaceProcessedThisFrame = false;
        
        // Cursor and selection
        private int cursorPosition = 0;
        private int selectionStart = -1; // -1 means no selection
        private int selectionEnd = -1;
        private bool isSelecting = false;
        private int mouseClickPosition = -1;
        
        // Horizontal scrolling for long text
        private float scrollOffset = 0; // Horizontal offset in pixels
        
        // Key repeat tracking
        private double arrowLeftLastPress = -1;
        private double arrowRightLastPress = -1;
        private double backspaceLastPress = -1;
        private double arrowLeftLastRepeat = -1;
        private double arrowRightLastRepeat = -1;
        private double backspaceLastRepeat = -1;
        private const double KEY_REPEAT_DELAY = 0.5; // Initial delay in seconds
        private const double KEY_REPEAT_INTERVAL = 0.05; // Repeat interval in seconds

        public TextInput(Font font, string placeholder = "", int fontSize = 14) : base("TextInput")
        {
            this.font = font;
            this.fontSize = fontSize;
            this.Placeholder = placeholder;
            this.Text = "";
            this.cursorPosition = 0;
            
            IsClickable = true;
            IsHoverable = true;
        }

        public void SetBounds(Rectangle bounds)
        {
            this.Bounds = bounds;
        }

        public void SetText(string text)
        {
            this.Text = text;
            cursorPosition = text.Length;
            ClearSelection();
            scrollOffset = 0; // Reset scroll when text is set programmatically
            ValidateInput();
            OnTextChanged?.Invoke(text);
        }
        
        /// <summary>
        /// Checks if a character is valid for the current input constraint.
        /// </summary>
        private bool IsValidChar(char c)
        {
            // Get the text that would exist after inserting this character
            // (accounting for selection that will be deleted)
            string testText;
            int testPosition;
            if (HasSelection())
            {
                int start = GetSelectionStart();
                int end = GetSelectionEnd();
                testText = Text.Substring(0, start) + Text.Substring(end);
                testPosition = start;
            }
            else
            {
                testText = Text;
                testPosition = cursorPosition;
            }
            
            switch (InputConstraint)
            {
                case InputType.Integer:
                    // Allow digits and negative sign (only at the start)
                    if (char.IsDigit(c))
                        return true;
                    if (c == '-' && testPosition == 0 && !testText.Contains("-"))
                        return true;
                    return false;
                    
                case InputType.Decimal:
                    // Allow digits, negative sign (only at start), and decimal point (only one)
                    if (char.IsDigit(c))
                        return true;
                    if (c == '-' && testPosition == 0 && !testText.Contains("-"))
                        return true;
                    if (c == '.' && !testText.Contains("."))
                        return true;
                    return false;
                    
                case InputType.Any:
                default:
                    return true;
            }
        }
        
        /// <summary>
        /// Validates the current text and sets IsInvalid accordingly.
        /// </summary>
        private void ValidateInput()
        {
            if (InputConstraint == InputType.Any || string.IsNullOrEmpty(Text))
            {
                IsInvalid = false;
                return;
            }
            
            switch (InputConstraint)
            {
                case InputType.Integer:
                    IsInvalid = !int.TryParse(Text, out _);
                    break;
                    
                case InputType.Decimal:
                    IsInvalid = !float.TryParse(Text, out _) && !double.TryParse(Text, out _);
                    break;
                    
                default:
                    IsInvalid = false;
                    break;
            }
        }
        
        private void ClearSelection()
        {
            selectionStart = -1;
            selectionEnd = -1;
        }
        
        private bool HasSelection()
        {
            return selectionStart >= 0 && selectionEnd >= 0 && selectionStart != selectionEnd;
        }
        
        private int GetSelectionStart()
        {
            if (!HasSelection()) return cursorPosition;
            return Math.Min(selectionStart, selectionEnd);
        }
        
        private int GetSelectionEnd()
        {
            if (!HasSelection()) return cursorPosition;
            return Math.Max(selectionStart, selectionEnd);
        }
        
        private void DeleteSelection()
        {
            if (!HasSelection()) return;
            
            int start = GetSelectionStart();
            int end = GetSelectionEnd();
            Text = Text.Substring(0, start) + Text.Substring(end);
            cursorPosition = start;
            ClearSelection();
            ValidateInput();
            OnTextChanged?.Invoke(Text);
        }
        
        private bool IsWordChar(char c)
        {
            // Words are any non-whitespace characters (simplified for regex input)
            return !char.IsWhiteSpace(c);
        }
        
        private int FindWordStart(int pos)
        {
            if (pos <= 0) return 0;
            
            // Clamp pos to valid range for character access
            int searchPos = pos;
            if (searchPos > Text.Length) searchPos = Text.Length;
            
            // Move back while we're in non-whitespace characters
            while (searchPos > 0 && !char.IsWhiteSpace(Text[searchPos - 1]))
            {
                searchPos--;
            }
            return searchPos;
        }
        
        private int FindWordStartIncludingSpaces(int pos)
        {
            if (pos <= 0) return 0;
            
            // Clamp pos to valid range
            int searchPos = pos;
            if (searchPos > Text.Length) searchPos = Text.Length;
            
            // First, skip backwards through any spaces/whitespace
            while (searchPos > 0 && char.IsWhiteSpace(Text[searchPos - 1]))
            {
                searchPos--;
            }
            
            // Then find the start of the word (non-whitespace sequence)
            return FindWordStart(searchPos);
        }
        
        private int FindWordEnd(int pos)
        {
            if (pos >= Text.Length) return Text.Length;
            
            // Move forward while we're in non-whitespace characters
            while (pos < Text.Length && !char.IsWhiteSpace(Text[pos]))
            {
                pos++;
            }
            return pos;
        }
        
        private int FindWordEndIncludingSpaces(int pos)
        {
            if (pos >= Text.Length) return Text.Length;
            
            // First find the end of the word (non-whitespace sequence)
            int wordEnd = FindWordEnd(pos);
            
            // Then skip forward through any spaces/whitespace
            while (wordEnd < Text.Length && char.IsWhiteSpace(Text[wordEnd]))
            {
                wordEnd++;
            }
            
            return wordEnd;
        }
        
        private int FindPreviousWordEnd(int pos)
        {
            if (pos <= 0) return 0;
            
            // Move backwards to find the end of the word that ends just before pos
            int searchPos = pos - 1;
            while (searchPos >= 0 && char.IsWhiteSpace(Text[searchPos]))
            {
                searchPos--;
            }
            
            // Now we're at the end of a word (or before any word)
            if (searchPos < 0) return 0;
            
            // Find where this word starts
            int wordStart = searchPos;
            while (wordStart > 0 && !char.IsWhiteSpace(Text[wordStart - 1]))
            {
                wordStart--;
            }
            
            // Now find where this word ends (starting from wordStart)
            return FindWordEnd(wordStart);
        }
        
        private int GetCursorPositionFromMouse(int mouseX)
        {
            float textX = Bounds.X + Padding - scrollOffset;
            
            // Find the closest cursor position
            int bestPos = Text.Length;
            float bestDist = float.MaxValue;
            
            // Check each character position
            for (int i = 0; i <= Text.Length; i++)
            {
                string before = Text.Substring(0, i);
                float x = textX + FontManager.MeasureText(font, before, fontSize);
                float dist = Math.Abs(x - mouseX);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    bestPos = i;
                }
            }
            
            return bestPos;
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        public override void Update()
        {
            base.Update();

            // Reset frame-specific flags at the start of each frame
            backspaceProcessedThisFrame = false;

            if (!IsVisible || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;
            
            // Don't process input if disabled
            if (!IsEnabled)
            {
                // Clear focus if we become disabled
                if (IsFocused)
                {
                    IsFocused = false;
                    ClearSelection();
                }
                return;
            }

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            bool isHovered = IsHovering(mouseX, mouseY);

            // Handle scroll wheel increment/decrement (when enabled and hovered)
            if (EnableScrollIncrement && isHovered)
            {
                float wheelMove = Raylib.GetMouseWheelMove();
                if (wheelMove != 0)
                {
                    // Try to parse the current text as a float
                    if (float.TryParse(Text, out float currentValue))
                    {
                        // Increment or decrement by ScrollIncrementAmount
                        float newValue = currentValue + (wheelMove * ScrollIncrementAmount);
                        
                        // Clamp to min/max values if specified
                        if (MinValue.HasValue)
                        {
                            newValue = Math.Max(newValue, MinValue.Value);
                        }
                        if (MaxValue.HasValue)
                        {
                            newValue = Math.Min(newValue, MaxValue.Value);
                        }
                        
                        // Format based on input constraint
                        string newText;
                        if (InputConstraint == InputType.Integer)
                        {
                            // For integers, format as integer (no decimals)
                            newText = ((int)newValue).ToString();
                        }
                        else
                        {
                            // For decimals or any, format with 2 decimal places
                            newText = newValue.ToString("F2");
                        }
                        
                        SetText(newText);
                    }
                }
            }

            // Handle focus and mouse selection
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                // Don't process if click was consumed by a dropdown
                if (Dropdown.WasClickConsumed())
                {
                    return;
                }

                bool wasFocused = IsFocused;
                // Only allow focus if enabled
                IsFocused = isHovered && IsEnabled;
                
                if (IsFocused)
                {
                    cursorPosition = GetCursorPositionFromMouse(mouseX);
                    mouseClickPosition = cursorPosition;
                    ClearSelection();
                    isSelecting = true;
                }
                else if (wasFocused)
                {
                    isSelecting = false;
                }
            }
            
            // Handle mouse drag selection
            if (IsFocused && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) && isSelecting)
            {
                int newPos = GetCursorPositionFromMouse(mouseX);
                if (newPos != cursorPosition)
                {
                    selectionStart = mouseClickPosition;
                    selectionEnd = newPos;
                    cursorPosition = newPos;
                }
            }
            
            if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
            {
                isSelecting = false;
            }

            // Handle text input when focused (only if enabled)
            if (IsFocused && IsEnabled)
            {
                // Check Ctrl state first - need to check BEFORE GetCharPressed() which might consume events
                bool leftCtrlDown = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL);
                bool rightCtrlDown = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
                bool ctrlDown = leftCtrlDown || rightCtrlDown;
                bool shiftDown = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
                
                // Ctrl state is tracked above for use in key combinations
                
                // Ctrl state is checked above for use in key combinations
                
                double currentTime = Raylib.GetTime();
                bool leftArrowPressed = Raylib.IsKeyPressed(KeyboardKey.KEY_LEFT);
                bool rightArrowPressed = Raylib.IsKeyPressed(KeyboardKey.KEY_RIGHT);
                bool backspacePressed = Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE);
                bool leftArrowDown = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT);
                bool rightArrowDown = Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT);
                bool backspaceDown = Raylib.IsKeyDown(KeyboardKey.KEY_BACKSPACE);
                
                // Handle arrow keys with smooth repeating
                bool shouldMoveLeft = false;
                bool shouldMoveRight = false;
                
                // Check modifier state for arrow keys at the time of press/repeat
                bool ctrlDownForArrows = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
                bool shiftDownForArrows = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
                
                if (leftArrowPressed)
                {
                    arrowLeftLastPress = currentTime;
                    arrowLeftLastRepeat = currentTime;
                    shouldMoveLeft = true;
                }
                else if (leftArrowDown && arrowLeftLastPress >= 0)
                {
                    double timeSincePress = currentTime - arrowLeftLastPress;
                    if (timeSincePress > KEY_REPEAT_DELAY)
                    {
                        // We've passed the initial delay, now repeat at intervals
                        if (arrowLeftLastRepeat < arrowLeftLastPress + KEY_REPEAT_DELAY)
                        {
                            arrowLeftLastRepeat = arrowLeftLastPress + KEY_REPEAT_DELAY;
                            shouldMoveLeft = true;
                        }
                        else if (currentTime - arrowLeftLastRepeat >= KEY_REPEAT_INTERVAL)
                        {
                            arrowLeftLastRepeat = currentTime;
                            shouldMoveLeft = true;
                        }
                    }
                }
                else if (!leftArrowDown)
                {
                    arrowLeftLastPress = -1;
                    arrowLeftLastRepeat = -1;
                }
                
                if (rightArrowPressed)
                {
                    arrowRightLastPress = currentTime;
                    arrowRightLastRepeat = currentTime;
                    shouldMoveRight = true;
                }
                else if (rightArrowDown && arrowRightLastPress >= 0)
                {
                    double timeSincePress = currentTime - arrowRightLastPress;
                    if (timeSincePress > KEY_REPEAT_DELAY)
                    {
                        // We've passed the initial delay, now repeat at intervals
                        if (arrowRightLastRepeat < arrowRightLastPress + KEY_REPEAT_DELAY)
                        {
                            arrowRightLastRepeat = arrowRightLastPress + KEY_REPEAT_DELAY;
                            shouldMoveRight = true;
                        }
                        else if (currentTime - arrowRightLastRepeat >= KEY_REPEAT_INTERVAL)
                        {
                            arrowRightLastRepeat = currentTime;
                            shouldMoveRight = true;
                        }
                    }
                }
                else if (!rightArrowDown)
                {
                    arrowRightLastPress = -1;
                    arrowRightLastRepeat = -1;
                }
                
                if (shouldMoveLeft)
                {
                    // Re-check modifier keys right now
                    bool ctrlDownNow = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
                    bool shiftDownNow = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
                    
                    if (ctrlDownNow && shiftDownNow)
                    {
                        // Ctrl+Shift+Left: Extend selection to word start (including spaces before word)
                        if (!HasSelection())
                        {
                            selectionStart = cursorPosition;
                        }
                        cursorPosition = FindWordStartIncludingSpaces(cursorPosition);
                        selectionEnd = cursorPosition;
                    }
                    else if (ctrlDownNow)
                    {
                        // Ctrl+Left: Move cursor to word start (including spaces before word, cancel selection)
                        if (HasSelection())
                        {
                            cursorPosition = GetSelectionStart();
                            ClearSelection();
                        }
                        cursorPosition = FindWordStartIncludingSpaces(cursorPosition);
                        ClearSelection();
                    }
                    else if (shiftDownNow)
                    {
                        // Shift+Left: Extend selection
                        if (!HasSelection())
                        {
                            selectionStart = cursorPosition;
                        }
                        cursorPosition = Math.Max(0, cursorPosition - 1);
                        selectionEnd = cursorPosition;
                    }
                    else
                    {
                        // Left: Move cursor (cancel selection)
                        if (HasSelection())
                        {
                            cursorPosition = GetSelectionStart();
                            ClearSelection();
                        }
                        else
                        {
                            cursorPosition = Math.Max(0, cursorPosition - 1);
                        }
                    }
                }
                
                if (shouldMoveRight)
                {
                    // Re-check modifier keys right now
                    bool ctrlDownNow = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
                    bool shiftDownNow = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_SHIFT) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_SHIFT);
                    
                    if (ctrlDownNow && shiftDownNow)
                    {
                        // Ctrl+Shift+Right: Extend selection to word end (including spaces after word)
                        if (!HasSelection())
                        {
                            selectionStart = cursorPosition;
                        }
                        cursorPosition = FindWordEndIncludingSpaces(cursorPosition);
                        selectionEnd = cursorPosition;
                    }
                    else if (ctrlDownNow)
                    {
                        // Ctrl+Right: Move cursor to word end (including spaces after word, cancel selection)
                        if (HasSelection())
                        {
                            cursorPosition = GetSelectionEnd();
                            ClearSelection();
                        }
                        cursorPosition = FindWordEndIncludingSpaces(cursorPosition);
                        ClearSelection();
                    }
                    else if (shiftDownNow)
                    {
                        // Shift+Right: Extend selection
                        if (!HasSelection())
                        {
                            selectionStart = cursorPosition;
                        }
                        cursorPosition = Math.Min(Text.Length, cursorPosition + 1);
                        selectionEnd = cursorPosition;
                    }
                    else
                    {
                        // Right: Move cursor (cancel selection)
                        if (HasSelection())
                        {
                            cursorPosition = GetSelectionEnd();
                            ClearSelection();
                        }
                        else
                        {
                            cursorPosition = Math.Min(Text.Length, cursorPosition + 1);
                        }
                    }
                }
                
                // Handle text input (delete selection first)
                // Skip if Ctrl is held (for shortcuts like Ctrl+C, etc.)
                if (!ctrlDown)
                {
                    int key = Raylib.GetCharPressed();
                    while (key > 0)
                    {
                        // Only accept printable characters
                        if (key >= 32 && key < 127)
                        {
                            char c = (char)key;
                            
                            // Check if character is valid for the input constraint
                            if (IsValidChar(c))
                            {
                                if (HasSelection())
                                {
                                    DeleteSelection();
                                }
                                
                                Text = Text.Insert(cursorPosition, c.ToString());
                                cursorPosition++;
                                ValidateInput();
                                OnTextChanged?.Invoke(Text);
                            }
                        }
                        key = Raylib.GetCharPressed();
                    }
                }
                else
                {
                    // Ctrl is held - consume any characters to prevent processing
                    while (Raylib.GetCharPressed() > 0) { }
                }

                // Handle backspace with smooth repeating
                bool shouldBackspace = false;
                
                if (backspacePressed && !backspaceProcessedThisFrame)
                {
                    backspaceLastPress = currentTime;
                    backspaceLastRepeat = currentTime;
                    shouldBackspace = true;
                    backspaceProcessedThisFrame = true;
                }
                else if (backspaceDown && backspaceLastPress >= 0)
                {
                    double timeSincePress = currentTime - backspaceLastPress;
                    if (timeSincePress > KEY_REPEAT_DELAY)
                    {
                        // We've passed the initial delay, now repeat at intervals
                        if (backspaceLastRepeat < backspaceLastPress + KEY_REPEAT_DELAY)
                        {
                            backspaceLastRepeat = backspaceLastPress + KEY_REPEAT_DELAY;
                            shouldBackspace = true;
                        }
                        else if (currentTime - backspaceLastRepeat >= KEY_REPEAT_INTERVAL)
                        {
                            backspaceLastRepeat = currentTime;
                            shouldBackspace = true;
                        }
                    }
                }
                else if (!backspaceDown)
                {
                    backspaceLastPress = -1;
                    backspaceLastRepeat = -1;
                    backspaceProcessedThisFrame = false;
                }
                
                if (shouldBackspace)
                {
                    // Re-check modifier keys right now
                    bool ctrlDownNow = Raylib.IsKeyDown(KeyboardKey.KEY_LEFT_CONTROL) || Raylib.IsKeyDown(KeyboardKey.KEY_RIGHT_CONTROL);
                    
                    if (HasSelection())
                    {
                        DeleteSelection();
                        ValidateInput();
                    }
                    else if (ctrlDownNow && cursorPosition > 0)
                    {
                        // Ctrl+Backspace: Delete word and spaces before it, cursor ends at end of previous word
                        int deleteStart = FindWordStartIncludingSpaces(cursorPosition);
                        if (deleteStart < cursorPosition)
                        {
                            // Find where cursor should be after deletion (end of word before deletion start)
                            int newCursorPos = FindPreviousWordEnd(deleteStart);
                            
                            Text = Text.Substring(0, deleteStart) + Text.Substring(cursorPosition);
                            cursorPosition = newCursorPos;
                            ValidateInput();
                            OnTextChanged?.Invoke(Text);
                        }
                    }
                    else if (cursorPosition > 0)
                    {
                        // Regular backspace
                        Text = Text.Substring(0, cursorPosition - 1) + Text.Substring(cursorPosition);
                        cursorPosition--;
                        ValidateInput();
                        OnTextChanged?.Invoke(Text);
                    }
                }

                // Handle escape to unfocus
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
                {
                    IsFocused = false;
                    ClearSelection();
                }
            }
            else
            {
                // Clear selection when not focused
                ClearSelection();
            }
            
            // Update scroll offset to keep cursor visible
            UpdateScrollOffset();
        }
        
        private void UpdateScrollOffset()
        {
            // Reset scroll when not focused or when text is empty
            if (string.IsNullOrEmpty(Text) || !IsFocused)
            {
                scrollOffset = 0;
                return;
            }
            
            float availableWidth = Bounds.Width - (Padding * 2);
            
            // Calculate cursor position in pixels (relative to text start, not screen)
            string beforeCursor = Text.Substring(0, cursorPosition);
            float cursorX = FontManager.MeasureText(font, beforeCursor, fontSize);
            
            // Calculate full text width
            float fullTextWidth = FontManager.MeasureText(font, Text, fontSize);
            
            // If text fits entirely, no scrolling needed
            if (fullTextWidth <= availableWidth)
            {
                scrollOffset = 0;
                return;
            }
            
            // Calculate where cursor appears on screen with current scroll
            // The visible text area starts at Bounds.X + Padding
            // With scrollOffset, text is drawn at (Bounds.X + Padding - scrollOffset)
            // So cursor appears at: (Bounds.X + Padding - scrollOffset) + cursorX
            float visibleLeft = Bounds.X + Padding;
            float cursorScreenX = visibleLeft - scrollOffset + cursorX;
            float visibleRight = visibleLeft + availableWidth;
            
            // If cursor is to the left of visible area, scroll left (increase scrollOffset)
            if (cursorScreenX < visibleLeft)
            {
                scrollOffset = cursorX;
            }
            // If cursor is to the right of visible area, scroll right
            else if (cursorScreenX > visibleRight)
            {
                scrollOffset = cursorX - availableWidth;
            }
            
            // Clamp scroll offset to valid range
            float maxScroll = fullTextWidth - availableWidth;
            if (scrollOffset < 0)
            {
                scrollOffset = 0;
            }
            else if (scrollOffset > maxScroll)
            {
                scrollOffset = maxScroll;
            }
        }

        protected override void DrawSelf()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            // Background
            Color bgColor = IsFocused ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(Bounds, bgColor);
            
            // Border (red if invalid, otherwise normal)
            Color borderColor;
            if (IsInvalid)
            {
                borderColor = Color.RED;
            }
            else
            {
                borderColor = IsFocused ? UITheme.TextColor : UITheme.BorderColor;
            }
            Raylib.DrawRectangleLinesEx(Bounds, 1, borderColor);

            // Text or placeholder
            string displayText = string.IsNullOrEmpty(Text) ? Placeholder : Text;
            // Use grayed out color if disabled or if it's a placeholder
            Color textColor;
            if (!IsEnabled)
            {
                // When disabled, use a more muted gray color
                textColor = new Color(128, 128, 128, 255); // Gray color for disabled state
            }
            else if (string.IsNullOrEmpty(Text))
            {
                textColor = UITheme.TextSecondaryColor; // Placeholder color
            }
            else
            {
                textColor = UITheme.TextColor; // Normal text color
            }
            
            float textX = Bounds.X + Padding - scrollOffset;
            float textY = Bounds.Y + (Bounds.Height - fontSize) / 2;
            float availableWidth = Bounds.Width - (Padding * 2);
            
            // Draw selection highlight if there is one (only visible portion)
            if (IsFocused && HasSelection() && !string.IsNullOrEmpty(Text))
            {
                int selStart = GetSelectionStart();
                int selEnd = GetSelectionEnd();
                
                string beforeSelection = Text.Substring(0, selStart);
                string selection = Text.Substring(selStart, selEnd - selStart);
                
                float selectionX = textX + FontManager.MeasureText(font, beforeSelection, fontSize);
                float selectionWidth = FontManager.MeasureText(font, selection, fontSize);
                
                // Clamp selection rectangle to visible bounds
                float visibleSelectionX = Math.Max(selectionX, Bounds.X + Padding);
                float visibleSelectionWidth = Math.Min(selectionWidth, availableWidth - (visibleSelectionX - (Bounds.X + Padding)));
                if (visibleSelectionWidth > 0 && visibleSelectionX < Bounds.X + Bounds.Width - Padding)
                {
                    Rectangle selectionRect = new Rectangle(visibleSelectionX, textY, visibleSelectionWidth, fontSize);
                    Raylib.DrawRectangleRec(selectionRect, new Color(100, 150, 255, 150)); // Light blue selection
                }
            }
            
            // Draw text (clipped to visible area)
            float textWidth = FontManager.MeasureText(font, displayText, fontSize);
            Rectangle clipRect = new Rectangle(Bounds.X + Padding, Bounds.Y, availableWidth, Bounds.Height);
            
            // Only draw if text is within visible area
            if (textX + textWidth >= Bounds.X + Padding && textX < Bounds.X + Bounds.Width - Padding)
            {
                // Use scissor mode to clip text to bounds
                Raylib.BeginScissorMode((int)clipRect.X, (int)clipRect.Y, (int)clipRect.Width, (int)clipRect.Height);
                FontManager.DrawText(font, displayText, (int)textX, (int)textY, fontSize, textColor);
                Raylib.EndScissorMode();
            }
            
            // Draw cursor when focused (only if visible)
            if (IsFocused && !string.IsNullOrEmpty(Text) && (int)(Raylib.GetTime() * 2) % 2 == 0)
            {
                string beforeCursor = Text.Substring(0, cursorPosition);
                float cursorX = textX + FontManager.MeasureText(font, beforeCursor, fontSize);
                
                // Only draw cursor if it's visible
                if (cursorX >= Bounds.X + Padding && cursorX <= Bounds.X + Bounds.Width - Padding)
                {
                    Raylib.DrawLineEx(
                        new System.Numerics.Vector2(cursorX, textY),
                        new System.Numerics.Vector2(cursorX, textY + fontSize),
                        1.0f,
                        UITheme.TextColor
                    );
                }
            }
        }
    }
}

