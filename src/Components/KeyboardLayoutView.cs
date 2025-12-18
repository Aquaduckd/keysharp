using Raylib_cs;
using System;
using System.Collections.Generic;
using Keysharp.Core;

namespace Keysharp.Components
{
    /// <summary>
    /// View modes for the keyboard layout display.
    /// </summary>
    public enum KeyboardViewMode
    {
        Regular,        // Standard view
        FingerColors,   // Color keys based on finger used
        Heatmap         // Show heatmap based on monogram usage
    }

    /// <summary>
    /// A component that renders a keyboard layout by drawing physical keys.
    /// </summary>
    public class KeyboardLayoutView : UIElement
    {
        private Font font;
        private Layout? layout;
        private float pixelsPerU = 50.0f; // Scale factor: 50 pixels per 1U
        private float padding = 0.0f; // No padding around the keyboard view
        private PhysicalKey? selectedKey;
        private Dictionary<PhysicalKey, Rectangle> keyRectangles = new Dictionary<PhysicalKey, Rectangle>();
        private KeyboardViewMode viewMode = KeyboardViewMode.Regular;
        private bool showDisabledKeys = false; // Whether to show disabled keys (with outline) or hide them
        
        // Heatmap data (monogram counts per key)
        private Dictionary<string, long>? monogramCounts;
        private float cachedNormalizationBase = 1.0f; // Cached normalization base for heatmap
        
        // HSV color values for heatmap (defaults)
        private float lowH = 210f, lowS = 0.15f, lowV = 0.30f;
        private float midH = 200f, midS = 0.70f, midV = 0.60f;
        private float highH = 130f, highS = 0.60f, highV = 0.90f;
        
        // Drag and drop state
        private PhysicalKey? draggedKey;
        private PhysicalKey? dragTargetKey;
        private bool isDragging = false;
        private int dragStartMouseX = 0;
        private int dragStartMouseY = 0;
        private const int DRAG_THRESHOLD = 5; // Pixels to move before starting drag

        public Layout? Layout
        {
            get => layout;
            set
            {
                layout = value;
                keyRectangles.Clear();
            }
        }

        public float PixelsPerU
        {
            get => pixelsPerU;
            set
            {
                pixelsPerU = value;
                keyRectangles.Clear();
            }
        }

        public PhysicalKey? SelectedKey
        {
            get => selectedKey;
            set
            {
                selectedKey = value;
                OnSelectedKeyChanged?.Invoke(value);
            }
        }

        public Action<PhysicalKey?>? OnSelectedKeyChanged { get; set; }

        public KeyboardViewMode ViewMode
        {
            get => viewMode;
            set
            {
                viewMode = value;
                keyRectangles.Clear(); // Clear cache to redraw with new mode
            }
        }

        public bool ShowDisabledKeys
        {
            get => showDisabledKeys;
            set
            {
                showDisabledKeys = value;
                keyRectangles.Clear(); // Force redraw when show disabled setting changes
            }
        }

        /// <summary>
        /// Invalidates the cached rectangle for a specific key, forcing it to be recalculated on next access.
        /// </summary>
        public void InvalidateKeyCache(PhysicalKey key)
        {
            keyRectangles.Remove(key);
        }

        /// <summary>
        /// Sets monogram counts for heatmap view mode.
        /// Dictionary maps character strings to their usage counts.
        /// </summary>
        public void SetMonogramCounts(Dictionary<string, long>? counts)
        {
            monogramCounts = counts;
            UpdateNormalizationBase(); // Recalculate normalization base when counts change
            if (viewMode == KeyboardViewMode.Heatmap)
            {
                keyRectangles.Clear(); // Force redraw
            }
        }

        /// <summary>
        /// Sets HSV color values for the heatmap gradient.
        /// </summary>
        public void SetHeatmapColors(float lowH, float lowS, float lowV, 
                                     float midH, float midS, float midV, 
                                     float highH, float highS, float highV)
        {
            this.lowH = lowH;
            this.lowS = lowS;
            this.lowV = lowV;
            this.midH = midH;
            this.midS = midS;
            this.midV = midV;
            this.highH = highH;
            this.highS = highS;
            this.highV = highV;
            
            if (viewMode == KeyboardViewMode.Heatmap)
            {
                keyRectangles.Clear(); // Force redraw with new colors
            }
        }

