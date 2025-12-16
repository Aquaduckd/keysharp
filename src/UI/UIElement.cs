using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;

namespace Keysharp.UI
{
    public enum PositionMode
    {
        Absolute,  // Positioned absolutely (default, current behavior)
        Relative   // Positioned relative to parent
    }

    public enum ChildJustification
    {
        Left,      // Children aligned to the left
        Center,    // Children centered
        Right      // Children aligned to the right
    }

    public enum LayoutDirection
    {
        Horizontal, // Children arranged horizontally (default)
        Vertical    // Children arranged vertically
    }

    public abstract class UIElement
    {
        public Rectangle Bounds { get; set; }
        public string Name { get; set; }
        public List<UIElement> Children { get; protected set; }
        public UIElement? Parent { get; set; }

        // Flags for element behavior
        public bool IsClickable { get; protected set; } = false;
        public bool IsHoverable { get; protected set; } = false;
        public bool IsVisible { get; protected set; } = true;
        public bool IsEnabled { get; protected set; } = true;

        // Positioning
        /// <summary>
        /// How this element is positioned relative to its parent.
        /// Absolute: Positioned using absolute coordinates (default, current behavior).
        /// Relative: Positioned relative to parent's position using RelativePosition.
        /// </summary>
        public PositionMode PositionMode { get; set; } = PositionMode.Absolute;
        
        /// <summary>
        /// Position relative to parent (only used when PositionMode is Relative).
        /// </summary>
        public Vector2 RelativePosition { get; set; } = Vector2.Zero;

        // Layout control (for parents)
        /// <summary>
        /// How children are justified when AutoLayoutChildren is true.
        /// </summary>
        public ChildJustification ChildJustification { get; set; } = ChildJustification.Left;
        
        /// <summary>
        /// Direction children are laid out (horizontal or vertical).
        /// </summary>
        public LayoutDirection LayoutDirection { get; set; } = LayoutDirection.Horizontal;
        
        /// <summary>
        /// Gap between children when AutoLayoutChildren is true.
        /// </summary>
        public float ChildGap { get; set; } = 0;
        
        /// <summary>
        /// Padding around children when AutoLayoutChildren is true.
        /// </summary>
        public float ChildPadding { get; set; } = 0;
        
        /// <summary>
        /// Whether to automatically layout children based on ChildJustification, LayoutDirection, ChildGap, and ChildPadding.
        /// When true, children are positioned automatically regardless of their PositionMode.
        /// When false, children use their own PositionMode to determine positioning.
        /// </summary>
        public bool AutoLayoutChildren { get; set; } = false;

        protected UIElement(string name)
        {
            Name = name;
            Children = new List<UIElement>();
            Bounds = new Rectangle(0, 0, 0, 0);
        }

        public virtual void Update()
        {
            // Only update if enabled
            if (!IsEnabled)
                return;

            // Update own position if relative to parent
            if (PositionMode == PositionMode.Relative && Parent != null)
            {
                Bounds = new Rectangle(
                    Parent.Bounds.X + RelativePosition.X,
                    Parent.Bounds.Y + RelativePosition.Y,
                    Bounds.Width,
                    Bounds.Height
                );
            }

            // Layout children if auto-layout is enabled
            if (AutoLayoutChildren)
            {
                LayoutChildren();
            }

            // Update all children
            foreach (var child in Children)
            {
                child.Update();
            }
        }

        protected virtual void LayoutChildren()
        {
            if (Children.Count == 0)
                return;

            // Filter visible children
            var visibleChildren = new List<UIElement>();
            foreach (var child in Children)
            {
                if (child.IsVisible)
                {
                    visibleChildren.Add(child);
                }
            }

            if (visibleChildren.Count == 0)
                return;

            if (LayoutDirection == LayoutDirection.Horizontal)
            {
                LayoutChildrenHorizontal(visibleChildren);
            }
            else
            {
                LayoutChildrenVertical(visibleChildren);
            }
        }

