using System;
using System.Collections.Generic;
using System.Linq;

namespace Keysharp.Core
{
    /// <summary>
    /// Represents a collection of character sequence counts (n-grams) with helper methods.
    /// </summary>
    public class Grams
    {
        private Dictionary<string, long> counts;
        private long total;

        /// <summary>
        /// Gets the dictionary mapping sequences to their counts.
        /// </summary>
        public IReadOnlyDictionary<string, long> Counts => counts;

        /// <summary>
        /// Gets the total number of grams (sum of all counts).
        /// </summary>
        public long Total => total;

        /// <summary>
        /// Gets the number of unique sequences.
        /// </summary>
        public int UniqueCount => counts.Count;

        public Grams()
        {
            counts = new Dictionary<string, long>();
            total = 0;
        }

        /// <summary>
        /// Adds or increments the count for a sequence.
        /// </summary>
        public void Add(string sequence)
        {
            if (counts.ContainsKey(sequence))
            {
                counts[sequence]++;
            }
            else
            {
                counts[sequence] = 1;
            }
            total++;
        }

        /// <summary>
        /// Adds or increments the count for a sequence by a specific amount.
        /// </summary>
        public void Add(string sequence, long count)
        {
            if (counts.ContainsKey(sequence))
            {
                counts[sequence] += count;
            }
            else
            {
                counts[sequence] = count;
            }
            total += count;
        }

        /// <summary>
        /// Gets the count for a specific sequence.
        /// </summary>
        public long GetCount(string sequence)
        {
            return counts.ContainsKey(sequence) ? counts[sequence] : 0;
        }

        /// <summary>
        /// Gets the frequency (probability) of a sequence.
        /// </summary>
        public double GetFrequency(string sequence)
        {
            if (total == 0)
                return 0.0;
            
            return (double)GetCount(sequence) / total;
        }

        /// <summary>
        /// Gets the top N sequences by count, sorted in descending order.
        /// </summary>
        public List<(string sequence, long count, double frequency)> GetTopN(int n)
        {
            return counts
                .OrderByDescending(kvp => kvp.Value)
                .Take(n)
                .Select(kvp => (kvp.Key, kvp.Value, GetFrequency(kvp.Key)))
                .ToList();
        }

        /// <summary>
        /// Gets all sequences sorted by count in descending order.
        /// </summary>
        public List<(string sequence, long count, double frequency)> GetAllSorted()
        {
            return counts
                .OrderByDescending(kvp => kvp.Value)
                .Select(kvp => (kvp.Key, kvp.Value, GetFrequency(kvp.Key)))
                .ToList();
        }

        /// <summary>
        /// Clears all counts and resets the total.
        /// </summary>
        public void Clear()
        {
            counts.Clear();
            total = 0;
        }

        /// <summary>
        /// Creates a Grams instance from a dictionary of counts.
        /// </summary>
        public static Grams FromDictionary(Dictionary<string, long> counts)
        {
            Grams grams = new Grams();
            foreach (var kvp in counts)
            {
                grams.counts[kvp.Key] = kvp.Value;
                grams.total += kvp.Value;
            }
            return grams;
        }
    }
}

