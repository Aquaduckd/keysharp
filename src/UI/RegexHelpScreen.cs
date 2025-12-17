using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Raylib_cs;
using Keysharp.Components;
using Keysharp;

namespace Keysharp.UI
{
    public class RegexHelpScreen : Components.UIElement
    {
        private Font font;
        private Components.Container? helpContainer;
        private Components.Container? tipsContainer;
        private Components.Label? titleLabel;
        private Components.TextInput? characterInput;
        private Components.TextInput? regexInput;
        private Components.TextInput? testTextInput;
        private Components.Button? pasteRegexButton;
        private Components.Label? testResultLabel;
        private List<(int start, int length)> matchPositions = new List<(int, int)>();
        private List<Components.Button> patternButtons = new List<Components.Button>();
        private List<Components.Button> staticPatternButtons = new List<Components.Button>();
        private Components.Button? closeButton;
        private Action<string>? onPatternSelected;
        private bool justShown = false; // Flag to ignore the click that opened the help screen
        
        // Pattern definitions: (label template, pattern template, isDynamic)
        private List<(string labelTemplate, string patternTemplate, bool isDynamic)> patternDefinitions = new List<(string, string, bool)>
        {
            ("Starts with exact", "^{0}", true),
            ("Ends with exact", "{0}$", true),
            ("Starts with any", "^[{0}]", true),
            ("Ends with any", "[{0}]$", true),
            ("Contains exact", "{0}", true),
            ("Contains any", "[{0}]", true),
            ("Any two in a row", "[{0}]{{2}}", true),
            ("Any three in a row", "[{0}]{{3}}", true),
            ("Any two, no repeats", "([{0}])(?!\\1)[{0}]", true)
        };

