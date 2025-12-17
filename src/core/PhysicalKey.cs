namespace Keysharp.Core
{
    /// <summary>
    /// Represents a physical key on a keyboard, describing its position, size, and which finger is used to press it.
    /// Position and size use the standard U unit, where 1U represents one standard key length.
    /// </summary>
    public class PhysicalKey
    {
        /// <summary>
        /// The X position of the key on the keyboard layout (in U units, where 1U = one standard key length).
        /// </summary>
        public float X { get; set; }

        /// <summary>
        /// The Y position of the key on the keyboard layout (in U units, where 1U = one standard key length).
        /// </summary>
        public float Y { get; set; }

        /// <summary>
        /// The width of the key (in U units, where 1U = one standard key length).
        /// </summary>
        public float Width { get; set; }

        /// <summary>
        /// The height of the key (in U units, where 1U = one standard key length).
        /// </summary>
        public float Height { get; set; }

        /// <summary>
        /// The finger used to press this key.
        /// </summary>
        public Finger Finger { get; set; }

        /// <summary>
        /// Optional identifier or label for this key (e.g., "A", "Space", "Left Shift").
        /// </summary>
        public string? Identifier { get; set; }

        public PhysicalKey(float x, float y, float width, float height, Finger finger, string? identifier = null)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Finger = finger;
            Identifier = identifier;
        }
    }
}

