using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        private Components.Label? rotationLabel;
        private Components.TextInput? rotationInput;
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
        private bool isSecondLayoutEnabled = false; // Track whether second layout is enabled
        
        // Second layout key info fields
        private Components.Container? keyInfoContainer2;
        private Components.Label? titleLabel2;
        private Components.Label? identifierLabel2;
        private Components.TextInput? identifierInput2;
        private Components.Container? identifierRowContainer2;
        private Components.Label? positionLabel2;
        private Components.TextInput? positionXInput2;
        private Components.TextInput? positionYInput2;
        private Components.Label? sizeLabel2;
        private Components.TextInput? sizeWidthInput2;
        private Components.TextInput? sizeHeightInput2;
        private Components.Label? rotationLabel2;
        private Components.TextInput? rotationInput2;
        private Components.Label? fingerLabel2;
        private Components.Dropdown? fingerDropdown2;
        private Components.Label? primaryCharacterLabel2;
        private Components.TextInput? primaryCharacterInput2;
        private Components.Container? primaryCharacterRowContainer2;
        private Components.Label? shiftCharacterLabel2;
        private Components.TextInput? shiftCharacterInput2;
        private Components.Container? shiftCharacterRowContainer2;
        private Components.Checkbox? shiftAutoCheckbox2;
        private Components.Label? shiftAutoLabel2;
        private Components.Label? disabledLabel2;
        private Components.Checkbox? disabledCheckbox2;
        private Components.Button? deleteKeyButton2;
        private Components.Label? placeholderLabel2;
        private HashSet<Keysharp.Core.PhysicalKey> selectedKeys2 = new HashSet<Keysharp.Core.PhysicalKey>();
        private Components.KeyboardLayoutView? keyboardView2; // Reference to keyboard view 2 for cache invalidation
        private bool isEditingLayout1 = true; // Track which layout's keys we're currently editing
        private bool isUpdatingFromKey = false; // Flag to prevent circular updates
        private LayoutTab? layoutTab; // Reference to layout tab for updating checkbox visibility
        
        // Scrolling
        private float scrollOffset = 0f; // Current scroll offset (positive = scrolled down)
        private const float ScrollSpeed = 20f; // Pixels per scroll wheel tick
        private const int ScrollbarWidth = 12; // Width of the scrollbar
        private bool isDraggingScrollbar = false;
        private int scrollbarDragStartY = 0;
        private float scrollbarDragStartOffset = 0f;
        
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
        private Components.Dropdown? metadataLayoutDropdown;
        private bool editingLayout1Metadata = true; // Track which layout's metadata we're editing
        private Components.TextInput? displayNameInput;
        private Components.TextInput? authorsInput;
        private Components.TextInput? creationDateInput;
        private Components.TextInput? descriptionInput;
        
        // Key mappings controls
        private Components.Container? mappingsContainer;
        private Components.Label? mappingsHeaderLabel;
        private Components.Button? addMappingButton;
        private Components.Button? saveMappingsButton;
        private Components.Button? loadMappingsButton;
        private Components.Button? applyMappingsButton;
        private Components.Button? deleteMappingsButton;
        private Components.Dropdown? mappingsDropdown;
        private Components.Table? mappingsTable;
        private Core.KeyMappings keyMappings = new Core.KeyMappings(); // Source ID -> Target ID

        public SidePanel(Font font) : base(font, "SidePanel")
        {
            // Disable auto-layout for SidePanel - we manually position containers in ResolveBounds()
            AutoLayoutChildren = false;
            LayoutDirection = Components.LayoutDirection.Vertical;
            ChildGap = 0; // No gap between main containers (they handle their own spacing)
            ChildPadding = 10; // Padding around all children
            
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

            // Create dropdown to switch between Layout 1 and Layout 2 metadata
            List<string> layoutOptions = new List<string> { "Layout 1", "Layout 2" };
            metadataLayoutDropdown = new Components.Dropdown(Font, layoutOptions, 14);
            metadataLayoutDropdown.SetBounds(new Rectangle(0, 0, 0, 30));
            metadataLayoutDropdown.AutoSize = false;
            metadataLayoutDropdown.SetSelectedItem("Layout 1"); // Default to Layout 1
            metadataLayoutDropdown.IsVisible = false; // Hidden by default (second layout is disabled by default)
            metadataLayoutDropdown.OnSelectionChanged = (selected) => {
                SwitchMetadataLayout(selected == "Layout 1");
            };
            metadataContainer.AddChild(metadataLayoutDropdown);

            // Create Display Name row
            var displayNameRow = CreateInfoRowWithInput(font, "Display Name:");
            displayNameInput = displayNameRow.input;
            displayNameInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    var metadata = editingLayout1Metadata ? layoutTab.Metadata : layoutTab.Metadata2;
                    metadata.DisplayName = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(displayNameRow.container);

            // Create Authors row (comma-separated)
            var authorsRow = CreateInfoRowWithInput(font, "Authors:");
            authorsInput = authorsRow.input;
            authorsInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    var metadata = editingLayout1Metadata ? layoutTab.Metadata : layoutTab.Metadata2;
                    if (string.IsNullOrEmpty(text))
                    {
                        metadata.Authors.Clear();
                    }
                    else
                    {
                        // Split by comma and trim each author
                        var authors = text.Split(',')
                            .Select(a => a.Trim())
                            .Where(a => !string.IsNullOrEmpty(a))
                            .ToList();
                        metadata.Authors = authors;
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
                    var metadata = editingLayout1Metadata ? layoutTab.Metadata : layoutTab.Metadata2;
                    metadata.CreationDate = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(creationDateRow.container);

            // Create Description row (needs to be taller for multi-line)
            var descriptionRow = CreateInfoRowWithMultilineInput(font, "Description:");
            descriptionInput = descriptionRow.input;
            descriptionInput.OnTextChanged = (text) => {
                if (layoutTab != null)
                {
                    var metadata = editingLayout1Metadata ? layoutTab.Metadata : layoutTab.Metadata2;
                    metadata.Description = string.IsNullOrEmpty(text) ? null : text;
                }
            };
            metadataContainer.AddChild(descriptionRow.container);

            // Create mappings container (before color controls)
            mappingsContainer = new Components.Container("MappingsContainer");
            mappingsContainer.AutoLayoutChildren = true;
            mappingsContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            mappingsContainer.AutoSize = false; // Size will be set in PrepareResolveBounds
            mappingsContainer.ChildPadding = 0;
            mappingsContainer.ChildGap = 8;
            mappingsContainer.PositionMode = Components.PositionMode.Relative;
            mappingsContainer.IsVisible = false; // Initially hidden (second layout disabled by default)
            AddChild(mappingsContainer);
            
            // Create header label
            mappingsHeaderLabel = new Components.Label(font, "Key Mappings", 18);
            mappingsHeaderLabel.AutoSize = false;
            mappingsHeaderLabel.Bounds = new Rectangle(0, 0, 0, 24);
            mappingsHeaderLabel.PositionMode = Components.PositionMode.Relative;
            mappingsContainer.AddChild(mappingsHeaderLabel);
            
            // Create first row button container (Load, Dropdown, Save)
            var mappingButtonsRow1 = new Components.Container("MappingButtonsRow1");
            mappingButtonsRow1.AutoLayoutChildren = true;
            mappingButtonsRow1.LayoutDirection = Components.LayoutDirection.Horizontal;
            mappingButtonsRow1.AutoSize = false;
            mappingButtonsRow1.Bounds = new Rectangle(0, 0, 0, 28);
            mappingButtonsRow1.ChildPadding = 0;
            mappingButtonsRow1.ChildGap = 5;
            mappingButtonsRow1.PositionMode = Components.PositionMode.Relative;
            mappingsContainer.AddChild(mappingButtonsRow1);
            
            // Create Load Mappings button (first row)
            loadMappingsButton = new Components.Button(font, "Load", 14);
            loadMappingsButton.AutoSize = false;
            loadMappingsButton.Bounds = new Rectangle(0, 0, 60, 28);
            loadMappingsButton.PositionMode = Components.PositionMode.Relative;
            loadMappingsButton.OnClick = () => {
                LoadMappings();
            };
            mappingButtonsRow1.AddChild(loadMappingsButton);
            
            // Create mappings dropdown (first row)
            mappingsDropdown = new Components.Dropdown(font, new List<string>(), 14);
            mappingsDropdown.AutoSize = false;
            mappingsDropdown.Bounds = new Rectangle(0, 0, 0, 28);
            mappingsDropdown.PositionMode = Components.PositionMode.Relative;
            mappingsDropdown.OnSelectionChanged = (selected) => {
                if (!string.IsNullOrEmpty(selected))
                {
                    LoadMappingFromFile(selected);
                }
            };
            RefreshMappingsDropdown();
            mappingButtonsRow1.AddChild(mappingsDropdown);
            
            // Create Save Mappings button (first row)
            saveMappingsButton = new Components.Button(font, "Save", 14);
            saveMappingsButton.AutoSize = false;
            saveMappingsButton.Bounds = new Rectangle(0, 0, 60, 28);
            saveMappingsButton.PositionMode = Components.PositionMode.Relative;
            saveMappingsButton.OnClick = () => {
                SaveMappings();
            };
            mappingButtonsRow1.AddChild(saveMappingsButton);
            
            // Create second row button container (Add, Delete, Apply)
            var mappingButtonsRow2 = new Components.Container("MappingButtonsRow2");
            mappingButtonsRow2.AutoLayoutChildren = true;
            mappingButtonsRow2.LayoutDirection = Components.LayoutDirection.Horizontal;
            mappingButtonsRow2.AutoSize = false;
            mappingButtonsRow2.Bounds = new Rectangle(0, 0, 0, 28);
            mappingButtonsRow2.ChildPadding = 0;
            mappingButtonsRow2.ChildGap = 5;
            mappingButtonsRow2.PositionMode = Components.PositionMode.Relative;
            mappingsContainer.AddChild(mappingButtonsRow2);
            
            // Create Add Mapping button (second row)
            addMappingButton = new Components.Button(font, "Add", 14);
            addMappingButton.AutoSize = false;
            addMappingButton.Bounds = new Rectangle(0, 0, 60, 28);
            addMappingButton.PositionMode = Components.PositionMode.Relative;
            addMappingButton.OnClick = () => {
                AddMappingFromIdentifiers();
            };
            mappingButtonsRow2.AddChild(addMappingButton);
            
            // Create Delete Mappings button (second row)
            deleteMappingsButton = new Components.Button(font, "Delete", 14);
            deleteMappingsButton.AutoSize = false;
            deleteMappingsButton.Bounds = new Rectangle(0, 0, 60, 28);
            deleteMappingsButton.PositionMode = Components.PositionMode.Relative;
            deleteMappingsButton.OnClick = () => {
                DeleteMappingsFromIdentifiers();
            };
            mappingButtonsRow2.AddChild(deleteMappingsButton);
            
            // Create Apply Mappings button (second row)
            applyMappingsButton = new Components.Button(font, "Apply", 14);
            applyMappingsButton.AutoSize = false;
            applyMappingsButton.Bounds = new Rectangle(0, 0, 60, 28);
            applyMappingsButton.PositionMode = Components.PositionMode.Relative;
            applyMappingsButton.OnClick = () => {
                ApplyMappings();
            };
            mappingButtonsRow2.AddChild(applyMappingsButton);
            
            // Create mappings table
            mappingsTable = new Components.Table(font, 12, "Source", "Target");
            mappingsTable.AutoSize = false;
            mappingsTable.Bounds = new Rectangle(0, 0, 0, 150); // Default height, will be adjusted
            mappingsTable.PositionMode = Components.PositionMode.Relative;
            mappingsTable.Rows = new List<List<string>>();
            mappingsContainer.AddChild(mappingsTable);

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
            titleLabel = new Components.Label(font, "Key Info (Layout 1)", 18);
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
                if (!isUpdatingFromKey && selectedKeys.Count > 0)
                {
                    if (selectedKeys.Count == 1)
                    {
                        // Single selection: assign directly
                        var key = selectedKeys.First();
                        key.Identifier = string.IsNullOrEmpty(text) ? null : text;
                    }
                    else
                    {
                        // Multi-selection: parse space-separated identifiers and assign to keys in order
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        string[] identifiers = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        for (int i = 0; i < sortedKeys.Count; i++)
                        {
                            if (i < identifiers.Length)
                            {
                                sortedKeys[i].Identifier = identifiers[i];
                            }
                            else
                            {
                                sortedKeys[i].Identifier = null;
                            }
                        }
                    }
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

            // Create rotation row with input
            rotationLabel = new Components.Label(font, "Rotation:", 14);
            rotationLabel.AutoSize = false;
            rotationLabel.Bounds = new Rectangle(0, 0, 90, 18);
            rotationLabel.PositionMode = Components.PositionMode.Relative;
            
            rotationInput = new Components.TextInput(font, "0.00", 14);
            rotationInput.Bounds = new Rectangle(0, 0, 80, 24);
            rotationInput.AutoSize = false;
            rotationInput.InputConstraint = Components.InputType.Decimal; // Constrain to decimal values
            rotationInput.EnableScrollIncrement = true; // Enable scroll wheel increment/decrement
            rotationInput.ScrollIncrementAmount = 5.0f; // Increment by 5 degrees
            rotationInput.MinValue = -90.0f; // Minimum rotation angle
            rotationInput.MaxValue = 90.0f; // Maximum rotation angle
            rotationInput.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys.Count > 0 && float.TryParse(text, out float value))
                {
                    // Clamp to -90 to 90 degree range
                    value = System.Math.Clamp(value, -90.0f, 90.0f);
                    
                    if (selectedKeys.Count == 1)
                    {
                        // Single selection: just update rotation
                        var key = selectedKeys.First();
                        key.Rotation = value;
                        keyboardView?.InvalidateKeyCache(key);
                    }
                    else
                    {
                        // Multi-selection: rotate keys around the center key
                        // Find center key (top-leftmost key)
                        PhysicalKey? centerKey = null;
                        float minX = float.MaxValue;
                        float minY = float.MaxValue;
                        foreach (var key in selectedKeys)
                        {
                            if (key.X < minX || (key.X == minX && key.Y < minY))
                            {
                                minX = key.X;
                                minY = key.Y;
                                centerKey = key;
                            }
                        }
                        
                        if (centerKey != null)
                        {
                            float centerX = centerKey.X + centerKey.Width / 2.0f;
                            float centerY = centerKey.Y + centerKey.Height / 2.0f;
                            
                            // Convert rotation to radians
                            float rotationRad = value * (float)(System.Math.PI / 180.0);
                            float cos = (float)System.Math.Cos(rotationRad);
                            float sin = (float)System.Math.Sin(rotationRad);
                            
                            // Get the original (unrotated) rotation to reverse it
                            float oldRotationRad = centerKey.Rotation * (float)(System.Math.PI / 180.0);
                            float oldCos = (float)System.Math.Cos(-oldRotationRad); // Reverse rotation
                            float oldSin = (float)System.Math.Sin(-oldRotationRad);
                            
                            // Update all keys
                            foreach (var key in selectedKeys)
                            {
                                if (key == centerKey)
                                {
                                    // Center key: only update rotation, position stays fixed
                                    key.Rotation = value;
                                }
                                else
                                {
                                    // Other keys: rotate around center key
                                    // First, get the current offset from center (already rotated by old rotation)
                                    float offsetX = (key.X + key.Width / 2.0f) - centerX;
                                    float offsetY = (key.Y + key.Height / 2.0f) - centerY;
                                    
                                    // Reverse the old rotation to get original offset
                                    float origOffsetX = offsetX * oldCos - offsetY * oldSin;
                                    float origOffsetY = offsetX * oldSin + offsetY * oldCos;
                                    
                                    // Apply new rotation to original offset
                                    float newOffsetX = origOffsetX * cos - origOffsetY * sin;
                                    float newOffsetY = origOffsetX * sin + origOffsetY * cos;
                                    
                                    // Update key position (center the key at the new offset position)
                                    key.X = centerX + newOffsetX - key.Width / 2.0f;
                                    key.Y = centerY + newOffsetY - key.Height / 2.0f;
                                    key.Rotation = value;
                                    keyboardView?.InvalidateKeyCache(key);
                                }
                            }
                            
                            // Also invalidate center key cache
                            keyboardView?.InvalidateKeyCache(centerKey);
                        }
                    }
                }
            };
            
            var rotationRowContainer = new Components.Container("RotationRowContainer");
            rotationRowContainer.AutoLayoutChildren = true;
            rotationRowContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            rotationRowContainer.AutoSize = false;
            rotationRowContainer.Bounds = new Rectangle(0, 0, 0, 24);
            rotationRowContainer.ChildPadding = 0;
            rotationRowContainer.ChildGap = 8;
            rotationRowContainer.ChildJustification = Components.ChildJustification.Left;
            rotationRowContainer.PositionMode = Components.PositionMode.Relative;
            rotationRowContainer.AddChild(rotationLabel);
            rotationRowContainer.AddChild(rotationInput);
            keyInfoContainer.AddChild(rotationRowContainer);

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
                    // Notify layout tab that layout has changed
                    layoutTab?.NotifyLayoutChanged();
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
                    // Notify layout tab that layout has changed
                    layoutTab?.NotifyLayoutChanged();
                }
            };
            keyInfoContainer.AddChild(shiftCharacterRowContainer);

            // Create shift auto-fill checkbox row
            var shiftAutoRow = CreateInfoRowWithCheckbox(font, "Auto Shift:");
            shiftAutoLabel = shiftAutoRow.label;
            shiftAutoCheckbox = shiftAutoRow.checkbox;
            shiftAutoCheckbox.IsChecked = true; // Enable autoshift by default
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
                    // Notify layout tab that layout has changed
                    layoutTab?.NotifyLayoutChanged();
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
            
            // Initially hide container (no keys selected yet)
            if (keyInfoContainer != null)
            {
                keyInfoContainer.IsVisible = false;
            }
            
            // Create second key info container for layout 2 (duplicate of first)
            CreateKeyInfoContainer2(font);
            
            // Initially hide container (no keys selected yet)
            if (keyInfoContainer2 != null)
            {
                keyInfoContainer2.IsVisible = false;
            }
        }
        
        private void CreateKeyInfoContainer2(Font font)
        {
            // Create main container for key info 2 (last)
            keyInfoContainer2 = new Components.Container("KeyInfoContainer2");
            keyInfoContainer2.AutoLayoutChildren = true;
            keyInfoContainer2.LayoutDirection = Components.LayoutDirection.Vertical;
            keyInfoContainer2.AutoSize = true;
            keyInfoContainer2.ChildPadding = 0; // No padding, outer container handles it
            keyInfoContainer2.ChildGap = 12;
            keyInfoContainer2.PositionMode = Components.PositionMode.Relative;
            AddChild(keyInfoContainer2);

            // Create placeholder label for when no key is selected (add first so it appears at top)
            placeholderLabel2 = new Components.Label(font, "Click a key to view its information (Layout 2)", 14, null, Components.Label.TextAlignment.Center);
            placeholderLabel2.AutoSize = false;
            placeholderLabel2.Bounds = new Rectangle(0, 0, 0, 18);
            placeholderLabel2.PositionMode = Components.PositionMode.Relative;
            keyInfoContainer2.AddChild(placeholderLabel2);

            // Create title label
            titleLabel2 = new Components.Label(font, "Key Info (Layout 2)", 18);
            titleLabel2.AutoSize = false;
            titleLabel2.Bounds = new Rectangle(0, 0, 0, 24);
            titleLabel2.PositionMode = Components.PositionMode.Relative;
            keyInfoContainer2.AddChild(titleLabel2);

            // Create identifier row (editable)
            var identifierRow2 = CreateInfoRowWithInput(font, "Identifier:");
            identifierLabel2 = identifierRow2.label;
            identifierInput2 = identifierRow2.input;
            identifierRowContainer2 = identifierRow2.container;
            identifierInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    if (selectedKeys2.Count == 1)
                    {
                        // Single selection: assign directly
                        var key = selectedKeys2.First();
                        key.Identifier = string.IsNullOrEmpty(text) ? null : text;
                    }
                    else
                    {
                        // Multi-selection: parse space-separated identifiers and assign to keys in order
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        string[] identifiers = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        
                        for (int i = 0; i < sortedKeys.Count; i++)
                        {
                            if (i < identifiers.Length)
                            {
                                sortedKeys[i].Identifier = identifiers[i];
                            }
                            else
                            {
                                sortedKeys[i].Identifier = null;
                            }
                        }
                    }
                }
            };
            keyInfoContainer2.AddChild(identifierRowContainer2);

            // Create position row with two inputs (X and Y)
            positionLabel2 = new Components.Label(font, "Position:", 14);
            positionLabel2.AutoSize = false;
            positionLabel2.Bounds = new Rectangle(0, 0, 90, 18);
            positionLabel2.PositionMode = Components.PositionMode.Relative;
            
            var positionContainer2 = new Components.Container("PositionContainer2");
            positionContainer2.AutoLayoutChildren = true;
            positionContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            positionContainer2.AutoSize = false;
            positionContainer2.ChildPadding = 0;
            positionContainer2.ChildGap = 8;
            positionContainer2.PositionMode = Components.PositionMode.Relative;
            
            positionXInput2 = new Components.TextInput(font, "0.00", 14);
            positionXInput2.Bounds = new Rectangle(0, 0, 80, 24);
            positionXInput2.AutoSize = false;
            positionXInput2.EnableScrollIncrement = true;
            positionXInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0 && float.TryParse(text, out float value))
                {
                    PhysicalKey? refKey = null;
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    foreach (var key in selectedKeys2)
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
                        float offsetX = value - refKey.X;
                        foreach (var key in selectedKeys2)
                        {
                            key.X += offsetX;
                            keyboardView2?.InvalidateKeyCache(key);
                        }
                    }
                }
            };
            
            positionYInput2 = new Components.TextInput(font, "0.00", 14);
            positionYInput2.Bounds = new Rectangle(0, 0, 80, 24);
            positionYInput2.AutoSize = false;
            positionYInput2.EnableScrollIncrement = true;
            positionYInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0 && float.TryParse(text, out float value))
                {
                    PhysicalKey? refKey = null;
                    float minX = float.MaxValue;
                    float minY = float.MaxValue;
                    foreach (var key in selectedKeys2)
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
                        float offsetY = value - refKey.Y;
                        foreach (var key in selectedKeys2)
                        {
                            key.Y += offsetY;
                            keyboardView2?.InvalidateKeyCache(key);
                        }
                    }
                }
            };
            
            positionContainer2.AddChild(positionXInput2);
            positionContainer2.AddChild(positionYInput2);
            
            var positionRowContainer2 = new Components.Container("PositionRowContainer2");
            positionRowContainer2.AutoLayoutChildren = true;
            positionRowContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            positionRowContainer2.AutoSize = false;
            positionRowContainer2.Bounds = new Rectangle(0, 0, 0, 24);
            positionRowContainer2.ChildPadding = 0;
            positionRowContainer2.ChildGap = 8;
            positionRowContainer2.ChildJustification = Components.ChildJustification.Left;
            positionRowContainer2.PositionMode = Components.PositionMode.Relative;
            positionRowContainer2.AddChild(positionLabel2);
            positionRowContainer2.AddChild(positionContainer2);
            keyInfoContainer2.AddChild(positionRowContainer2);

            // Create size row with two inputs (Width and Height)
            sizeLabel2 = new Components.Label(font, "Size:", 14);
            sizeLabel2.AutoSize = false;
            sizeLabel2.Bounds = new Rectangle(0, 0, 90, 18);
            sizeLabel2.PositionMode = Components.PositionMode.Relative;
            
            var sizeContainer2 = new Components.Container("SizeContainer2");
            sizeContainer2.AutoLayoutChildren = true;
            sizeContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            sizeContainer2.AutoSize = false;
            sizeContainer2.ChildPadding = 0;
            sizeContainer2.ChildGap = 8;
            sizeContainer2.PositionMode = Components.PositionMode.Relative;
            
            sizeWidthInput2 = new Components.TextInput(font, "1.00", 14);
            sizeWidthInput2.Bounds = new Rectangle(0, 0, 80, 24);
            sizeWidthInput2.AutoSize = false;
            sizeWidthInput2.InputConstraint = Components.InputType.Decimal;
            sizeWidthInput2.EnableScrollIncrement = true;
            sizeWidthInput2.ScrollIncrementAmount = 0.25f;
            sizeWidthInput2.MinValue = 0.25f;
            sizeWidthInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0 && float.TryParse(text, out float value))
                {
                    value = System.Math.Max(value, 0.25f);
                    foreach (var key in selectedKeys2)
                    {
                        key.Width = value;
                        keyboardView2?.InvalidateKeyCache(key);
                    }
                }
            };
            
            sizeHeightInput2 = new Components.TextInput(font, "1.00", 14);
            sizeHeightInput2.Bounds = new Rectangle(0, 0, 80, 24);
            sizeHeightInput2.AutoSize = false;
            sizeHeightInput2.InputConstraint = Components.InputType.Decimal;
            sizeHeightInput2.EnableScrollIncrement = true;
            sizeHeightInput2.ScrollIncrementAmount = 0.25f;
            sizeHeightInput2.MinValue = 0.25f;
            sizeHeightInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0 && float.TryParse(text, out float value))
                {
                    value = System.Math.Max(value, 0.25f);
                    foreach (var key in selectedKeys2)
                    {
                        key.Height = value;
                        keyboardView2?.InvalidateKeyCache(key);
                    }
                }
            };
            
            sizeContainer2.AddChild(sizeWidthInput2);
            sizeContainer2.AddChild(sizeHeightInput2);
            
            var sizeRowContainer2 = new Components.Container("SizeRowContainer2");
            sizeRowContainer2.AutoLayoutChildren = true;
            sizeRowContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            sizeRowContainer2.AutoSize = false;
            sizeRowContainer2.Bounds = new Rectangle(0, 0, 0, 24);
            sizeRowContainer2.ChildPadding = 0;
            sizeRowContainer2.ChildGap = 8;
            sizeRowContainer2.ChildJustification = Components.ChildJustification.Left;
            sizeRowContainer2.PositionMode = Components.PositionMode.Relative;
            sizeRowContainer2.AddChild(sizeLabel2);
            sizeRowContainer2.AddChild(sizeContainer2);
            keyInfoContainer2.AddChild(sizeRowContainer2);

            // Create rotation row with input
            rotationLabel2 = new Components.Label(font, "Rotation:", 14);
            rotationLabel2.AutoSize = false;
            rotationLabel2.Bounds = new Rectangle(0, 0, 90, 18);
            rotationLabel2.PositionMode = Components.PositionMode.Relative;
            
            rotationInput2 = new Components.TextInput(font, "0.00", 14);
            rotationInput2.Bounds = new Rectangle(0, 0, 80, 24);
            rotationInput2.AutoSize = false;
            rotationInput2.InputConstraint = Components.InputType.Decimal;
            rotationInput2.EnableScrollIncrement = true;
            rotationInput2.ScrollIncrementAmount = 5.0f;
            rotationInput2.MinValue = -90.0f;
            rotationInput2.MaxValue = 90.0f;
            rotationInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0 && float.TryParse(text, out float value))
                {
                    value = System.Math.Clamp(value, -90.0f, 90.0f);
                    if (selectedKeys2.Count == 1)
                    {
                        var key = selectedKeys2.First();
                        key.Rotation = value;
                        keyboardView2?.InvalidateKeyCache(key);
                    }
                    else
                    {
                        PhysicalKey? centerKey = null;
                        float minX = float.MaxValue;
                        float minY = float.MaxValue;
                        foreach (var key in selectedKeys2)
                        {
                            if (key.X < minX || (key.X == minX && key.Y < minY))
                            {
                                minX = key.X;
                                minY = key.Y;
                                centerKey = key;
                            }
                        }
                        if (centerKey != null)
                        {
                            float centerX = centerKey.X + centerKey.Width / 2.0f;
                            float centerY = centerKey.Y + centerKey.Height / 2.0f;
                            float rotationRad = value * (float)(System.Math.PI / 180.0);
                            float cos = (float)System.Math.Cos(rotationRad);
                            float sin = (float)System.Math.Sin(rotationRad);
                            float oldRotationRad = centerKey.Rotation * (float)(System.Math.PI / 180.0);
                            float oldCos = (float)System.Math.Cos(-oldRotationRad);
                            float oldSin = (float)System.Math.Sin(-oldRotationRad);
                            foreach (var key in selectedKeys2)
                            {
                                if (key == centerKey)
                                {
                                    key.Rotation = value;
                                }
                                else
                                {
                                    float offsetX = (key.X + key.Width / 2.0f) - centerX;
                                    float offsetY = (key.Y + key.Height / 2.0f) - centerY;
                                    float origOffsetX = offsetX * oldCos - offsetY * oldSin;
                                    float origOffsetY = offsetX * oldSin + offsetY * oldCos;
                                    float newOffsetX = origOffsetX * cos - origOffsetY * sin;
                                    float newOffsetY = origOffsetX * sin + origOffsetY * cos;
                                    key.X = centerX + newOffsetX - key.Width / 2.0f;
                                    key.Y = centerY + newOffsetY - key.Height / 2.0f;
                                    key.Rotation = value;
                                    keyboardView2?.InvalidateKeyCache(key);
                                }
                            }
                            keyboardView2?.InvalidateKeyCache(centerKey);
                        }
                    }
                }
            };
            
            var rotationRowContainer2 = new Components.Container("RotationRowContainer2");
            rotationRowContainer2.AutoLayoutChildren = true;
            rotationRowContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            rotationRowContainer2.AutoSize = false;
            rotationRowContainer2.Bounds = new Rectangle(0, 0, 0, 24);
            rotationRowContainer2.ChildPadding = 0;
            rotationRowContainer2.ChildGap = 8;
            rotationRowContainer2.ChildJustification = Components.ChildJustification.Left;
            rotationRowContainer2.PositionMode = Components.PositionMode.Relative;
            rotationRowContainer2.AddChild(rotationLabel2);
            rotationRowContainer2.AddChild(rotationInput2);
            keyInfoContainer2.AddChild(rotationRowContainer2);

            // Create finger row with dropdown
            var fingerRow2 = CreateInfoRowWithDropdown(font, "Finger:", GetFingerNames());
            fingerLabel2 = fingerRow2.label;
            fingerDropdown2 = fingerRow2.dropdown;
            fingerDropdown2.OnSelectionChanged = (selectedItem) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    int index = fingerDropdown2.SelectedIndex;
                    if (index >= 0 && index < GetFingerNames().Count)
                    {
                        Finger newFinger = GetFingerFromIndex(index);
                        foreach (var key in selectedKeys2)
                        {
                            key.Finger = newFinger;
                        }
                    }
                }
            };
            keyInfoContainer2.AddChild(fingerRow2.container);

            // Create primary character row
            var primaryCharacterRow2 = CreateInfoRowWithInput(font, "Primary:");
            primaryCharacterLabel2 = primaryCharacterRow2.label;
            primaryCharacterInput2 = primaryCharacterRow2.input;
            primaryCharacterRowContainer2 = primaryCharacterRow2.container;
            primaryCharacterInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    if (selectedKeys2.Count == 1)
                    {
                        var key = selectedKeys2.First();
                        key.PrimaryCharacter = string.IsNullOrEmpty(text) ? null : text;
                        if (shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked && !string.IsNullOrEmpty(text))
                        {
                            key.ShiftCharacter = ConvertToShiftCharacter(text);
                            if (shiftCharacterInput2 != null)
                            {
                                isUpdatingFromKey = true;
                                shiftCharacterInput2.SetText(key.ShiftCharacter ?? "");
                                isUpdatingFromKey = false;
                            }
                        }
                        else if (shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked && string.IsNullOrEmpty(text))
                        {
                            key.ShiftCharacter = null;
                            if (shiftCharacterInput2 != null)
                            {
                                isUpdatingFromKey = true;
                                shiftCharacterInput2.SetText("");
                                isUpdatingFromKey = false;
                            }
                        }
                    }
                    else
                    {
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        string[] characters = text.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                        for (int i = 0; i < sortedKeys.Count; i++)
                        {
                            if (i < characters.Length)
                            {
                                sortedKeys[i].PrimaryCharacter = characters[i];
                                if (shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked)
                                {
                                    sortedKeys[i].ShiftCharacter = ConvertToShiftCharacter(characters[i]);
                                }
                            }
                            else
                            {
                                sortedKeys[i].PrimaryCharacter = null;
                                if (shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked)
                                {
                                    sortedKeys[i].ShiftCharacter = null;
                                }
                            }
                        }
                        if (shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked && shiftCharacterInput2 != null)
                        {
                            isUpdatingFromKey = true;
                            var shiftChars = sortedKeys.Select(k => k.ShiftCharacter ?? "").Where(c => !string.IsNullOrEmpty(c));
                            shiftCharacterInput2.SetText(string.Join(" ", shiftChars));
                            isUpdatingFromKey = false;
                        }
                    }
                    layout?.RebuildMappings();
                    layoutTab?.NotifyLayout2Changed(); // Notify that layout 2 has changed
                }
            };
            keyInfoContainer2.AddChild(primaryCharacterRowContainer2);

            // Create shift character row
            var shiftCharacterRow2 = CreateInfoRowWithInput(font, "Shift:");
            shiftCharacterLabel2 = shiftCharacterRow2.label;
            shiftCharacterInput2 = shiftCharacterRow2.input;
            shiftCharacterRowContainer2 = shiftCharacterRow2.container;
            shiftCharacterInput2.OnTextChanged = (text) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    if (selectedKeys2.Count == 1)
                    {
                        var key = selectedKeys2.First();
                        key.ShiftCharacter = string.IsNullOrEmpty(text) ? null : text;
                    }
                    else
                    {
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
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
                    layoutTab?.NotifyLayout2Changed(); // Notify that layout 2 has changed
                }
            };
            keyInfoContainer2.AddChild(shiftCharacterRowContainer2);

            // Create shift auto-fill checkbox row
            var shiftAutoRow2 = CreateInfoRowWithCheckbox(font, "Auto Shift:");
            shiftAutoLabel2 = shiftAutoRow2.label;
            shiftAutoCheckbox2 = shiftAutoRow2.checkbox;
            shiftAutoCheckbox2.IsChecked = true;
            shiftAutoCheckbox2.OnCheckedChanged = (isChecked) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    if (isChecked)
                    {
                        foreach (var key in selectedKeys2)
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
                        UpdateShiftInputDisplay2();
                        layout?.RebuildMappings();
                        layoutTab?.NotifyLayout2Changed(); // Notify that layout 2 has changed
                    }
                    if (shiftCharacterInput2 != null)
                    {
                        shiftCharacterInput2.IsEnabled = !isChecked;
                    }
                }
            };
            keyInfoContainer2.AddChild(shiftAutoRow2.container);

            // Create disabled row with checkbox
            var disabledRow2 = CreateInfoRowWithCheckbox(font, "Disabled:");
            disabledLabel2 = disabledRow2.label;
            disabledCheckbox2 = disabledRow2.checkbox;
            disabledCheckbox2.OnCheckedChanged = (isChecked) => {
                if (!isUpdatingFromKey && selectedKeys2.Count > 0)
                {
                    foreach (var key in selectedKeys2)
                    {
                        key.Disabled = isChecked;
                    }
                    layoutTab?.UpdateShowDisabledCheckboxVisibility();
                }
            };
            keyInfoContainer2.AddChild(disabledRow2.container);

            // Create delete key button
            var deleteKeyButton2 = new Components.Button(Font, "Delete Key", 14);
            deleteKeyButton2.Bounds = new Rectangle(0, 0, 0, 30);
            deleteKeyButton2.AutoSize = true;
            deleteKeyButton2.PositionMode = Components.PositionMode.Relative;
            deleteKeyButton2.IsVisible = false;
            deleteKeyButton2.OnClick = DeleteSelectedKey2;
            keyInfoContainer2.AddChild(deleteKeyButton2);
            this.deleteKeyButton2 = deleteKeyButton2;

            // Initially show placeholder, hide info rows
            UpdateKeyInfo2();
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
            row.AutoSize = false;
            row.Bounds = new Rectangle(0, 0, 0, 24);
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.ChildJustification = Components.ChildJustification.Left;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 90, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var input = new Components.TextInput(font, "", 14);
            input.AutoSize = false;
            input.Bounds = new Rectangle(0, 0, 0, 24); // Width 0 means expand to fill remaining space
            input.PositionMode = Components.PositionMode.Relative;
            row.AddChild(input);

            return (row, label, input);
        }

        private (Components.Container container, Components.Label label, Components.Dropdown dropdown) CreateInfoRowWithDropdown(Font font, string labelText, List<string> options)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = false;
            row.Bounds = new Rectangle(0, 0, 0, 35);
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.ChildJustification = Components.ChildJustification.Left;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 90, 18);
            label.PositionMode = Components.PositionMode.Relative;
            row.AddChild(label);

            var dropdown = new Components.Dropdown(font, options, 14);
            dropdown.AutoSize = false;
            dropdown.Bounds = new Rectangle(0, 0, 0, 35); // Width 0 means expand to fill remaining space
            dropdown.PositionMode = Components.PositionMode.Relative;
            row.AddChild(dropdown);

            return (row, label, dropdown);
        }

        private (Components.Container container, Components.Label label, Components.Checkbox checkbox) CreateInfoRowWithCheckbox(Font font, string labelText)
        {
            var row = new Components.Container($"InfoRow_{labelText}");
            row.AutoLayoutChildren = true;
            row.LayoutDirection = Components.LayoutDirection.Horizontal;
            row.AutoSize = false;
            row.Bounds = new Rectangle(0, 0, 0, 18);
            row.ChildPadding = 0;
            row.ChildGap = 8;
            row.ChildJustification = Components.ChildJustification.Left;
            row.PositionMode = Components.PositionMode.Relative;

            var label = new Components.Label(font, labelText, 14);
            label.AutoSize = false;
            label.Bounds = new Rectangle(0, 0, 90, 18);
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
        
        public void SetKeyboardView(Components.KeyboardLayoutView? keyboardView, bool isLayout2 = false)
        {
            if (isLayout2)
            {
                this.keyboardView2 = keyboardView;
            }
            else
            {
                this.keyboardView = keyboardView;
            }
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
            // Only update UI if we're editing this layout's metadata
            if (editingLayout1Metadata)
            {
                UpdateMetadataUI(metadata);
            }
        }
        
        /// <summary>
        /// Switches between editing Layout 1 and Layout 2 metadata.
        /// </summary>
        private void SwitchMetadataLayout(bool isLayout1)
        {
            if (layoutTab == null)
                return;
                
            editingLayout1Metadata = isLayout1;
            var metadata = isLayout1 ? layoutTab.Metadata : layoutTab.Metadata2;
            UpdateMetadataUI(metadata);
        }
        
        /// <summary>
        /// Switches back to Layout 1 metadata editing and hides the dropdown.
        /// Called when the second layout is disabled.
        /// </summary>
        public void SwitchToLayout1MetadataAndHideDropdown()
        {
            editingLayout1Metadata = true;
            if (metadataLayoutDropdown != null)
            {
                metadataLayoutDropdown.SetSelectedItem("Layout 1");
                metadataLayoutDropdown.IsVisible = false;
            }
            if (layoutTab != null)
            {
                UpdateMetadataUI(layoutTab.Metadata);
            }
        }
        
        /// <summary>
        /// Shows the metadata layout dropdown.
        /// Called when the second layout is enabled.
        /// </summary>
        public void ShowMetadataLayoutDropdown()
        {
            if (metadataLayoutDropdown != null)
            {
                metadataLayoutDropdown.IsVisible = true;
            }
        }
        
        /// <summary>
        /// Updates the metadata UI fields with the given metadata.
        /// </summary>
        private void UpdateMetadataUI(LayoutMetadataJson metadata)
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

        public void SetSelectedKey(Keysharp.Core.PhysicalKey? key, bool isLayout2 = false)
        {
            isEditingLayout1 = !isLayout2;
            if (isLayout2)
            {
                selectedKeys2.Clear();
                if (key != null)
                {
                    selectedKeys2.Add(key);
                }
            }
            else
            {
                selectedKeys.Clear();
                if (key != null)
                {
                    selectedKeys.Add(key);
                }
            }
            UpdateKeyInfo();
        }

        public void SetSelectedKeys(HashSet<Keysharp.Core.PhysicalKey> keys, bool isLayout2 = false)
        {
            isEditingLayout1 = !isLayout2;
            if (isLayout2)
            {
                selectedKeys2 = keys ?? new HashSet<Keysharp.Core.PhysicalKey>();
            }
            else
            {
                selectedKeys = keys ?? new HashSet<Keysharp.Core.PhysicalKey>();
            }
            UpdateKeyInfo();
        }

        public void SetSecondLayoutVisibility(bool isVisible)
        {
            isSecondLayoutEnabled = isVisible;
            
            // Update mappings container visibility
            if (mappingsContainer != null)
            {
                mappingsContainer.IsVisible = isVisible;
            }
            
            // Update keyInfoContainer2 visibility - UpdateKeyInfo2() will handle this based on both enabled state and selection
            UpdateKeyInfo2();
        }

        private void UpdateKeyInfo()
        {
            // Always update both key info sections - they show/hide based on whether they have selections
            UpdateKeyInfo1();
            UpdateKeyInfo2();
        }
        
        private void UpdateKeyInfo1()
        {
            bool hasKey = selectedKeys.Count > 0;
            bool isMultiSelect = selectedKeys.Count > 1;
            
            // Show/hide container based on whether it has selections
            if (keyInfoContainer != null)
            {
                keyInfoContainer.IsVisible = hasKey;
            }
            
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

            // Identifier: show for both single and multi-selection
            if (identifierRowContainer != null)
            {
                identifierRowContainer.IsVisible = hasKey;
                if (hasKey && identifierInput != null)
                {
                    isUpdatingFromKey = true;
                    if (isMultiSelect)
                    {
                        // Multi-selection: combine identifiers from all selected keys
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var identifiers = sortedKeys
                            .Select(k => k.Identifier ?? "")
                            .Where(id => !string.IsNullOrEmpty(id));
                        identifierInput.SetText(string.Join(" ", identifiers));
                    }
                    else if (topLeftKey != null)
                    {
                        // Single selection: show single identifier
                        identifierInput.SetText(topLeftKey.Identifier ?? "");
                    }
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

            // Rotation: show first key's rotation, apply to all when edited
            if (rotationLabel != null && rotationInput != null)
            {
                rotationLabel.IsVisible = hasKey;
                rotationInput.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    rotationInput.SetText(topLeftKey.Rotation.ToString("F2"));
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
                    
                    // If autoshift is enabled, ensure shift characters are set from primary characters
                    if (autoEnabled)
                    {
                        foreach (var key in selectedKeys)
                        {
                            if (!string.IsNullOrEmpty(key.PrimaryCharacter))
                            {
                                // Only update if shift character is not already set or doesn't match expected
                                string expectedShift = ConvertToShiftCharacter(key.PrimaryCharacter);
                                if (key.ShiftCharacter != expectedShift)
                                {
                                    key.ShiftCharacter = expectedShift;
                                }
                            }
                            else if (key.ShiftCharacter != null)
                            {
                                key.ShiftCharacter = null;
                            }
                        }
                    }
                    
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

        private void AddMappingFromIdentifiers()
        {
            // Get identifiers from both layouts' identifier inputs
            string sourceIds = identifierInput?.Text ?? "";
            string targetIds = identifierInput2?.Text ?? "";
            
            // Split by spaces (same logic as the identifier input handlers)
            string[] sourceIdentifiers = sourceIds.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] targetIdentifiers = targetIds.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // Check if the number of identifiers match
            if (sourceIdentifiers.Length != targetIdentifiers.Length)
            {
                System.Console.WriteLine($"Cannot add mapping: number of source keys ({sourceIdentifiers.Length}) does not match number of target keys ({targetIdentifiers.Length})");
                return;
            }
            
            // Check if we have at least one identifier pair
            if (sourceIdentifiers.Length == 0)
            {
                System.Console.WriteLine("Cannot add mapping: no identifiers provided");
                return;
            }
            
            // Map them 1:1 (first to first, second to second, etc.)
            for (int i = 0; i < sourceIdentifiers.Length; i++)
            {
                string sourceId = sourceIdentifiers[i];
                string targetId = targetIdentifiers[i];
                
                // Update or add the mapping using KeyMappings class
                keyMappings.AddMapping(sourceId, targetId);
            }
            
            // Update the table display
            UpdateMappingsTable();
        }
        
        private void DeleteMappingsFromIdentifiers()
        {
            // Get identifiers from both layouts' identifier inputs
            string sourceIds = identifierInput?.Text ?? "";
            string targetIds = identifierInput2?.Text ?? "";
            
            // Split by spaces (same logic as the identifier input handlers)
            string[] sourceIdentifiers = sourceIds.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            string[] targetIdentifiers = targetIds.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            
            // Check if the number of identifiers match
            if (sourceIdentifiers.Length != targetIdentifiers.Length)
            {
                System.Console.WriteLine($"Cannot delete mapping: number of source keys ({sourceIdentifiers.Length}) does not match number of target keys ({targetIdentifiers.Length})");
                return;
            }
            
            // Check if we have at least one identifier pair
            if (sourceIdentifiers.Length == 0)
            {
                System.Console.WriteLine("Cannot delete mapping: no identifiers provided");
                return;
            }
            
            // Remove each mapping pair
            int removedCount = 0;
            for (int i = 0; i < sourceIdentifiers.Length; i++)
            {
                string sourceId = sourceIdentifiers[i];
                string targetId = targetIdentifiers[i];
                
                // Only remove if the mapping exists and matches the target
                if (keyMappings.GetTargetIdentifier(sourceId) == targetId)
                {
                    if (keyMappings.RemoveMapping(sourceId))
                    {
                        removedCount++;
                    }
                }
            }
            
            if (removedCount > 0)
            {
                System.Console.WriteLine($"Removed {removedCount} mapping(s)");
            }
            else
            {
                System.Console.WriteLine("No matching mappings found to remove");
            }
            
            // Update the table display
            UpdateMappingsTable();
        }
        
        private void UpdateMappingsTable()
        {
            if (mappingsTable == null)
                return;
            
            // Clear existing rows
            mappingsTable.Rows = new List<List<string>>();
            
            // Add rows for each mapping (sorted by source identifier for consistency)
            var sortedMappings = keyMappings.Mappings.OrderBy(kvp => kvp.Key).ToList();
            foreach (var mapping in sortedMappings)
            {
                mappingsTable.Rows.Add(new List<string> { mapping.Key, mapping.Value });
            }
        }
        
        private void SaveMappings()
        {
            try
            {
                // Use zenity for file save dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Save Mapping File\" --save --confirm-overwrite",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    string? output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        string filePath = output.Trim();
                        if (!filePath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
                        {
                            filePath += ".json";
                        }

                        // Save mappings to file
                        keyMappings.SaveToFile(filePath);
                        
                        // Refresh dropdown to include the new file
                        RefreshMappingsDropdown();
                        
                        // Set dropdown selection to the saved file
                        if (mappingsDropdown != null)
                        {
                            string fileName = Path.GetFileName(filePath);
                            mappingsDropdown.SetSelectedItem(fileName);
                        }
                        
                        System.Console.WriteLine($"Mappings saved to: {filePath}");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file save dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving mappings: {ex.Message}");
            }
        }
        
        private void LoadMappings()
        {
            try
            {
                // Use zenity for file open dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Load Mapping File\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                Process? process = Process.Start(startInfo);
                if (process != null)
                {
                    string? output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output))
                    {
                        string filePath = output.Trim();
                        LoadMappingFromPath(filePath);
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file open dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading mappings: {ex.Message}");
            }
        }
        
        private void LoadMappingFromFile(string fileName)
        {
            string mappingsDir = Path.Combine(Directory.GetCurrentDirectory(), "mappings");
            string filePath = Path.Combine(mappingsDir, fileName);
            LoadMappingFromPath(filePath);
        }
        
        private void LoadMappingFromPath(string filePath)
        {
            if (File.Exists(filePath))
            {
                try
                {
                    keyMappings = Core.KeyMappings.LoadFromFile(filePath);
                    UpdateMappingsTable();
                    
                    // Refresh dropdown to show the loaded file as selected
                    RefreshMappingsDropdown();
                    
                    // Set dropdown selection to the loaded file (without triggering callback to avoid infinite loop)
                    if (mappingsDropdown != null)
                    {
                        string fileName = Path.GetFileName(filePath);
                        // Find the index of the file in the dropdown items
                        string mappingsDir = Path.Combine(Directory.GetCurrentDirectory(), "mappings");
                        if (Directory.Exists(mappingsDir))
                        {
                            var files = Directory.GetFiles(mappingsDir, "*.json")
                                .Select(f => Path.GetFileName(f))
                                .OrderBy(f => f)
                                .ToList();
                            int index = files.IndexOf(fileName);
                            if (index >= 0)
                            {
                                mappingsDropdown.SetSelectedIndex(index, triggerCallback: false);
                            }
                        }
                    }
                    
                    System.Console.WriteLine($"Mappings loaded from: {filePath}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error loading mapping file: {ex.Message}");
                }
            }
            else
            {
                System.Console.WriteLine($"File not found: {filePath}");
            }
        }
        
        private void RefreshMappingsDropdown()
        {
            if (mappingsDropdown == null)
                return;
                
            string mappingsDir = Path.Combine(Directory.GetCurrentDirectory(), "mappings");
            List<string> mappingFiles = new List<string>();
            
            if (Directory.Exists(mappingsDir))
            {
                try
                {
                    var files = Directory.GetFiles(mappingsDir, "*.json");
                    foreach (var file in files)
                    {
                        string fileName = Path.GetFileName(file);
                        mappingFiles.Add(fileName);
                    }
                    mappingFiles.Sort();
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Error reading mappings directory: {ex.Message}");
                }
            }
            
            mappingsDropdown.SetItems(mappingFiles);
        }
        
        private void ApplyMappings()
        {
            if (layoutTab == null)
            {
                System.Console.WriteLine("LayoutTab not available");
                return;
            }

            try
            {
                // Apply mappings from layout 1 to layout 2
                keyMappings.ApplyMappings(layoutTab.Layout, layoutTab.Layout2);
                
                // Notify layout tab that layout 2 has changed
                layoutTab.NotifyLayout2Changed();
                
                System.Console.WriteLine("Mappings applied successfully");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error applying mappings: {ex.Message}");
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
                // Notify layout tab that layout has changed
                layoutTab?.NotifyLayoutChanged();
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
        
        private void UpdateKeyInfo2()
        {
            bool hasKey = selectedKeys2.Count > 0;
            bool isMultiSelect = selectedKeys2.Count > 1;
            
            // Show/hide container based on whether second layout is enabled AND we have selections
            if (keyInfoContainer2 != null)
            {
                keyInfoContainer2.IsVisible = isSecondLayoutEnabled && hasKey;
            }
            
            // Find top-leftmost key for position display
            PhysicalKey? topLeftKey = null;
            if (hasKey)
            {
                float minX = float.MaxValue;
                float minY = float.MaxValue;
                foreach (var key in selectedKeys2)
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
            if (deleteKeyButton2 != null)
            {
                deleteKeyButton2.IsVisible = hasKey;
            }

            // Show/hide placeholder and info rows
            if (placeholderLabel2 != null)
            {
                placeholderLabel2.IsVisible = !hasKey;
            }

            if (titleLabel2 != null)
            {
                titleLabel2.IsVisible = hasKey;
            }

            // Identifier: show for both single and multi-selection
            if (identifierRowContainer2 != null)
            {
                identifierRowContainer2.IsVisible = hasKey;
                if (hasKey && identifierInput2 != null)
                {
                    isUpdatingFromKey = true;
                    if (isMultiSelect)
                    {
                        // Multi-selection: combine identifiers from all selected keys
                        // Sort keys by position (left to right, top to bottom)
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var identifiers = sortedKeys
                            .Select(k => k.Identifier ?? "")
                            .Where(id => !string.IsNullOrEmpty(id));
                        identifierInput2.SetText(string.Join(" ", identifiers));
                    }
                    else if (topLeftKey != null)
                    {
                        // Single selection: show single identifier
                        identifierInput2.SetText(topLeftKey.Identifier ?? "");
                    }
                    isUpdatingFromKey = false;
                }
            }

            // Position shows top-leftmost key's position
            if (positionLabel2 != null && positionXInput2 != null && positionYInput2 != null)
            {
                positionLabel2.IsVisible = hasKey;
                positionXInput2.IsVisible = hasKey;
                positionYInput2.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    positionXInput2.SetText(topLeftKey.X.ToString("F2"));
                    positionYInput2.SetText(topLeftKey.Y.ToString("F2"));
                    isUpdatingFromKey = false;
                }
            }

            // Size: show first key's size, apply to all when edited
            if (sizeLabel2 != null && sizeWidthInput2 != null && sizeHeightInput2 != null)
            {
                sizeLabel2.IsVisible = hasKey;
                sizeWidthInput2.IsVisible = hasKey;
                sizeHeightInput2.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    sizeWidthInput2.SetText(topLeftKey.Width.ToString("F2"));
                    sizeHeightInput2.SetText(topLeftKey.Height.ToString("F2"));
                    isUpdatingFromKey = false;
                }
            }

            // Rotation: show first key's rotation, apply to all when edited
            if (rotationLabel2 != null && rotationInput2 != null)
            {
                rotationLabel2.IsVisible = hasKey;
                rotationInput2.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    rotationInput2.SetText(topLeftKey.Rotation.ToString("F2"));
                    isUpdatingFromKey = false;
                }
            }

            // Finger: show first key's finger, apply to all when edited
            if (fingerLabel2 != null && fingerDropdown2 != null)
            {
                fingerLabel2.IsVisible = hasKey;
                fingerDropdown2.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    int fingerIndex = GetIndexFromFinger(topLeftKey.Finger);
                    fingerDropdown2.SetSelectedIndex(fingerIndex, triggerCallback: false);
                    isUpdatingFromKey = false;
                }
            }

            // Primary/Shift characters: show for both single and multi-selection
            if (primaryCharacterRowContainer2 != null)
            {
                primaryCharacterRowContainer2.IsVisible = hasKey;
                if (hasKey && primaryCharacterInput2 != null)
                {
                    isUpdatingFromKey = true;
                    if (isMultiSelect)
                    {
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var primaryChars = sortedKeys
                            .Select(k => k.PrimaryCharacter ?? "")
                            .Where(c => !string.IsNullOrEmpty(c));
                        primaryCharacterInput2.SetText(string.Join(" ", primaryChars));
                    }
                    else if (topLeftKey != null)
                    {
                        primaryCharacterInput2.SetText(topLeftKey.PrimaryCharacter ?? "");
                    }
                    isUpdatingFromKey = false;
                }
            }

            if (shiftCharacterRowContainer2 != null)
            {
                shiftCharacterRowContainer2.IsVisible = hasKey;
                if (hasKey && shiftCharacterInput2 != null)
                {
                    isUpdatingFromKey = true;
                    bool autoEnabled = shiftAutoCheckbox2 != null && shiftAutoCheckbox2.IsChecked;
                    shiftCharacterInput2.IsEnabled = !autoEnabled;
                    
                    if (autoEnabled)
                    {
                        foreach (var key in selectedKeys2)
                        {
                            if (!string.IsNullOrEmpty(key.PrimaryCharacter))
                            {
                                string expectedShift = ConvertToShiftCharacter(key.PrimaryCharacter);
                                if (key.ShiftCharacter != expectedShift)
                                {
                                    key.ShiftCharacter = expectedShift;
                                }
                            }
                            else if (key.ShiftCharacter != null)
                            {
                                key.ShiftCharacter = null;
                            }
                        }
                    }
                    
                    if (isMultiSelect)
                    {
                        var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                        var shiftChars = sortedKeys
                            .Select(k => k.ShiftCharacter ?? "")
                            .Where(c => !string.IsNullOrEmpty(c));
                        shiftCharacterInput2.SetText(string.Join(" ", shiftChars));
                    }
                    else if (topLeftKey != null)
                    {
                        shiftCharacterInput2.SetText(topLeftKey.ShiftCharacter ?? "");
                    }
                    isUpdatingFromKey = false;
                }
            }

            // Shift auto checkbox: show when key is selected
            if (shiftAutoLabel2 != null && shiftAutoCheckbox2 != null)
            {
                shiftAutoLabel2.IsVisible = hasKey;
                shiftAutoCheckbox2.IsVisible = hasKey;
            }
            
            // Disabled: show if all keys have same state, apply to all when edited
            if (disabledLabel2 != null && disabledCheckbox2 != null)
            {
                disabledLabel2.IsVisible = hasKey;
                disabledCheckbox2.IsVisible = hasKey;
                if (hasKey && topLeftKey != null)
                {
                    isUpdatingFromKey = true;
                    bool allDisabled = selectedKeys2.All(k => k.Disabled);
                    bool allEnabled = selectedKeys2.All(k => !k.Disabled);
                    disabledCheckbox2.IsChecked = allDisabled || (!allEnabled && topLeftKey.Disabled);
                    isUpdatingFromKey = false;
                }
            }
        }
        
        private void DeleteSelectedKey2()
        {
            if (layout != null && selectedKeys2.Count > 0)
            {
                foreach (var key in selectedKeys2)
                {
                    layout.RemovePhysicalKey(key);
                }
                layout.RebuildMappings();
                layoutTab?.NotifyLayout2Changed(); // Notify that layout 2 has changed
                selectedKeys2.Clear();
                if (keyboardView2 != null)
                {
                    var emptySelection = new HashSet<PhysicalKey>();
                    keyboardView2.SelectedKeys = emptySelection;
                }
                UpdateKeyInfo2();
                if (keyboardView2 != null)
                {
                    keyboardView2.Layout = layout;
                }
            }
        }
        
        private void UpdateShiftInputDisplay2()
        {
            if (shiftCharacterInput2 == null || isUpdatingFromKey)
                return;
                
            isUpdatingFromKey = true;
            bool isMultiSelect = selectedKeys2.Count > 1;
            
            if (isMultiSelect)
            {
                var sortedKeys = selectedKeys2.OrderBy(k => k.Y).ThenBy(k => k.X).ToList();
                var shiftChars = sortedKeys
                    .Select(k => k.ShiftCharacter ?? "")
                    .Where(c => !string.IsNullOrEmpty(c));
                shiftCharacterInput2.SetText(string.Join(" ", shiftChars));
            }
            else if (selectedKeys2.Count == 1)
            {
                var key = selectedKeys2.First();
                shiftCharacterInput2.SetText(key.ShiftCharacter ?? "");
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
            // Note: Positions are set in ResolveBounds(), not here
            
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
            
            // Set keyInfoContainer2 size (same as keyInfoContainer)
            if (keyInfoContainer2 != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                // Set container width (height will be auto-calculated based on content)
                // Width accounts for 15px padding on each side
                float containerWidth = Bounds.Width - 30;
                keyInfoContainer2.SetSize(containerWidth, 0); // Height will be calculated by auto-size
                
                // Set label widths to fill available space (accounting for container padding)
                float availableWidth = containerWidth - (keyInfoContainer2.ChildPadding * 2);
                
                if (titleLabel2 != null)
                {
                    titleLabel2.SetSize(availableWidth, 24);
                }

                if (placeholderLabel2 != null)
                {
                    placeholderLabel2.SetSize(availableWidth, 18);
                }

                // Set info row label widths (fixed width for labels)
                float labelWidth = 90;
                float valueWidth = availableWidth - labelWidth - 8; // 8 is gap between label and value

                if (identifierLabel2 != null && identifierInput2 != null)
                {
                    identifierLabel2.SetSize(labelWidth, 18);
                    identifierInput2.SetSize(valueWidth, 24);
                }
                
                if (positionLabel2 != null && positionXInput2 != null && positionYInput2 != null)
                {
                    positionLabel2.SetSize(labelWidth, 18);
                    // Position inputs share the available width (half each minus gap)
                    float inputWidth = (valueWidth - 8) / 2; // 8 is gap between inputs
                    positionXInput2.SetSize(inputWidth, 24);
                    positionYInput2.SetSize(inputWidth, 24);
                }
                
                if (sizeLabel2 != null && sizeWidthInput2 != null && sizeHeightInput2 != null)
                {
                    sizeLabel2.SetSize(labelWidth, 18);
                    // Size inputs share the available width (half each minus gap)
                    float inputWidth = (valueWidth - 8) / 2; // 8 is gap between inputs
                    sizeWidthInput2.SetSize(inputWidth, 24);
                    sizeHeightInput2.SetSize(inputWidth, 24);
                }
                
                if (rotationLabel2 != null && rotationInput2 != null)
                {
                    rotationLabel2.SetSize(labelWidth, 18);
                    rotationInput2.SetSize(valueWidth, 24);
                }
                
                if (fingerLabel2 != null && fingerDropdown2 != null)
                {
                    fingerLabel2.SetSize(labelWidth, 18);
                    fingerDropdown2.SetSize(valueWidth, 35);
                }
                
                if (primaryCharacterLabel2 != null && primaryCharacterInput2 != null)
                {
                    primaryCharacterLabel2.SetSize(labelWidth, 18);
                    primaryCharacterInput2.SetSize(valueWidth, 24);
                }
                
                if (shiftCharacterLabel2 != null && shiftCharacterInput2 != null)
                {
                    shiftCharacterLabel2.SetSize(labelWidth, 18);
                    shiftCharacterInput2.SetSize(valueWidth, 24);
                }
                
                if (shiftAutoLabel2 != null && shiftAutoCheckbox2 != null)
                {
                    shiftAutoLabel2.SetSize(labelWidth, 18);
                    shiftAutoCheckbox2.SetSize(16, 16);
                }

                if (disabledLabel2 != null && disabledCheckbox2 != null)
                {
                    disabledLabel2.SetSize(labelWidth, 18);
                    disabledCheckbox2.SetSize(16, 16);
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
            
            // Set mappings container size
            if (mappingsContainer != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                float availableWidth = Bounds.Width - 30; // Account for 15px padding on each side
                // Height: header (24) + gap (8) + row1 buttons (28) + gap (5) + row2 buttons (28) + gap (8) + table (150) = 251
                mappingsContainer.SetSize(availableWidth, 251);
                
                // Set header label width
                if (mappingsHeaderLabel != null)
                {
                    mappingsHeaderLabel.SetSize(availableWidth, 24);
                }
                
                // Set button container width (buttons are in a horizontal container)
                // Set button container widths (two rows of buttons)
                var buttonContainerRow1 = mappingsContainer.Children.FirstOrDefault(c => c.Name == "MappingButtonsRow1");
                if (buttonContainerRow1 != null)
                {
                    buttonContainerRow1.SetSize(availableWidth, 28);
                }
                var buttonContainerRow2 = mappingsContainer.Children.FirstOrDefault(c => c.Name == "MappingButtonsRow2");
                if (buttonContainerRow2 != null)
                {
                    buttonContainerRow2.SetSize(availableWidth, 28);
                }
                
                // Set dropdown width (fills remaining space in first row)
                if (mappingsDropdown != null && buttonContainerRow1 != null)
                {
                    // Calculate remaining width: container width minus Load button (60) minus Save button (60) minus gaps (10 = 2*5)
                    float dropdownWidth = availableWidth - 60 - 60 - 10;
                    mappingsDropdown.SetSize(dropdownWidth, 28);
                }
                
                // Set table width and height
                if (mappingsTable != null)
                {
                    mappingsTable.SetSize(availableWidth, 150);
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
            // Call base.ResolveBounds() first to resolve our own position
            base.ResolveBounds();
            
            // Position containers manually (AutoLayoutChildren is disabled so LayoutChildren() won't override our positions)
            const float containerGap = 20f; // Gap between containers
            
            // Track cumulative Y position to properly space containers
            float currentY = 15 - scrollOffset;
            
            // Position metadata container first (at top, with scroll offset)
            if (metadataContainer != null)
            {
                metadataContainer.RelativePosition = new System.Numerics.Vector2(15, currentY);
                metadataContainer.ResolveBounds();
                if (metadataContainer.Bounds.Height > 0)
                {
                    currentY += metadataContainer.Bounds.Height + containerGap;
                }
            }
            
            // Position key info container (below metadata container)
            if (keyInfoContainer != null)
            {
                keyInfoContainer.RelativePosition = new System.Numerics.Vector2(15, currentY);
                keyInfoContainer.ResolveBounds();
                if (keyInfoContainer.IsVisible && keyInfoContainer.Bounds.Height > 0)
                {
                    currentY += keyInfoContainer.Bounds.Height + containerGap;
                }
            }
            
            // Position keyInfoContainer2 below keyInfoContainer
            if (keyInfoContainer2 != null)
            {
                keyInfoContainer2.RelativePosition = new System.Numerics.Vector2(15, currentY);
                keyInfoContainer2.ResolveBounds();
                if (keyInfoContainer2.IsVisible && keyInfoContainer2.Bounds.Height > 0)
                {
                    currentY += keyInfoContainer2.Bounds.Height + containerGap;
                }
            }
            
            // Position mappings container before color controls
            if (mappingsContainer != null)
            {
                mappingsContainer.RelativePosition = new System.Numerics.Vector2(15, currentY);
                mappingsContainer.ResolveBounds();
                if (mappingsContainer.IsVisible && mappingsContainer.Bounds.Height > 0)
                {
                    currentY += mappingsContainer.Bounds.Height + containerGap;
                }
            }
            
            // Position color controls container below mappings container
            if (colorControlsContainer != null)
            {
                colorControlsContainer.RelativePosition = new System.Numerics.Vector2(15, currentY);
                colorControlsContainer.ResolveBounds();
            }
        }

        /// <summary>
        /// Recursively checks if any TextInput component in the given element and its children is focused.
        /// </summary>
        private bool HasFocusedTextInput(UIElement element)
        {
            if (element == null || !element.IsVisible)
                return false;
            
            // Check if this element is a TextInput and is focused
            if (element is Components.TextInput textInput && textInput.IsFocused)
            {
                return true;
            }
            
            // Recursively check children
            foreach (var child in element.Children)
            {
                if (HasFocusedTextInput(child))
                {
                    return true;
                }
            }
            
            return false;
        }

        public override void Update()
        {
            // Update dropdowns first to ensure they can consume clicks before buttons process them
            // This prevents click-through when dropdowns are open
            if (fingerDropdown != null)
            {
                fingerDropdown.Update();
            }
            if (fingerDropdown2 != null)
            {
                fingerDropdown2.Update();
            }
            if (metadataLayoutDropdown != null)
            {
                metadataLayoutDropdown.Update();
            }
            if (mappingsDropdown != null)
            {
                mappingsDropdown.Update();
            }
            
            base.Update();
            // No bounds setting here - that's done in PrepareResolveBounds()
            
            // Handle mouse wheel scrolling when mouse is over the panel
            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();
            
            // Calculate total content height for scrollbar and scrolling
            float totalContentHeight = 15; // Top padding
            const float containerGap = 20f;
            
            if (metadataContainer != null && metadataContainer.IsVisible && metadataContainer.Bounds.Height > 0)
            {
                totalContentHeight += metadataContainer.Bounds.Height + containerGap;
            }
            if (keyInfoContainer != null && keyInfoContainer.IsVisible && keyInfoContainer.Bounds.Height > 0)
            {
                totalContentHeight += keyInfoContainer.Bounds.Height + containerGap;
            }
            if (keyInfoContainer2 != null && keyInfoContainer2.IsVisible && keyInfoContainer2.Bounds.Height > 0)
            {
                totalContentHeight += keyInfoContainer2.Bounds.Height + containerGap;
            }
            if (mappingsContainer != null && mappingsContainer.IsVisible && mappingsContainer.Bounds.Height > 0)
            {
                totalContentHeight += mappingsContainer.Bounds.Height + containerGap;
            }
            if (colorControlsContainer != null && colorControlsContainer.IsVisible && colorControlsContainer.Bounds.Height > 0)
            {
                totalContentHeight += colorControlsContainer.Bounds.Height + containerGap;
            }
            
            float maxScroll = System.Math.Max(0, totalContentHeight - Bounds.Height);
            
            // Handle scrollbar dragging (check this BEFORE early returns, and allow even when TextInput is focused)
            // Continue dragging even if mouse leaves sidebar bounds (as long as button is held)
            if (isDraggingScrollbar)
            {
                if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    float deltaY = mouseY - scrollbarDragStartY;
                    float scrollbarHeight = Bounds.Height;
                    float visibleRatio = Bounds.Height / totalContentHeight;
                    float thumbHeight = scrollbarHeight * visibleRatio;
                    float maxThumbY = scrollbarHeight - thumbHeight;
                    if (maxThumbY > 0)
                    {
                        float scrollRatio = deltaY / maxThumbY;
                        scrollOffset = System.Math.Clamp(scrollbarDragStartOffset + scrollRatio * maxScroll, 0, maxScroll);
                    }
                }
                else
                {
                    isDraggingScrollbar = false;
                }
            }
            else if (maxScroll > 0 && mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                     mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height)
            {
                // Only check for starting a new drag when mouse is within sidebar bounds
                float scrollbarX = Bounds.X + Bounds.Width - ScrollbarWidth;
                Rectangle scrollbarArea = new Rectangle(scrollbarX, Bounds.Y, ScrollbarWidth, Bounds.Height);
                bool hoveringScrollbar = mouseX >= scrollbarArea.X && mouseX <= scrollbarArea.X + scrollbarArea.Width &&
                                        mouseY >= scrollbarArea.Y && mouseY <= scrollbarArea.Y + scrollbarArea.Height;
                
                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT) && hoveringScrollbar)
                {
                    isDraggingScrollbar = true;
                    scrollbarDragStartY = mouseY;
                    scrollbarDragStartOffset = scrollOffset;
                }
            }
            
            // Don't handle mouse wheel scrolling if we're dragging the scrollbar
            if (isDraggingScrollbar)
            {
                return;
            }
            
            if (Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT) || 
                Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_RIGHT) ||
                Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_MIDDLE))
            {
                // Don't scroll while dragging (except scrollbar dragging, which is handled above)
                return;
            }
            
            // Don't scroll if a TextInput is focused (scrolling is used to modify text input values)
            if (HasFocusedTextInput(this))
            {
                return;
            }
            
            // Check if mappings table is being hovered/scrolled - if so, don't scroll sidebar
            bool tableIsHovered = false;
            if (mappingsTable != null && mappingsTable.IsVisible && mappingsTable.IsEnabled)
            {
                tableIsHovered = mappingsTable.IsHovering(mouseX, mouseY);
            }
            
            // Handle mouse wheel scrolling
            if (!tableIsHovered && 
                mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width - ScrollbarWidth && // Don't scroll when over scrollbar
                mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height)
            {
                float wheelMove = Raylib.GetMouseWheelMove();
                if (wheelMove != 0)
                {
                    scrollOffset -= wheelMove * ScrollSpeed; // Negate to scroll down when wheel moves down
                    scrollOffset = System.Math.Clamp(scrollOffset, 0, maxScroll);
                    // ResolveBounds will be called in the next frame by RootUI, which will apply the scroll offset
                }
            }
        }

        public override void UpdateFont(Font newFont)
        {
            base.UpdateFont(newFont);
            // Update fonts in child components that use fonts
            // Note: Components store their own font references, so we'd need to update them individually
            // For now, this is a limitation - components will need to be recreated to use new fonts
            // This is acceptable for a debug feature
        }

        public override void Draw()
        {
            // Only draw if visible
            if (!IsVisible)
                return;

            // Enable scissor mode to clip content to panel bounds (exclude scrollbar width)
            // Calculate if scrollbar is needed to determine clipping width
            float totalContentHeight = 15; // Top padding
            const float containerGap = 20f;
            if (metadataContainer != null && metadataContainer.IsVisible && metadataContainer.Bounds.Height > 0)
                totalContentHeight += metadataContainer.Bounds.Height + containerGap;
            if (keyInfoContainer != null && keyInfoContainer.IsVisible && keyInfoContainer.Bounds.Height > 0)
                totalContentHeight += keyInfoContainer.Bounds.Height + containerGap;
            if (keyInfoContainer2 != null && keyInfoContainer2.IsVisible && keyInfoContainer2.Bounds.Height > 0)
                totalContentHeight += keyInfoContainer2.Bounds.Height + containerGap;
            if (mappingsContainer != null && mappingsContainer.IsVisible && mappingsContainer.Bounds.Height > 0)
                totalContentHeight += mappingsContainer.Bounds.Height + containerGap;
            if (colorControlsContainer != null && colorControlsContainer.IsVisible && colorControlsContainer.Bounds.Height > 0)
                totalContentHeight += colorControlsContainer.Bounds.Height + containerGap;
            
            int clipWidth = (int)Bounds.Width;
            if (totalContentHeight > Bounds.Height)
            {
                clipWidth -= ScrollbarWidth; // Exclude scrollbar from clipping area
            }
            
            // Draw panel background and borders first (before scissor mode so it's always visible)
            DrawPanelContent(Bounds);
            
            // Enable scissor mode to clip content to panel bounds (exclude scrollbar width)
            ScissorModeManager.BeginScissorMode((int)Bounds.X, (int)Bounds.Y, clipWidth, (int)Bounds.Height);
            
            // Draw all children (they will be clipped by scissor mode)
            // Only draw children that intersect with the visible scrolling area
            // This prevents elements from drawing above/below the visible area
            float visibleTop = Bounds.Y;
            float visibleBottom = Bounds.Y + Bounds.Height;
            foreach (var child in Children)
            {
                // Only draw if element's bounds intersect with visible area
                // Element intersects if: bottom >= top AND top <= bottom
                float childTop = child.Bounds.Y;
                float childBottom = child.Bounds.Y + child.Bounds.Height;
                if (childBottom > visibleTop && childTop < visibleBottom)
                {
                    child.Draw();
                }
            }
            
            // End scissor mode
            ScissorModeManager.EndScissorMode();
            
            // Draw scrollbar if needed (draw outside scissor mode so it's always visible)
            DrawScrollbar();
        }
        
        private void DrawScrollbar()
        {
            // Calculate total content height
            float totalContentHeight = 15; // Top padding
            const float containerGap = 20f;
            
            if (metadataContainer != null && metadataContainer.IsVisible && metadataContainer.Bounds.Height > 0)
            {
                totalContentHeight += metadataContainer.Bounds.Height + containerGap;
            }
            if (keyInfoContainer != null && keyInfoContainer.IsVisible && keyInfoContainer.Bounds.Height > 0)
            {
                totalContentHeight += keyInfoContainer.Bounds.Height + containerGap;
            }
            if (keyInfoContainer2 != null && keyInfoContainer2.IsVisible && keyInfoContainer2.Bounds.Height > 0)
            {
                totalContentHeight += keyInfoContainer2.Bounds.Height + containerGap;
            }
            if (mappingsContainer != null && mappingsContainer.IsVisible && mappingsContainer.Bounds.Height > 0)
            {
                totalContentHeight += mappingsContainer.Bounds.Height + containerGap;
            }
            if (colorControlsContainer != null && colorControlsContainer.IsVisible && colorControlsContainer.Bounds.Height > 0)
            {
                totalContentHeight += colorControlsContainer.Bounds.Height + containerGap;
            }
            
            // Only show scrollbar if content exceeds visible area
            if (totalContentHeight <= Bounds.Height)
                return;
            
            float scrollbarX = Bounds.X + Bounds.Width - ScrollbarWidth;
            float scrollbarY = Bounds.Y;
            float scrollbarHeight = Bounds.Height;
            
            // Draw scrollbar track
            Rectangle trackRect = new Rectangle(scrollbarX, scrollbarY, ScrollbarWidth, scrollbarHeight);
            Raylib.DrawRectangleRec(trackRect, new Color(40, 40, 40, 255)); // Dark gray track
            
            // Calculate scrollbar thumb size and position
            float visibleRatio = Bounds.Height / totalContentHeight;
            float thumbHeight = scrollbarHeight * visibleRatio;
            float maxThumbY = scrollbarHeight - thumbHeight;
            float thumbY = scrollbarY + (scrollOffset / (totalContentHeight - Bounds.Height)) * maxThumbY;
            thumbY = System.Math.Clamp(thumbY, scrollbarY, scrollbarY + maxThumbY);
            
            Rectangle thumbRect = new Rectangle(scrollbarX + 2, (int)thumbY, ScrollbarWidth - 4, (int)thumbHeight);
            Color thumbColor = isDraggingScrollbar ? new Color(120, 120, 120, 255) : new Color(100, 100, 100, 255);
            Raylib.DrawRectangleRec(thumbRect, thumbColor);
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
            if (fingerDropdown2 != null)
            {
                fingerDropdown2.DrawDropdown();
            }
            if (metadataLayoutDropdown != null)
            {
                metadataLayoutDropdown.DrawDropdown();
            }
            if (mappingsDropdown != null)
            {
                mappingsDropdown.DrawDropdown();
            }
        }
    }
}