        public RegexHelpScreen(Font font) : base("RegexHelpScreen")
        {
            this.font = font;
            IsVisible = false;
            IsClickable = true;
            IsHoverable = true;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create main container
            helpContainer = new Components.Container("HelpContainer");
            helpContainer.AutoLayoutChildren = true;
            helpContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            helpContainer.AutoSize = true; // Auto-size to fit content
            helpContainer.ChildPadding = 15;
            helpContainer.ChildGap = 10;
            AddChild(helpContainer);

            // Create title
            titleLabel = new Components.Label(font, "Regex Help", 20);
            titleLabel.Bounds = new Rectangle(0, 0, 0, 30);
            helpContainer.AddChild(titleLabel);

            // Create tips section with 3 columns
            tipsContainer = new Components.Container("TipsContainer");
            tipsContainer.AutoLayoutChildren = true;
            tipsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            tipsContainer.AutoSize = true;
            tipsContainer.ChildPadding = 0;
            tipsContainer.ChildGap = 15;
            helpContainer.AddChild(tipsContainer);

            // Add tips organized into 3 columns
            var tips = new List<string>
            {
                "• . matches any single character",
                "• * matches zero or more (like 'none' or 'many')",
                "• + matches one or more (like 'at least one')",
                "• ? matches zero or one (like 'maybe one')",
                "• {n} matches exactly n times (e.g. {3} = 3 times)",
                "• {n,m} matches between n and m times",
                "• [abc] matches any one of a, b, or c",
                "• [^abc] matches any character except a, b, or c",
                "• [a-z] matches any lowercase letter",
                "• ^ matches start of text, $ matches end",
                "• \\d = digit, \\w = word, \\s = space/tab",
                "• \\D = not digit, \\W = not word, \\S = not space",
                "• | means OR (this|that matches 'this' or 'that')",
                "• () groups characters together",
                "• (?:) groups without capturing",
                "• (?=x) matches if followed by x (but doesn't include x)",
                "• (?!x) matches if NOT followed by x",
                "• (?<=x) matches if preceded by x",
                "• (?<!x) matches if NOT preceded by x",
                "• \\1 refers back to the first () group",
                "• \\b matches word boundaries (start/end of words)",
                "• \\B matches non-word boundaries"
            };

            // Split tips into 3 columns
            int tipsPerColumn = (tips.Count + 2) / 3; // Round up division
            for (int col = 0; col < 3; col++)
            {
                var tipColumn = new Components.Container($"TipColumn{col}");
                tipColumn.AutoLayoutChildren = true;
                tipColumn.LayoutDirection = Components.LayoutDirection.Vertical;
                tipColumn.AutoSize = true;
                tipColumn.ChildPadding = 0;
                tipColumn.ChildGap = 8;
                tipsContainer.AddChild(tipColumn);

                int startIdx = col * tipsPerColumn;
                int endIdx = Math.Min(startIdx + tipsPerColumn, tips.Count);
                for (int i = startIdx; i < endIdx; i++)
                {
                    var tipLabel = new Components.Label(font, tips[i], 14);
                    tipLabel.Bounds = new Rectangle(0, 0, 0, 20);
                    tipColumn.AddChild(tipLabel);
                }
            }

            // Create regex testing section
            var regexTestLabel = new Components.Label(font, "Test Your Regex:", 14);
            regexTestLabel.Bounds = new Rectangle(0, 0, 0, 24);
            helpContainer.AddChild(regexTestLabel);

            // Create container for regex input and paste button
            var regexInputContainer = new Components.Container("RegexInputContainer");
            regexInputContainer.AutoLayoutChildren = true;
            regexInputContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            regexInputContainer.AutoSize = true;
            regexInputContainer.ChildPadding = 0;
            regexInputContainer.ChildGap = 8;
            helpContainer.AddChild(regexInputContainer);

            // Create regex input
            regexInput = new Components.TextInput(font, "Enter regex pattern", 14);
            regexInput.Bounds = new Rectangle(0, 0, 0, 35);
            regexInput.OnTextChanged = (text) => {
                ValidateRegex(text);
                TestRegex(text);
            };
            regexInputContainer.AddChild(regexInput);

            // Create paste button
            pasteRegexButton = new Components.Button(font, "Use in Search", 14);
            pasteRegexButton.Bounds = new Rectangle(0, 0, 0, 35);
            pasteRegexButton.OnClick = () => {
                if (regexInput != null && !string.IsNullOrEmpty(regexInput.Text))
                {
                    onPatternSelected?.Invoke(regexInput.Text);
                    Hide();
                }
            };
            regexInputContainer.AddChild(pasteRegexButton);

            // Create test text input label
            var testTextLabel = new Components.Label(font, "Test Text:", 14);
            testTextLabel.Bounds = new Rectangle(0, 0, 0, 20);
            helpContainer.AddChild(testTextLabel);

            // Create test text input
            testTextInput = new Components.TextInput(font, "Enter text to test against", 14);
            testTextInput.Bounds = new Rectangle(0, 0, 0, 35);
            testTextInput.OnTextChanged = (text) => {
                if (regexInput != null)
                {
                    TestRegex(regexInput.Text);
                }
            };
            helpContainer.AddChild(testTextInput);

            // Create test result label
            testResultLabel = new Components.Label(font, "", 14);
            testResultLabel.Bounds = new Rectangle(0, 0, 0, 24);
            helpContainer.AddChild(testResultLabel);

            // Create pattern buttons section
            var patternsLabel = new Components.Label(font, "Common Patterns:", 14);
            patternsLabel.Bounds = new Rectangle(0, 0, 0, 24);
            helpContainer.AddChild(patternsLabel);

            // Create static pattern buttons (immediately after "Common Patterns:" label)
            var staticPatternsContainer = new Components.Container("StaticPatternsContainer");
            staticPatternsContainer.AutoLayoutChildren = true;
            staticPatternsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            staticPatternsContainer.AutoSize = true;
            staticPatternsContainer.ChildPadding = 0;
            staticPatternsContainer.ChildGap = 8;
            helpContainer.AddChild(staticPatternsContainer);

            // Define static patterns: (label, pattern)
            var staticPatterns = new List<(string label, string pattern)>
            {
                ("No whitespace", "^\\S+$"),
                ("No punctuation", "^[^.,!?;:]+$"),
                ("Lowercase", "^[a-z]+$"),
                ("Double letters", "([a-zA-Z])\\1")
            };

            foreach (var (label, pattern) in staticPatterns)
            {
                var button = new Components.Button(font, $"{label}: {pattern}", 14);
                button.Bounds = new Rectangle(0, 0, 0, 35);
                string capturedPattern = pattern; // Capture for closure
                button.OnClick = () => {
                    onPatternSelected?.Invoke(capturedPattern);
                    Hide();
                };
                staticPatternButtons.Add(button);
                staticPatternsContainer.AddChild(button);
            }

            // Create character input for dynamic patterns
            var inputLabel = new Components.Label(font, "Character/Text:", 14);
            inputLabel.Bounds = new Rectangle(0, 0, 0, 20);
            helpContainer.AddChild(inputLabel);

            characterInput = new Components.TextInput(font, "Enter character or text", 14);
            characterInput.Bounds = new Rectangle(0, 0, 0, 35);
            characterInput.OnTextChanged = (text) => UpdatePatternButtonLabels();
            helpContainer.AddChild(characterInput);

            // Create patterns container with vertical layout for rows
            var patternsContainer = new Components.Container("PatternsContainer");
            patternsContainer.AutoLayoutChildren = true;
            patternsContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            patternsContainer.AutoSize = true;
            patternsContainer.ChildPadding = 0;
            patternsContainer.ChildGap = 8;
            helpContainer.AddChild(patternsContainer);

            // Create pattern buttons from definitions, arranged in rows of 4
            const int buttonsPerRow = 4;
            Components.Container? currentRow = null;
            int buttonIndex = 0;

            foreach (var (labelTemplate, patternTemplate, isDynamic) in patternDefinitions)
            {
                // Start a new row if needed
                if (buttonIndex % buttonsPerRow == 0)
                {
                    currentRow = new Components.Container($"PatternRow{buttonIndex / buttonsPerRow}");
                    currentRow.AutoLayoutChildren = true;
                    currentRow.LayoutDirection = Components.LayoutDirection.Horizontal;
                    currentRow.AutoSize = true;
                    currentRow.ChildPadding = 0;
                    currentRow.ChildGap = 8;
                    patternsContainer.AddChild(currentRow);
                }

                string inputValue = characterInput?.Text ?? "X";
                string displayValue = string.IsNullOrEmpty(inputValue) ? "X" : inputValue;
                
                // Determine which escaping to use based on pattern template
                string escapedValue;
                if (patternTemplate.Contains("[{0}]"))
                {
                    // Character class pattern - use character class escaping
                    escapedValue = EscapeForCharacterClass(displayValue);
                }
                else
                {
                    // Regular pattern - use standard regex escaping
                    escapedValue = EscapeRegexSpecialChars(displayValue);
                }
                
                string label = isDynamic ? string.Format(labelTemplate, displayValue) : labelTemplate;
                string pattern = isDynamic ? string.Format(patternTemplate, escapedValue) : patternTemplate;
                
                var button = new Components.Button(font, $"{label}: {pattern}", 14);
                button.Bounds = new Rectangle(0, 0, 0, 35);
                
                // Capture values for closure
                string capturedLabelTemplate = labelTemplate;
                string capturedPatternTemplate = patternTemplate;
                bool capturedIsDynamic = isDynamic;
                
                button.OnClick = () => {
                    string finalPattern;
                    if (capturedIsDynamic)
                    {
                        string currentInput = characterInput?.Text ?? "X";
                        string inputValue = string.IsNullOrEmpty(currentInput) ? "X" : currentInput;
                        
                        // Determine which escaping to use based on pattern template
                        string escapedValue;
                        if (capturedPatternTemplate.Contains("[{0}]"))
                        {
                            // Character class pattern - use character class escaping
                            escapedValue = EscapeForCharacterClass(inputValue);
                        }
                        else
                        {
                            // Regular pattern - use standard regex escaping
                            escapedValue = EscapeRegexSpecialChars(inputValue);
                        }
                        
                        finalPattern = string.Format(capturedPatternTemplate, escapedValue);
                    }
                    else
                    {
                        finalPattern = capturedPatternTemplate;
                    }
                    onPatternSelected?.Invoke(finalPattern);
                    Hide();
                };
                patternButtons.Add(button);
                if (currentRow != null)
                {
                    currentRow.AddChild(button);
                }
                buttonIndex++;
            }

            // Create close button
            closeButton = new Components.Button(font, "Close", 14);
            closeButton.Bounds = new Rectangle(0, 0, 0, 35);
            closeButton.OnClick = () => { IsVisible = false; };
            helpContainer.AddChild(closeButton);
        }

