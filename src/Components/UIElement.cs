using Raylib_cs;
using System.Collections.Generic;
using System.Numerics;

namespace Keysharp.Components
{
    public enum PositionMode
    {
        Absolute,  // Positioned absolutely (default, current behavior)
        Relative   // Positioned relative to parent
    }

    public enum ChildJustification
    {
        Left,         // Children aligned to the left
        Center,       // Children centered
        Right,        // Children aligned to the right
        SpaceBetween  // Children spread out with space between first and last
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
        public bool IsClickable { get; set; } = false;
        public bool IsHoverable { get; set; } = false;
        public bool IsVisible { get; set; } = true;
        public bool IsEnabled { get; set; } = true;

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
        
        /// <summary>
        /// Whether to automatically resize this element to fit its children plus padding.
        /// When true, the element's bounds will be updated to accommodate all children and padding.
        /// Only works when AutoLayoutChildren is true.
        /// </summary>
        public bool AutoSize { get; set; } = false;

        /// <summary>
        /// Target height for this element. When set and AutoLayoutChildren is true with vertical layout,
        /// children with FillRemaining will be sized to fill remaining space to reach this target.
        /// </summary>
        public float? TargetHeight { get; set; } = null;

        /// <summary>
        /// When true and this element is a child in a vertical auto-layout container,
        /// it will automatically fill the remaining vertical space after other children and padding.
        /// Only works when parent has TargetHeight set.
        /// </summary>
        public bool FillRemaining { get; set; } = false;

        protected UIElement(string name)
        {
            Name = name;
            Children = new List<UIElement>();
            Bounds = new Rectangle(0, 0, 0, 0);
        }

        public virtual void Update()
        {
            // Only update if visible and enabled
            if (!IsVisible || !IsEnabled)
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

            // Layout children if auto-layout is enabled (this positions children correctly)
            if (AutoLayoutChildren)
            {
                LayoutChildren();
            }

            // Update all children (after layout so their bounds are correct for input detection)
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
            
            // Auto-resize if enabled
            if (AutoSize)
            {
                ResizeToFitChildren(visibleChildren);
            }
        }
        
        private void ResizeToFitChildren(List<UIElement> visibleChildren)
        {
            if (visibleChildren.Count == 0)
                return;
            
            float oldWidth = Bounds.Width;
            float oldHeight = Bounds.Height;
            float oldX = Bounds.X;
            float oldY = Bounds.Y;
                
            if (LayoutDirection == LayoutDirection.Horizontal)
            {
                // Calculate required width
                float totalChildrenWidth = 0;
                float maxHeight = 0;
                
                foreach (var child in visibleChildren)
                {
                    totalChildrenWidth += child.Bounds.Width;
                    if (child.Bounds.Height > maxHeight)
                    {
                        maxHeight = child.Bounds.Height;
                    }
                }
                
                // Add gaps between children
                float totalGap = 0;
                if (visibleChildren.Count > 1)
                {
                    if (ChildJustification == ChildJustification.SpaceBetween)
                    {
                        // For SpaceBetween, don't auto-size width - it needs to be set externally
                        // Only auto-size height
                        float height = maxHeight + ChildPadding * 2;
                        Bounds = new Rectangle(
                            Bounds.X,
                            Bounds.Y,
                            Bounds.Width, // Keep existing width
                            height
                        );
                        // If height changed, notify parent
                        if (height != oldHeight && Parent != null && Parent.AutoSize)
                        {
                            Parent.LayoutChildren();
                        }
                        return;
                    }
                    else
                    {
                        totalGap = ChildGap * (visibleChildren.Count - 1);
                    }
                }
                
                // Add padding
                float requiredWidth = totalChildrenWidth + totalGap + ChildPadding * 2;
                float requiredHeight = maxHeight + ChildPadding * 2;
                
                // Update bounds (preserve position)
                Bounds = new Rectangle(
                    Bounds.X,
                    Bounds.Y,
                    requiredWidth,
                    requiredHeight
                );
            }
            else // Vertical
            {
                // Calculate required height
                float totalChildrenHeight = 0;
                float maxWidth = 0;
                
                foreach (var child in visibleChildren)
                {
                    totalChildrenHeight += child.Bounds.Height;
                    if (child.Bounds.Width > maxWidth)
                    {
                        maxWidth = child.Bounds.Width;
                    }
                }
                
                // Add gaps between children
                float totalGap = 0;
                if (visibleChildren.Count > 1)
                {
                    totalGap = ChildGap * (visibleChildren.Count - 1);
                }
                
                // Add padding
                float requiredWidth = maxWidth + ChildPadding * 2;
                float requiredHeight = totalChildrenHeight + totalGap + ChildPadding * 2;
                
                // Update bounds (preserve position)
                // If using relative positioning, preserve the relative position calculation
                float newX = Bounds.X;
                float newY = Bounds.Y;
                
                // If position is relative, recalculate absolute position after size change
                if (PositionMode == PositionMode.Relative && Parent != null)
                {
                    newX = Parent.Bounds.X + RelativePosition.X;
                    newY = Parent.Bounds.Y + RelativePosition.Y;
                }
                
                Bounds = new Rectangle(
                    newX,
                    newY,
                    requiredWidth,
                    requiredHeight
                );
            }
            
            // If size or position changed, update children's relative positions if needed
            if ((Bounds.Width != oldWidth || Bounds.Height != oldHeight || Bounds.X != oldX || Bounds.Y != oldY))
            {
                // Update children that use relative positioning to recalculate their absolute positions
                foreach (var child in Children)
                {
                    if (child.PositionMode == PositionMode.Relative && child.IsVisible)
                    {
                        child.Bounds = new Rectangle(
                            Bounds.X + child.RelativePosition.X,
                            Bounds.Y + child.RelativePosition.Y,
                            child.Bounds.Width,
                            child.Bounds.Height
                        );
                    }
                }
                
                // Notify parent to resize if it has AutoSize enabled
                if (Parent != null && 
                    Parent.AutoSize && 
                    Parent.AutoLayoutChildren)
                {
                    // Trigger parent's layout which will resize it if needed
                    Parent.LayoutChildren();
                }
            }
        }

