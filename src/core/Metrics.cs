using System;
using System.Collections.Generic;
using System.Linq;

namespace Keysharp.Core
{
    /// <summary>
    /// Provides methods for detecting various keyboard layout metrics in key sequences.
    /// </summary>
    public static class Metrics
    {
        /// <summary>
        /// Checks if any overlapping n-gram of the specified size in a sequence matches a predicate.
        /// </summary>
        public static bool CheckAnyNgram(List<PhysicalKey> sequence, int ngramSize, Func<List<PhysicalKey>, bool> predicate)
        {
            if (sequence == null || sequence.Count < ngramSize)
                return false;

            // Check all overlapping n-grams in the sequence
            for (int i = 0; i <= sequence.Count - ngramSize; i++)
            {
                var ngram = sequence.Skip(i).Take(ngramSize).ToList();
                if (predicate(ngram))
                {
                    return true; // If any n-gram matches, return true
                }
            }

            return false; // No matching n-gram found
        }

        /// <summary>
        /// Checks if a bigram (2 keys) forms a Same Finger Bigram (SFB).
        /// Returns true if the keys are on the same finger but are different keys.
        /// </summary>
        public static bool IsSFBPair(List<PhysicalKey> bigram)
        {
            if (bigram == null || bigram.Count != 2)
                return false;

            var firstKey = bigram[0];
            var secondKey = bigram[1];

            // Check if same finger
            if (firstKey.Finger != secondKey.Finger)
                return false;

            // SFB if same finger but different key
            return !IsSameKey(firstKey, secondKey);
        }