        public void SetPatternSelectedCallback(Action<string> callback)
        {
            onPatternSelected = callback;
        }

        private void UpdatePatternButtonLabels()
        {
            if (characterInput == null || patternButtons.Count == 0)
                return;

            string inputValue = characterInput.Text ?? "X";
            string displayValue = string.IsNullOrEmpty(inputValue) ? "X" : inputValue;
            string escapedDisplayValue = EscapeRegexSpecialChars(displayValue);

            for (int i = 0; i < patternButtons.Count && i < patternDefinitions.Count; i++)
            {
                var (labelTemplate, patternTemplate, isDynamic) = patternDefinitions[i];
                if (isDynamic)
                {
                    // Determine which escaping to use based on pattern template
                    string escapedValue;
                    if (patternTemplate.Contains("[{0}]"))
                    {
                        // Character class pattern - use character class escaping
                        escapedValue = EscapeForCharacterClass(displayValue);
                    }
                    else
                    {
                        // Regular pattern - use standard regex escaping
                        escapedValue = escapedDisplayValue;
                    }
                    
                    string label = string.Format(labelTemplate, displayValue);
                    string pattern = string.Format(patternTemplate, escapedValue);
                    patternButtons[i].Text = $"{label}: {pattern}";
                    // Also update button height to match larger font
                    patternButtons[i].Bounds = new Rectangle(
                        patternButtons[i].Bounds.X,
                        patternButtons[i].Bounds.Y,
                        patternButtons[i].Bounds.Width,
                        35
                    );
                }
            }
        }