        private void LayoutChildrenHorizontal(List<UIElement> visibleChildren)
        {
            float currentX = ChildPadding;
            float currentY = ChildPadding;
            float maxHeight = 0;

            // Calculate total width of all visible children (without gaps)
            float totalChildrenWidth = 0;
            foreach (var child in visibleChildren)
            {
                totalChildrenWidth += child.Bounds.Width;
                if (child.Bounds.Height > maxHeight)
                {
                    maxHeight = child.Bounds.Height;
                }
            }

            // Calculate gap between children
            float gapBetween = ChildGap;
            if (ChildJustification == ChildJustification.SpaceBetween)
            {
                if (visibleChildren.Count <= 1)
                {
                    gapBetween = 0;
                }
                else
                {
                    // Calculate available space for gaps
                    float availableSpace = Bounds.Width - ChildPadding * 2 - totalChildrenWidth;
                    // Distribute space evenly between children
                    gapBetween = availableSpace / (visibleChildren.Count - 1);
                }
            }

            // Calculate total width including gaps
            float totalWidth = totalChildrenWidth;
            if (visibleChildren.Count > 1)
            {
                totalWidth += gapBetween * (visibleChildren.Count - 1);
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
                case ChildJustification.SpaceBetween:
                    // First child starts at padding
                    currentX = ChildPadding;
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

                currentX += child.Bounds.Width + gapBetween;
            }
        }

        private float CalculateExpectedAutoSizedHeight(UIElement child)
        {
            if (!child.AutoSize || !child.AutoLayoutChildren || child.Children.Count == 0)
            {
                return child.Bounds.Height;
            }

            // Filter visible children
            var visibleGrandchildren = new List<UIElement>();
            foreach (var grandchild in child.Children)
            {
                if (grandchild.IsVisible)
                {
                    visibleGrandchildren.Add(grandchild);
                }
            }

            if (visibleGrandchildren.Count == 0)
            {
                return child.Bounds.Height;
            }

            if (child.LayoutDirection == LayoutDirection.Vertical)
            {
                // For vertical layout: sum of child heights + gaps + padding
                float totalHeight = 0;
                foreach (var grandchild in visibleGrandchildren)
                {
                    float grandchildHeight = grandchild.Bounds.Height;
                    // Recursively calculate if grandchild also has AutoSize
                    if (grandchild.AutoSize && grandchild.AutoLayoutChildren && grandchild.Children.Count > 0)
                    {
                        grandchildHeight = CalculateExpectedAutoSizedHeight(grandchild);
                    }
                    totalHeight += grandchildHeight;
                }
                if (visibleGrandchildren.Count > 1)
                {
                    totalHeight += child.ChildGap * (visibleGrandchildren.Count - 1);
                }
                return totalHeight + (child.ChildPadding * 2);
            }
            else
            {
                // For horizontal layout: max child height + padding
                float maxHeight = 0;
                foreach (var grandchild in visibleGrandchildren)
                {
                    float grandchildHeight = grandchild.Bounds.Height;
                    // Recursively calculate if grandchild also has AutoSize
                    if (grandchild.AutoSize && grandchild.AutoLayoutChildren && grandchild.Children.Count > 0)
                    {
                        grandchildHeight = CalculateExpectedAutoSizedHeight(grandchild);
                    }
                    if (grandchildHeight > maxHeight)
                    {
                        maxHeight = grandchildHeight;
                    }
                }
                return maxHeight + (child.ChildPadding * 2);
            }
        }

