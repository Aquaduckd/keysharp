using Raylib_cs;
using System.Collections.Generic;

namespace Keysharp
{
    /// <summary>
    /// Manages nested scissor modes by maintaining a stack and calculating intersections.
    /// Raylib's scissor modes don't support nesting properly, so we handle it manually.
    /// </summary>
    public static class ScissorModeManager
    {
        private static Stack<Rectangle> scissorStack = new Stack<Rectangle>();

        /// <summary>
        /// Begins a scissor mode, calculating the intersection with any existing scissor mode.
        /// </summary>
        /// <param name="x">X coordinate of the scissor rectangle</param>
        /// <param name="y">Y coordinate of the scissor rectangle</param>
        /// <param name="width">Width of the scissor rectangle</param>
        /// <param name="height">Height of the scissor rectangle</param>
        public static void BeginScissorMode(int x, int y, int width, int height)
        {
            Rectangle newRect = new Rectangle(x, y, width, height);

            if (scissorStack.Count > 0)
            {
                // Calculate intersection with the current top scissor rectangle
                Rectangle currentRect = scissorStack.Peek();
                Rectangle intersection = CalculateIntersection(currentRect, newRect);
                
                // If there's a valid intersection, use it
                if (intersection.Width > 0 && intersection.Height > 0)
                {
                    newRect = intersection;
                }
                else
                {
                    // No intersection - use empty rectangle (nothing will be drawn)
                    newRect = new Rectangle(x, y, 0, 0);
                }
            }

            // Push the (possibly intersected) rectangle onto the stack
            scissorStack.Push(newRect);
            
            // Apply the scissor mode to Raylib
            Raylib.BeginScissorMode((int)newRect.X, (int)newRect.Y, (int)newRect.Width, (int)newRect.Height);
        }

        /// <summary>
        /// Ends the current scissor mode and restores the previous one.
        /// </summary>
        public static void EndScissorMode()
        {
            if (scissorStack.Count > 0)
            {
                scissorStack.Pop();
            }

            // Restore the previous scissor mode (or disable if stack is empty)
            if (scissorStack.Count > 0)
            {
                Rectangle previousRect = scissorStack.Peek();
                Raylib.BeginScissorMode((int)previousRect.X, (int)previousRect.Y, (int)previousRect.Width, (int)previousRect.Height);
            }
            else
            {
                // No more scissor modes - disable clipping by ending Raylib's scissor mode
                Raylib.EndScissorMode();
            }
        }

        /// <summary>
        /// Calculates the intersection of two rectangles.
        /// </summary>
        private static Rectangle CalculateIntersection(Rectangle a, Rectangle b)
        {
            float left = System.Math.Max(a.X, b.X);
            float top = System.Math.Max(a.Y, b.Y);
            float right = System.Math.Min(a.X + a.Width, b.X + b.Width);
            float bottom = System.Math.Min(a.Y + a.Height, b.Y + b.Height);

            float width = System.Math.Max(0, right - left);
            float height = System.Math.Max(0, bottom - top);

            return new Rectangle(left, top, width, height);
        }

        /// <summary>
        /// Clears the scissor mode stack (useful for cleanup or error recovery).
        /// </summary>
        public static void Clear()
        {
            while (scissorStack.Count > 0)
            {
                scissorStack.Pop();
                Raylib.EndScissorMode();
            }
        }
    }
}