        /// <summary>
        /// Updates the cached normalization base using 95th percentile of all monogram counts.
        /// This avoids recalculating for every key during rendering.
        /// </summary>
        private void UpdateNormalizationBase()
        {
            if (monogramCounts == null || monogramCounts.Count == 0)
            {
                cachedNormalizationBase = 1.0f;
                return;
            }

            // Collect all counts and find the 95th percentile
            var allCounts = new List<long>(monogramCounts.Values);
            allCounts.Sort();
            int percentileIndex = (int)(allCounts.Count * 0.95f);
            long percentile95 = allCounts[Math.Min(percentileIndex, allCounts.Count - 1)];
            
            // Cache the square root of the percentile for faster lookup
            cachedNormalizationBase = (float)Math.Sqrt(percentile95 > 0 ? percentile95 : 1);
        }

        public KeyboardLayoutView(Font font) : base("KeyboardLayoutView")
        {
            this.font = font;
            IsClickable = true;
            IsHoverable = true;
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
        }

        public override void ResolveBounds()
        {
            // Store old bounds BEFORE base.ResolveBounds() updates them via relative positioning
            float oldX = Bounds.X;
            float oldY = Bounds.Y;
            
            base.ResolveBounds();
            
            // Clear cached rectangles if position changed (parent bounds may have moved)
            if (oldX != Bounds.X || oldY != Bounds.Y)
            {
                keyRectangles.Clear();
            }
            
            if (layout != null)
            {
                // Calculate the bounding box of all keys
                float maxX = 0;
                float maxY = 0;

                foreach (var key in layout.GetPhysicalKeys())
                {
                    float keyRight = key.X + key.Width;
                    float keyBottom = key.Y + key.Height;
                    if (keyRight > maxX) maxX = keyRight;
                    if (keyBottom > maxY) maxY = keyBottom;
                }

                // Set bounds to fit all keys with padding (preserve position, only set width/height if needed)
                if (maxX > 0 && maxY > 0)
                {
                    float newWidth = maxX * pixelsPerU + padding * 2;
                    float newHeight = maxY * pixelsPerU + padding * 2;
                    
                    // Only update if width/height changed or if bounds were invalid
                    if (Bounds.Width != newWidth || Bounds.Height != newHeight || Bounds.Width <= 0 || Bounds.Height <= 0)
                    {
                        Bounds = new Rectangle(
                            Bounds.X,
                            Bounds.Y,
                            newWidth,
                            newHeight
                        );
                    }
                }
            }
        }