        private void LayoutChildrenVertical(List<UIElement> visibleChildren)
        {
            float currentX = ChildPadding;
            float currentY = ChildPadding;
            float maxWidth = 0;

            // Determine target height (use TargetHeight if set, otherwise current bounds height)
            float targetHeight = TargetHeight ?? Bounds.Height;

            // Separate children into those that fill remaining and those that don't
            var fillRemainingChildren = new List<UIElement>();
            var fixedChildren = new List<UIElement>();
            foreach (var child in visibleChildren)
            {
                if (child.FillRemaining && TargetHeight.HasValue)
                {
                    fillRemainingChildren.Add(child);
                }
                else
                {
                    fixedChildren.Add(child);
                }
            }

            // Calculate space used by fixed children
            // For children with AutoSize, calculate their expected auto-sized height
            float fixedChildrenHeight = 0;
            var fixedChildrenExpectedHeights = new Dictionary<UIElement, float>();
            foreach (var child in fixedChildren)
            {
                float childHeight = child.Bounds.Height;
                
                // If child has AutoSize enabled, calculate expected auto-sized height
                if (child.AutoSize && child.AutoLayoutChildren && child.Children.Count > 0)
                {
                    childHeight = CalculateExpectedAutoSizedHeight(child);
                    fixedChildrenExpectedHeights[child] = childHeight;
                }
                else
                {
                    fixedChildrenExpectedHeights[child] = childHeight;
                }
                
                fixedChildrenHeight += childHeight;
            }
            // Add gaps between fixed children
            if (fixedChildren.Count > 1)
            {
                fixedChildrenHeight += ChildGap * (fixedChildren.Count - 1);
            }
            // Add gaps between fixed children and fill-remaining children
            if (fixedChildren.Count > 0 && fillRemainingChildren.Count > 0)
            {
                fixedChildrenHeight += ChildGap;
            }
            // Add gaps between multiple fill-remaining children
            if (fillRemainingChildren.Count > 1)
            {
                fixedChildrenHeight += ChildGap * (fillRemainingChildren.Count - 1);
            }

            // Calculate remaining space for fill-remaining children
            float availableSpace = targetHeight - ChildPadding * 2 - fixedChildrenHeight;
            float fillRemainingHeight = 0;
            if (fillRemainingChildren.Count > 0 && availableSpace > 0)
            {
                fillRemainingHeight = availableSpace / fillRemainingChildren.Count;
            }

            // Set heights for fill-remaining children
            foreach (var child in fillRemainingChildren)
            {
                child.Bounds = new Rectangle(
                    child.Bounds.X,
                    child.Bounds.Y,
                    child.Bounds.Width,
                    fillRemainingHeight
                );
            }

            // Calculate total height of all visible children (now that fill-remaining have been sized)
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
            // For vertical layouts, children should get the full available width (minus padding) if their width is 0 or very small
            float availableChildWidth = Bounds.Width - (ChildPadding * 2);
            
            foreach (var child in visibleChildren)
            {
                // If child has zero or very small width, give it the full available width
                float childWidth = child.Bounds.Width;
                if (childWidth <= 1)
                {
                    childWidth = availableChildWidth;
                }
                
                if (child.PositionMode == PositionMode.Relative)
                {
                    // Update relative position
                    child.RelativePosition = new Vector2(currentX, currentY);
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        childWidth,
                        child.Bounds.Height
                    );
                }
                else
                {
                    // Update position relative to parent
                    child.Bounds = new Rectangle(
                        Bounds.X + currentX,
                        Bounds.Y + currentY,
                        childWidth,
                        child.Bounds.Height
                    );
                }

                currentY += child.Bounds.Height + ChildGap;
            }
        }

        public virtual void Draw()
        {
            // Only draw if visible - this check happens at the base level
            // so derived classes don't need to check IsVisible themselves
            if (!IsVisible)
                return;

            // Derived classes should override this method and call base.Draw() at the end
            // to draw children. The base implementation only draws children.
            DrawSelf();
            
            // Draw all children
            foreach (var child in Children)
            {
                child.Draw();
            }
        }
        
        /// <summary>
        /// Override this method to draw the element itself (not children).
        /// This method is only called when IsVisible is true.
        /// </summary>
        protected virtual void DrawSelf()
        {
            // Default implementation does nothing - derived classes override to draw themselves
        }

        public virtual bool IsHovering(int mouseX, int mouseY)
        {
            // Only check hovering if visible, hoverable, and enabled
            // Also check if any parent is hidden
            if (!IsVisible || !IsHoverable || !IsEnabled || IsAnyParentHidden())
                return false;

            return mouseX >= Bounds.X && mouseX <= Bounds.X + Bounds.Width &&
                   mouseY >= Bounds.Y && mouseY <= Bounds.Y + Bounds.Height;
        }
        
        /// <summary>
        /// Checks if any parent in the hierarchy is hidden.
        /// </summary>
        protected bool IsAnyParentHidden()
        {
            UIElement? current = Parent;
            while (current != null)
            {
                if (!current.IsVisible)
                    return true;
                current = current.Parent;
            }
            return false;
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

