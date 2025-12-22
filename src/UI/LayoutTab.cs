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
    public class LayoutTab
    {
        private Components.TabContent tabContent;
        private Components.Container? viewControlsContainer;
        private Components.Container? radioButtonsContainer;
        private Components.RadioButton? regularRadioButton;
        private Components.RadioButton? fingerColorsRadioButton;
        private Components.RadioButton? heatmapRadioButton;
        private Components.Checkbox? showDisabledCheckbox;
        private Components.Checkbox? enableSecondLayoutCheckbox;
        private Components.Button? saveLayoutButton;
        private Components.Button? loadLayoutButton;
        private Components.Button? addKeyButton;
        
        // Second layout buttons
        private Components.Button? saveLayoutButton2;
        private Components.Button? loadLayoutButton2;
        private Components.Button? addKeyButton2;
        private Components.Dropdown? layoutsDropdown;
        private Components.Canvas keyboardCanvas;
        private string? initialLayoutFile = null; // Track which file was initially loaded
        private bool isLoadingLayout = false; // Flag to prevent recursive loading
        private string? currentlyLoadedFile = null; // Track which file is currently loaded
        private Components.KeyboardLayoutView keyboardView;
        private Layout layout;
        
        // Second layout fields
        private Components.Dropdown? layoutsDropdown2;
        private Components.Container? layoutControlsContainer2;
        private Components.Canvas keyboardCanvas2;
        private bool isLoadingLayout2 = false; // Flag to prevent recursive loading for second layout
        private string? currentlyLoadedFile2 = null; // Track which file is currently loaded for second layout
        private Components.KeyboardLayoutView keyboardView2;
        private Layout layout2;
        private LayoutMetadataJson metadata2 = new LayoutMetadataJson(); // Second layout metadata
        
        private SidePanel? sidePanel;
        private Font font;
        private CorpusTab? corpusTab; // Reference to corpus tab for checking loaded corpus
        private LayoutMetadataJson metadata = new LayoutMetadataJson(); // Layout metadata

        public Components.TabContent TabContent => tabContent;
        public Components.Dropdown? LayoutsDropdown => layoutsDropdown;
        public Components.Dropdown? LayoutsDropdown2 => layoutsDropdown2;

        public SidePanel? SidePanel
        {
            get => sidePanel;
            set
            {
                sidePanel = value;
                // Set keyboard view reference in side panel for HSV color controls
                if (sidePanel != null && keyboardView != null)
                {
                    sidePanel.SetKeyboardView(keyboardView);
                    // Also notify of current view mode (ensure it matches the selected radio button)
                    // Heatmap is the default, so use that if heatmapRadioButton is selected
                    var viewMode = keyboardView.ViewMode;
                    if (heatmapRadioButton != null && heatmapRadioButton.IsSelected && viewMode != Components.KeyboardViewMode.Heatmap)
                    {
                        // Sync view mode to match selected radio button
                        keyboardView.ViewMode = Components.KeyboardViewMode.Heatmap;
                        viewMode = Components.KeyboardViewMode.Heatmap;
                    }
                    sidePanel.SetViewMode(viewMode);
                    // Update metadata in side panel if we loaded from a file
                    sidePanel.SetLayoutMetadata(metadata);
                }
            }
        }

        public CorpusTab? CorpusTab
        {
            get => corpusTab;
            set
            {
                corpusTab = value;
                // Update heatmap button visibility when corpus tab changes
                UpdateHeatmapButtonVisibility();
            }
        }


        /// <summary>
        /// Gets the current layout metadata.
        /// </summary>
        public LayoutMetadataJson Metadata => metadata;
        
        /// <summary>
        /// Gets the second layout metadata.
        /// </summary>
        public LayoutMetadataJson Metadata2 => metadata2;

        /// <summary>
        /// Gets the current layout (first layout).
        /// </summary>
        public Layout Layout => layout;
        
        /// <summary>
        /// Gets the second layout.
        /// </summary>
        public Layout Layout2 => layout2;

        /// <summary>
        /// Notifies dependent tabs that the layout has changed (e.g., keys swapped, mappings rebuilt).
        /// </summary>
        public void NotifyLayoutChanged()
        {
            // Update corpus tab with current layout reference
            corpusTab?.SetLayout(layout);
        }

        public LayoutTab(Font font)
        {
            this.font = font;
            
            // Initialize default metadata
            metadata = new LayoutMetadataJson();
            
            // Try to load pinev4.json layout, fallback to programmatic creation if it doesn't exist
            string layoutDir = Path.Combine(Directory.GetCurrentDirectory(), "layouts");
            string pinev4Path = Path.Combine(layoutDir, "pinev4.json");
            
            if (File.Exists(pinev4Path))
            {
                try
                {
                    initialLayoutFile = "pinev4.json";
                    LoadLayoutFromPath(pinev4Path, skipSidePanelUpdate: true); // Skip side panel update since it's not connected yet
                    // Ensure layout is initialized (fallback if load failed)
                    if (layout == null)
                    {
                        System.Console.WriteLine("Layout load returned null, falling back to programmatic creation");
                        layout = Layout.CreateStandard60PercentQwerty();
                        initialLayoutFile = null; // Clear if fallback was used
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Exception during initial layout load: {ex.Message}");
                    System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    layout = Layout.CreateStandard60PercentQwerty();
                    initialLayoutFile = null;
                }
            }
            else
            {
                // Fallback to programmatic creation
                layout = Layout.CreateStandard60PercentQwerty();
            }
            
            // Initialize second layout - try to load qwerty.json, fallback to programmatic creation if it doesn't exist
            string qwertyPath = Path.Combine(layoutDir, "qwerty.json");
            bool loadedQwerty = false;
            
            if (File.Exists(qwertyPath))
            {
                try
                {
                    LoadLayoutFromPath2(qwertyPath);
                    // Ensure layout2 is initialized (fallback if load failed)
                    if (layout2 == null)
                    {
                        System.Console.WriteLine("Layout2 load returned null, falling back to programmatic creation");
                        layout2 = Layout.CreateStandard60PercentQwerty();
                        metadata2 = new LayoutMetadataJson();
                    }
                    else
                    {
                        loadedQwerty = true;
                    }
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"Exception during initial layout2 load: {ex.Message}");
                    System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                    layout2 = Layout.CreateStandard60PercentQwerty();
                    metadata2 = new LayoutMetadataJson();
                }
            }
            else
            {
                // Fallback to programmatic creation
                layout2 = Layout.CreateStandard60PercentQwerty();
                metadata2 = new LayoutMetadataJson();
            }

            // Create tab content without title (we'll use a Label element instead)
            tabContent = new Components.TabContent(font, "", null);
            tabContent.PositionMode = Components.PositionMode.Relative;
            tabContent.AutoLayoutChildren = true;
            tabContent.LayoutDirection = Components.LayoutDirection.Vertical;
            tabContent.ChildJustification = Components.ChildJustification.Left;
            tabContent.ChildPadding = 20;
            tabContent.ChildGap = 10; // Gap between title, controls, and keyboard

            // Create view controls container
            viewControlsContainer = new Components.Container("ViewControlsContainer");
            viewControlsContainer.AutoLayoutChildren = true;
            viewControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            viewControlsContainer.AutoSize = false;
            viewControlsContainer.Bounds = new Rectangle(0, 0, 0, 30); // Height for controls
            viewControlsContainer.ChildPadding = 0;
            viewControlsContainer.ChildGap = 15;
            viewControlsContainer.ChildJustification = Components.ChildJustification.SpaceBetween; // Space between radio buttons container and checkbox
            viewControlsContainer.PositionMode = Components.PositionMode.Relative;

            // Calculate button height (used for all controls)
            int buttonHeight = 30;

            // Create container for left-side controls (Load Layout and dropdown)
            var leftControlsContainer = new Components.Container("LeftControlsContainer");
            leftControlsContainer.AutoLayoutChildren = true;
            leftControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            leftControlsContainer.AutoSize = false;
            leftControlsContainer.Bounds = new Rectangle(0, 0, 0, buttonHeight); // Height to match controls
            leftControlsContainer.ChildPadding = 0;
            leftControlsContainer.ChildGap = 15;
            leftControlsContainer.ChildJustification = Components.ChildJustification.Left; // Left-justify controls
            leftControlsContainer.PositionMode = Components.PositionMode.Relative;

            // Create Load Layout button (first item on left)
            loadLayoutButton = new Components.Button(font, "Load Layout", 14);
            loadLayoutButton.Bounds = new Rectangle(0, 0, 120, buttonHeight);
            loadLayoutButton.AutoSize = false;
            loadLayoutButton.OnClick = LoadLayoutFromFile;
            leftControlsContainer.AddChild(loadLayoutButton);

            // Initialize layouts dropdown (second item on left)
            List<string> layoutFiles = GetLayoutFiles();
            layoutsDropdown = new Components.Dropdown(font, layoutFiles, 14);
            layoutsDropdown.SetBounds(new Rectangle(0, 0, 200, buttonHeight));
            layoutsDropdown.AutoSize = false;
            layoutsDropdown.OnSelectionChanged = OnLayoutSelected;
            // Set initial selection if we loaded a file
            if (initialLayoutFile != null && layoutFiles.Contains(initialLayoutFile))
            {
                layoutsDropdown.SetSelectedItem(initialLayoutFile);
            }
            leftControlsContainer.AddChild(layoutsDropdown);

            viewControlsContainer.AddChild(leftControlsContainer);

            // Create container for radio buttons (view mode controls)
            radioButtonsContainer = new Components.Container("RadioButtonsContainer");
            radioButtonsContainer.AutoLayoutChildren = true;
            radioButtonsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            radioButtonsContainer.AutoSize = false;
            radioButtonsContainer.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            radioButtonsContainer.ChildPadding = 0;
            radioButtonsContainer.ChildGap = 15;
            radioButtonsContainer.ChildJustification = Components.ChildJustification.SpaceBetween;
            radioButtonsContainer.PositionMode = Components.PositionMode.Relative;
            
            // Create container for left side (radio buttons)
            var radioButtonsLeftContainer = new Components.Container("RadioButtonsLeftContainer");
            radioButtonsLeftContainer.AutoLayoutChildren = true;
            radioButtonsLeftContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            radioButtonsLeftContainer.AutoSize = false;
            radioButtonsLeftContainer.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            radioButtonsLeftContainer.ChildPadding = 0;
            radioButtonsLeftContainer.ChildGap = 15;
            radioButtonsLeftContainer.ChildJustification = Components.ChildJustification.Left;
            radioButtonsLeftContainer.PositionMode = Components.PositionMode.Relative;

            // Create radio buttons for view modes
            const string radioGroup = "ViewMode";
            
            // Helper function to deselect other radio buttons
            Action<Components.RadioButton> deselectOthers = (selectedButton) => {
                if (regularRadioButton != null && selectedButton != regularRadioButton) regularRadioButton.IsSelected = false;
                if (fingerColorsRadioButton != null && selectedButton != fingerColorsRadioButton) fingerColorsRadioButton.IsSelected = false;
                if (heatmapRadioButton != null && selectedButton != heatmapRadioButton) heatmapRadioButton.IsSelected = false;
            };

            // Calculate button widths based on text
            float regularWidth = FontManager.MeasureText(font, "Regular", 14) + 16 + 8; // text + radio + spacing
            float fingerColorsWidth = FontManager.MeasureText(font, "Finger Colors", 14) + 16 + 8;
            float heatmapWidth = FontManager.MeasureText(font, "Heatmap", 14) + 16 + 8;

            regularRadioButton = new Components.RadioButton(font, "Regular", radioGroup, 14);
            regularRadioButton.Bounds = new Rectangle(0, 0, regularWidth, buttonHeight);
            regularRadioButton.AutoSize = false;
            regularRadioButton.IsSelected = false;
            regularRadioButton!.OnSelectedInGroup = deselectOthers;
            regularRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.Regular;
                    if (keyboardView2 != null)
                        keyboardView2.ViewMode = Components.KeyboardViewMode.Regular;
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.Regular);
                }
            };

            fingerColorsRadioButton = new Components.RadioButton(font, "Finger Colors", radioGroup, 14);
            fingerColorsRadioButton.Bounds = new Rectangle(0, 0, fingerColorsWidth, buttonHeight);
            fingerColorsRadioButton.AutoSize = false;
            fingerColorsRadioButton.IsSelected = false;
            fingerColorsRadioButton!.OnSelectedInGroup = deselectOthers;
            fingerColorsRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.FingerColors;
                    if (keyboardView2 != null)
                        keyboardView2.ViewMode = Components.KeyboardViewMode.FingerColors;
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.FingerColors);
                }
            };

            heatmapRadioButton = new Components.RadioButton(font, "Heatmap", radioGroup, 14);
            heatmapRadioButton.Bounds = new Rectangle(0, 0, heatmapWidth, buttonHeight);
            heatmapRadioButton.AutoSize = false;
            heatmapRadioButton.IsSelected = true; // Default selection
            heatmapRadioButton!.OnSelectedInGroup = deselectOthers;
            heatmapRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.Heatmap;
                    if (keyboardView2 != null)
                        keyboardView2.ViewMode = Components.KeyboardViewMode.Heatmap;
                    UpdateHeatmapData(); // Update heatmap data when selected
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.Heatmap);
                }
            };
            
            // Ensure keyboardView is in heatmap mode to match the selected radio button
            // This is needed because keyboardView might be created after radio buttons
            // We'll set it again after keyboardView is created, but this ensures consistency

            radioButtonsLeftContainer.AddChild(regularRadioButton);
            radioButtonsLeftContainer.AddChild(fingerColorsRadioButton);
            radioButtonsLeftContainer.AddChild(heatmapRadioButton);
            
            radioButtonsContainer.AddChild(radioButtonsLeftContainer);
            
            // Create checkbox for enabling second layout (right side of radio buttons container)
            float enableSecondLayoutWidth = FontManager.MeasureText(font, "Enable Second Layout", 14) + 16 + 8; // text + checkbox + spacing
            enableSecondLayoutCheckbox = new Components.Checkbox(font, "Enable Second Layout", 14);
            enableSecondLayoutCheckbox.Bounds = new Rectangle(0, 0, enableSecondLayoutWidth, buttonHeight);
            enableSecondLayoutCheckbox.AutoSize = false;
            enableSecondLayoutCheckbox.IsChecked = false; // Default: second layout disabled
            enableSecondLayoutCheckbox.OnCheckedChanged = (isChecked) => {
                UpdateSecondLayoutVisibility(isChecked);
                sidePanel?.SetSecondLayoutVisibility(isChecked);
                // Switch back to Layout 1 metadata and hide dropdown if second layout is disabled
                if (!isChecked)
                {
                    sidePanel?.SwitchToLayout1MetadataAndHideDropdown();
                }
                else
                {
                    sidePanel?.ShowMetadataLayoutDropdown();
                }
            };
            radioButtonsContainer.AddChild(enableSecondLayoutCheckbox);

            // Create container for right-side controls (Save Layout button and Show Disabled checkbox)
            var rightControlsContainer = new Components.Container("RightControlsContainer");
            rightControlsContainer.AutoLayoutChildren = true;
            rightControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            rightControlsContainer.AutoSize = false;
            rightControlsContainer.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            rightControlsContainer.ChildPadding = 0;
            rightControlsContainer.ChildGap = 15;
            rightControlsContainer.ChildJustification = Components.ChildJustification.Right;
            rightControlsContainer.PositionMode = Components.PositionMode.Relative;

            // Create Save Layout button (first item on right)
            saveLayoutButton = new Components.Button(font, "Save Layout", 14);
            saveLayoutButton.Bounds = new Rectangle(0, 0, 120, buttonHeight);
            saveLayoutButton.AutoSize = false;
            saveLayoutButton.OnClick = SaveLayoutToJson;
            rightControlsContainer.AddChild(saveLayoutButton);

            // Create Add Key button (after Save Layout)
            addKeyButton = new Components.Button(font, "Add Key", 14);
            addKeyButton.Bounds = new Rectangle(0, 0, 100, buttonHeight);
            addKeyButton.AutoSize = false;
            addKeyButton.OnClick = AddNewKey;
            rightControlsContainer.AddChild(addKeyButton);

            // Create checkbox for showing disabled keys (last item on right)
            float showDisabledWidth = FontManager.MeasureText(font, "Show Disabled", 14) + 16 + 8; // text + checkbox + spacing
            showDisabledCheckbox = new Components.Checkbox(font, "Show Disabled", 14);
            showDisabledCheckbox.Bounds = new Rectangle(0, 0, showDisabledWidth, buttonHeight);
            showDisabledCheckbox.AutoSize = false;
            showDisabledCheckbox.IsChecked = true; // Default: show disabled keys
            showDisabledCheckbox.OnCheckedChanged = (isChecked) => {
                keyboardView.ShowDisabledKeys = isChecked;
                if (keyboardView2 != null)
                    keyboardView2.ShowDisabledKeys = isChecked;
            };
            rightControlsContainer.AddChild(showDisabledCheckbox);

            viewControlsContainer.AddChild(rightControlsContainer);

            // Initially hide the checkbox if there are no disabled keys
            UpdateShowDisabledCheckboxVisibility();

            // Create canvas to hold the keyboard view
            keyboardCanvas = new Components.Canvas("KeyboardCanvas");
            keyboardCanvas.AutoSize = true; // Auto-size to fit keyboard
            keyboardCanvas.AutoLayoutChildren = true; // Enable layout to calculate size
            keyboardCanvas.PositionMode = Components.PositionMode.Relative;

            // Create keyboard layout view
            keyboardView = new Components.KeyboardLayoutView(font);
            keyboardView.Layout = layout; // Layout was either loaded from file or created programmatically
            keyboardView.PositionMode = Components.PositionMode.Relative;
            keyboardView.ShowDisabledKeys = true; // Default: show disabled keys
            keyboardView.ViewMode = Components.KeyboardViewMode.Heatmap; // Default to heatmap view (matches heatmapRadioButton.IsSelected = true)
            keyboardView.OnSelectedKeysChanged = (keys) => {
                System.Console.WriteLine($"LayoutTab.OnSelectedKeysChanged callback: {keys?.Count ?? 0} keys");
                // Use the property getter to get current value
                var currentSidePanel = SidePanel;
                System.Console.WriteLine($"LayoutTab: currentSidePanel is {(currentSidePanel != null ? "not null" : "null")}");
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetLayout(layout); // Set layout reference for rebuilding mappings
                    currentSidePanel.SetKeyboardView(keyboardView); // Set keyboardView for HSV color controls
                    currentSidePanel.SetSelectedKeys(keys);
                }
            };
            
            // Legacy callback for backwards compatibility
            keyboardView.OnSelectedKeyChanged = (key) => {
                var currentSidePanel = SidePanel;
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetLayout(layout);
                    currentSidePanel.SetKeyboardView(keyboardView); // Set keyboardView for HSV color controls
                    currentSidePanel.SetSelectedKey(key);
                }
            };
            
            // Notify when keys are swapped
            keyboardView.OnKeysSwapped = () => {
                NotifyLayoutChanged();
            };
            
            keyboardCanvas.AddChild(keyboardView);
            
            // Create second layout controls container (similar structure to first)
            layoutControlsContainer2 = new Components.Container("LayoutControlsContainer2");
            layoutControlsContainer2.AutoLayoutChildren = true;
            layoutControlsContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            layoutControlsContainer2.AutoSize = false;
            layoutControlsContainer2.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            layoutControlsContainer2.ChildPadding = 0;
            layoutControlsContainer2.ChildGap = 15;
            layoutControlsContainer2.ChildJustification = Components.ChildJustification.SpaceBetween;
            layoutControlsContainer2.PositionMode = Components.PositionMode.Relative;
            
            // Create container for left-side controls (Load Layout and dropdown)
            var leftControlsContainer2 = new Components.Container("LeftControlsContainer2");
            leftControlsContainer2.AutoLayoutChildren = true;
            leftControlsContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            leftControlsContainer2.AutoSize = false;
            leftControlsContainer2.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            leftControlsContainer2.ChildPadding = 0;
            leftControlsContainer2.ChildGap = 15;
            leftControlsContainer2.ChildJustification = Components.ChildJustification.Left;
            leftControlsContainer2.PositionMode = Components.PositionMode.Relative;
            
            // Create Load Layout button for second layout (first item on left)
            loadLayoutButton2 = new Components.Button(font, "Load Layout", 14);
            loadLayoutButton2.Bounds = new Rectangle(0, 0, 120, buttonHeight);
            loadLayoutButton2.AutoSize = false;
            loadLayoutButton2.OnClick = LoadLayoutFromFile2;
            leftControlsContainer2.AddChild(loadLayoutButton2);
            
            // Initialize second layouts dropdown (second item on left)
            layoutsDropdown2 = new Components.Dropdown(font, layoutFiles, 14);
            layoutsDropdown2.SetBounds(new Rectangle(0, 0, 200, buttonHeight));
            layoutsDropdown2.AutoSize = false;
            layoutsDropdown2.OnSelectionChanged = OnLayoutSelected2;
            // Set initial selection if we loaded qwerty.json
            if (loadedQwerty && layoutFiles.Contains("qwerty.json"))
            {
                layoutsDropdown2.SetSelectedItem("qwerty.json");
            }
            leftControlsContainer2.AddChild(layoutsDropdown2);
            
            layoutControlsContainer2.AddChild(leftControlsContainer2);
            
            // Create container for right-side controls (Save Layout and Add Key buttons)
            var rightControlsContainer2 = new Components.Container("RightControlsContainer2");
            rightControlsContainer2.AutoLayoutChildren = true;
            rightControlsContainer2.LayoutDirection = Components.LayoutDirection.Horizontal;
            rightControlsContainer2.AutoSize = false;
            rightControlsContainer2.Bounds = new Rectangle(0, 0, 0, buttonHeight);
            rightControlsContainer2.ChildPadding = 0;
            rightControlsContainer2.ChildGap = 15;
            rightControlsContainer2.ChildJustification = Components.ChildJustification.Right;
            rightControlsContainer2.PositionMode = Components.PositionMode.Relative;
            
            // Create Save Layout button for second layout (first item on right)
            saveLayoutButton2 = new Components.Button(font, "Save Layout", 14);
            saveLayoutButton2.Bounds = new Rectangle(0, 0, 120, buttonHeight);
            saveLayoutButton2.AutoSize = false;
            saveLayoutButton2.OnClick = SaveLayoutToJson2;
            rightControlsContainer2.AddChild(saveLayoutButton2);
            
            // Create Add Key button for second layout (after Save Layout)
            addKeyButton2 = new Components.Button(font, "Add Key", 14);
            addKeyButton2.Bounds = new Rectangle(0, 0, 100, buttonHeight);
            addKeyButton2.AutoSize = false;
            addKeyButton2.OnClick = AddNewKey2;
            rightControlsContainer2.AddChild(addKeyButton2);
            
            layoutControlsContainer2.AddChild(rightControlsContainer2);
            
            // Create second canvas to hold the second keyboard view
            keyboardCanvas2 = new Components.Canvas("KeyboardCanvas2");
            keyboardCanvas2.AutoSize = true; // Auto-size to fit keyboard
            keyboardCanvas2.AutoLayoutChildren = true; // Enable layout to calculate size
            keyboardCanvas2.PositionMode = Components.PositionMode.Relative;
            
            // Create second keyboard layout view
            keyboardView2 = new Components.KeyboardLayoutView(font);
            keyboardView2.Layout = layout2;
            keyboardView2.PositionMode = Components.PositionMode.Relative;
            keyboardView2.ShowDisabledKeys = true; // Default: show disabled keys
            keyboardView2.ViewMode = Components.KeyboardViewMode.Heatmap; // Default to heatmap view (matches first view)
            keyboardView2.OnSelectedKeysChanged = (keys) => {
                System.Console.WriteLine($"LayoutTab.OnSelectedKeysChanged callback (layout 2): {keys?.Count ?? 0} keys");
                var currentSidePanel = SidePanel;
                System.Console.WriteLine($"LayoutTab: currentSidePanel is {(currentSidePanel != null ? "not null" : "null")}");
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetLayout(layout2); // Set layout2 reference for editing
                    currentSidePanel.SetKeyboardView(keyboardView2, isLayout2: true); // Set keyboardView2 for HSV color controls
                    currentSidePanel.SetSelectedKeys(keys, isLayout2: true);
                }
            };
            
            // Legacy callback for backwards compatibility
            keyboardView2.OnSelectedKeyChanged = (key) => {
                var currentSidePanel = SidePanel;
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetLayout(layout2); // Set layout2 reference for editing
                    currentSidePanel.SetKeyboardView(keyboardView2, isLayout2: true); // Set keyboardView2 for HSV color controls
                    currentSidePanel.SetSelectedKey(key, isLayout2: true);
                }
            };
            
            // Notify when keys are swapped in second layout
            keyboardView2.OnKeysSwapped = () => {
                // Only update corpus tab if layout2 changes? Actually, corpus tab uses first layout only
                // So we don't need to call NotifyLayoutChanged() here
            };
            
            keyboardCanvas2.AddChild(keyboardView2);
            
            tabContent.AddChild(radioButtonsContainer);
            tabContent.AddChild(viewControlsContainer);
            tabContent.AddChild(keyboardCanvas);
            tabContent.AddChild(layoutControlsContainer2);
            tabContent.AddChild(keyboardCanvas2);
            
            // Initially hide the checkbox if there are no disabled keys
            UpdateShowDisabledCheckboxVisibility();
            
            // Initially hide second layout (checkbox is unchecked by default)
            UpdateSecondLayoutVisibility(false);
            sidePanel?.SetSecondLayoutVisibility(false);
            
            // Set keyboard view reference in side panel for HSV color controls
            if (sidePanel != null)
            {
                sidePanel.SetKeyboardView(keyboardView);
                // Set initial view mode to heatmap (matches default radio button selection)
                sidePanel.SetViewMode(Components.KeyboardViewMode.Heatmap);
            }
            
            // Initially hide heatmap button (will be shown when corpus is loaded)
            UpdateHeatmapButtonVisibility();
            
            // Initialize heatmap data if we have a corpus
            UpdateHeatmapData();
        }

        /// <summary>
        /// Called when corpus is loaded or changed to update heatmap data and button visibility.
        /// </summary>
        public void OnCorpusChanged()
        {
            UpdateHeatmapButtonVisibility();
            if (keyboardView.ViewMode == Components.KeyboardViewMode.Heatmap)
            {
                UpdateHeatmapData();
            }
        }

        public void Update(Rectangle contentArea)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);
            tabContent.TargetHeight = contentArea.Height; // Set target height for vertical layout

            // Set view controls container width
            float availableWidth = contentArea.Width - (tabContent.ChildPadding * 2);
            if (viewControlsContainer != null)
            {
                viewControlsContainer.Bounds = new Rectangle(0, 0, availableWidth, 30);
                // Note: showDisabledCheckbox width is set in InitializeUI, no need to update here
            }

            // Set radio buttons container width
            if (radioButtonsContainer != null)
            {
                radioButtonsContainer.Bounds = new Rectangle(0, 0, availableWidth, 30);
            }

            // Set second layout controls container width
            if (layoutControlsContainer2 != null)
            {
                layoutControlsContainer2.Bounds = new Rectangle(0, 0, availableWidth, 30);
            }

            // Set keyboard view initial size calculation (width/height will be calculated in ResolveBounds)
            // We just need to trigger the initial calculation
            if (keyboardView.Layout != null && keyboardView.Bounds.Width <= 0)
            {
                // Calculate the bounding box of all keys to set initial size
                float maxX = 0;
                float maxY = 0;
                foreach (var key in keyboardView.Layout.GetPhysicalKeys())
                {
                    float keyRight = key.X + key.Width;
                    float keyBottom = key.Y + key.Height;
                    if (keyRight > maxX) maxX = keyRight;
                    if (keyBottom > maxY) maxY = keyBottom;
                }
                if (maxX > 0 && maxY > 0)
                {
                    keyboardView.Bounds = new Rectangle(0, 0, maxX * keyboardView.PixelsPerU + 40, maxY * keyboardView.PixelsPerU + 40);
                }
            }

            // Set second keyboard view initial size calculation
            if (keyboardView2 != null && keyboardView2.Layout != null && keyboardView2.Bounds.Width <= 0)
            {
                // Calculate the bounding box of all keys to set initial size
                float maxX = 0;
                float maxY = 0;
                foreach (var key in keyboardView2.Layout.GetPhysicalKeys())
                {
                    float keyRight = key.X + key.Width;
                    float keyBottom = key.Y + key.Height;
                    if (keyRight > maxX) maxX = keyRight;
                    if (keyBottom > maxY) maxY = keyBottom;
                }
                if (maxX > 0 && maxY > 0)
                {
                    keyboardView2.Bounds = new Rectangle(0, 0, maxX * keyboardView2.PixelsPerU + 40, maxY * keyboardView2.PixelsPerU + 40);
                }
            }

            // Align the keyboard views to top-left within their canvases using relative position
            keyboardView.RelativePosition = new System.Numerics.Vector2(0, 0);
            if (keyboardView2 != null)
            {
                keyboardView2.RelativePosition = new System.Numerics.Vector2(0, 0);
            }
        }

        /// <summary>
        /// Called after ResolveBounds() to constrain canvas width to available space.
        /// This should be called from MainPanel after ResolveBounds() has been called.
        /// </summary>
        public void ConstrainCanvasWidth(Rectangle contentArea)
        {
            // Calculate available width accounting for padding
            float availableWidth = contentArea.Width - (tabContent.ChildPadding * 2);
            
            // Constrain first canvas width if it exceeds available space
            if (keyboardCanvas != null)
            {
                if (keyboardCanvas.Bounds.Width > availableWidth)
                {
                    keyboardCanvas.Bounds = new Rectangle(
                        keyboardCanvas.Bounds.X,
                        keyboardCanvas.Bounds.Y,
                        availableWidth,
                        keyboardCanvas.Bounds.Height
                    );
                }
            }
            
            // Constrain second canvas width if it exceeds available space
            if (keyboardCanvas2 != null)
            {
                if (keyboardCanvas2.Bounds.Width > availableWidth)
                {
                    keyboardCanvas2.Bounds = new Rectangle(
                        keyboardCanvas2.Bounds.X,
                        keyboardCanvas2.Bounds.Y,
                        availableWidth,
                        keyboardCanvas2.Bounds.Height
                    );
                }
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update font in keyboard views
            keyboardView.UpdateFont(newFont);
            keyboardView2?.UpdateFont(newFont);
            // Update font in title label (would need UpdateFont in Label component)
            // For now, just update the keyboard views which are most visible
        }

        public void UpdateShowDisabledCheckboxVisibility()
        {
            if (showDisabledCheckbox != null && layout != null)
            {
                // Check if any keys in the layout are disabled
                bool hasDisabledKeys = layout.GetPhysicalKeys().Any(key => key.Disabled);
                showDisabledCheckbox.IsVisible = hasDisabledKeys;
            }
        }

        private void UpdateSecondLayoutVisibility(bool isVisible)
        {
            if (layoutControlsContainer2 != null)
            {
                layoutControlsContainer2.IsVisible = isVisible;
            }
            if (keyboardCanvas2 != null)
            {
                keyboardCanvas2.IsVisible = isVisible;
            }
        }

        private void UpdateHeatmapButtonVisibility()
        {
            if (heatmapRadioButton == null)
                return;

            // Check if corpus is loaded (we'll need to add a method to check this)
            bool hasCorpus = corpusTab != null && HasLoadedCorpus();
            
            heatmapRadioButton.IsVisible = hasCorpus;
            
            // If heatmap was selected but corpus was unloaded, switch to regular view
            // BUT: Don't switch if heatmap is the default selection (heatmapRadioButton.IsSelected = true)
            // This allows the default heatmap selection to persist even if corpus loads later during startup
            if (!hasCorpus && keyboardView.ViewMode == Components.KeyboardViewMode.Heatmap && heatmapRadioButton.IsSelected)
            {
                // Heatmap is the default selection, keep it even without corpus
                // The heatmap will just show no data initially, which is fine
                // Don't switch away from default selection
            }
            else if (!hasCorpus && keyboardView.ViewMode == Components.KeyboardViewMode.Heatmap)
            {
                // Heatmap mode but radio button not selected - user must have manually changed view mode
                // Switch to regular view
                keyboardView.ViewMode = Components.KeyboardViewMode.Regular;
                if (keyboardView2 != null)
                    keyboardView2.ViewMode = Components.KeyboardViewMode.Regular;
                if (regularRadioButton != null)
                {
                    regularRadioButton.IsSelected = true;
                    if (heatmapRadioButton.IsSelected)
                    {
                        heatmapRadioButton.IsSelected = false;
                    }
                }
            }
        }

        private bool HasLoadedCorpus()
        {
            return corpusTab != null && corpusTab.HasLoadedCorpus;
        }

        /// <summary>
        /// Updates the heatmap data from the loaded corpus's monograms.
        /// Should be called when corpus is loaded or changed.
        /// </summary>
        public void UpdateHeatmapData()
        {
            if (corpusTab == null)
            {
                keyboardView.SetMonogramCounts(null);
                keyboardView2?.SetMonogramCounts(null);
                return;
            }

            var corpus = corpusTab.LoadedCorpus;
            if (corpus != null && corpus.IsLoaded)
            {
                var monograms = corpus.GetMonograms();
                var counts = new Dictionary<string, long>();
                foreach (var kvp in monograms.Counts)
                {
                    counts[kvp.Key] = kvp.Value;
                }
                keyboardView.SetMonogramCounts(counts);
                keyboardView2?.SetMonogramCounts(counts);
            }
            else
            {
                keyboardView.SetMonogramCounts(null);
                keyboardView2?.SetMonogramCounts(null);
            }
        }

        private List<string> GetLayoutFiles()
        {
            List<string> files = new List<string>();
            string layoutDir = Path.Combine(Directory.GetCurrentDirectory(), "layouts");

            if (Directory.Exists(layoutDir))
            {
                var jsonFiles = Directory.GetFiles(layoutDir, "*.json")
                    .Select(f => Path.GetFileName(f))
                    .OrderBy(f => f)
                    .ToList();
                files.AddRange(jsonFiles);
            }

            return files;
        }

        private void SaveLayoutToJson()
        {
            if (layout == null)
                return;

            try
            {
                // Use zenity for file save dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Save Layout File\" --save --confirm-overwrite",
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

                        // Convert layout to JSON DTO (include metadata)
                        var layoutJson = LayoutJson.FromLayout(layout, metadata);

                        // Serialize to JSON
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        string json = JsonSerializer.Serialize(layoutJson, options);

                        // Write JSON file
                        File.WriteAllText(filePath, json);

                        System.Console.WriteLine($"Layout saved to: {filePath}");

                        // Refresh layouts dropdowns
                        RefreshLayoutsDropdown();
                        RefreshLayoutsDropdown2();
                    }
                    else
                    {
                        System.Console.WriteLine("Layout save cancelled");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file save dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving layout: {ex.Message}");
            }
        }

        private void LoadLayoutFromFile()
        {
            try
            {
                // Use zenity for file open dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Load Layout File\"",
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
                        LoadLayoutFromPath(filePath);
                    }
                    else
                    {
                        System.Console.WriteLine("Layout load cancelled");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file open dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading layout: {ex.Message}");
            }
        }

        private void OnLayoutSelected(string layoutFile)
        {
            // Prevent recursive loading
            if (isLoadingLayout)
                return;
                
            string layoutDir = Path.Combine(Directory.GetCurrentDirectory(), "layouts");
            string fullPath = Path.Combine(layoutDir, layoutFile);

            if (File.Exists(fullPath))
            {
                LoadLayoutFromPath(fullPath);
            }
        }

        private void LoadLayoutFromPath(string filePath, bool skipSidePanelUpdate = false)
        {
            // Prevent recursive loading and loading the same file twice
            string normalizedPath = Path.GetFullPath(filePath);
            if (isLoadingLayout || currentlyLoadedFile == normalizedPath)
                return;
                
            isLoadingLayout = true;
            string? previousFile = currentlyLoadedFile;
            currentlyLoadedFile = normalizedPath;
            try
            {
                System.Console.WriteLine($"Loading layout from: {filePath}");
                
                // Read JSON file
                string json = File.ReadAllText(filePath);
                System.Console.WriteLine($"Read {json.Length} characters from file");

                // Deserialize JSON
                var layoutJson = JsonSerializer.Deserialize<LayoutJson>(json);
                if (layoutJson == null)
                {
                    System.Console.WriteLine("Failed to deserialize layout JSON");
                    isLoadingLayout = false;
                    return;
                }
                
                System.Console.WriteLine($"Deserialized JSON: {layoutJson.Keys.Count} keys, {layoutJson.Mappings.Count} mappings");

                // Convert JSON DTO to Layout
                var newLayout = layoutJson.ToLayout();
                System.Console.WriteLine($"Converted to Layout: {newLayout.PhysicalKeyCount} physical keys, {newLayout.MappingCount} mappings");

                // Load metadata if present
                if (layoutJson.Metadata != null)
                {
                    metadata = layoutJson.Metadata;
                }
                else
                {
                    // Initialize default metadata if not present
                    metadata = new LayoutMetadataJson();
                }

                // Update the current layout
                layout = newLayout;
                
                // Only update keyboardView if it exists (it might not be created yet during initialization)
                if (keyboardView != null)
                {
                    keyboardView.Layout = layout;
                }

                // Update side panel if available and not skipping
                if (!skipSidePanelUpdate)
                {
                    sidePanel?.SetLayout(layout);
                    sidePanel?.SetLayoutMetadata(metadata);
                }

                // Update corpus tab with new layout reference
                System.Console.WriteLine($"Updating corpus tab with layout: {metadata.DisplayName ?? "Unknown"} ({newLayout.PhysicalKeyCount} keys, {newLayout.MappingCount} mappings)");
                corpusTab?.SetLayout(layout);

                // Update dropdown selection if this is a file from the layouts directory
                // Only update if it's different from what's already selected to avoid infinite recursion
                string layoutDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "layouts"));
                string fullFilePath = Path.GetFullPath(filePath);
                if (fullFilePath.StartsWith(layoutDir, StringComparison.Ordinal))
                {
                    string fileName = Path.GetFileName(filePath);
                    if (layoutsDropdown != null)
                    {
                        // Only update if it's different to avoid unnecessary work
                        if (layoutsDropdown.SelectedItem != fileName)
                        {
                            // Temporarily remove the callback to prevent recursion when programmatically setting selection
                            var oldCallback = layoutsDropdown.OnSelectionChanged;
                            layoutsDropdown.OnSelectionChanged = null;
                            
                            // Refresh the dropdown items first in case the file list changed
                            RefreshLayoutsDropdown();
                            // Then set the selection (this won't trigger callback since we removed it)
                            layoutsDropdown.SetSelectedItem(fileName);
                            
                            // Restore the callback
                            layoutsDropdown.OnSelectionChanged = oldCallback;
                        }
                    }
                    initialLayoutFile = fileName;
                }

                System.Console.WriteLine($"Layout loaded from: {filePath}");

                // Update show disabled checkbox visibility (only if keyboardView exists, since it might not be created yet during initialization)
                if (keyboardView != null)
                {
                    UpdateShowDisabledCheckboxVisibility();
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading layout: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // If loading fails during initialization, fallback to programmatic creation
                if (layout == null)
                {
                    System.Console.WriteLine("Falling back to programmatic layout creation");
                    layout = Layout.CreateStandard60PercentQwerty();
                    metadata = new LayoutMetadataJson();
                }
            }
            finally
            {
                isLoadingLayout = false;
                // Only clear currentlyLoadedFile if we successfully loaded, otherwise restore previous
                // (in case of error, we want to allow retry)
                if (layout != null)
                {
                    // Keep currentlyLoadedFile set
                }
                else
                {
                    currentlyLoadedFile = previousFile;
                }
            }
        }

        private void OnLayoutSelected2(string layoutFile)
        {
            // Prevent recursive loading
            if (isLoadingLayout2)
                return;
                
            string layoutDir = Path.Combine(Directory.GetCurrentDirectory(), "layouts");
            string fullPath = Path.Combine(layoutDir, layoutFile);

            if (File.Exists(fullPath))
            {
                LoadLayoutFromPath2(fullPath);
            }
        }

        private void LoadLayoutFromPath2(string filePath)
        {
            // Prevent recursive loading and loading the same file twice
            string normalizedPath = Path.GetFullPath(filePath);
            if (isLoadingLayout2 || currentlyLoadedFile2 == normalizedPath)
                return;
                
            isLoadingLayout2 = true;
            string? previousFile = currentlyLoadedFile2;
            currentlyLoadedFile2 = normalizedPath;
            try
            {
                System.Console.WriteLine($"Loading second layout from: {filePath}");
                
                // Read JSON file
                string json = File.ReadAllText(filePath);
                System.Console.WriteLine($"Read {json.Length} characters from file");

                // Deserialize JSON
                var layoutJson = JsonSerializer.Deserialize<LayoutJson>(json);
                if (layoutJson == null)
                {
                    System.Console.WriteLine("Failed to deserialize layout JSON");
                    isLoadingLayout2 = false;
                    return;
                }
                
                System.Console.WriteLine($"Deserialized JSON: {layoutJson.Keys.Count} keys");

                // Convert JSON DTO to Layout
                var newLayout = layoutJson.ToLayout();
                System.Console.WriteLine($"Converted to Layout: {newLayout.PhysicalKeyCount} physical keys, {newLayout.MappingCount} mappings");

                // Load metadata if present
                if (layoutJson.Metadata != null)
                {
                    metadata2 = layoutJson.Metadata;
                }
                else
                {
                    // Initialize default metadata if not present
                    metadata2 = new LayoutMetadataJson();
                }

                // Update the second layout
                layout2 = newLayout;
                
                // Update keyboardView2 if it exists
                if (keyboardView2 != null)
                {
                    keyboardView2.Layout = layout2;
                }

                // Update dropdown selection if this is a file from the layouts directory
                string layoutDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "layouts"));
                string fullFilePath = Path.GetFullPath(filePath);
                if (fullFilePath.StartsWith(layoutDir, StringComparison.Ordinal))
                {
                    string fileName = Path.GetFileName(filePath);
                    if (layoutsDropdown2 != null)
                    {
                        // Only update if it's different to avoid unnecessary work
                        if (layoutsDropdown2.SelectedItem != fileName)
                        {
                            // Temporarily remove the callback to prevent recursion when programmatically setting selection
                            var oldCallback = layoutsDropdown2.OnSelectionChanged;
                            layoutsDropdown2.OnSelectionChanged = null;
                            
                            // Refresh the dropdown items first in case the file list changed
                            RefreshLayoutsDropdown2();
                            // Then set the selection (this won't trigger callback since we removed it)
                            layoutsDropdown2.SetSelectedItem(fileName);
                            
                            // Restore the callback
                            layoutsDropdown2.OnSelectionChanged = oldCallback;
                        }
                    }
                }

                System.Console.WriteLine($"Second layout loaded from: {filePath}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading second layout: {ex.Message}");
                System.Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // If loading fails, fallback to programmatic creation
                if (layout2 == null)
                {
                    System.Console.WriteLine("Falling back to programmatic layout creation for second layout");
                    layout2 = Layout.CreateStandard60PercentQwerty();
                    metadata2 = new LayoutMetadataJson();
                }
            }
            finally
            {
                isLoadingLayout2 = false;
                // Only clear currentlyLoadedFile2 if we successfully loaded, otherwise restore previous
                if (layout2 != null)
                {
                    // Keep currentlyLoadedFile2 set
                }
                else
                {
                    currentlyLoadedFile2 = previousFile;
                }
            }
        }

        private void AddNewKey()
        {
            if (layout == null)
                return;

            // Find the bottom-rightmost key (key with highest bottom Y, and if tie, highest right X)
            // NOTE: We should consider ALL keys (including disabled ones) when determining placement
            float maxBottomY = float.MinValue;
            float maxRightX = float.MinValue;
            PhysicalKey? bottomRightmostKey = null;
            bool hasKeys = false;

            foreach (var key in layout.GetPhysicalKeys())
            {
                float rightX = key.X + key.Width;
                float bottomY = key.Y + key.Height;

                // Check if this key is further down, or same row but further right
                if (bottomY > maxBottomY || (bottomY == maxBottomY && rightX > maxRightX))
                {
                    maxBottomY = bottomY;
                    maxRightX = rightX;
                    bottomRightmostKey = key;
                }

                hasKeys = true;
            }

            // Calculate position for new key: aligned with the right edge of the bottom-rightmost key
            // Use maxRightX directly since we already computed the right edge
            float newX = hasKeys ? maxRightX : 0.0f;
            float newY = hasKeys && bottomRightmostKey != null ? bottomRightmostKey.Y : 0.0f; // Same Y level (same row)

            // Auto-generate identifier (Key1, Key2, etc.)
            int keyNumber = 1;
            string identifier;
            bool identifierExists = true;
            while (identifierExists)
            {
                identifier = $"Key{keyNumber}";
                identifierExists = false;
                foreach (var key in layout.GetPhysicalKeys())
                {
                    if (key.Identifier == identifier)
                    {
                        identifierExists = true;
                        keyNumber++;
                        break;
                    }
                }
                if (!identifierExists)
                {
                    identifier = $"Key{keyNumber}";
                    break;
                }
            }
            identifier = $"Key{keyNumber}";

            // Create new key with default properties
            var newKey = new Core.PhysicalKey(
                x: newX,
                y: newY,
                width: 1.0f,  // 1U width
                height: 1.0f, // 1U height
                finger: Core.Finger.LeftIndex, // Default finger
                identifier: identifier
            );
            // Characters are empty by default (PrimaryCharacter and ShiftCharacter are null)

            // Add key to layout
            layout.AddPhysicalKey(newKey);
            layout.RebuildMappings();

            // Update keyboard view
            keyboardView.Layout = layout; // This will clear cache and force redraw

            // Select the new key
            keyboardView.SelectedKey = newKey;
        }

        private void RefreshLayoutsDropdown()
        {
            if (layoutsDropdown != null)
            {
                List<string> layoutFiles = GetLayoutFiles();
                layoutsDropdown.SetItems(layoutFiles);
            }
        }

        private void RefreshLayoutsDropdown2()
        {
            if (layoutsDropdown2 != null)
            {
                List<string> layoutFiles = GetLayoutFiles();
                layoutsDropdown2.SetItems(layoutFiles);
            }
        }

        private void SaveLayoutToJson2()
        {
            if (layout2 == null)
                return;

            try
            {
                // Use zenity for file save dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Save Layout File (Layout 2)\" --save --confirm-overwrite",
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

                        // Convert layout2 to JSON DTO (include metadata2)
                        var layoutJson = LayoutJson.FromLayout(layout2, metadata2);

                        // Serialize to JSON
                        var options = new JsonSerializerOptions
                        {
                            WriteIndented = true
                        };
                        string json = JsonSerializer.Serialize(layoutJson, options);

                        // Write JSON file
                        File.WriteAllText(filePath, json);

                        System.Console.WriteLine($"Layout 2 saved to: {filePath}");

                        // Refresh layouts dropdowns
                        RefreshLayoutsDropdown();
                        RefreshLayoutsDropdown2();
                    }
                    else
                    {
                        System.Console.WriteLine("Layout 2 save cancelled");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file save dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error saving layout 2: {ex.Message}");
            }
        }

        private void LoadLayoutFromFile2()
        {
            try
            {
                // Use zenity for file open dialog on Linux
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = "zenity",
                    Arguments = "--file-selection --title=\"Load Layout File (Layout 2)\"",
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
                        LoadLayoutFromPath2(filePath);
                    }
                    else
                    {
                        System.Console.WriteLine("Layout 2 load cancelled");
                    }
                }
                else
                {
                    System.Console.WriteLine("zenity not available for file open dialog");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Error loading layout 2: {ex.Message}");
            }
        }

        private void AddNewKey2()
        {
            if (layout2 == null)
                return;

            // Find the bottom-rightmost key (key with highest bottom Y, and if tie, highest right X)
            // NOTE: We should consider ALL keys (including disabled ones) when determining placement
            float maxBottomY = float.MinValue;
            float maxRightX = float.MinValue;
            PhysicalKey? bottomRightmostKey = null;
            bool hasKeys = false;

            foreach (var key in layout2.GetPhysicalKeys())
            {
                float rightX = key.X + key.Width;
                float bottomY = key.Y + key.Height;

                // Check if this key is further down, or same row but further right
                if (bottomY > maxBottomY || (bottomY == maxBottomY && rightX > maxRightX))
                {
                    maxBottomY = bottomY;
                    maxRightX = rightX;
                    bottomRightmostKey = key;
                }

                hasKeys = true;
            }

            // Calculate position for new key: aligned with the right edge of the bottom-rightmost key
            // Use maxRightX directly since we already computed the right edge
            float newX = hasKeys ? maxRightX : 0.0f;
            float newY = hasKeys && bottomRightmostKey != null ? bottomRightmostKey.Y : 0.0f; // Same Y level (same row)

            // Auto-generate identifier (Key1, Key2, etc.)
            int keyNumber = 1;
            string identifier;
            bool identifierExists = true;
            while (identifierExists)
            {
                identifier = $"Key{keyNumber}";
                identifierExists = false;
                foreach (var key in layout2.GetPhysicalKeys())
                {
                    if (key.Identifier == identifier)
                    {
                        identifierExists = true;
                        keyNumber++;
                        break;
                    }
                }
                if (!identifierExists)
                {
                    identifier = $"Key{keyNumber}";
                    break;
                }
            }
            identifier = $"Key{keyNumber}";

            // Create new key with default properties
            var newKey = new Core.PhysicalKey(
                x: newX,
                y: newY,
                width: 1.0f,  // 1U width
                height: 1.0f, // 1U height
                finger: Core.Finger.LeftIndex, // Default finger
                identifier: identifier
            );
            // Characters are empty by default (PrimaryCharacter and ShiftCharacter are null)

            // Add key to layout2
            layout2.AddPhysicalKey(newKey);
            layout2.RebuildMappings();

            // Update keyboard view
            if (keyboardView2 != null)
            {
                keyboardView2.Layout = layout2; // This will clear cache and force redraw
                // Select the new key
                keyboardView2.SelectedKey = newKey;
            }
        }
    }
}

