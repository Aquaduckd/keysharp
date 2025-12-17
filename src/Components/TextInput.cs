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

        public TextInput(Font font, string placeholder = "", int fontSize = 14) : base("TextInput")
        {
            this.font = font;
            this.fontSize = fontSize;
            this.Placeholder = placeholder;
            this.Text = "";
            
            IsClickable = true;
            IsHoverable = true;
        }

        public void SetBounds(Rectangle bounds)
        {
            this.Bounds = bounds;
        }

        public override bool IsHovering(int mouseX, int mouseY)
        {
            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        public override void Update()
        {
            base.Update();

            if (!IsVisible || !IsEnabled || Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            int mouseX = Raylib.GetMouseX();
            int mouseY = Raylib.GetMouseY();

            // Handle focus on click
            if (Raylib.IsMouseButtonPressed(MouseButton.MOUSE_BUTTON_LEFT))
            {
                IsFocused = IsHovering(mouseX, mouseY);
            }

            // Handle text input when focused
            if (IsFocused)
            {
                int key = Raylib.GetCharPressed();
                while (key > 0)
                {
                    // Only accept printable characters
                    if (key >= 32 && key < 127)
                    {
                        Text += (char)key;
                        OnTextChanged?.Invoke(Text);
                    }
                    key = Raylib.GetCharPressed();
                }

                // Handle backspace
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_BACKSPACE) && Text.Length > 0)
                {
                    Text = Text.Substring(0, Text.Length - 1);
                    OnTextChanged?.Invoke(Text);
                }

                // Handle escape to unfocus
                if (Raylib.IsKeyPressed(KeyboardKey.KEY_ESCAPE))
                {
                    IsFocused = false;
                }
            }
        }

        protected override void DrawSelf()
        {
            if (Bounds.Width <= 0 || Bounds.Height <= 0)
                return;

            // Background
            Color bgColor = IsFocused ? UITheme.MainPanelColor : UITheme.SidePanelColor;
            Raylib.DrawRectangleRec(Bounds, bgColor);
            
            // Border
            Color borderColor = IsFocused ? UITheme.TextColor : UITheme.BorderColor;
            Raylib.DrawRectangleLinesEx(Bounds, 1, borderColor);

            // Text or placeholder
            string displayText = string.IsNullOrEmpty(Text) ? Placeholder : Text;
            Color textColor = string.IsNullOrEmpty(Text) ? UITheme.TextSecondaryColor : UITheme.TextColor;
            
            // Clip text if too long
            float maxWidth = Bounds.Width - Padding * 2;
            string clippedText = displayText;
            float textWidth = FontManager.MeasureText(font, displayText, fontSize);
            if (textWidth > maxWidth)
            {
                // Show end of text with ellipsis
                StringBuilder sb = new StringBuilder();
                sb.Append("...");
                for (int i = displayText.Length - 1; i >= 0; i--)
                {
                    string test = displayText.Substring(i) + "...";
                    if (FontManager.MeasureText(font, test, fontSize) <= maxWidth)
                    {
                        clippedText = test;
                        break;
                    }
                }
            }
            
            TextContainer.DrawLeftAlignedText(font, clippedText, Bounds, fontSize, textColor, Padding);
            
            // Draw cursor when focused
            if (IsFocused && (int)(Raylib.GetTime() * 2) % 2 == 0)
            {
                float cursorX = Bounds.X + Padding + (string.IsNullOrEmpty(Text) ? 0 : FontManager.MeasureText(font, Text, fontSize));
                Raylib.DrawLineEx(
                    new System.Numerics.Vector2(cursorX, Bounds.Y + Padding),
                    new System.Numerics.Vector2(cursorX, Bounds.Y + Bounds.Height - Padding),
                    1, UITheme.TextColor);
            }
        }
    }
}

