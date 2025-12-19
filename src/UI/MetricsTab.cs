using Raylib_cs;
using Keysharp.Components;

namespace Keysharp.UI
{
    public class MetricsTab
    {
        private Components.TabContent tabContent;
        private Font font;

        // Metrics tab containers
        private Components.Container? metricsMainContainer;
        private Components.Container? metricsHeaderContainer;
        private Components.Container? metricsControlsContainer;
        private Components.Container? metricsContentContainer;
        private Components.Label? metricsHeaderLabel;

        public Components.TabContent TabContent => tabContent;

        public MetricsTab(Font font)
        {
            this.font = font;
            tabContent = new Components.TabContent(font, "", null); // Empty title - we'll use our own header
            tabContent.PositionMode = Components.PositionMode.Relative;

            InitializeUI();
        }

        private void InitializeUI()
        {
            // Create main vertical container that wraps header, controls, and content
            metricsMainContainer = new Components.Container("MetricsMain");
            metricsMainContainer.AutoLayoutChildren = true;
            metricsMainContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            metricsMainContainer.AutoSize = false; // Size will be set explicitly in Update method
            metricsMainContainer.PositionMode = Components.PositionMode.Relative;
            metricsMainContainer.ChildPadding = 20;
            metricsMainContainer.ChildGap = 10;
            tabContent.AddChild(metricsMainContainer);

            // Create header container for metrics tab (will display centered text)
            metricsHeaderContainer = new Components.Container("MetricsHeader");
            metricsHeaderContainer.AutoSize = false; // Size will be set explicitly in Update
            metricsHeaderContainer.PositionMode = Components.PositionMode.Relative;
            metricsHeaderContainer.ChildPadding = 0; // No padding needed since label fills it
            metricsMainContainer.AddChild(metricsHeaderContainer);

            // Create header label with centered text
            metricsHeaderLabel = new Components.Label(font, "Metrics", 24);
            metricsHeaderLabel.Bounds = new Rectangle(0, 0, 0, 40); // Height for header text
            metricsHeaderContainer.AddChild(metricsHeaderLabel);

            // Create container for metrics controls bar
            metricsControlsContainer = new Components.Container("MetricsControls");
            metricsControlsContainer.AutoLayoutChildren = true;
            metricsControlsContainer.LayoutDirection = Components.LayoutDirection.Horizontal;
            metricsControlsContainer.AutoSize = true; // Auto-size based on children + padding
            metricsControlsContainer.ChildJustification = Components.ChildJustification.Left;
            metricsControlsContainer.ChildGap = 10;
            metricsControlsContainer.ChildPadding = 0;
            metricsMainContainer.AddChild(metricsControlsContainer);

            // Create content container for the rest of the metrics tab content
            metricsContentContainer = new Components.Container("MetricsContent");
            metricsContentContainer.AutoSize = false; // Size will be set explicitly in Update
            metricsContentContainer.PositionMode = Components.PositionMode.Relative;
            metricsContentContainer.AutoLayoutChildren = false; // Will be set up as needed
            metricsContentContainer.LayoutDirection = Components.LayoutDirection.Vertical;
            metricsContentContainer.ChildGap = 10;
            metricsContentContainer.ChildPadding = 0;
            metricsContentContainer.FillRemaining = true; // Fill remaining space in parent container
            metricsMainContainer.AddChild(metricsContentContainer);
        }

        public void Update(Rectangle contentArea, bool isActive)
        {
            tabContent.Bounds = new Rectangle(0, 0, contentArea.Width, contentArea.Height);
            tabContent.RelativePosition = new System.Numerics.Vector2(0, 0);

            if (isActive)
            {
                // Update metrics containers if metrics tab is active
                const int headerHeight = 40; // Height for header text

                // Set main container bounds (fills parent, uses auto-layout)
                if (metricsMainContainer != null)
                {
                    metricsMainContainer.Bounds = new Rectangle(
                        0, 0,
                        (int)contentArea.Width,
                        (int)contentArea.Height
                    );
                    // Set target height so fill-remaining children can calculate their size
                    metricsMainContainer.TargetHeight = contentArea.Height;
                    metricsMainContainer.IsVisible = true;
                }

                // Calculate available width accounting for parent padding
                int availableWidth = (int)contentArea.Width;
                if (metricsMainContainer != null)
                {
                    availableWidth = (int)contentArea.Width - (int)(metricsMainContainer.ChildPadding * 2);
                }

                // Set header container bounds (width accounts for parent padding, fixed height for label)
                // Position will be handled by auto-layout
                if (metricsHeaderContainer != null)
                {
                    metricsHeaderContainer.Bounds = new Rectangle(0, 0, availableWidth, headerHeight);
                    metricsHeaderContainer.IsVisible = true;
                }

                // Update header label bounds (fills header container)
                if (metricsHeaderLabel != null)
                {
                    metricsHeaderLabel.Bounds = new Rectangle(
                        0, 0,
                        availableWidth,
                        headerHeight
                    );
                    metricsHeaderLabel.IsVisible = true;
                }

                // Set controls container bounds (width accounts for parent padding, fixed height)
                if (metricsControlsContainer != null)
                {
                    metricsControlsContainer.Bounds = new Rectangle(0, 0, availableWidth, 35);
                    metricsControlsContainer.IsVisible = true;
                }

                // Content container will fill remaining space via FillRemaining
                if (metricsContentContainer != null)
                {
                    metricsContentContainer.Bounds = new Rectangle(0, 0, availableWidth, 0); // Height calculated by FillRemaining
                    metricsContentContainer.IsVisible = true;
                }
            }
            else
            {
                // Hide containers when tab is not active
                if (metricsMainContainer != null)
                    metricsMainContainer.IsVisible = false;
            }
        }

        public void SetVisible(bool visible)
        {
            tabContent.IsVisible = visible;
        }

        public void UpdateFont(Font newFont)
        {
            font = newFont;
            // Update fonts in components if needed
        }
    }
}