        private string EscapeRegexSpecialChars(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Escape special regex characters
            return System.Text.RegularExpressions.Regex.Escape(text);
        }

        private string EscapeForCharacterClass(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;

            // Inside character classes, only ], \, -, and ^ (at start) need escaping
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (c == ']' || c == '\\' || c == '-')
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
                else if (c == '^' && i == 0)
                {
                    sb.Append('\\');
                    sb.Append(c);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private void ValidateRegex(string pattern)
        {
            if (regexInput == null)
                return;

            if (string.IsNullOrEmpty(pattern))
            {
                regexInput.IsInvalid = false;
                return;
            }

            try
            {
                var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                regexInput.IsInvalid = false;
            }
            catch
            {
                regexInput.IsInvalid = true;
            }
        }

        private void TestRegex(string pattern)
        {
            if (testResultLabel == null || testTextInput == null)
                return;

            if (string.IsNullOrEmpty(pattern))
            {
                testResultLabel.SetText("");
                matchPositions.Clear(); // Clear highlights when pattern is empty
                return;
            }

            string testText = testTextInput.Text ?? "";
            if (string.IsNullOrEmpty(testText))
            {
                testResultLabel.SetText("");
                matchPositions.Clear(); // Clear highlights when test text is empty
                return;
            }

            try
            {
                var regex = new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromMilliseconds(100));
                var matches = regex.Matches(testText);
                
                // Store match positions for highlighting
                matchPositions.Clear();
                foreach (Match match in matches)
                {
                    matchPositions.Add((match.Index, match.Length));
                }
                
                if (matches.Count > 0)
                {
                    var matchValues = matches.Cast<Match>().Select(m => $"\"{m.Value}\"").Take(10).ToList();
                    string matchText = string.Join(", ", matchValues);
                    if (matches.Count > 10)
                    {
                        matchText += $" ... and {matches.Count - 10} more";
                    }
                    testResultLabel.SetText($"Found {matches.Count} match(es): {matchText}");
                }
                else
                {
                    testResultLabel.SetText("No matches found");
                    matchPositions.Clear();
                }
            }
            catch (Exception ex)
            {
                testResultLabel.SetText($"Invalid regex: {ex.Message}");
                matchPositions.Clear();
            }
        }

        public void Show()
        {
            IsVisible = true;
            justShown = true; // Set flag to ignore the next click (the one that opened us)
        }

        public void Hide()
        {
            IsVisible = false;
        }

        public override void Update()
        {
            int screenWidth = Raylib.GetScreenWidth();
            int screenHeight = Raylib.GetScreenHeight();
            
            // Fixed width for the help screen (wider for 5 columns with longer text)
            int helpWidth = 1400;

            if (!IsVisible)
            {
                // Set minimal bounds when not visible (for click detection)
                Bounds = new Rectangle(0, 0, helpWidth, 100);
                return;
            }

            // Set temporary bounds first (needed for relative positioning of children)
            // We'll update with the actual height after auto-sizing
            int tempHeight = helpContainer != null && helpContainer.Bounds.Height > 0 
                ? (int)helpContainer.Bounds.Height 
                : 700;
            Bounds = new Rectangle(
                (screenWidth - helpWidth) / 2,
                (screenHeight - tempHeight) / 2,
                helpWidth,
                tempHeight
            );

            if (helpContainer != null)
            {
                // Calculate available width for children (accounting for container's internal padding)
                float childWidth = helpWidth - (helpContainer.ChildPadding * 2);
                
                // Set container width (it will auto-size height based on content)
                // Start with a temporary height if we don't have one yet
                helpContainer.Bounds = new Rectangle(
                    0,
                    0,
                    helpWidth,
                    helpContainer.Bounds.Height > 0 ? helpContainer.Bounds.Height : 100
                );
                helpContainer.PositionMode = Components.PositionMode.Relative;
                helpContainer.RelativePosition = new System.Numerics.Vector2(0, 0);
                helpContainer.IsVisible = true;
                
                // Update title label width
                if (titleLabel != null)
                {
                    titleLabel.Bounds = new Rectangle(
                        titleLabel.Bounds.X,
                        titleLabel.Bounds.Y,
                        childWidth,
                        titleLabel.Bounds.Height
                    );
                }

                // Update tip column widths (3 columns)
                if (tipsContainer != null)
                {
                    const int tipColumnGap = 15;
                    float tipColumnWidth = (childWidth - (tipColumnGap * 2)) / 3;
                    foreach (var child in tipsContainer.Children)
                    {
                        if (child is Components.Container tipColumn && tipColumn.Name.StartsWith("TipColumn"))
                        {
                            tipColumn.Bounds = new Rectangle(
                                tipColumn.Bounds.X,
                                tipColumn.Bounds.Y,
                                tipColumnWidth,
                                tipColumn.Bounds.Height
                            );
                    }
                }
                }

                // Update regex input and paste button widths
                if (regexInput != null && pasteRegexButton != null)
                {
                    // Regex input takes most of the width, button takes fixed width
                    float pasteButtonWidth = 120;
                    float regexInputWidth = childWidth - pasteButtonWidth - 8; // 8 is gap
                    regexInput.Bounds = new Rectangle(
                        regexInput.Bounds.X,
                        regexInput.Bounds.Y,
                        regexInputWidth,
                        regexInput.Bounds.Height
                    );
                    pasteRegexButton.Bounds = new Rectangle(
                        pasteRegexButton.Bounds.X,
                        pasteRegexButton.Bounds.Y,
                        pasteButtonWidth,
                        pasteRegexButton.Bounds.Height
                    );
                }

                // Update test text input width
                if (testTextInput != null)
                {
                    testTextInput.Bounds = new Rectangle(
                        testTextInput.Bounds.X,
                        testTextInput.Bounds.Y,
                        childWidth,
                        testTextInput.Bounds.Height
                    );
                }

                // Update test result label width and height
                if (testResultLabel != null)
                {
                    testResultLabel.Bounds = new Rectangle(
                        testResultLabel.Bounds.X,
                        testResultLabel.Bounds.Y,
                        childWidth,
                        24
                    );
                }

                // Update character input width
                if (characterInput != null)
                {
                    characterInput.Bounds = new Rectangle(
                        characterInput.Bounds.X,
                        characterInput.Bounds.Y,
                        childWidth,
                        characterInput.Bounds.Height
                    );
                }
                
                // Update close button width
                if (closeButton != null)
                {
                    closeButton.Bounds = new Rectangle(
                        closeButton.Bounds.X,
                        closeButton.Bounds.Y,
                        childWidth,
                        closeButton.Bounds.Height
                    );
                }

                // Update static pattern button widths (distributed evenly)
                const int staticButtonsCount = 4;
                const int gapBetweenButtons = 8;
                if (staticPatternButtons.Count > 0)
                {
                    float staticButtonWidth = (childWidth - (gapBetweenButtons * (staticButtonsCount - 1))) / staticButtonsCount;
                    foreach (var button in staticPatternButtons)
                    {
                        button.Bounds = new Rectangle(
                            button.Bounds.X,
                            button.Bounds.Y,
                            staticButtonWidth,
                            button.Bounds.Height > 0 ? button.Bounds.Height : 35
                        );
                    }
                }

                // Update pattern button widths in rows (4 columns)
                // Calculate button width: (available width - gaps) / buttons per row
                const int buttonsPerRow = 4;
                float buttonWidth = (childWidth - (gapBetweenButtons * (buttonsPerRow - 1))) / buttonsPerRow;
                
                foreach (var button in patternButtons)
                {
                    if (button.Bounds.Width == 0 || button.Bounds.Width > childWidth)
                    {
                        button.Bounds = new Rectangle(
                            button.Bounds.X,
                            button.Bounds.Y,
                            buttonWidth,
                            button.Bounds.Height > 0 ? button.Bounds.Height : 35
                        );
                    }
                }
            }

            // Update the container so it can auto-size based on its children
            base.Update();

            // After container has auto-sized, use its calculated height to update help screen bounds
            if (helpContainer != null && helpContainer.Bounds.Height > 0)
            {
                int helpHeight = (int)helpContainer.Bounds.Height;
                
                // Only update if height changed to avoid unnecessary repositioning
                if (Bounds.Height != helpHeight)
                {
                    Bounds = new Rectangle(
                        (screenWidth - helpWidth) / 2,
                        (screenHeight - helpHeight) / 2,
                        helpWidth,
                        helpHeight
                    );
                }
            }

            // Handle clicks to close when clicking outside
            // Skip this check if we were just shown (ignore the click that opened us)
            if (justShown)
            {
                justShown = false; // Reset flag after one frame
            }
            else if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                int mouseX = Raylib.GetMouseX();
                int mouseY = Raylib.GetMouseY();
                
                // Check if clicked outside the help container (check if click is outside Bounds)
                if (mouseX < Bounds.X || mouseX > Bounds.X + Bounds.Width ||
                    mouseY < Bounds.Y || mouseY > Bounds.Y + Bounds.Height)
                {
                    IsVisible = false;
                    return;
                }
            }
        }

