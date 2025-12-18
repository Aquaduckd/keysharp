using Raylib_cs;
using System.Collections.Generic;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class SidePanel : Panel
    {
        private Components.Container? keyInfoContainer;
        private Components.Label? titleLabel;
        private Components.Label? identifierLabel;
        private Components.Label? identifierValue;
        private Components.Label? positionLabel;
        private Components.Label? positionValue;
        private Components.Label? sizeLabel;
        private Components.Label? sizeValue;
        private Components.Label? fingerLabel;
        private Components.Dropdown? fingerDropdown;
        private Components.Label? primaryCharacterLabel;
        private Components.TextInput? primaryCharacterInput;
        private Components.Label? shiftCharacterLabel;
        private Components.TextInput? shiftCharacterInput;
        private Components.Label? disabledLabel;
        private Components.Checkbox? disabledCheckbox;
        private Components.Label? placeholderLabel;
        private Keysharp.Core.PhysicalKey? selectedKey;
        private Core.Layout? layout; // Reference to layout for rebuilding mappings
        private bool isUpdatingFromKey = false; // Flag to prevent circular updates
        private LayoutTab? layoutTab; // Reference to layout tab for updating checkbox visibility
        
        // HSV color controls for heatmap
        private Components.Container? colorControlsContainer;
        private Components.Label? colorControlsHeaderLabel;
        private Components.TextInput? lowHInput;
        private Components.TextInput? lowSInput;
        private Components.TextInput? lowVInput;
        private Components.TextInput? midHInput;
        private Components.TextInput? midSInput;
        private Components.TextInput? midVInput;
        private Components.TextInput? highHInput;
        private Components.TextInput? highSInput;
        private Components.TextInput? highVInput;
        private Components.KeyboardLayoutView? keyboardView; // Reference to keyboard view for updating heatmap colors

        public SidePanel(Font font) : base(font, "SidePanel")
        {
            // Create main container for key info
            keyInfoContainer = new Components.Container("KeyInfoContainer");
            keyInfoContainer.AutoLayoutChildren = true;
            keyInfoContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            keyInfoContainer.AutoSize = true;
            keyInfoContainer.ChildPadding = 0; // No padding, outer container handles it
            keyInfoContainer.ChildGap = 12;
            keyInfoContainer.PositionMode = Components.PositionMode.Relative;
            AddChild(keyInfoContainer);

            // Create placeholder label for when no key is selected (add first so it appears at top)
            placeholderLabel = new Components.Label(font, "Click a key to view its information", 14, null, Components.Label.TextAlignment.Center);
            placeholderLabel.AutoSize = false;
            placeholderLabel.Bounds = new Rectangle(0, 0, 0, 18);
            placeholderLabel.PositionMode = Components.PositionMode.Relative;
            keyInfoContainer.AddChild(placeholderLabel);

            // Create title label
            titleLabel = new Components.Label(font, "Key Information", 18);
            titleLabel.AutoSize = false;
            titleLabel.Bounds = new Rectangle(0, 0, 0, 24);
            titleLabel.PositionMode = Components.PositionMode.Relative;
            keyInfoContainer.AddChild(titleLabel);

            // Create identifier row
            var identifierRow = CreateInfoRow(font, "Identifier:", "None");
            identifierLabel = identifierRow.label;
            identifierValue = identifierRow.value;
            keyInfoContainer.AddChild(identifierRow.container);

            // Create position row
            var positionRow = CreateInfoRow(font, "Position:", "(0.00U, 0.00U)");
            positionLabel = positionRow.label;
            positionValue = positionRow.value;
            keyInfoContainer.AddChild(positionRow.container);

            // Create size row
            var sizeRow = CreateInfoRow(font, "Size:", "0.00U × 0.00U");
            sizeLabel = sizeRow.label;
            sizeValue = sizeRow.value;
            keyInfoContainer.AddChild(sizeRow.container);

            // Create finger row with dropdown
            var fingerRow = CreateInfoRowWithDropdown(font, "Finger:", GetFingerNames());
            fingerLabel = fingerRow.label;
            fingerDropdown = fingerRow.dropdown;
            fingerDropdown.OnSelectionChanged = (selectedItem) => {
                if (!isUpdatingFromKey && selectedKey != null)
                {
                    int index = fingerDropdown.SelectedIndex;
                    if (index >= 0 && index < GetFingerNames().Count)
                    {
                        selectedKey.Finger = GetFingerFromIndex(index);
                    }
                }
            };
            keyInfoContainer.AddChild(fingerRow.container);

            // Create primary character row (with editable text input)
            var primaryCharacterRow = CreateInfoRowWithInput(font, "Primary:");
            primaryCharacterLabel = primaryCharacterRow.label;
            primaryCharacterInput = primaryCharacterRow.input;
            primaryCharacterInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKey != null)
                {
                    selectedKey.PrimaryCharacter = string.IsNullOrEmpty(text) ? null : text;
                    layout?.RebuildMappings();
                }
            };
            keyInfoContainer.AddChild(primaryCharacterRow.container);

            // Create shift character row (with editable text input)
            var shiftCharacterRow = CreateInfoRowWithInput(font, "Shift:");
            shiftCharacterLabel = shiftCharacterRow.label;
            shiftCharacterInput = shiftCharacterRow.input;
            shiftCharacterInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKey != null)
                {
                    selectedKey.ShiftCharacter = string.IsNullOrEmpty(text) ? null : text;
                    layout?.RebuildMappings();
                }
            };
            keyInfoContainer.AddChild(shiftCharacterRow.container);

            // Create disabled row with checkbox
            var disabledRow = CreateInfoRowWithCheckbox(font, "Disabled:");
            disabledLabel = disabledRow.label;
            disabledCheckbox = disabledRow.checkbox;
            disabledCheckbox.OnCheckedChanged = (isChecked) => {
                if (!isUpdatingFromKey && selectedKey != null)
                {
                    selectedKey.Disabled = isChecked;
                    // Notify layout tab to update checkbox visibility
                    layoutTab?.UpdateShowDisabledCheckboxVisibility();
                }
            };
            keyInfoContainer.AddChild(disabledRow.container);

            // Create color controls container for HSV inputs
            colorControlsContainer = new Components.Container("ColorControlsContainer");
            colorControlsContainer.AutoLayoutChildren = true;
            colorControlsContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            colorControlsContainer.AutoSize = false;
            colorControlsContainer.Bounds = new Rectangle(0, 0, 0, 135); // Height for header + 3 rows
            colorControlsContainer.ChildPadding = 0; // No padding, outer container handles it
            colorControlsContainer.ChildGap = 5;
            colorControlsContainer.PositionMode = Components.PositionMode.Relative;
            AddChild(colorControlsContainer);
            
            // Create header label
            colorControlsHeaderLabel = new Components.Label(Font, "Heatmap Colors", 16);
            colorControlsHeaderLabel.AutoSize = false;
            colorControlsHeaderLabel.Bounds = new Rectangle(0, 0, 0, 24);
            colorControlsHeaderLabel.PositionMode = Components.PositionMode.Relative;
            colorControlsContainer.AddChild(colorControlsHeaderLabel);
            
            // Create three rows: Low, Mid, High, each with H, S, V inputs
            CreateColorInputRow("Low:", out lowHInput, out lowSInput, out lowVInput, "210", "15", "30");
            CreateColorInputRow("Mid:", out midHInput, out midSInput, out midVInput, "200", "70", "60");
            CreateColorInputRow("High:", out highHInput, out highSInput, out highVInput, "130", "60", "90");
            
            // Initially hide the container (will be shown when heatmap mode is selected)
            colorControlsContainer.IsVisible = false;

            // Initially show placeholder, hide info rows
            UpdateKeyInfo();
        }

        private (Components.Container container, Components.Label label, Components.Label value) CreateInfoRow(Font font, string labelText, string valueText)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = true;
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 0, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var value = new Components.Label(font, valueText, 14);
            value.AutoSize = false;
            value.Bounds = new Rectangle(0, 0, 0, 18);
            value.PositionMode = Components.PositionMode.Relative;
            row.AddChild(value);

            return (row, label, value);
        }

        private (Components.Container container, Components.Label label, Components.TextInput input) CreateInfoRowWithInput(Font font, string labelText)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = true;
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 0, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var input = new Components.TextInput(font, "", 14);
            input.AutoSize = false;
            input.Bounds = new Rectangle(0, 0, 0, 24); // Slightly taller for text input
            input.PositionMode = Components.PositionMode.Relative;
            row.AddChild(input);

            return (row, label, input);
        }

        private (Components.Container container, Components.Label label, Components.Dropdown dropdown) CreateInfoRowWithDropdown(Font font, string labelText, List<string> options)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = true;
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 0, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var dropdown = new Components.Dropdown(font, options, 14);
            dropdown.AutoSize = false;
            dropdown.Bounds = new Rectangle(0, 0, 0, 35); // Standard dropdown height
            dropdown.PositionMode = Components.PositionMode.Relative;
            row.AddChild(dropdown);

            return (row, label, dropdown);
        }

        private (Components.Container container, Components.Label label, Components.Checkbox checkbox) CreateInfoRowWithCheckbox(Font font, string labelText)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = true;
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 0, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var checkbox = new Components.Checkbox(font, "", 14);
            checkbox.AutoSize = false;
            checkbox.Bounds = new Rectangle(0, 0, 16, 16);
            checkbox.PositionMode = Components.PositionMode.Relative;
            row.AddChild(checkbox);

            return (row, label, checkbox);
        }

        private List<string> GetFingerNames()
        {
            return new List<string>
            {
                "Left Pinky",
                "Left Ring",
                "Left Middle",
                "Left Index",
                "Left Thumb",
                "Right Thumb",
                "Right Index",
                "Right Middle",
                "Right Ring",
                "Right Pinky"
            };
        }

        private Finger GetFingerFromIndex(int index)
        {
            return index switch
            {
                0 => Finger.LeftPinky,
                1 => Finger.LeftRing,
                2 => Finger.LeftMiddle,
                3 => Finger.LeftIndex,
                4 => Finger.LeftThumb,
                5 => Finger.RightThumb,
                6 => Finger.RightIndex,
                7 => Finger.RightMiddle,
                8 => Finger.RightRing,
                9 => Finger.RightPinky,
                _ => Finger.LeftPinky
            };
        }

        private int GetIndexFromFinger(Finger finger)
        {
            return finger switch
            {
                Finger.LeftPinky => 0,
                Finger.LeftRing => 1,
                Finger.LeftMiddle => 2,
                Finger.LeftIndex => 3,
                Finger.LeftThumb => 4,
                Finger.RightThumb => 5,
                Finger.RightIndex => 6,
                Finger.RightMiddle => 7,
                Finger.RightRing => 8,
                Finger.RightPinky => 9,
                _ => 0
            };
        }

        private void CreateColorInputRow(string labelText, out Components.TextInput hInput, out Components.TextInput sInput, out Components.TextInput vInput, string hDefault, string sDefault, string vDefault)
        {
            var rowContainer = new Components.Container($"ColorInputRow_{labelText}");
            rowContainer.AutoLayoutChildren = true;
            rowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            rowContainer.AutoSize = false;
            rowContainer.Bounds = new Rectangle(0, 0, 0, 25);
            rowContainer.ChildPadding = 0;
            rowContainer.ChildGap = 8;
            rowContainer.PositionMode = Components.PositionMode.Relative;
            
            // Label for the row (Low/Mid/High)
            var label = new Components.Label(Font, labelText, 14);
            label.Bounds = new Rectangle(0, 0, 50, 25);
            label.AutoSize = false;
            rowContainer.AddChild(label);
            
            // H input with label
            var hLabel = new Components.Label(Font, "H:", 14);
            hLabel.Bounds = new Rectangle(0, 0, 20, 25);
            hLabel.AutoSize = false;
            hInput = new Components.TextInput(Font, "", 14);
            hInput.Bounds = new Rectangle(0, 0, 50, 25);
            hInput.SetText(hDefault);
            hInput.OnTextChanged = (text) => UpdateHeatmapColors();
            
            var hContainer = new Components.Container($"HContainer_{labelText}");
            hContainer.AutoLayoutChildren = true;
            hContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            hContainer.AutoSize = false;
            hContainer.Bounds = new Rectangle(0, 0, 75, 25);
            hContainer.ChildPadding = 0;
            hContainer.ChildGap = 5;
            hContainer.PositionMode = Components.PositionMode.Relative;
            hContainer.AddChild(hLabel);
            hContainer.AddChild(hInput);
            rowContainer.AddChild(hContainer);
            
            // S input with label
            var sLabel = new Components.Label(Font, "S:", 14);
            sLabel.Bounds = new Rectangle(0, 0, 20, 25);
            sLabel.AutoSize = false;
            sInput = new Components.TextInput(Font, "", 14);
            sInput.Bounds = new Rectangle(0, 0, 50, 25);
            sInput.SetText(sDefault);
            sInput.OnTextChanged = (text) => UpdateHeatmapColors();
            
            var sContainer = new Components.Container($"SContainer_{labelText}");
            sContainer.AutoLayoutChildren = true;
            sContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            sContainer.AutoSize = false;
            sContainer.Bounds = new Rectangle(0, 0, 75, 25);
            sContainer.ChildPadding = 0;
            sContainer.ChildGap = 5;
            sContainer.PositionMode = Components.PositionMode.Relative;
            sContainer.AddChild(sLabel);
            sContainer.AddChild(sInput);
            rowContainer.AddChild(sContainer);
            
            // V input with label
            var vLabel = new Components.Label(Font, "V:", 14);
            vLabel.Bounds = new Rectangle(0, 0, 20, 25);
            vLabel.AutoSize = false;
            vInput = new Components.TextInput(Font, "", 14);
            vInput.Bounds = new Rectangle(0, 0, 50, 25);
            vInput.SetText(vDefault);
            vInput.OnTextChanged = (text) => UpdateHeatmapColors();
            
            var vContainer = new Components.Container($"VContainer_{labelText}");
            vContainer.AutoLayoutChildren = true;
            vContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            vContainer.AutoSize = false;
            vContainer.Bounds = new Rectangle(0, 0, 75, 25);
            vContainer.ChildPadding = 0;
            vContainer.ChildGap = 5;
            vContainer.PositionMode = Components.PositionMode.Relative;
            vContainer.AddChild(vLabel);
            vContainer.AddChild(vInput);
            rowContainer.AddChild(vContainer);
            
            colorControlsContainer!.AddChild(rowContainer);
        }

        private void UpdateHeatmapColors()
        {
            // Parse HSV values and update keyboard view
            if (keyboardView == null || lowHInput == null || lowSInput == null || lowVInput == null ||
                midHInput == null || midSInput == null || midVInput == null ||
                highHInput == null || highSInput == null || highVInput == null)
                return;
            
            try
            {
                float lowH = float.Parse(lowHInput.Text);
                float lowS = float.Parse(lowSInput.Text) / 100f; // Convert percentage to 0-1
                float lowV = float.Parse(lowVInput.Text) / 100f;
                
                float midH = float.Parse(midHInput.Text);
                float midS = float.Parse(midSInput.Text) / 100f;
                float midV = float.Parse(midVInput.Text) / 100f;
                
                float highH = float.Parse(highHInput.Text);
                float highS = float.Parse(highSInput.Text) / 100f;
                float highV = float.Parse(highVInput.Text) / 100f;
                
                keyboardView.SetHeatmapColors(lowH, lowS, lowV, midH, midS, midV, highH, highS, highV);
            }
            catch
            {
                // Invalid input, ignore for now
            }
        }

        public void SetLayout(Core.Layout? layout)
        {
            this.layout = layout;
        }
        
        public void SetKeyboardView(Components.KeyboardLayoutView? keyboardView)
        {
            this.keyboardView = keyboardView;
            // Initialize heatmap colors when keyboard view is set
            UpdateHeatmapColors();
            // Update visibility based on current view mode
            UpdateColorControlsVisibility();
        }
        
        public void SetViewMode(Components.KeyboardViewMode viewMode)
        {
            UpdateColorControlsVisibility();
        }
        
        private void UpdateColorControlsVisibility()
        {
            if (colorControlsContainer != null && keyboardView != null)
            {
                // Show color controls only when heatmap mode is selected
                colorControlsContainer.IsVisible = keyboardView.ViewMode == Components.KeyboardViewMode.Heatmap;
            }
            else if (colorControlsContainer != null)
            {
                colorControlsContainer.IsVisible = false;
            }
        }

        public LayoutTab? LayoutTab
        {
            get => layoutTab;
            set => layoutTab = value;
        }

        public void SetSelectedKey(Keysharp.Core.PhysicalKey? key)
        {
            selectedKey = key;
            UpdateKeyInfo();
        }

        private void UpdateKeyInfo()
        {
            bool hasKey = selectedKey != null;

            // Show/hide placeholder and info rows
            if (placeholderLabel != null)
            {
                placeholderLabel.IsVisible = !hasKey;
            }

            if (titleLabel != null)
            {
                titleLabel.IsVisible = hasKey;
            }

            if (identifierLabel != null && identifierValue != null)
            {
                identifierLabel.IsVisible = hasKey;
                identifierValue.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    identifierValue.SetText(selectedKey.Identifier ?? "None");
                }
            }

            if (positionLabel != null && positionValue != null)
            {
                positionLabel.IsVisible = hasKey;
                positionValue.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    positionValue.SetText($"({selectedKey.X:F2}U, {selectedKey.Y:F2}U)");
                }
            }

            if (sizeLabel != null && sizeValue != null)
            {
                sizeLabel.IsVisible = hasKey;
                sizeValue.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    sizeValue.SetText($"{selectedKey.Width:F2}U × {selectedKey.Height:F2}U");
                }
            }

            if (fingerLabel != null && fingerDropdown != null)
            {
                fingerLabel.IsVisible = hasKey;
                fingerDropdown.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    isUpdatingFromKey = true;
                    int fingerIndex = GetIndexFromFinger(selectedKey.Finger);
                    fingerDropdown.SetSelectedIndex(fingerIndex, triggerCallback: false);
                    isUpdatingFromKey = false;
                }
            }

            if (primaryCharacterLabel != null && primaryCharacterInput != null)
            {
                primaryCharacterLabel.IsVisible = hasKey;
                primaryCharacterInput.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    isUpdatingFromKey = true;
                    primaryCharacterInput.SetText(selectedKey.PrimaryCharacter ?? "");
                    isUpdatingFromKey = false;
                }
            }

            if (shiftCharacterLabel != null && shiftCharacterInput != null)
            {
                shiftCharacterLabel.IsVisible = hasKey;
                shiftCharacterInput.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    isUpdatingFromKey = true;
                    shiftCharacterInput.SetText(selectedKey.ShiftCharacter ?? "");
                    isUpdatingFromKey = false;
                }
            }

            if (disabledLabel != null && disabledCheckbox != null)
            {
                disabledLabel.IsVisible = hasKey;
                disabledCheckbox.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    isUpdatingFromKey = true;
                    disabledCheckbox.IsChecked = selectedKey.Disabled;
                    isUpdatingFromKey = false;
                }
            }
        }

        private string GetFingerName(Finger finger)
        {
            return finger switch
            {
                Finger.LeftPinky => "Left Pinky",
                Finger.LeftRing => "Left Ring",
                Finger.LeftMiddle => "Left Middle",
                Finger.LeftIndex => "Left Index",
                Finger.LeftThumb => "Left Thumb",
                Finger.RightThumb => "Right Thumb",
                Finger.RightIndex => "Right Index",
                Finger.RightMiddle => "Right Middle",
                Finger.RightRing => "Right Ring",
                Finger.RightPinky => "Right Pinky",
                _ => finger.ToString()
            };
        }

        protected override void PrepareResolveBounds()
        {
            base.PrepareResolveBounds();

            // Set child sizes before ResolveBounds() runs (this is the correct place, not Update())
            if (keyInfoContainer != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                // Set container width (height will be auto-calculated based on content)
                keyInfoContainer.SetSize(Bounds.Width, 0); // Height will be calculated by auto-size
                keyInfoContainer.RelativePosition = new System.Numerics.Vector2(15, 15);
                
                // Set label widths to fill available space (accounting for container padding)
                float availableWidth = Bounds.Width - 30 - (keyInfoContainer.ChildPadding * 2);
                
                if (titleLabel != null)
                {
                    titleLabel.SetSize(availableWidth, 24);
                }

                if (placeholderLabel != null)
                {
                    placeholderLabel.SetSize(availableWidth, 18);
                }

                // Set info row label widths (fixed width for labels)
                float labelWidth = 90;
                float valueWidth = availableWidth - labelWidth - 8; // 8 is gap between label and value

                if (identifierLabel != null && identifierValue != null)
                {
                    identifierLabel.SetSize(labelWidth, 18);
                    identifierValue.SetSize(valueWidth, 18);
                }

                if (positionLabel != null && positionValue != null)
                {
                    positionLabel.SetSize(labelWidth, 18);
                    positionValue.SetSize(valueWidth, 18);
                }

                if (sizeLabel != null && sizeValue != null)
                {
                    sizeLabel.SetSize(labelWidth, 18);
                    sizeValue.SetSize(valueWidth, 18);
                }

                if (fingerLabel != null && fingerDropdown != null)
                {
                    fingerLabel.SetSize(labelWidth, 18);
                    fingerDropdown.SetSize(valueWidth, 35);
                }

                if (primaryCharacterLabel != null && primaryCharacterInput != null)
                {
                    primaryCharacterLabel.SetSize(labelWidth, 18);
                    primaryCharacterInput.SetSize(valueWidth, 24);
                }

                if (shiftCharacterLabel != null && shiftCharacterInput != null)
                {
                    shiftCharacterLabel.SetSize(labelWidth, 18);
                    shiftCharacterInput.SetSize(valueWidth, 24);
                }

                if (disabledLabel != null && disabledCheckbox != null)
                {
                    disabledLabel.SetSize(labelWidth, 18);
                    disabledCheckbox.SetSize(16, 16);
                }
            }
            
            // Set color controls container size (position will be set after ResolveBounds)
            if (colorControlsContainer != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                // Container width should account for padding (15px on each side)
                float availableWidth = Bounds.Width - 30;
                colorControlsContainer.SetSize(availableWidth, 135); // Header + 3 rows
                
                // Set header label width
                if (colorControlsHeaderLabel != null)
                {
                    colorControlsHeaderLabel.SetSize(availableWidth, 24);
                }
                
                // Set row container widths to fit within available width
                // Each row has: label (50) + gap (8) + H (75) + gap (8) + S (75) + gap (8) + V (75) = 299px
                // But we need to fit in availableWidth, so we'll scale down proportionally if needed
                // For now, let's use availableWidth for row containers
                foreach (var child in colorControlsContainer.Children)
                {
                    if (child is Components.Container rowContainer && rowContainer.Name.StartsWith("ColorInputRow_"))
                    {
                        rowContainer.SetSize(availableWidth, 25);
                    }
                }
            }
        }
        
        public override void ResolveBounds()
        {
            base.ResolveBounds();
            
            // Position color controls container directly below keyInfoContainer after bounds are resolved
            if (colorControlsContainer != null && keyInfoContainer != null)
            {
                // keyInfoContainer starts at y=15, add its height, then add a small gap (15px)
                float colorControlsY = 15 + keyInfoContainer.Bounds.Height + 15;
                colorControlsContainer.RelativePosition = new System.Numerics.Vector2(15, colorControlsY);
                // Need to resolve bounds again for colorControlsContainer since we changed its position
                colorControlsContainer.ResolveBounds();
            }
        }

        public override void Update()
        {
            base.Update();
            // No bounds setting here - that's done in PrepareResolveBounds()
        }

        public override void UpdateFont(Font newFont)
        {
            base.UpdateFont(newFont);
            // Update fonts in child components that use fonts
            // Note: Components store their own font references, so we'd need to update them individually
            // For now, this is a limitation - components will need to be recreated to use new fonts
            // This is acceptable for a debug feature
        }

        protected override void DrawPanelContent(Rectangle bounds)
        {
            // Draw background
            Raylib.DrawRectangleRec(bounds, UITheme.SidePanelColor);
            
            // Draw borders only on outer edges (left, top, bottom)
            // Right edge is handled by the vertical splitter
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Left
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y),
                1, UITheme.BorderColor); // Top
            Raylib.DrawLineEx(
                new System.Numerics.Vector2(bounds.X, bounds.Y + bounds.Height),
                new System.Numerics.Vector2(bounds.X + bounds.Width, bounds.Y + bounds.Height),
                1, UITheme.BorderColor); // Bottom
        }

        public void DrawDropdowns()
        {
            // Draw dropdown lists on top of everything
            if (fingerDropdown != null)
            {
                fingerDropdown.DrawDropdown();
            }
        }
    }
}

