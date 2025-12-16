using Raylib_cs;
using System.Collections.Generic;

namespace Keysharp.UI
{
    public abstract class UIElement
    {
        public Rectangle Bounds { get; set; }
        public string Name { get; set; }
        public List<UIElement> Children { get; protected set; }
        public UIElement? Parent { get; set; }

        protected UIElement(string name)
        {
            Name = name;
            Children = new List<UIElement>();
            Bounds = new Rectangle(0, 0, 0, 0);
        }

        public virtual void Update()
        {
            // Update all children
            foreach (var child in Children)
            {
                child.Update();
            }
        }

        public virtual void Draw()
        {
            // Draw all children
            foreach (var child in Children)
            {
                child.Draw();
            }
        }

        public virtual bool IsHovering(int mouseX, int mouseY)
        {
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