        public override void Update()
        {
            // Store old bounds to detect changes for cache invalidation
            float oldX = Bounds.X;
            float oldY = Bounds.Y;
            float oldWidth = Bounds.Width;
            float oldHeight = Bounds.Height;
            
            base.Update();
            
            // Clear cached rectangles if position or size changed during update
            if (oldX != Bounds.X || oldY != Bounds.Y || oldWidth != Bounds.Width || oldHeight != Bounds.Height)
            {
                keyRectangles.Clear();
            }

            // Handle mouse input for selection and drag-and-drop
            if (layout != null && IsVisible && IsEnabled && !IsAnyParentHidden())
            {
                int mouseX = Raylib.GetMouseX();
                int mouseY = Raylib.GetMouseY();

                // Check if mouse is over a key
                PhysicalKey? hoveredKey = null;
                foreach (var key in layout.GetPhysicalKeys())
                {
                    // Skip disabled keys if we're not showing them
                    if (key.Disabled && !showDisabledKeys)
                    {
                        continue;
                    }

                    Rectangle keyRect = GetKeyRectangle(key);
                    if (mouseX >= keyRect.X && mouseX <= keyRect.X + keyRect.Width &&
                        mouseY >= keyRect.Y && mouseY <= keyRect.Y + keyRect.Height)
                    {
                        hoveredKey = key;
                        break;
                    }
                }

                // Handle mouse button press (start drag or select)
                if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    if (hoveredKey != null)
                    {
                        draggedKey = hoveredKey;
                        dragStartMouseX = mouseX;
                        dragStartMouseY = mouseY;
                        isDragging = false;
                        SelectedKey = hoveredKey;
                    }
                }

                // Handle mouse button release (complete drag or just selection)
                if (Raylib.IsMouseButtonReleased(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    if (draggedKey != null && isDragging && hoveredKey != null && hoveredKey != draggedKey)
                    {
                        // Swap the keys
                        layout.SwapKeys(draggedKey, hoveredKey);
                        
                        // Clear cache to force redraw with new identifiers
                        keyRectangles.Clear();
                        
                        // Update selected key to the target (since identifiers were swapped)
                        SelectedKey = hoveredKey;
                    }
                    
                    // Reset drag state
                    draggedKey = null;
                    dragTargetKey = null;
                    isDragging = false;
                }

                // Handle mouse move (track dragging)
                if (draggedKey != null && Raylib.IsMouseButtonDown(MouseButton.MOUSE_BUTTON_LEFT))
                {
                    int dx = mouseX - dragStartMouseX;
                    int dy = mouseY - dragStartMouseY;
                    int distance = (int)Math.Sqrt(dx * dx + dy * dy);
                    
                    if (distance > DRAG_THRESHOLD)
                    {
                        isDragging = true;
                        dragTargetKey = hoveredKey;
                    }
                }
            }
        }

        private Rectangle GetKeyRectangle(PhysicalKey key)
        {
            if (!keyRectangles.TryGetValue(key, out Rectangle rect))
            {
                // Calculate position relative to keyboard view bounds (which are now relative to parent)
                float x = Bounds.X + padding + key.X * pixelsPerU;
                float y = Bounds.Y + padding + key.Y * pixelsPerU;
                float width = key.Width * pixelsPerU;
                float height = key.Height * pixelsPerU;
                rect = new Rectangle(x, y, width, height);
                keyRectangles[key] = rect;
            }
            return rect;
        }

