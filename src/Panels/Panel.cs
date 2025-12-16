using Raylib_cs;

namespace Keysharp.Panels
{
    public abstract class Panel
    {
        protected Font Font { get; }

        protected Panel(Font font)
        {
            Font = font;
        }

        public abstract void Draw(Rectangle bounds);
    }
}

