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
        private Finger _finger;
        public Finger Finger 
        { 
            get => _finger;
            set 
            {
                _finger = value;
                (HandIndex, FingerIndex) = FingerToIndices(value);
            }
        }

        /// <summary>
        /// The hand index: 0 for left hand, 1 for right hand.
        /// </summary>
        public int HandIndex { get; set; }

        /// <summary>
        /// The finger index within the hand: 0=Pinky, 1=Ring, 2=Middle, 3=Index, 4=Thumb (left) or 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky (right).
        /// Used for easy adjacent finger checking (difference of 1 = adjacent, excluding thumb).
        /// </summary>
        public int FingerIndex { get; set; }

        /// <summary>
        /// Optional identifier or label for this key (e.g., "A", "Space", "Left Shift").
        /// </summary>
        public string? Identifier { get; set; }

        /// <summary>
        /// The primary (base/unshifted) character produced by this key (e.g., "a", "1", ";").
        /// </summary>
        public string? PrimaryCharacter { get; set; }

        /// <summary>
        /// The shift character produced by this key when Shift is held (e.g., "A", "!", ":").
        /// </summary>
        public string? ShiftCharacter { get; set; }

        /// <summary>
        /// Whether this key is disabled. Disabled keys may be hidden or shown with an outline depending on view settings.
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// The rotation angle of the key in degrees (0 = no rotation, positive = clockwise).
        /// </summary>
        public float Rotation { get; set; } = 0.0f;

        public PhysicalKey(float x, float y, float width, float height, Finger finger, string? identifier = null)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
            Identifier = identifier;
            // Setting Finger property will automatically set HandIndex and FingerIndex via the setter
            Finger = finger;
        }

        /// <summary>
        /// Converts a Finger enum value to hand index and finger index.
        /// Hand index: 0 = left, 1 = right
        /// Finger index: 0=Pinky, 1=Ring, 2=Middle, 3=Index, 4=Thumb (left)
        ///                 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky (right)
        /// </summary>
        private static (int handIndex, int fingerIndex) FingerToIndices(Finger finger)
        {
            return finger switch
            {
                Finger.LeftPinky => (0, 0),
                Finger.LeftRing => (0, 1),
                Finger.LeftMiddle => (0, 2),
                Finger.LeftIndex => (0, 3),
                Finger.LeftThumb => (0, 4),
                Finger.RightThumb => (1, 0),
                Finger.RightIndex => (1, 1),
                Finger.RightMiddle => (1, 2),
                Finger.RightRing => (1, 3),
                Finger.RightPinky => (1, 4),
                _ => (0, 0) // Default fallback
            };
        }

    }
}

