using Raylib_cs;
using System;
using System.Collections.Generic;
using System.Linq;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class LayoutTab
    {
        private Components.TabContent tabContent;
        private Components.Label titleLabel;
        private Components.Container? viewControlsContainer;
        private Components.Container? radioButtonsContainer;
        private Components.RadioButton? regularRadioButton;
        private Components.RadioButton? fingerColorsRadioButton;
        private Components.RadioButton? heatmapRadioButton;
        private Components.Checkbox? showDisabledCheckbox;
        private Components.Canvas keyboardCanvas;
        private Components.KeyboardLayoutView keyboardView;
        private Layout layout;
        private SidePanel? sidePanel;
        private Font font;
        private CorpusTab? corpusTab; // Reference to corpus tab for checking loaded corpus

        public Components.TabContent TabContent => tabContent;

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
                    // Also notify of current view mode
                    sidePanel.SetViewMode(keyboardView.ViewMode);
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

        public LayoutTab(Font font)
        {
            this.font = font;
            // Create the standard 60% QWERTY layout
            layout = Layout.CreateStandard60PercentQwerty();

            // Create tab content without title (we'll use a Label element instead)
            tabContent = new Components.TabContent(font, "", null);
            tabContent.PositionMode = Components.PositionMode.Relative;
            tabContent.AutoLayoutChildren = true;
            tabContent.LayoutDirection = Components.LayoutDirection.Vertical;
            tabContent.ChildJustification = Components.ChildJustification.Left;
            tabContent.ChildPadding = 20;
            tabContent.ChildGap = 10; // Gap between title, controls, and keyboard

            // Create title label
            titleLabel = new Components.Label(font, "Layout", 24);
            titleLabel.AutoSize = false;
            titleLabel.Bounds = new Rectangle(0, 0, 0, 40); // Height for title
            titleLabel.PositionMode = Components.PositionMode.Relative;

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

            // Create container for radio buttons (so they stay left-justified together)
            radioButtonsContainer = new Components.Container("RadioButtonsContainer");
            radioButtonsContainer.AutoLayoutChildren = true;
            radioButtonsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            radioButtonsContainer.AutoSize = false;
            radioButtonsContainer.Bounds = new Rectangle(0, 0, 0, 30); // Height to match controls
            radioButtonsContainer.ChildPadding = 0;
            radioButtonsContainer.ChildGap = 15;
            radioButtonsContainer.ChildJustification = Components.ChildJustification.Left; // Left-justify radio buttons
            radioButtonsContainer.PositionMode = Components.PositionMode.Relative;
            viewControlsContainer.AddChild(radioButtonsContainer);

            // Create radio buttons for view modes
            const string radioGroup = "ViewMode";
            
            // Helper function to deselect other radio buttons
            Action<Components.RadioButton> deselectOthers = (selectedButton) => {
                if (regularRadioButton != null && selectedButton != regularRadioButton) regularRadioButton.IsSelected = false;
                if (fingerColorsRadioButton != null && selectedButton != fingerColorsRadioButton) fingerColorsRadioButton.IsSelected = false;
                if (heatmapRadioButton != null && selectedButton != heatmapRadioButton) heatmapRadioButton.IsSelected = false;
            };

            // Calculate button widths based on text
            int buttonHeight = 30;
            float regularWidth = FontManager.MeasureText(font, "Regular", 14) + 16 + 8; // text + radio + spacing
            float fingerColorsWidth = FontManager.MeasureText(font, "Finger Colors", 14) + 16 + 8;
            float heatmapWidth = FontManager.MeasureText(font, "Heatmap", 14) + 16 + 8;

            regularRadioButton = new Components.RadioButton(font, "Regular", radioGroup, 14);
            regularRadioButton.Bounds = new Rectangle(0, 0, regularWidth, buttonHeight);
            regularRadioButton.AutoSize = false;
            regularRadioButton.IsSelected = true; // Default selection
            regularRadioButton!.OnSelectedInGroup = deselectOthers;
            regularRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.Regular;
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.Regular);
                }
            };

            fingerColorsRadioButton = new Components.RadioButton(font, "Finger Colors", radioGroup, 14);
            fingerColorsRadioButton.Bounds = new Rectangle(0, 0, fingerColorsWidth, buttonHeight);
            fingerColorsRadioButton.AutoSize = false;
            fingerColorsRadioButton!.OnSelectedInGroup = deselectOthers;
            fingerColorsRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.FingerColors;
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.FingerColors);
                }
            };

            heatmapRadioButton = new Components.RadioButton(font, "Heatmap", radioGroup, 14);
            heatmapRadioButton.Bounds = new Rectangle(0, 0, heatmapWidth, buttonHeight);
            heatmapRadioButton.AutoSize = false;
            heatmapRadioButton!.OnSelectedInGroup = deselectOthers;
            heatmapRadioButton.OnSelectedChanged = (selected) => {
                if (selected)
                {
                    keyboardView.ViewMode = Components.KeyboardViewMode.Heatmap;
                    UpdateHeatmapData(); // Update heatmap data when selected
                    // Notify side panel of view mode change
                    sidePanel?.SetViewMode(Components.KeyboardViewMode.Heatmap);
                }
            };

            radioButtonsContainer.AddChild(regularRadioButton);
            radioButtonsContainer.AddChild(fingerColorsRadioButton);
            radioButtonsContainer.AddChild(heatmapRadioButton);

            // Create checkbox for showing disabled keys (right-justified)
            float showDisabledWidth = FontManager.MeasureText(font, "Show Disabled", 14) + 16 + 8; // text + checkbox + spacing
            showDisabledCheckbox = new Components.Checkbox(font, "Show Disabled", 14);
            showDisabledCheckbox.Bounds = new Rectangle(0, 0, showDisabledWidth, buttonHeight);
            showDisabledCheckbox.AutoSize = false;
            showDisabledCheckbox.IsChecked = true; // Default: show disabled keys
            showDisabledCheckbox.OnCheckedChanged = (isChecked) => {
                keyboardView.ShowDisabledKeys = isChecked;
            };
            viewControlsContainer.AddChild(showDisabledCheckbox);

            // Initially hide the checkbox if there are no disabled keys
            UpdateShowDisabledCheckboxVisibility();

            // Create canvas to hold the keyboard view
            keyboardCanvas = new Components.Canvas("KeyboardCanvas");
            keyboardCanvas.AutoSize = true; // Auto-size to fit keyboard
            keyboardCanvas.AutoLayoutChildren = true; // Enable layout to calculate size
            keyboardCanvas.PositionMode = Components.PositionMode.Relative;

            // Create keyboard layout view
            keyboardView = new Components.KeyboardLayoutView(font);
            keyboardView.Layout = layout;
            keyboardView.PositionMode = Components.PositionMode.Relative;
            keyboardView.ShowDisabledKeys = true; // Default: show disabled keys
            keyboardView.OnSelectedKeyChanged = (key) => {
                // Use the property getter to get current value
                var currentSidePanel = SidePanel;
                if (currentSidePanel != null)
                {
                    currentSidePanel.SetLayout(layout); // Set layout reference for rebuilding mappings
                    currentSidePanel.SetSelectedKey(key);
                }
            };
            
            keyboardCanvas.AddChild(keyboardView);
            tabContent.AddChild(titleLabel);
            tabContent.AddChild(viewControlsContainer);
            tabContent.AddChild(keyboardCanvas);
            
            // Initially hide the checkbox if there are no disabled keys
            UpdateShowDisabledCheckboxVisibility();
            
            // Set keyboard view reference in side panel for HSV color controls
            if (sidePanel != null)
            {
                sidePanel.SetKeyboardView(keyboardView);
            }
            
            // Initially hide heatmap button (will be shown when corpus is loaded)
            UpdateHeatmapButtonVisibility();
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

            // Set title label width to fill available space (accounting for padding)
            float availableWidth = contentArea.Width - (tabContent.ChildPadding * 2);
            titleLabel.Bounds = new Rectangle(0, 0, availableWidth, 40);

            // Set view controls container width
            if (viewControlsContainer != null)
            {
                viewControlsContainer.Bounds = new Rectangle(0, 0, availableWidth, 30);
                // Note: showDisabledCheckbox width is set in InitializeUI, no need to update here
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

            // Align the keyboard view to top-left within the canvas using relative position
            keyboardView.RelativePosition = new System.Numerics.Vector2(0, 0);
        }

        /// <summary>
        /// Called after ResolveBounds() to constrain canvas width to available space.
        /// This should be called from MainPanel after ResolveBounds() has been called.
        /// </summary>
        public void ConstrainCanvasWidth(Rectangle contentArea)
        {
            if (keyboardCanvas != null)
            {
                // Calculate available width accounting for padding
                float availableWidth = contentArea.Width - (tabContent.ChildPadding * 2);
                
                // Constrain canvas width if it exceeds available space
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
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update font in keyboard view
            keyboardView.UpdateFont(newFont);
            // Update font in title label (would need UpdateFont in Label component)
            // For now, just update the keyboard view which is most visible
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

        private void UpdateHeatmapButtonVisibility()
        {
            if (heatmapRadioButton == null)
                return;

            // Check if corpus is loaded (we'll need to add a method to check this)
            bool hasCorpus = corpusTab != null && HasLoadedCorpus();
            
            heatmapRadioButton.IsVisible = hasCorpus;
            
            // If heatmap was selected but corpus was unloaded, switch to regular view
            if (!hasCorpus && keyboardView.ViewMode == Components.KeyboardViewMode.Heatmap)
            {
                keyboardView.ViewMode = Components.KeyboardViewMode.Regular;
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
            }
            else
            {
                keyboardView.SetMonogramCounts(null);
            }
        }
    }
}

