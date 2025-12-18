using Raylib_cs;
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
        private Components.Label? fingerValue;
        private Components.Label? placeholderLabel;
        private Keysharp.Core.PhysicalKey? selectedKey;

        public SidePanel(Font font) : base(font, "SidePanel")
        {
            // Create main container for key info
            keyInfoContainer = new Components.Container("KeyInfoContainer");
            keyInfoContainer.AutoLayoutChildren = true;
            keyInfoContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            keyInfoContainer.AutoSize = true;
            keyInfoContainer.ChildPadding = 15;
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

            // Create finger row
            var fingerRow = CreateInfoRow(font, "Finger:", "None");
            fingerLabel = fingerRow.label;
            fingerValue = fingerRow.value;
            keyInfoContainer.AddChild(fingerRow.container);

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

            if (fingerLabel != null && fingerValue != null)
            {
                fingerLabel.IsVisible = hasKey;
                fingerValue.IsVisible = hasKey;
                if (hasKey && selectedKey != null)
                {
                    fingerValue.SetText(GetFingerName(selectedKey.Finger));
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
                // Set container size (preserve position - it will be resolved by ResolveBounds)
                keyInfoContainer.SetSize(Bounds.Width, Bounds.Height);
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

                if (fingerLabel != null && fingerValue != null)
                {
                    fingerLabel.SetSize(labelWidth, 18);
                    fingerValue.SetSize(valueWidth, 18);
                }
            }
        }

        public override void Update()
        {
            base.Update();
            // No bounds setting here - that's done in PrepareResolveBounds()
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
    }
}