        /// <summary>
        /// Checks if a bigram (2 keys) forms a Lateral Stretch Bigram (LSB).
        /// Returns true if they are on the same hand, adjacent fingers (excluding thumb), and 2U+ apart horizontally.
        /// </summary>
        public static bool IsLSBPair(List<PhysicalKey> bigram)
        {
            if (bigram == null || bigram.Count != 2)
                return false;

            var firstKey = bigram[0];
            var secondKey = bigram[1];

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
        /// Checks if a bigram (2 keys) forms a Full Scissor Bigram (FSB).
        /// Returns true if they are on the same hand (excluding thumbs), have vertical distance >= 2U,
        /// and the key with greater Y has a Middle or Ring finger.
        /// </summary>
        public static bool IsFSBPair(List<PhysicalKey> bigram)
        {
            if (bigram == null || bigram.Count != 2)
                return false;

            var firstKey = bigram[0];
            var secondKey = bigram[1];

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
        /// Checks if a bigram (2 keys) forms a Half Scissor Bigram (HSB).
        /// Returns true if they are on the same hand (excluding thumbs), are adjacent fingers,
        /// have vertical distance >= 1U and < 2U, and the key with greater Y has a Middle or Ring finger.
        /// </summary>
        public static bool IsHSBPair(List<PhysicalKey> bigram)
        {
            if (bigram == null || bigram.Count != 2)
                return false;

            var firstKey = bigram[0];
            var secondKey = bigram[1];

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

        /// <summary>
        /// Checks if a trigram (3 keys) is an InHand.
        /// An InHand occurs when all three keys are:
        /// 1. On the same hand
        /// 2. Roll direction is towards the center of the keyboard:
        ///    - Left hand: finger indices strictly increasing (a < b < c)
        ///    - Right hand: finger indices strictly decreasing (a > b > c)
        /// </summary>
        public static bool IsInHandTrigram(List<PhysicalKey> trigram)
        {
            if (trigram == null || trigram.Count != 3)
                return false;

            var firstKey = trigram[0];
            var secondKey = trigram[1];
            var thirdKey = trigram[2];

            // Check if all keys are on the same hand
            if (firstKey.HandIndex != secondKey.HandIndex || secondKey.HandIndex != thirdKey.HandIndex)
                return false;

            int a = firstKey.FingerIndex;
            int b = secondKey.FingerIndex;
            int c = thirdKey.FingerIndex;

            // Direction depends on which hand:
            // Left hand (HandIndex == 0): increasing indices = towards center (right)
            // Right hand (HandIndex == 1): decreasing indices = towards center (left)
            if (firstKey.HandIndex == 0)
            {
                // Left hand: InHand = increasing indices (a < b < c)
                return a < b && b < c;
            }
            else
            {
                // Right hand: InHand = decreasing indices (a > b > c)
                return a > b && b > c;
            }
        }

        /// <summary>
        /// Checks if a trigram (3 keys) is an OutHand.
        /// An OutHand occurs when all three keys are:
        /// 1. On the same hand
        /// 2. Roll direction is towards the outside of the keyboard:
        ///    - Left hand: finger indices strictly decreasing (a > b > c)
        ///    - Right hand: finger indices strictly increasing (a < b < c)
        /// </summary>
        public static bool IsOutHandTrigram(List<PhysicalKey> trigram)
        {
            if (trigram == null || trigram.Count != 3)
                return false;

            var firstKey = trigram[0];
            var secondKey = trigram[1];
            var thirdKey = trigram[2];

            // Check if all keys are on the same hand
            if (firstKey.HandIndex != secondKey.HandIndex || secondKey.HandIndex != thirdKey.HandIndex)
                return false;

            int a = firstKey.FingerIndex;
            int b = secondKey.FingerIndex;
            int c = thirdKey.FingerIndex;

            // Direction depends on which hand:
            // Left hand (HandIndex == 0): decreasing indices = towards outside (left)
            // Right hand (HandIndex == 1): increasing indices = towards outside (right)
            if (firstKey.HandIndex == 0)
            {
                // Left hand: OutHand = decreasing indices (a > b > c)
                return a > b && b > c;
            }
            else
            {
                // Right hand: OutHand = increasing indices (a < b < c)
                return a < b && b < c;
            }
        }

        /// <summary>
        /// Checks if two PhysicalKey objects represent the same key.
        /// Compares by identifier if available, otherwise by position.
        /// </summary>
        private static bool IsSameKey(PhysicalKey key1, PhysicalKey key2)
        {
            if (!string.IsNullOrEmpty(key1.Identifier) && !string.IsNullOrEmpty(key2.Identifier))
            {
                return key1.Identifier == key2.Identifier;
            }
            else
            {
                // Compare by position (X, Y, Width, Height)
                return key1.X == key2.X &&
                       key1.Y == key2.Y &&
                       key1.Width == key2.Width &&
                       key1.Height == key2.Height;
            }
        }

        /// <summary>
        /// Checks if a trigram (3 keys) is a Redirect.
        /// A Redirect occurs when all three keys are:
        /// 1. On the same hand
        /// 2. The directions of the two bigrams that make up the trigram are opposite
        /// 3. Neither of the two bigrams uses the same key (a.key != b.key and b.key != c.key)
        /// (This automatically excludes Onehands, as they require a continuous rolling direction)
        /// </summary>
        public static bool IsRedirectTrigram(List<PhysicalKey> trigram)
        {
            if (trigram == null || trigram.Count != 3)
                return false;

            var firstKey = trigram[0];
            var secondKey = trigram[1];
            var thirdKey = trigram[2];

            // Check if all keys are on the same hand
            if (firstKey.HandIndex != secondKey.HandIndex || secondKey.HandIndex != thirdKey.HandIndex)
                return false;

            // Check that neither bigram uses the same key
            if (IsSameKey(firstKey, secondKey) || IsSameKey(secondKey, thirdKey))
                return false;

            int a = firstKey.FingerIndex;
            int b = secondKey.FingerIndex;
            int c = thirdKey.FingerIndex;

            // Check if the two bigrams have opposite directions:
            // Bigram 1: (a, b) - direction: increasing if a < b, decreasing if a > b
            // Bigram 2: (b, c) - direction: increasing if b < c, decreasing if b > c
            // They are opposite if one is increasing and the other is decreasing
            bool firstBigramIncreasing = a < b;
            bool secondBigramIncreasing = b < c;

            return firstBigramIncreasing != secondBigramIncreasing;
        }

    }
}

