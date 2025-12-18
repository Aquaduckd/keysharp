using Raylib_cs;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class SidePanel : Panel
    {
        private Components.Container? keyInfoContainer;
        private Components.Label? titleLabel;
        private Components.Label? identifierLabel;
        private Components.TextInput? identifierInput;
        private Components.Container? identifierRowContainer;
        private Components.Label? positionLabel;
        private Components.TextInput? positionXInput;
        private Components.TextInput? positionYInput;
        private Components.Label? sizeLabel;
        private Components.TextInput? sizeWidthInput;
        private Components.TextInput? sizeHeightInput;
        private Components.Label? fingerLabel;
        private Components.Dropdown? fingerDropdown;
        private Components.Label? primaryCharacterLabel;
        private Components.TextInput? primaryCharacterInput;
        private Components.Container? primaryCharacterRowContainer;
        private Components.Label? shiftCharacterLabel;
        private Components.TextInput? shiftCharacterInput;
        private Components.Container? shiftCharacterRowContainer;
        private Components.Checkbox? shiftAutoCheckbox;
        private Components.Label? shiftAutoLabel;
        private Components.Label? disabledLabel;
        private Components.Checkbox? disabledCheckbox;
        private Components.Button? deleteKeyButton;
        private Components.Label? placeholderLabel;
        private HashSet<Keysharp.Core.PhysicalKey> selectedKeys = new HashSet<Keysharp.Core.PhysicalKey>();
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
        
        // Layout metadata controls
        private Components.Container? metadataContainer;
        private Components.Label? metadataHeaderLabel;
        private Components.TextInput? displayNameInput;
        private Components.TextInput? authorsInput;
        private Components.TextInput? creationDateInput;
        private Components.TextInput? descriptionInput;

        public SidePanel(Font font) : base(font, "SidePanel")
        {
            // Create metadata container (first)
            metadataContainer = new Components.Container("MetadataContainer");
            metadataContainer.AutoLayoutChildren = true;
            metadataContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            metadataContainer.AutoSize = true; // Auto-size based on content
            metadataContainer.ChildPadding = 0; // No padding, outer container handles it
            metadataContainer.ChildGap = 12;
            metadataContainer.PositionMode = Components.PositionMode.Relative;
            AddChild(metadataContainer);

            // Create metadata header label
            metadataHeaderLabel = new Components.Label(Font, "Layout Metadata", 18);
            metadataHeaderLabel.AutoSize = false;
            metadataHeaderLabel.Bounds = new Rectangle(0, 0, 0, 24);
            metadataHeaderLabel.PositionMode = Components.PositionMode.Relative;
            metadataContainer.AddChild(metadataHeaderLabel);

            // Create Display Name row
            var displayNameRow = CreateInfoRowWithInput(font, "Display Name:");
            displayNameInput = displayNameRow.input;
            displayNameInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    layoutTab.Metadata.DisplayName = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(displayNameRow.container);

            // Create Authors row (comma-separated)
            var authorsRow = CreateInfoRowWithInput(font, "Authors:");
            authorsInput = authorsRow.input;
            authorsInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    if (string.IsNullOrEmpty(text))
                    {
                        layoutTab.Metadata.Authors.Clear();
                    }
                    else
                    {
                        // Split by comma and trim each author
                        var authors = text.Split(',')
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrEmpty(a))
                            .ToList();
                        layoutTab.Metadata.Authors = authors;
                    }
                }
            };
            metadataContainer.AddChild(authorsRow.container);

            // Create Creation Date row
            var creationDateRow = CreateInfoRowWithInput(font, "Creation Date:");
            creationDateInput = creationDateRow.input;
            creationDateInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    layoutTab.Metadata.CreationDate = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(creationDateRow.container);

            // Create Description row (needs to be taller for multi-line)
            var descriptionRow = CreateInfoRowWithMultilineInput(font, "Description:");
            descriptionInput = descriptionRow.input;
            descriptionInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    layoutTab.Metadata.Description = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(descriptionRow.container);

            // Create color controls container for HSV inputs (second)
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

            // Create main container for key info (last)
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

            // Create identifier row (editable)
            var identifierRow = CreateInfoRowWithInput(font, "Identifier:");
            identifierLabel = identifierRow.label;
            identifierInput = identifierRow.input;
            identifierRowContainer = identifierRow.container;
            identifierInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count == 1)
                {
                    // Only allow editing identifier for single selection
                    var key = selectedKeys.First();
                    key.Identifier = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            keyInfoContainer.AddChild(identifierRowContainer);

            // Create position row with two inputs (X and Y)
            positionLabel = new Components.Label(font, "Position:", 14);
            positionLabel.AutoSize = false;
            positionLabel.Bounds = new Rectangle(0, 0, 90, 18);
            positionLabel.PositionMode = Components.PositionMode.Relative;
            
            var positionContainer = new Components.Container("PositionContainer");
            positionContainer.AutoLayoutChildren = true;
            positionContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            positionContainer.AutoSize = false;
            positionContainer.ChildPadding = 0;
            positionContainer.ChildGap = 8;
            positionContainer.PositionMode = Components.PositionMode.Relative;
            
            positionXInput = new Components.TextInput(font, "0.00", 14);
            positionXInput.Bounds = new Rectangle(0, 0, 80, 24);
            positionXInput.AutoSize = false;
            positionXInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            positionXInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0 && float.TryParse(text, out float value))
                {
                    // Find top-leftmost key to get reference position
                    PhysicalKey? refKey = null;
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    foreach (var key in selectedKeys)
                    {
                        if (key.X < minX || (key.X == minX && key.Y < minY))
                        {
                            minX = key.X;
                            minY = key.Y;
                            refKey = key;
                        }
                    }
                    
                    if (refKey != null)
                    {
                        // Calculate offset and apply to all selected keys
                        float offsetX = value - refKey.X;
                        foreach (var key in selectedKeys)
                        {
                            key.X += offsetX;
                            keyboardView?.InvalidateKeyCache(key);
                        }
                    }
                }
            };
            
            positionYInput = new Components.TextInput(font, "0.00", 14);
            positionYInput.Bounds = new Rectangle(0, 0, 80, 24);
            positionYInput.AutoSize = false;
            positionYInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            positionYInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0 && float.TryParse(text, out float value))
                {
                    // Find top-leftmost key to get reference position
                    PhysicalKey? refKey = null;
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    foreach (var key in selectedKeys)
                    {
                        if (key.X < minX || (key.X == minX && key.Y < minY))
                        {
                            minX = key.X;
                            minY = key.Y;
                            refKey = key;
                        }
                    }
                    
                    if (refKey != null)
                    {
                        // Calculate offset and apply to all selected keys
                        float offsetY = value - refKey.Y;
                        foreach (var key in selectedKeys)
                        {
                            key.Y += offsetY;
                            keyboardView?.InvalidateKeyCache(key);
                        }
                    }
                }
            };
            
            positionContainer.AddChild(positionXInput);
            positionContainer.AddChild(positionYInput);
            
            var positionRowContainer = new Components.Container("PositionRowContainer");
            positionRowContainer.AutoLayoutChildren = true;
            positionRowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            positionRowContainer.AutoSize = false;
            positionRowContainer.Bounds = new Rectangle(0, 0, 0, 24);
            positionRowContainer.ChildPadding = 0;
            positionRowContainer.ChildGap = 8;
            positionRowContainer.ChildJustification = Components.ChildJustification.Left;
            positionRowContainer.PositionMode = Components.PositionMode.Relative;
            positionRowContainer.AddChild(positionLabel);
            positionRowContainer.AddChild(positionContainer);
            keyInfoContainer.AddChild(positionRowContainer);

            // Create size row with two inputs (Width and Height)
            sizeLabel = new Components.Label(font, "Size:", 14);
            sizeLabel.AutoSize = false;
            sizeLabel.Bounds = new Rectangle(0, 0, 90, 18);
            sizeLabel.PositionMode = Components.PositionMode.Relative;
            
            var sizeContainer = new Components.Container("SizeContainer");
            sizeContainer.AutoLayoutChildren = true;
            sizeContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            sizeContainer.AutoSize = false;
            sizeContainer.ChildPadding = 0;
            sizeContainer.ChildGap = 8;
            sizeContainer.PositionMode = Components.PositionMode.Relative;
            
            sizeWidthInput = new Components.TextInput(font, "1.00", 14);
            sizeWidthInput.Bounds = new Rectangle(0, 0, 80, 24);
            sizeWidthInput.AutoSize = false;
            sizeWidthInput.InputConstraint = Components.InputType.Decimal; // Constrain to decimal values
            sizeWidthInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            sizeWidthInput.ScrollIncrementAmount = 0.25f; // Increment by 0.25 for size values
            sizeWidthInput.MinValue = 0.25f; // Minimum size to keep keys clickable (0.25U)
            sizeWidthInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0 && float.TryParse(text, out float value))
                {
                    // Clamp to minimum value
                    value = System.Math.Max(value, 0.25f);
                    // Apply to all selected keys
                    foreach (var key in selectedKeys)
                    {
                        key.Width = value;
                        keyboardView?.InvalidateKeyCache(key);
                    }
                }
            };
            
            sizeHeightInput = new Components.TextInput(font, "1.00", 14);
            sizeHeightInput.Bounds = new Rectangle(0, 0, 80, 24);
            sizeHeightInput.AutoSize = false;
            sizeHeightInput.InputConstraint = Components.InputType.Decimal; // Constrain to decimal values
            sizeHeightInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            sizeHeightInput.ScrollIncrementAmount = 0.25f; // Increment by 0.25 for size values
            sizeHeightInput.MinValue = 0.25f; // Minimum size to keep keys clickable (0.25U)
            sizeHeightInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0 && float.TryParse(text, out float value))
                {
                    // Clamp to minimum value
                    value = System.Math.Max(value, 0.25f);
                    // Apply to all selected keys
                    foreach (var key in selectedKeys)
                    {
                        key.Height = value;
                        keyboardView?.InvalidateKeyCache(key);
                    }
                }
            };
            
            sizeContainer.AddChild(sizeWidthInput);
            sizeContainer.AddChild(sizeHeightInput);
            
            var sizeRowContainer = new Components.Container("SizeRowContainer");
            sizeRowContainer.AutoLayoutChildren = true;
            sizeRowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            sizeRowContainer.AutoSize = false;
            sizeRowContainer.Bounds = new Rectangle(0, 0, 0, 24);
            sizeRowContainer.ChildPadding = 0;
            sizeRowContainer.ChildGap = 8;
            sizeRowContainer.ChildJustification = Components.ChildJustification.Left;
            sizeRowContainer.PositionMode = Components.PositionMode.Relative;
            sizeRowContainer.AddChild(sizeLabel);
            sizeRowContainer.AddChild(sizeContainer);
            keyInfoContainer.AddChild(sizeRowContainer);

            // Create finger row with dropdown
            var fingerRow = CreateInfoRowWithDropdown(font, "Finger:", GetFingerNames());
            fingerLabel = fingerRow.label;
            fingerDropdown = fingerRow.dropdown;
            fingerDropdown.OnSelectionChanged = (selectedItem) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    int index = fingerDropdown.SelectedIndex;
                    if (index >= 0 && index < GetFingerNames().Count)
                    {
                        Finger newFinger = GetFingerFromIndex(index);
                        // Apply to all selected keys
                        foreach (var key in selectedKeys)
                        {
                            key.Finger = newFinger;
                        }
                    }
                }
            };
            keyInfoContainer.AddChild(fingerRow.container);

            // Create primary character row (with editable text input)
            var primaryCharacterRow = CreateInfoRowWithInput(font, "Primary:");
            primaryCharacterLabel = primaryCharacterRow.label;
            primaryCharacterInput = primaryCharacterRow.input;
            primaryCharacterRowContainer = primaryCharacterRow.container;
            primaryCharacterInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    if (selectedKeys.Count == 1)
                    {
                        // Single selection: assign directly
                        var key = selectedKeys.First();
                        key.PrimaryCharacter = string.IsNullOrEmpty(text) ? null : text;
                        
                        // Auto-update shift character if checkbox is enabled
                        if (shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked && !string.IsNullOrEmpty(text))
                        {
                            key.ShiftCharacter = ConvertToShiftCharacter(text);
                            // Update shift input display without triggering callback
                            if (shiftCharacterInput != null)
                            {
                                isUpdatingFromKey = true;
                                shiftCharacterInput.SetText(key.ShiftCharacter ?? "");
                                isUpdatingFromKey = false;
                            }
                        }
                        else if (shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked && string.IsNullOrEmpty(text))
                        {
                            key.ShiftCharacter = null;
                            if (shiftCharacterInput != null)
                            {
                                isUpdatingFromKey = true;
                                shiftCharacterInput.SetText("");
                                isUpdatingFromKey = false;
                            }
                        }
                    }
                    else
                    {
                        // Multi-selection: parse space-separated characters and assign to keys in order
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        string[] characters = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        for (int i = 0; i < sortedKeys.Count; i++)
                        {
                            if (i < characters.Length)
                            {
                                sortedKeys[i].PrimaryCharacter = characters[i];
                                
                                // Auto-update shift character if checkbox is enabled
                                if (shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked)
                                {
                                    sortedKeys[i].ShiftCharacter = ConvertToShiftCharacter(characters[i]);
                                }
                            }
                            else
                            {
                                sortedKeys[i].PrimaryCharacter = null;
                                if (shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked)
                                {
                                    sortedKeys[i].ShiftCharacter = null;
                                }
                            }
                        }
                        
                        // Update shift input display for multi-select if checkbox is enabled
                        if (shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked && shiftCharacterInput != null)
                        {
                            isUpdatingFromKey = true;
                            var shiftChars = sortedKeys
                                .Select(k => k.ShiftCharacter ?? "")
                                .Where(c => !string.IsNullOrEmpty(c));
                            shiftCharacterInput.SetText(string.Join(" ", shiftChars));
                            isUpdatingFromKey = false;
                        }
                    }
                    layout?.RebuildMappings();
                }
            };
            keyInfoContainer.AddChild(primaryCharacterRowContainer);

            // Create shift character row (with editable text input)
            var shiftCharacterRow = CreateInfoRowWithInput(font, "Shift:");
            shiftCharacterLabel = shiftCharacterRow.label;
            shiftCharacterInput = shiftCharacterRow.input;
            shiftCharacterRowContainer = shiftCharacterRow.container;
            shiftCharacterInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    if (selectedKeys.Count == 1)
                    {
                        // Single selection: assign directly
                        var key = selectedKeys.First();
                        key.ShiftCharacter = string.IsNullOrEmpty(text) ? null : text;
                    }
                    else
                    {
                        // Multi-selection: parse space-separated characters and assign to keys in order
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        string[] characters = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        for (int i = 0; i < sortedKeys.Count; i++)
                        {
                            if (i < characters.Length)
                            {
                                sortedKeys[i].ShiftCharacter = characters[i];
                            }
                            else
                            {
                                sortedKeys[i].ShiftCharacter = null;
                            }
                        }
                    }
                    layout?.RebuildMappings();
                }
            };
            keyInfoContainer.AddChild(shiftCharacterRowContainer);

            // Create shift auto-fill checkbox row
            var shiftAutoRow = CreateInfoRowWithCheckbox(font, "Auto Shift:");
            shiftAutoLabel = shiftAutoRow.label;
            shiftAutoCheckbox = shiftAutoRow.checkbox;
            shiftAutoCheckbox.OnCheckedChanged = (isChecked) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    // When enabled, convert all primary characters to shift characters
                    if (isChecked)
                    {
                        foreach (var key in selectedKeys)
                        {
                            if (!string.IsNullOrEmpty(key.PrimaryCharacter))
                            {
                                key.ShiftCharacter = ConvertToShiftCharacter(key.PrimaryCharacter);
                            }
                            else
                            {
                                key.ShiftCharacter = null;
                            }
                        }
                        // Update shift input display
                        UpdateShiftInputDisplay();
                        layout?.RebuildMappings();
                    }
                    // Update shift input enabled state
                    if (shiftCharacterInput != null)
                    {
                        shiftCharacterInput.IsEnabled = !isChecked;
                    }
                }
            };
            keyInfoContainer.AddChild(shiftAutoRow.container);

            // Create disabled row with checkbox
            var disabledRow = CreateInfoRowWithCheckbox(font, "Disabled:");
            disabledLabel = disabledRow.label;
            disabledCheckbox = disabledRow.checkbox;
            disabledCheckbox.OnCheckedChanged = (isChecked) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    // Apply to all selected keys
                    foreach (var key in selectedKeys)
                    {
                        key.Disabled = isChecked;
                    }
                    // Notify layout tab to update checkbox visibility
                    layoutTab?.UpdateShowDisabledCheckboxVisibility();
                }
            };
            keyInfoContainer.AddChild(disabledRow.container);

            // Create delete key button (only visible when a key is selected)
            var deleteKeyButton = new Components.Button(Font, "Delete Key", 14);
            deleteKeyButton.Bounds = new Rectangle(0, 0, 0, 30);
            deleteKeyButton.AutoSize = true;
            deleteKeyButton.PositionMode = Components.PositionMode.Relative;
            deleteKeyButton.IsVisible = false; // Initially hidden
            deleteKeyButton.OnClick = DeleteSelectedKey;
            keyInfoContainer.AddChild(deleteKeyButton);
            this.deleteKeyButton = deleteKeyButton; // Store reference

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

        private (Components.Container container, Components.Label label, Components.TextInput input) CreateInfoRowWithMultilineInput(Font font, string labelText)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Vertical;
            row.AutoSize = true;
            row.ChildPadding = 0;
            row.ChildGap = 5;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 0, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var input = new Components.TextInput(font, "", 14);
            input.AutoSize = false;
            input.Bounds = new Rectangle(0, 0, 0, 60); // Taller for multi-line description
            input.PositionMode = Components.PositionMode.Relative;
            row.AddChild(input);

            return (row, label, input);
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
            hInput.InputConstraint = Components.InputType.Integer; // Constrain to integers
            hInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            hInput.ScrollIncrementAmount = 10.0f; // Increment by 10 for HSV values
            hInput.MinValue = 0.0f; // Hue range: 0-360
            hInput.MaxValue = 360.0f;
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
            sInput.InputConstraint = Components.InputType.Integer; // Constrain to integers
            sInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            sInput.ScrollIncrementAmount = 10.0f; // Increment by 10 for HSV values
            sInput.MinValue = 0.0f; // Saturation range: 0-100
            sInput.MaxValue = 100.0f;
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
            vInput.InputConstraint = Components.InputType.Integer; // Constrain to integers
            vInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            vInput.ScrollIncrementAmount = 10.0f; // Increment by 10 for HSV values
            vInput.MinValue = 0.0f; // Value/Brightness range: 0-100
            vInput.MaxValue = 100.0f;
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

        /// <summary>
        /// Sets the layout metadata and updates the UI.
        /// </summary>
        public void SetLayoutMetadata(LayoutMetadataJson metadata)
        {
            if (displayNameInput != null)
            {
                displayNameInput.Text = metadata.DisplayName ?? "";
            }
            if (authorsInput != null)
            {
                authorsInput.Text = string.Join(", ", metadata.Authors);
            }
            if (creationDateInput != null)
            {
                creationDateInput.Text = metadata.CreationDate ?? "";
            }
            if (descriptionInput != null)
            {
                descriptionInput.Text = metadata.Description ?? "";
            }
        }

        public void SetSelectedKey(Keysharp.Core.PhysicalKey? key)
        {
            selectedKeys.Clear();
            if (key != null)
            {
                selectedKeys.Add(key);
            }
            UpdateKeyInfo();
        }

        public void SetSelectedKeys(HashSet<Keysharp.Core.PhysicalKey> keys)
        {
            System.Console.WriteLine($"SidePanel.SetSelectedKeys called with {keys?.Count ?? 0} keys");
            selectedKeys = keys ?? new HashSet<Keysharp.Core.PhysicalKey>();
            System.Console.WriteLine($"SidePanel.selectedKeys now has {selectedKeys.Count} keys");
            UpdateKeyInfo();
        }

        private void UpdateKeyInfo()
        {
            bool hasKey = selectedKeys.Count > 0;
            bool isMultiSelect = selectedKeys.Count > 1;
            
            // Find top-leftmost key for position display
            PhysicalKey? topLeftKey = null;
            if (hasKey)
            {
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                foreach (var key in selectedKeys)
                {
                    if (key.X < minX || (key.X == minX && key.Y < minY))
                    {
                        minX = key.X;
                        minY = key.Y;
                        topLeftKey = key;
                    }
                }
            }
            
            // Show/hide delete button based on whether a key is selected
            if (deleteKeyButton != null)
            {
                deleteKeyButton.IsVisible = hasKey;
            }

            // Show/hide placeholder and info rows
            if (placeholderLabel != null)
            {
                placeholderLabel.IsVisible = !hasKey;
            }

            if (titleLabel != null)
            {
                titleLabel.IsVisible = hasKey;
            }

            // Hide identifier field container when multiple keys are selected
            if (identifierRowContainer != null)
            {
                identifierRowContainer.IsVisible = hasKey && !isMultiSelect;
                if (hasKey && !isMultiSelect && topLeftKey != null && identifierInput != null)
                {
                    isUpdatingFromKey = true;
                    identifierInput.SetText(topLeftKey.Identifier ?? "");
                    isUpdatingFromKey = false;
                }
            }

            // Position shows top-leftmost key's position
            if (positionLabel != null && positionXInput != null && positionYInput != null)
            {
                positionLabel.IsVisible = hasKey;
                positionXInput.IsVisible = hasKey;
                positionYInput.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    positionXInput.SetText(topLeftKey.X.ToString("F2"));
                    positionYInput.SetText(topLeftKey.Y.ToString("F2"));
                    isUpdatingFromKey = false;
                }
            }

            // Size: show first key's size, apply to all when edited
            if (sizeLabel != null && sizeWidthInput != null && sizeHeightInput != null)
            {
                sizeLabel.IsVisible = hasKey;
                sizeWidthInput.IsVisible = hasKey;
                sizeHeightInput.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    sizeWidthInput.SetText(topLeftKey.Width.ToString("F2"));
                    sizeHeightInput.SetText(topLeftKey.Height.ToString("F2"));
                    isUpdatingFromKey = false;
                }
            }

            // Finger: show first key's finger, apply to all when edited
            if (fingerLabel != null && fingerDropdown != null)
            {
                fingerLabel.IsVisible = hasKey;
                fingerDropdown.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    int fingerIndex = GetIndexFromFinger(topLeftKey.Finger);
                    fingerDropdown.SetSelectedIndex(fingerIndex, triggerCallback: false);
                    isUpdatingFromKey = false;
                }
            }

            // Primary/Shift characters: show for both single and multi-selection
            if (primaryCharacterRowContainer != null)
            {
                primaryCharacterRowContainer.IsVisible = hasKey;
                if (hasKey && primaryCharacterInput != null)
                {
                    isUpdatingFromKey = true;
                    if (isMultiSelect)
                    {
                        // Multi-selection: combine characters from all selected keys
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var primaryChars = sortedKeys
                            .Select(k => k.PrimaryCharacter ?? "")
                            .Where(c => !string.IsNullOrEmpty(c));
                        primaryCharacterInput.SetText(string.Join(" ", primaryChars));
                    }
                    else if (topLeftKey != null)
                    {
                        // Single selection: show single character
                        primaryCharacterInput.SetText(topLeftKey.PrimaryCharacter ?? "");
                    }
                    isUpdatingFromKey = false;
                }
            }

            if (shiftCharacterRowContainer != null)
            {
                shiftCharacterRowContainer.IsVisible = hasKey;
                if (hasKey && shiftCharacterInput != null)
                {
                    isUpdatingFromKey = true;
                    bool autoEnabled = shiftAutoCheckbox != null && shiftAutoCheckbox.IsChecked;
                    shiftCharacterInput.IsEnabled = !autoEnabled; // Disable input when auto is enabled
                    
                    if (isMultiSelect)
                    {
                        // Multi-selection: combine characters from all selected keys
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var shiftChars = sortedKeys
                            .Select(k => k.ShiftCharacter ?? "")
                            .Where(c => !string.IsNullOrEmpty(c));
                        shiftCharacterInput.SetText(string.Join(" ", shiftChars));
                    }
                    else if (topLeftKey != null)
                    {
                        // Single selection: show single character
                        shiftCharacterInput.SetText(topLeftKey.ShiftCharacter ?? "");
                    }
                    isUpdatingFromKey = false;
                }
            }

            // Shift auto checkbox: show when key is selected
            if (shiftAutoLabel != null && shiftAutoCheckbox != null)
            {
                shiftAutoLabel.IsVisible = hasKey;
                shiftAutoCheckbox.IsVisible = hasKey;
            }
            
            // Disabled: show if all keys have same state, apply to all when edited
            if (disabledLabel != null && disabledCheckbox != null)
            {
                disabledLabel.IsVisible = hasKey;
                disabledCheckbox.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    // Check if all selected keys have the same disabled state
                    bool allDisabled = selectedKeys.All(k => k.Disabled);
                    bool allEnabled = selectedKeys.All(k => !k.Disabled);
                    // If mixed state, show indeterminate (for now just show based on first key)
                    disabledCheckbox.IsChecked = allDisabled || (!allEnabled && topLeftKey.Disabled);
                    isUpdatingFromKey = false;
                }
            }
        }

        private void DeleteSelectedKey()
        {
            if (layout != null && selectedKeys.Count > 0)
            {
                // Remove all selected keys from the layout
                foreach (var key in selectedKeys)
                {
                    layout.RemovePhysicalKey(key);
                }
                // Rebuild mappings after removing keys
                layout.RebuildMappings();
                // Clear selection
                selectedKeys.Clear();
                // Notify keyboard view to clear selection
                if (keyboardView != null)
                {
                    var emptySelection = new HashSet<PhysicalKey>();
                    keyboardView.SelectedKeys = emptySelection;
                }
                // Update UI to reflect no key selected
                UpdateKeyInfo();
                // Invalidate keyboard view cache to force redraw
                if (keyboardView != null)
                {
                    keyboardView.Layout = layout; // This will trigger cache clear
                }
            }
        }

        private void UpdateShiftInputDisplay()
        {
            if (shiftCharacterInput == null || isUpdatingFromKey)
                return;
                
            isUpdatingFromKey = true;
            bool isMultiSelect = selectedKeys.Count > 1;
            
            if (isMultiSelect)
            {
                var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                var shiftChars = sortedKeys
                    .Select(k => k.ShiftCharacter ?? "")
                    .Where(c => !string.IsNullOrEmpty(c));
                shiftCharacterInput.SetText(string.Join(" ", shiftChars));
            }
            else if (selectedKeys.Count == 1)
            {
                var key = selectedKeys.First();
                shiftCharacterInput.SetText(key.ShiftCharacter ?? "");
            }
            
            isUpdatingFromKey = false;
        }
        
        private string ConvertToShiftCharacter(string primaryChar)
        {
            if (string.IsNullOrEmpty(primaryChar))
                return "";
            
            // Handle single character (most common case)
            if (primaryChar.Length == 1)
            {
                char c = primaryChar[0];
                
                // Letters: convert to uppercase
                if (c >= 'a' && c <= 'z')
                {
                    return char.ToUpper(c).ToString();
                }
                
                // Numbers and symbols on number row
                switch (c)
                {
                    case '1': return "!";
                    case '2': return "@";
                    case '3': return "#";
                    case '4': return "$";
                    case '5': return "%";
                    case '6': return "^";
                    case '7': return "&";
                    case '8': return "*";
                    case '9': return "(";
                    case '0': return ")";
                    case '-': return "_";
                    case '=': return "+";
                    case '[': return "{";
                    case ']': return "}";
                    case '\\': return "|";
                    case ';': return ":";
                    case '\'': return "\"";
                    case ',': return "<";
                    case '.': return ">";
                    case '/': return "?";
                    case '`': return "~";
                    default:
                        // For other characters, try uppercase first
                        if (char.IsLetter(c))
                        {
                            return char.ToUpper(c).ToString();
                        }
                        // Otherwise return as-is
                        return primaryChar;
                }
            }
            
            // For multi-character strings (shouldn't happen in normal use, but handle it)
            // Convert each character individually
            var converted = new System.Text.StringBuilder();
            foreach (char c in primaryChar)
            {
                converted.Append(ConvertToShiftCharacter(c.ToString()));
            }
            return converted.ToString();
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
                // Width accounts for 15px padding on each side
                float containerWidth = Bounds.Width - 30;
                keyInfoContainer.SetSize(containerWidth, 0); // Height will be calculated by auto-size
                keyInfoContainer.RelativePosition = new System.Numerics.Vector2(15, 15);
                
                if (metadataHeaderLabel != null)
                {
                    metadataHeaderLabel.SetSize(containerWidth, 24);
                }
                // Set input widths (accounting for label width)
                float labelWidth = 100;
                float inputWidth = containerWidth - labelWidth - 8; // 8 is gap
                
                // Find the parent containers of the inputs to set label and input sizes
                foreach (var child in metadataContainer.Children)
                {
                    if (child is Components.Container rowContainer && rowContainer.Children.Count >= 2)
                    {
                        var label = rowContainer.Children[0] as Components.Label;
                        var input = rowContainer.Children[1] as Components.TextInput;
                        if (label != null && input != null)
                        {
                            label.SetSize(labelWidth, label.Bounds.Height);
                            // For description, use taller height
                            float inputHeight = (input == descriptionInput) ? 60 : 24;
                            input.SetSize(inputWidth, inputHeight);
                        }
                    }
                }
            }
            
            // Set key info container size
            if (keyInfoContainer != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                // Set container width (height will be auto-calculated based on content)
                // Width accounts for 15px padding on each side
                float containerWidth = Bounds.Width - 30;
                keyInfoContainer.SetSize(containerWidth, 0); // Height will be calculated by auto-size
                
                // Set label widths to fill available space (accounting for container padding)
                float availableWidth = containerWidth - (keyInfoContainer.ChildPadding * 2);
                
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

                if (identifierLabel != null && identifierInput != null)
                {
                    identifierLabel.SetSize(labelWidth, 18);
                    identifierInput.SetSize(valueWidth, 24);
                }

                if (positionLabel != null && positionXInput != null && positionYInput != null)
                {
                    positionLabel.SetSize(labelWidth, 18);
                    // Position inputs share the available width (half each minus gap)
                    float inputWidth = (valueWidth - 8) / 2; // 8 is gap between inputs
                    positionXInput.SetSize(inputWidth, 24);
                    positionYInput.SetSize(inputWidth, 24);
                }

                if (sizeLabel != null && sizeWidthInput != null && sizeHeightInput != null)
                {
                    sizeLabel.SetSize(labelWidth, 18);
                    // Size inputs share the available width (half each minus gap)
                    float inputWidth = (valueWidth - 8) / 2; // 8 is gap between inputs
                    sizeWidthInput.SetSize(inputWidth, 24);
                    sizeHeightInput.SetSize(inputWidth, 24);
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
                
                if (shiftAutoLabel != null && shiftAutoCheckbox != null)
                {
                    shiftAutoLabel.SetSize(labelWidth, 18);
                    shiftAutoCheckbox.SetSize(16, 16);
                }

                if (disabledLabel != null && disabledCheckbox != null)
                {
                    disabledLabel.SetSize(labelWidth, 18);
                    disabledCheckbox.SetSize(16, 16);
                }
            }
            
            // Set metadata container size (position will be set after ResolveBounds in ResolveBounds method)
            if (metadataContainer != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                float availableWidth = Bounds.Width - 30; // Account for 15px padding on each side
                metadataContainer.SetSize(availableWidth, 0); // Auto-size height, width accounts for padding
                if (metadataHeaderLabel != null)
                {
                    metadataHeaderLabel.SetSize(availableWidth, 24);
                }
                // Set input widths (accounting for label width, similar to keyInfoContainer)
                float labelWidth = 100;
                float inputWidth = availableWidth - labelWidth - 8; // 8 is gap
                
                // Find the parent containers of the inputs to set label and input sizes
                // The inputs are in containers created by CreateInfoRowWithInput or CreateInfoRowWithMultilineInput
                // We need to iterate through metadataContainer's children and set sizes
                foreach (var child in metadataContainer.Children)
                {
                    if (child is Components.Container rowContainer && rowContainer.Children.Count >= 2)
                    {
                        var label = rowContainer.Children[0] as Components.Label;
                        var input = rowContainer.Children[1] as Components.TextInput;
                        if (label != null && input != null)
                        {
                            label.SetSize(labelWidth, label.Bounds.Height);
                            // For description, use taller height
                            float inputHeight = (input == descriptionInput) ? 60 : 24;
                            input.SetSize(inputWidth, inputHeight);
                        }
                    }
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
            
            // Position containers in order: metadata (first), colorControls (second), keyInfo (last)
            
            // Position metadata container first (at top)
            if (metadataContainer != null)
            {
                metadataContainer.RelativePosition = new System.Numerics.Vector2(15, 15);
                metadataContainer.ResolveBounds();
            }
            
            // Position color controls container below metadata container
            if (colorControlsContainer != null)
            {
                float colorControlsY;
                if (metadataContainer != null && metadataContainer.Bounds.Height > 0)
                {
                    colorControlsY = metadataContainer.RelativePosition.Y + metadataContainer.Bounds.Height + 15;
                }
                else
                {
                    colorControlsY = 15;
                }
                colorControlsContainer.RelativePosition = new System.Numerics.Vector2(15, colorControlsY);
                colorControlsContainer.ResolveBounds();
            }
            
            // Position key info container last (below color controls container or metadata container)
            if (keyInfoContainer != null)
            {
                float keyInfoY;
                if (colorControlsContainer != null && colorControlsContainer.IsVisible && colorControlsContainer.Bounds.Height > 0)
                {
                    keyInfoY = colorControlsContainer.RelativePosition.Y + colorControlsContainer.Bounds.Height + 15;
                }
                else if (metadataContainer != null && metadataContainer.Bounds.Height > 0)
                {
                    keyInfoY = metadataContainer.RelativePosition.Y + metadataContainer.Bounds.Height + 15;
                }
                else
                {
                    keyInfoY = 15;
                }
                keyInfoContainer.RelativePosition = new System.Numerics.Vector2(15, keyInfoY);
                keyInfoContainer.ResolveBounds();
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