        protected override void DrawSelf()
        {
            if (!IsVisible)
                return;

            // Draw semi-transparent background overlay (full screen)
            Raylib.DrawRectangle(0, 0, Raylib.GetScreenWidth(), Raylib.GetScreenHeight(), new Color(0, 0, 0, 180));

            // Draw help panel background (centered)
            Raylib.DrawRectangleRec(Bounds, UITheme.MainPanelColor);
            Raylib.DrawRectangleLinesEx(Bounds, 2, UITheme.BorderColor);
        }

        public override void Draw()
        {
            // Draw the element and its children first
            base.Draw();

            // Then draw highlighting on top of the test text input
            if (IsVisible && testTextInput != null && testTextInput.IsVisible && matchPositions.Count > 0)
            {
                DrawMatchHighlights();
            }
        }

        private void DrawMatchHighlights()
        {
            if (testTextInput == null || string.IsNullOrEmpty(testTextInput.Text))
                return;

            string testText = testTextInput.Text;
            int fontSize = 14;
            const int Padding = 5; // TextInput padding

            // Calculate text start position (same as TextInput draws it)
            float textX = testTextInput.Bounds.X + Padding;
            float textY = testTextInput.Bounds.Y + (testTextInput.Bounds.Height - fontSize) / 2;

            // Draw highlighting rectangles for each match
            Color highlightColor = new Color(144, 238, 144, 120); // Semi-transparent light green

            foreach (var (start, length) in matchPositions)
            {
                if (start >= testText.Length || start + length > testText.Length)
                    continue;

                // Measure text before the match
                string beforeMatch = testText.Substring(0, start);
                float beforeWidth = FontManager.MeasureText(font, beforeMatch, fontSize);

                // Measure the matched text
                string matchedText = testText.Substring(start, length);
                float matchWidth = FontManager.MeasureText(font, matchedText, fontSize);

                // Draw highlight rectangle
                Rectangle highlightRect = new Rectangle(
                    textX + beforeWidth,
                    textY,
                    matchWidth,
                    fontSize
                );
                Raylib.DrawRectangleRec(highlightRect, highlightColor);
            }
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            if (!IsVisible)
                return false;
            
            // Allow clicking outside to close
            return true;
        }
    }
}