        private void LayoutChildrenHorizontal(List<UIElement> visibleChildren)
        {
            float currentX = ChildPadding;
            float currentY = ChildPadding;
            float maxHeight = 0;

            // Calculate total width of all visible children
            float totalWidth = 0;
            foreach (var child in visibleChildren)
            {
                totalWidth += child.Bounds.Width;
                if (visibleChildren.IndexOf(child) < visibleChildren.Count - 1)
                {
                    totalWidth += ChildGap;
                }
                if (child.Bounds.Height > maxHeight)
                {
                    maxHeight = child.Bounds.Height;
                }
            }
            totalWidth += ChildPadding * 2; // Account for padding on both sides

            // Determine starting X based on justification
            switch (ChildJustification)
            {
                case ChildJustification.Left:
                    currentX = ChildPadding;
                    break;
                case ChildJustification.Center:
                    currentX = (Bounds.Width - totalWidth + ChildPadding * 2) / 2;
                    break;
                case ChildJustification.Right:
                    currentX = Bounds.Width - totalWidth + ChildPadding;
                    break;
            }

            // Position children horizontally
            foreach (var child in visibleChildren)
            {
                if (child.PositionMode == PositionMode.Relative)
                {
                    // Update relative position
                    child.RelativePosition = new Vector2(currentX, currentY);
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        child.Bounds.Width,
                        child.Bounds.Height
                    );
                }
                else
                {
                    // Update position relative to parent
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        child.Bounds.Width,
                        child.Bounds.Height
                    );
                }

                currentX += child.Bounds.Width + ChildGap;
            }
        }

        private void LayoutChildrenVertical(List<UIElement> visibleChildren)
        {
            float currentX = ChildPadding;
            float currentY = ChildPadding;
            float maxWidth = 0;

            // Calculate total height of all visible children
            float totalHeight = 0;
            foreach (var child in visibleChildren)
            {
                totalHeight += child.Bounds.Height;
                if (visibleChildren.IndexOf(child) < visibleChildren.Count - 1)
                {
                    totalHeight += ChildGap;
                }
                if (child.Bounds.Width > maxWidth)
                {
                    maxWidth = child.Bounds.Width;
                }
            }
            totalHeight += ChildPadding * 2; // Account for padding on both sides

            // Determine starting Y based on justification (for vertical, we use Y axis)
            // For vertical layout, justification affects horizontal alignment
            switch (ChildJustification)
            {
                case ChildJustification.Left:
                    currentX = ChildPadding;
                    break;
                case ChildJustification.Center:
                    currentX = (Bounds.Width - maxWidth) / 2;
                    break;
                case ChildJustification.Right:
                    currentX = Bounds.Width - maxWidth - ChildPadding;
                    break;
            }

            // Position children vertically
            foreach (var child in visibleChildren)
            {
                if (child.PositionMode == PositionMode.Relative)
                {
                    // Update relative position
                    child.RelativePosition = new Vector2(currentX, currentY);
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        child.Bounds.Width,
                        child.Bounds.Height
                    );
                }
                else
                {
                    // Update position relative to parent
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        child.Bounds.Width,
                        child.Bounds.Height
                    );
                }

                currentY += child.Bounds.Height + ChildGap;
            }
        }

        public virtual void Draw()
        {
            // Only draw if visible
            if (!IsVisible)
                return;

            // Draw all children
            foreach (var child in Children)
            {
                child.Draw();
            }
        }

        public virtual bool IsHovering(int mouseX, int mouseY)
        {
            // Only check hovering if hoverable and enabled
            if (!IsHoverable || !IsEnabled)
                return false;

            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }

        public void AddChild(UIElement child)
        {
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(UIElement child)
        {
            child.Parent = null;
            Children.Remove(child);
        }

        // Recursively get all descendants for debug overlay
        public IEnumerable<UIElement> GetAllDescendants()
        {
            yield return this;
            foreach (var child in Children)
            {
                foreach (var descendant in child.GetAllDescendants())
                {
                    yield return descendant;
                }
            }
        }
    }
}