        protected override void DrawSelf()
        {
            if (layout == null || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            // First pass: Draw unselected keys
            foreach (var key in layout.GetPhysicalKeys())
            {
                // Skip disabled keys if we're not showing them
                if (key.Disabled && !showDisabledKeys)
                {
                    continue;
                }

                // Skip selected key in first pass (will draw it in second pass)
                if (selectedKey == key)
                {
                    continue;
                }

                DrawKey(key);
            }

            // Second pass: Draw selected key on top
            if (selectedKey != null)
            {
                // Check if selected key should be drawn (not disabled or show disabled is enabled)
                if (!selectedKey.Disabled || showDisabledKeys)
                {
                    DrawKey(selectedKey);
                }
            }
        }

        private void DrawKey(PhysicalKey key)
        {
            Rectangle keyRect = GetKeyRectangle(key);
            float x = keyRect.X;
            float y = keyRect.Y;
            float width = keyRect.Width;
            float height = keyRect.Height;

            // Check if key is disabled
            bool isDisabled = key.Disabled;

            // Determine key color based on view mode (don't change for selection/drag)
            bool isSelected = selectedKey == key;
            bool isDragged = draggedKey == key;
            bool isDragTarget = dragTargetKey == key && isDragging;
            
            Color keyColor;
            
            // Apply view mode colors
            if (viewMode == KeyboardViewMode.FingerColors)
            {
                keyColor = GetFingerColor(key.Finger);
            }
            else if (viewMode == KeyboardViewMode.Heatmap)
            {
                keyColor = GetHeatmapColor(key);
            }
            else
            {
                // Regular view mode
                keyColor = UITheme.SidePanelColor;
            }

            // Apply opacity to disabled keys
            byte alpha = isDisabled ? (byte)77 : (byte)255; // 30% opacity for disabled keys
            
            // Draw key background (apply opacity for disabled keys)
            Color backgroundColor = new Color(keyColor.R, keyColor.G, keyColor.B, alpha);
            Raylib.DrawRectangleRec(keyRect, backgroundColor);
            
            // Draw key border - use different colors/thickness for selection and drag states
            Color borderColor = UITheme.BorderColor;
            float borderWidth = 1.0f;
            
            if (isDragged && isDragging)
            {
                // Dragged key: thicker blue border
                borderColor = new Color((byte)100, (byte)149, (byte)237, alpha); // Cornflower blue
                borderWidth = 3.0f;
            }
            else if (isDragTarget)
            {
                // Drop target: thicker green border
                borderColor = new Color((byte)144, (byte)238, (byte)144, alpha); // Light green
                borderWidth = 3.0f;
            }
            else if (isSelected)
            {
                // Selected key: thicker border with theme border color
                borderWidth = 3.0f;
            }
            
            // Apply opacity to border color
            borderColor = new Color(borderColor.R, borderColor.G, borderColor.B, alpha);
            Raylib.DrawRectangleLinesEx(keyRect, borderWidth, borderColor);

            // Draw key labels (primary and shift characters)
            if (layout != null)
            {
                var (primary, shift) = layout.GetCharactersForKey(key);
                
                // If we have character mappings, show them
                if (!string.IsNullOrEmpty(primary) || !string.IsNullOrEmpty(shift))
                {
                    int fontSize = 16; // Increased from 12 to 16 for primary characters
                    int smallFontSize = 10;
                    
                    // Draw primary character (bottom-center, like standard keyboards)
                    if (!string.IsNullOrEmpty(primary))
                    {
                        float primaryWidth = FontManager.MeasureText(font, primary, fontSize);
                        int primaryX = (int)(x + (width - primaryWidth) / 2);
                        int primaryY = (int)(y + height - fontSize - 4); // Bottom with padding
                        
                        Color textColor = new Color(UITheme.TextColor.R, UITheme.TextColor.G, UITheme.TextColor.B, alpha);
                        FontManager.DrawText(font, primary, primaryX, primaryY, fontSize, textColor);
                    }
                    
                    // Draw shift character (top-left, like standard keyboards)
                    if (!string.IsNullOrEmpty(shift))
                    {
                        float shiftWidth = FontManager.MeasureText(font, shift, smallFontSize);
                        int shiftX = (int)(x + 4); // Left edge with padding
                        int shiftY = (int)(y + 2); // Top with padding
                        
                        Color secondaryTextColor = new Color(UITheme.TextSecondaryColor.R, UITheme.TextSecondaryColor.G, UITheme.TextSecondaryColor.B, alpha);
                        FontManager.DrawText(font, shift, shiftX, shiftY, smallFontSize, secondaryTextColor);
                    }
                }
                // Fallback: show identifier for keys without character mappings (like Caps, Tab, Backspace, etc.)
                else if (!string.IsNullOrEmpty(key.Identifier))
                {
                    int fontSize = 12;
                    float textWidth = FontManager.MeasureText(font, key.Identifier, fontSize);
                    
                    // Center the text in the key
                    int textX = (int)(x + (width - textWidth) / 2);
                    int textY = (int)(y + (height - fontSize) / 2);
                    
                    Color textColor = new Color(UITheme.TextColor.R, UITheme.TextColor.G, UITheme.TextColor.B, alpha);
                    FontManager.DrawText(font, key.Identifier, textX, textY, fontSize, textColor);
                }
            }
            else if (!string.IsNullOrEmpty(key.Identifier))
            {
                // Fallback: draw identifier if layout not available
                int fontSize = 14;
                float textWidth = FontManager.MeasureText(font, key.Identifier, fontSize);
                
                // Center the text in the key
                int textX = (int)(x + (width - textWidth) / 2);
                int textY = (int)(y + (height - fontSize) / 2);
                
                Color textColor = new Color(UITheme.TextColor.R, UITheme.TextColor.G, UITheme.TextColor.B, alpha);
                FontManager.DrawText(font, key.Identifier, textX, textY, fontSize, textColor);
            }
        }

        private Color GetFingerColor(Core.Finger finger)
        {
            // Color scheme for different fingers
            switch (finger)
            {
                case Core.Finger.LeftPinky:
                    return new Color(200, 100, 100, 255); // Light red
                case Core.Finger.LeftRing:
                    return new Color(200, 150, 100, 255); // Light orange
                case Core.Finger.LeftMiddle:
                    return new Color(200, 200, 100, 255); // Light yellow
                case Core.Finger.LeftIndex:
                    return new Color(150, 200, 100, 255); // Light green
                case Core.Finger.LeftThumb:
                    return new Color(100, 200, 200, 255); // Light cyan
                case Core.Finger.RightThumb:
                    return new Color(100, 150, 200, 255); // Light blue
                case Core.Finger.RightIndex:
                    return new Color(150, 100, 200, 255); // Light purple
                case Core.Finger.RightMiddle:
                    return new Color(200, 100, 200, 255); // Light magenta
                case Core.Finger.RightRing:
                    return new Color(200, 100, 150, 255); // Light pink
                case Core.Finger.RightPinky:
                    return new Color(150, 150, 200, 255); // Light lavender
                default:
                    return UITheme.SidePanelColor;
            }
        }

        private Color GetHeatmapColor(PhysicalKey key)
        {
            if (monogramCounts == null || layout == null)
            {
                return UITheme.SidePanelColor;
            }

            // Get characters associated with this key
            var (primary, shift) = layout.GetCharactersForKey(key);
            
            // Sum up counts for primary and shift characters
            long totalCount = 0;
            if (!string.IsNullOrEmpty(primary) && monogramCounts.TryGetValue(primary, out long primaryCount))
            {
                totalCount += primaryCount;
            }
            if (!string.IsNullOrEmpty(shift) && monogramCounts.TryGetValue(shift, out long shiftCount))
            {
                totalCount += shiftCount;
            }

            // If no counts found, return default color
            if (totalCount == 0)
            {
                return UITheme.SidePanelColor;
            }

            // Calculate heatmap color (cool colors = low usage, warm colors = high usage)
            // Use percentile-based normalization (95th percentile) to avoid outliers dominating
            // Apply square root scaling for better visual distribution (more perceptually uniform)
            float scaledCount = (float)Math.Sqrt(totalCount);
            
            // Normalize to 0-1 range using cached normalization base
            // Clamp at 1.0 since some keys may exceed 95th percentile
            float normalized = Math.Min(1.0f, scaledCount / cachedNormalizationBase);

            // Interpolate HSV values, then convert to RGB
            float h, s, v;
            
            if (normalized < 0.5f)
            {
                // Interpolate between low and mid HSV values
                float t = normalized * 2.0f;
                h = lowH + (midH - lowH) * t;
                s = lowS + (midS - lowS) * t;
                v = lowV + (midV - lowV) * t;
            }
            else
            {
                // Interpolate between mid and high HSV values
                float t = (normalized - 0.5f) * 2.0f;
                h = midH + (highH - midH) * t;
                s = midS + (highS - midS) * t;
                v = midV + (highV - midV) * t;
            }
            
            // Convert HSV to RGB
            return HsvToRgb(h, s, v);
        }

        /// <summary>
        /// Converts HSV color values to RGB Color.
        /// H is in degrees (0-360), S and V are 0-1.
        /// </summary>
        private Color HsvToRgb(float h, float s, float v)
        {
            h = h % 360f;
            if (h < 0) h += 360f;
            
            float c = v * s;
            float x = c * (1 - Math.Abs((h / 60f) % 2 - 1));
            float m = v - c;
            
            float r = 0, g = 0, b = 0;
            
            if (h < 60)
            {
                r = c; g = x; b = 0;
            }
            else if (h < 120)
            {
                r = x; g = c; b = 0;
            }
            else if (h < 180)
            {
                r = 0; g = c; b = x;
            }
            else if (h < 240)
            {
                r = 0; g = x; b = c;
            }
            else if (h < 300)
            {
                r = x; g = 0; b = c;
            }
            else
            {
                r = c; g = 0; b = x;
            }
            
            return new Color(
                (byte)((r + m) * 255),
                (byte)((g + m) * 255),
                (byte)((b + m) * 255),
                (byte)255
            );
        }
    }
}

