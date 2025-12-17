using Raylib_cs;
using Keysharp.Components;
using Keysharp.Core;

namespace Keysharp.UI
{
    public class SidePanel : Panel
    {
        private Components.Label? keyInfoLabel;
        private Keysharp.Core.PhysicalKey? selectedKey;

        public SidePanel(Font font) : base(font, "SidePanel")
        {
            keyInfoLabel = new Components.Label(font, "Click a key to view its information", 12);
            keyInfoLabel.AutoSize = false; // We'll set bounds manually
            keyInfoLabel.PositionMode = Components.PositionMode.Absolute;
            keyInfoLabel.Bounds = new Rectangle(0, 0, 200, 100); // Initial bounds
            AddChild(keyInfoLabel);
        }

        public void SetSelectedKey(Keysharp.Core.PhysicalKey? key)
        {
            selectedKey = key;
            UpdateKeyInfo();
        }

        private void UpdateKeyInfo()
        {
            if (keyInfoLabel == null) return;

            if (selectedKey != null)
            {
                string info = $"Key Information\n\n" +
                             $"Identifier: {selectedKey.Identifier ?? "None"}\n" +
                             $"Position: ({selectedKey.X:F2}U, {selectedKey.Y:F2}U)\n" +
                             $"Size: {selectedKey.Width:F2}U × {selectedKey.Height:F2}U\n" +
                             $"Finger: {GetFingerName(selectedKey.Finger)}";
                keyInfoLabel.SetText(info);
            }
            else
            {
                keyInfoLabel.SetText("Click a key to view its information");
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

        public override void Update()
        {
            base.Update();

            // Update key info label position and size
            if (keyInfoLabel != null && Bounds.Width > 0 && Bounds.Height > 0)
            {
                keyInfoLabel.Bounds = new Rectangle(
                    Bounds.X + 15,
                    Bounds.Y + 15,
                    Bounds.Width - 30,
                    Bounds.Height - 30 // Use available height minus padding
                );
            }
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

