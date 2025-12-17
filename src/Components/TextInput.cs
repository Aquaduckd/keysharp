using Raylib_cs;
using System;
using System.Text;
using Keysharp.UI;

namespace Keysharp.Components
{
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
        private bool backspaceProcessedThisFrame = false;
        
        // Cursor and selection
        private int cursorPosition = 0;
        private int selectionStart = -1; // -1 means no selection
        private int selectionEnd = -1;
        private bool isSelecting = false;
        private int mouseClickPosition = -1;
        
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
            OnTextChanged?.Invoke(text);
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
            float textX = Bounds.X + Padding;
            
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

            if (!IsVisible || !IsEnabled || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Handle focus and mouse selection
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                bool wasFocused = IsFocused;
                IsFocused = IsHovering(mouseX, mouseY);
                
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

            // Handle text input when focused
            if (IsFocused)
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
                            if (HasSelection())
                            {
                                DeleteSelection();
                            }
                            
                            Text = Text.Insert(cursorPosition, ((char)key).ToString());
                            cursorPosition++;
                            OnTextChanged?.Invoke(Text);
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
                            OnTextChanged?.Invoke(Text);
                        }
                    }
                    else if (cursorPosition > 0)
                    {
                        // Regular backspace
                        Text = Text.Substring(0, cursorPosition - 1) + Text.Substring(cursorPosition);
                        cursorPosition--;
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
            Color textColor = string.IsNullOrEmpty(Text) ? UITheme.TextSecondaryColor : UITheme.TextColor;
            
            float textX = Bounds.X + Padding;
            float textY = Bounds.Y + (Bounds.Height - fontSize) / 2;
            
            // Draw selection highlight if there is one
            if (IsFocused && HasSelection() && !string.IsNullOrEmpty(Text))
            {
                int selStart = GetSelectionStart();
                int selEnd = GetSelectionEnd();
                
                string beforeSelection = Text.Substring(0, selStart);
                string selection = Text.Substring(selStart, selEnd - selStart);
                
                float selectionX = textX + FontManager.MeasureText(font, beforeSelection, fontSize);
                float selectionWidth = FontManager.MeasureText(font, selection, fontSize);
                
                Rectangle selectionRect = new Rectangle(selectionX, textY, selectionWidth, fontSize);
                Raylib.DrawRectangleRec(selectionRect, new Color(100, 150, 255, 150)); // Light blue selection
            }
            
            // Draw text
            TextContainer.DrawLeftAlignedText(font, displayText, Bounds, fontSize, textColor, Padding);
            
            // Draw cursor when focused
            if (IsFocused && (int)(Raylib.GetTime() * 2) % 2 == 0)
            {
                string beforeCursor = Text.Substring(0, cursorPosition);
                float cursorX = textX + FontManager.MeasureText(font, beforeCursor, fontSize);
                Raylib.DrawLineEx(
                    new System.Numerics.Vector2(cursorX, Bounds.Y + Padding),
                    new System.Numerics.Vector2(cursorX, Bounds.Y + Bounds.Height - Padding),
                    1, UITheme.TextColor);
            }
        }
    }
}

