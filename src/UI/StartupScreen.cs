using Raylib_cs;

namespace Keysharp.UI
{
    public class StartupScreen
    {
        private Font font;
        private float logoScale = 1.0f;
        private float logoPulse = 0.0f;
        private float loadingProgress = 0.0f;
        private string currentLoadingText = "";
        
        private readonly string[] loadingMessages = new[]
        {
            "Compiling Corpora...",
            "Loading fonts...",
            "Setting up layout...",
            "Preparing interface...",
            "Almost ready..."
        };

        public StartupScreen(Font font)
        {
            this.font = font;
        }

        public void Update(float progress)
        {
            loadingProgress = progress;
            
            // Update loading text based on progress
            int messageIndex = (int)(progress * loadingMessages.Length);
            messageIndex = System.Math.Clamp(messageIndex, 0, loadingMessages.Length - 1);
            currentLoadingText = loadingMessages[messageIndex];
            
            // Animate the logo with a subtle pulse
            logoPulse += 0.02f;
            if (logoPulse > 6.28f) // 2 * PI
            {
                logoPulse = 0.0f;
            }
            
            // Subtle scale pulse: 1.0 to 1.05
            logoScale = 1.0f + (float)(System.Math.Sin(logoPulse) * 0.05f);
        }

        public void Draw(int screenWidth, int screenHeight)
        {
            // Draw background
            Raylib.ClearBackground(UITheme.BackgroundColor);

            // Calculate center position
            float centerX = screenWidth / 2.0f;
            float centerY = screenHeight / 2.0f;

            // Draw app name/logo
            // Use a fixed font size (no scaling) to avoid blurriness
            // Measure with base size for positioning
            string appName = "Keysharp";
            float baseFontSize = 64.0f;
            float textWidth = Raylib.MeasureTextEx(font, appName, baseFontSize, 0).X;
            float textHeight = baseFontSize;

            // Draw text with subtle position animation (but no size scaling to avoid blur)
            Color textColor = UITheme.TextColor;
            // Apply subtle position offset based on scale for animation effect without scaling text
            float positionOffset = (logoScale - 1.0f) * 2.0f; // Small position movement
            Raylib.DrawTextEx(font, appName, 
                new System.Numerics.Vector2(
                    centerX - textWidth / 2.0f + positionOffset, 
                    centerY - textHeight / 2.0f - 30 + positionOffset),
                baseFontSize, 0, textColor);

            // Draw subtitle
            string subtitle = "Keyboard Layout Analyzer";
            float subtitleSize = 20.0f;
            float subtitleWidth = Raylib.MeasureTextEx(font, subtitle, subtitleSize, 0).X;
            Raylib.DrawTextEx(font, subtitle,
                new System.Numerics.Vector2(centerX - subtitleWidth / 2.0f, centerY + 50),
                subtitleSize, 0, UITheme.TextSecondaryColor);

            // Draw loading text
            float loadingTextSize = 16.0f;
            float loadingTextWidth = Raylib.MeasureTextEx(font, currentLoadingText, loadingTextSize, 0).X;
            float loadingTextY = centerY + 100;
            Raylib.DrawTextEx(font, currentLoadingText,
                new System.Numerics.Vector2(centerX - loadingTextWidth / 2.0f, loadingTextY - 25),
                loadingTextSize, 0, UITheme.TextSecondaryColor);

            // Draw loading bar
            float barWidth = 300.0f;
            float barHeight = 6.0f;
            float barX = centerX - barWidth / 2.0f;
            float barY = loadingTextY;

            // Draw background bar
            Color barBgColor = new Color(63, 63, 70, 255); // Dark gray background
            Raylib.DrawRectangleRounded(
                new Rectangle(barX, barY, barWidth, barHeight),
                0.5f, // Roundness
                8, // Segments
                barBgColor);

            // Draw progress bar
            float progressWidth = barWidth * System.Math.Clamp(loadingProgress, 0.0f, 1.0f);
            if (progressWidth > 0)
            {
                Color barFillColor = UITheme.TextColor; // White/light fill
                Raylib.DrawRectangleRounded(
                    new Rectangle(barX, barY, progressWidth, barHeight),
                    0.5f, // Roundness
                    8, // Segments
                    barFillColor);
            }
        }
    }
}

