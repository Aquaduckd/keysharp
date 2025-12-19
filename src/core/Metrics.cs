using System;
using System.Collections.Generic;

namespace Keysharp.Core
{
    /// <summary>
    /// Provides methods for detecting various keyboard layout metrics in key sequences.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Determines if a key sequence represents a Same Finger Bigram (SFB).
        /// For sequences longer than 2 keys, splits into overlapping bigrams and checks if ANY bigram is an SFB.
        /// An SFB occurs when two keys are:
        /// 1. On the same finger
        /// 2. Not the same key
        /// </summary>
        public static bool IsSameFingerBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                // Check if this bigram is an SFB
                if (IsSFBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an SFB, return true
                }
            }

            return false; // No SFB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Same Finger Bigram (SFB).
        /// Returns true if the keys are on the same finger but are different keys.
        /// </summary>
        private static bool IsSFBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if same finger
            if (firstKey.Finger != secondKey.Finger)
                return false;

            // Check if not the same key (compare by identifier if available, otherwise by position)
            bool isSameKey;
            if (!string.IsNullOrEmpty(firstKey.Identifier) && !string.IsNullOrEmpty(secondKey.Identifier))
            {
                isSameKey = firstKey.Identifier == secondKey.Identifier;
            }
            else
            {
                // Compare by position (X, Y, Width, Height)
                isSameKey = firstKey.X == secondKey.X &&
                           firstKey.Y == secondKey.Y &&
                           firstKey.Width == secondKey.Width &&
                           firstKey.Height == secondKey.Height;
            }

            // SFB if same finger but different key
            return !isSameKey;
        }

        /// <summary>
        /// Determines if a key sequence represents a Lateral Stretch Bigram (LSB).
        /// An LSB occurs when two keys are:
        /// 1. On the same hand
        /// 2. On adjacent fingers (excluding thumb)
        /// 3. Have a horizontal distance of 2U or greater
        /// </summary>
        public static bool IsLateralStretchBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                if (IsLSBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an LSB, return true
                }
            }

            return false; // No LSB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Lateral Stretch Bigram (LSB).
        /// Returns true if they are on the same hand, adjacent fingers (excluding thumb), and 2U+ apart horizontally.
        /// </summary>
        private static bool IsLSBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if on same hand
            if (firstKey.HandIndex != secondKey.HandIndex)
                return false;

            // Check if adjacent fingers (excluding thumb)
            // Adjacent means finger index difference is 1
            int fingerIndexDiff = Math.Abs(firstKey.FingerIndex - secondKey.FingerIndex);
            if (fingerIndexDiff != 1)
                return false;

            // Exclude thumb: index 0 on right hand, index 4 on left hand
            // Since both keys are on the same hand (already checked), we can use either key's HandIndex
            if (firstKey.HandIndex == 0)
            {
                // Left hand: exclude thumb at index 4
                if (firstKey.FingerIndex == 4 || secondKey.FingerIndex == 4)
                    return false;
            }
            else
            {
                // Right hand: exclude thumb at index 0
                if (firstKey.FingerIndex == 0 || secondKey.FingerIndex == 0)
                    return false;
            }

            // Check horizontal distance >= 2U
            float horizontalDistance = CalculateHorizontalDistance(firstKey, secondKey);
            if (horizontalDistance < 2.0f)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if a key sequence represents a Full Scissor Bigram (FSB).
        /// An FSB occurs when two keys are:
        /// 1. On the same hand (excluding thumbs)
        /// 2. Have a vertical distance of 2U or greater
        /// 3. The finger with the greater Y position is either Middle or Ring
        /// </summary>
        public static bool IsFullScissorBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                if (IsFSBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an FSB, return true
                }
            }

            return false; // No FSB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Full Scissor Bigram (FSB).
        /// Returns true if they are on the same hand (excluding thumbs), have vertical distance >= 2U,
        /// and the key with greater Y has a Middle or Ring finger.
        /// </summary>
        private static bool IsFSBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if on same hand
            if (firstKey.HandIndex != secondKey.HandIndex)
                return false;

            // Must be different fingers
            if (firstKey.FingerIndex == secondKey.FingerIndex)
                return false;

            // Exclude thumbs (index 0 on right hand, index 4 on left hand)
            if (firstKey.HandIndex == 0)
            {
                // Left hand: exclude thumb at index 4
                if (firstKey.FingerIndex == 4 || secondKey.FingerIndex == 4)
                    return false;
            }
            else
            {
                // Right hand: exclude thumb at index 0
                if (firstKey.FingerIndex == 0 || secondKey.FingerIndex == 0)
                    return false;
            }

            // Check vertical distance >= 2U
            float verticalDistance = CalculateVerticalDistance(firstKey, secondKey);
            if (verticalDistance < 2.0f)
                return false;

            // Find the key with greater Y position
            PhysicalKey keyWithGreaterY = firstKey.Y > secondKey.Y ? firstKey : secondKey;

            // Check if the finger with greater Y is Middle or Ring
            // FingerIndex mapping:
            // Left: 0=Pinky, 1=Ring, 2=Middle, 3=Index, 4=Thumb
            // Right: 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky
            // So Middle = 2 on both hands, Left Ring = 1, Right Ring = 3
            bool isMiddleOrRing = keyWithGreaterY.FingerIndex == 2 || // Middle on either hand
                                  (keyWithGreaterY.HandIndex == 0 && keyWithGreaterY.FingerIndex == 1) || // Left Ring
                                  (keyWithGreaterY.HandIndex == 1 && keyWithGreaterY.FingerIndex == 3); // Right Ring

            if (!isMiddleOrRing)
                return false;

            return true;
        }

        /// <summary>
        /// Determines if a key sequence represents a Half Scissor Bigram (HSB).
        /// An HSB occurs when two keys are:
        /// 1. On the same hand (excluding thumbs)
        /// 2. On adjacent fingers (excluding thumbs)
        /// 3. Have a vertical distance of 1U or greater but less than 2U
        /// 4. The finger with the greater Y position is either Middle or Ring
        /// </summary>
        public static bool IsHalfScissorBigram(List<PhysicalKey> sequence)
        {
            if (sequence == null || sequence.Count < 2)
                return false;

            // Check all overlapping bigrams in the sequence
            for (int i = 0; i < sequence.Count - 1; i++)
            {
                var firstKey = sequence[i];
                var secondKey = sequence[i + 1];

                if (IsHSBPair(firstKey, secondKey))
                {
                    return true; // If any bigram is an HSB, return true
                }
            }

            return false; // No HSB found in any bigram
        }

        /// <summary>
        /// Checks if two keys form a Half Scissor Bigram (HSB).
        /// Returns true if they are on the same hand (excluding thumbs), are adjacent fingers,
        /// have vertical distance >= 1U and < 2U, and the key with greater Y has a Middle or Ring finger.
        /// </summary>
        private static bool IsHSBPair(PhysicalKey firstKey, PhysicalKey secondKey)
        {
            // Check if on same hand
            if (firstKey.HandIndex != secondKey.HandIndex)
                return false;

            // Must be different fingers
            if (firstKey.FingerIndex == secondKey.FingerIndex)
                return false;

            // Check if adjacent fingers (excluding thumb)
            // Adjacent means finger index difference is 1
            int fingerIndexDiff = Math.Abs(firstKey.FingerIndex - secondKey.FingerIndex);
            if (fingerIndexDiff != 1)
                return false;

            // Exclude thumbs (index 0 on right hand, index 4 on left hand)
            if (firstKey.HandIndex == 0)
            {
                // Left hand: exclude thumb at index 4
                if (firstKey.FingerIndex == 4 || secondKey.FingerIndex == 4)
                    return false;
            }
            else
            {
                // Right hand: exclude thumb at index 0
                if (firstKey.FingerIndex == 0 || secondKey.FingerIndex == 0)
                    return false;
            }

            // Check vertical distance >= 1U and < 2U
            float verticalDistance = CalculateVerticalDistance(firstKey, secondKey);
            if (verticalDistance < 1.0f || verticalDistance >= 2.0f)
                return false;

            // Find the key with greater Y position
            PhysicalKey keyWithGreaterY = firstKey.Y > secondKey.Y ? firstKey : secondKey;

            // Check if the finger with greater Y is Middle or Ring
            // FingerIndex mapping:
            // Left: 0=Pinky, 1=Ring, 2=Middle, 3=Index, 4=Thumb
            // Right: 0=Thumb, 1=Index, 2=Middle, 3=Ring, 4=Pinky
            // So Middle = 2 on both hands, Left Ring = 1, Right Ring = 3
            bool isMiddleOrRing = keyWithGreaterY.FingerIndex == 2 || // Middle on either hand
                                  (keyWithGreaterY.HandIndex == 0 && keyWithGreaterY.FingerIndex == 1) || // Left Ring
                                  (keyWithGreaterY.HandIndex == 1 && keyWithGreaterY.FingerIndex == 3); // Right Ring

            if (!isMiddleOrRing)
                return false;

            return true;
        }

        /// <summary>
        /// Calculates the horizontal distance between two keys in U units.
        /// Uses center-to-center distance.
        /// </summary>
        public static float CalculateHorizontalDistance(PhysicalKey key1, PhysicalKey key2)
        {
            float center1X = key1.X + key1.Width / 2.0f;
            float center2X = key2.X + key2.Width / 2.0f;
            return Math.Abs(center2X - center1X);
        }

        /// <summary>
        /// Calculates the vertical distance between two keys in U units.
        /// Uses center-to-center distance.
        /// </summary>
        public static float CalculateVerticalDistance(PhysicalKey key1, PhysicalKey key2)
        {
            float center1Y = key1.Y + key1.Height / 2.0f;
            float center2Y = key2.Y + key2.Height / 2.0f;
            return Math.Abs(center2Y - center1Y);
        }
    }
}

