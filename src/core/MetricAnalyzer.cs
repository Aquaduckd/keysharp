using System;
using System.Collections.Generic;
using System.Linq;

namespace Keysharp.Core
{
    /// <summary>
    /// Represents the metric analysis result for a single n-gram.
    /// </summary>
    public class NgramMetricResult
    {
        public string NgramSequence { get; set; }
        public List<PhysicalKey>? KeySequence { get; set; }
        public string KeySequenceString { get; set; }
        public string FingerSequenceString { get; set; }
        public List<string> MetricMatches { get; set; }
        public long Count { get; set; }
        public double Frequency { get; set; }

        public NgramMetricResult(string ngramSequence, List<PhysicalKey>? keySequence, string keySequenceString, string fingerSequenceString, List<string> metricMatches, long count, double frequency)
        {
            NgramSequence = ngramSequence;
            KeySequence = keySequence;
            KeySequenceString = keySequenceString;
            FingerSequenceString = fingerSequenceString;
            MetricMatches = metricMatches;
            Count = count;
            Frequency = frequency;
        }
    }

    /// <summary>
    /// Analyzes a corpus against a layout and calculates keyboard layout metrics for n-grams.
    /// Tracks which sequences match which metrics and aggregates total counts per metric type.
    /// </summary>
    public class MetricAnalyzer
    {
        private readonly Layout _layout;
        private readonly Corpus _corpus;

        // Results stored per ngram type
        private Dictionary<string, NgramMetricResult> _monogramResults;
        private Dictionary<string, NgramMetricResult> _bigramResults;
        private Dictionary<string, NgramMetricResult> _skipgramResults;
        private Dictionary<string, NgramMetricResult> _trigramResults;

        // Total counts per metric type, per ngram type
        private Dictionary<string, long> _monogramMetricCounts;
        private Dictionary<string, long> _bigramMetricCounts;
        private Dictionary<string, long> _skipgramMetricCounts;
        private Dictionary<string, long> _trigramMetricCounts;

        public MetricAnalyzer(Layout layout, Corpus corpus)
        {
            _layout = layout ?? throw new ArgumentNullException(nameof(layout));
            _corpus = corpus ?? throw new ArgumentNullException(nameof(corpus));

            _monogramResults = new Dictionary<string, NgramMetricResult>();
            _bigramResults = new Dictionary<string, NgramMetricResult>();
            _skipgramResults = new Dictionary<string, NgramMetricResult>();
            _trigramResults = new Dictionary<string, NgramMetricResult>();

            _monogramMetricCounts = new Dictionary<string, long>();
            _bigramMetricCounts = new Dictionary<string, long>();
            _skipgramMetricCounts = new Dictionary<string, long>();
            _trigramMetricCounts = new Dictionary<string, long>();
        }

        /// <summary>
        /// Computes metrics for all monograms in the corpus.
        /// For monograms, the metric is simply which finger is used.
        /// </summary>
        public void ComputeMonogramMetrics()
        {
            _monogramResults.Clear();
            _monogramMetricCounts.Clear();

            var monograms = _corpus.GetMonograms();
            long monogramTotal = _corpus.GetMonogramTotal();

            foreach (var kvp in monograms.Counts)
            {
                string ngramSequence = kvp.Key;
                long count = kvp.Value;
                double frequency = monograms.GetFrequency(ngramSequence);

                // Convert ngram to key sequence
                var keySequence = _layout.ConvertNgramToKeySequence(ngramSequence);
                
                string keySequenceStr = "";
                string fingerSequenceStr = "";
                var metricMatches = new List<string>();

                if (keySequence != null && keySequence.Count > 0)
                {
                    // Format key sequence
                    keySequenceStr = FormatKeySequence(keySequence);

                    // Format finger sequence
                    fingerSequenceStr = FormatFingerSequence(keySequence);

                    // For monograms, the metric is which finger is used
                    // If the sequence is longer than 1 key (e.g., Shift+A), add each key's finger as a match
                    // This is similar to how bigram/trigram metrics handle sequences longer than expected
                    var uniqueFingers = new HashSet<string>();
                    foreach (var key in keySequence)
                    {
                        string fingerName = key.Finger.ToString();
                        if (!uniqueFingers.Contains(fingerName))
                        {
                            uniqueFingers.Add(fingerName);
                            metricMatches.Add(fingerName);
                            IncrementMetricCount(_monogramMetricCounts, fingerName, count);
                        }
                    }
                }

                var result = new NgramMetricResult(
                    ngramSequence,
                    keySequence,
                    keySequenceStr,
                    fingerSequenceStr,
                    metricMatches,
                    count,
                    frequency
                );

                _monogramResults[ngramSequence] = result;
            }
        }

        /// <summary>
        /// Computes metrics for all bigrams in the corpus.
        /// </summary>
        public void ComputeBigramMetrics()
        {
            _bigramResults.Clear();
            _bigramMetricCounts.Clear();

            var bigrams = _corpus.GetBigrams();
            long bigramTotal = _corpus.GetBigramTotal();

            foreach (var kvp in bigrams.Counts)
            {
                string ngramSequence = kvp.Key;
                long count = kvp.Value;
                double frequency = bigrams.GetFrequency(ngramSequence);

                // Convert ngram to key sequence
                var keySequence = _layout.ConvertNgramToKeySequence(ngramSequence);
                
                string keySequenceStr = "";
                string fingerSequenceStr = "";
                var metricMatches = new List<string>();

                if (keySequence != null)
                {
                    // Format key sequence
                    keySequenceStr = FormatKeySequence(keySequence);

                    // Format finger sequence
                    fingerSequenceStr = FormatFingerSequence(keySequence);

                    // Check bigram metrics
                    bool isSFB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsSFBPair);
                    bool isLSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsLSBPair);
                    bool isFSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsFSBPair);
                    bool isHSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsHSBPair);

                    if (isSFB) 
                    {
                        metricMatches.Add("SFB");
                        IncrementMetricCount(_bigramMetricCounts, "SFB", count);
                    }
                    if (isLSB) 
                    {
                        metricMatches.Add("LSB");
                        IncrementMetricCount(_bigramMetricCounts, "LSB", count);
                    }
                    if (isFSB) 
                    {
                        metricMatches.Add("FSB");
                        IncrementMetricCount(_bigramMetricCounts, "FSB", count);
                    }
                    if (isHSB) 
                    {
                        metricMatches.Add("HSB");
                        IncrementMetricCount(_bigramMetricCounts, "HSB", count);
                    }
                }

                var result = new NgramMetricResult(
                    ngramSequence,
                    keySequence,
                    keySequenceStr,
                    fingerSequenceStr,
                    metricMatches,
                    count,
                    frequency
                );

                _bigramResults[ngramSequence] = result;
            }
        }

        /// <summary>
        /// Computes metrics for all skipgrams in the corpus.
        /// Skipgrams use bigram metrics.
        /// </summary>
        public void ComputeSkipgramMetrics()
        {
            _skipgramResults.Clear();
            _skipgramMetricCounts.Clear();

            var skipgrams = _corpus.GetSkipgrams();
            long skipgramTotal = _corpus.GetSkipgramTotal();

            foreach (var kvp in skipgrams.Counts)
            {
                string ngramSequence = kvp.Key;
                long count = kvp.Value;
                double frequency = skipgrams.GetFrequency(ngramSequence);

                // Convert ngram to key sequence
                var keySequence = _layout.ConvertNgramToKeySequence(ngramSequence);
                
                string keySequenceStr = "";
                string fingerSequenceStr = "";
                var metricMatches = new List<string>();

                if (keySequence != null)
                {
                    // Format key sequence
                    keySequenceStr = FormatKeySequence(keySequence);

                    // Format finger sequence
                    fingerSequenceStr = FormatFingerSequence(keySequence);

                    // Check bigram metrics (skipgrams use bigram metrics)
                    bool isSFB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsSFBPair);
                    bool isLSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsLSBPair);
                    bool isFSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsFSBPair);
                    bool isHSB = Metrics.CheckAnyNgram(keySequence, 2, Metrics.IsHSBPair);

                    if (isSFB) 
                    {
                        metricMatches.Add("SFB");
                        IncrementMetricCount(_skipgramMetricCounts, "SFB", count);
                    }
                    if (isLSB) 
                    {
                        metricMatches.Add("LSB");
                        IncrementMetricCount(_skipgramMetricCounts, "LSB", count);
                    }
                    if (isFSB) 
                    {
                        metricMatches.Add("FSB");
                        IncrementMetricCount(_skipgramMetricCounts, "FSB", count);
                    }
                    if (isHSB) 
                    {
                        metricMatches.Add("HSB");
                        IncrementMetricCount(_skipgramMetricCounts, "HSB", count);
                    }
                }

                var result = new NgramMetricResult(
                    ngramSequence,
                    keySequence,
                    keySequenceStr,
                    fingerSequenceStr,
                    metricMatches,
                    count,
                    frequency
                );

                _skipgramResults[ngramSequence] = result;
            }
        }

        /// <summary>
        /// Computes metrics for all trigrams in the corpus.
        /// </summary>
        public void ComputeTrigramMetrics()
        {
            _trigramResults.Clear();
            _trigramMetricCounts.Clear();

            var trigrams = _corpus.GetTrigrams();
            long trigramTotal = _corpus.GetTrigramTotal();

            foreach (var kvp in trigrams.Counts)
            {
                string ngramSequence = kvp.Key;
                long count = kvp.Value;
                double frequency = trigrams.GetFrequency(ngramSequence);

                // Convert ngram to key sequence
                var keySequence = _layout.ConvertNgramToKeySequence(ngramSequence);
                
                string keySequenceStr = "";
                string fingerSequenceStr = "";
                var metricMatches = new List<string>();

                if (keySequence != null)
                {
                    // Format key sequence
                    keySequenceStr = FormatKeySequence(keySequence);

                    // Format finger sequence
                    fingerSequenceStr = FormatFingerSequence(keySequence);

                    // Check trigram metrics
                    bool isInHand = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsInHandTrigram);
                    bool isOutHand = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsOutHandTrigram);
                    bool isRedirect = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsRedirectTrigram);
                    bool isAlternate = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsAlternateTrigram);
                    bool isInRoll = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsInRollTrigram);
                    bool isOutRoll = Metrics.CheckAnyNgram(keySequence, 3, Metrics.IsOutRollTrigram);

                    if (isInHand) 
                    {
                        metricMatches.Add("InHand");
                        IncrementMetricCount(_trigramMetricCounts, "InHand", count);
                    }
                    if (isOutHand) 
                    {
                        metricMatches.Add("OutHand");
                        IncrementMetricCount(_trigramMetricCounts, "OutHand", count);
                    }
                    if (isRedirect) 
                    {
                        metricMatches.Add("Redirect");
                        IncrementMetricCount(_trigramMetricCounts, "Redirect", count);
                    }
                    if (isAlternate) 
                    {
                        metricMatches.Add("Alternate");
                        IncrementMetricCount(_trigramMetricCounts, "Alternate", count);
                    }
                    if (isInRoll) 
                    {
                        metricMatches.Add("InRoll");
                        IncrementMetricCount(_trigramMetricCounts, "InRoll", count);
                    }
                    if (isOutRoll) 
                    {
                        metricMatches.Add("OutRoll");
                        IncrementMetricCount(_trigramMetricCounts, "OutRoll", count);
                    }
                }

                var result = new NgramMetricResult(
                    ngramSequence,
                    keySequence,
                    keySequenceStr,
                    fingerSequenceStr,
                    metricMatches,
                    count,
                    frequency
                );

                _trigramResults[ngramSequence] = result;
            }
        }

        /// <summary>
        /// Computes metrics for all applicable n-gram types (monograms, bigrams, skipgrams, trigrams).
        /// </summary>
        public void ComputeAllMetrics()
        {
            ComputeMonogramMetrics();
            ComputeBigramMetrics();
            ComputeSkipgramMetrics();
            ComputeTrigramMetrics();
            
            // Print metric frequencies for testing
            PrintMetricFrequencies();
        }

        /// <summary>
        /// Prints the total frequencies for each metric across all n-gram types to the console.
        /// </summary>
        private void PrintMetricFrequencies()
        {
            System.Console.WriteLine("=== Metric Frequencies ===");
            
            // Monogram metrics (finger frequencies)
            System.Console.WriteLine("\nMonograms:");
            foreach (var kvp in _monogramMetricCounts.OrderByDescending(x => x.Value))
            {
                long monogramTotal = _corpus.GetMonogramTotal();
                double frequency = monogramTotal > 0 ? (double)kvp.Value / monogramTotal * 100.0 : 0.0;
                System.Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0} ({frequency:F3}%)");
            }
            
            // Bigram metrics
            System.Console.WriteLine("\nBigrams:");
            foreach (var kvp in _bigramMetricCounts.OrderByDescending(x => x.Value))
            {
                long bigramTotal = _corpus.GetBigramTotal();
                double frequency = bigramTotal > 0 ? (double)kvp.Value / bigramTotal * 100.0 : 0.0;
                System.Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0} ({frequency:F3}%)");
            }
            
            // Skipgram metrics
            System.Console.WriteLine("\nSkipgrams:");
            foreach (var kvp in _skipgramMetricCounts.OrderByDescending(x => x.Value))
            {
                long skipgramTotal = _corpus.GetSkipgramTotal();
                double frequency = skipgramTotal > 0 ? (double)kvp.Value / skipgramTotal * 100.0 : 0.0;
                System.Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0} ({frequency:F3}%)");
            }
            
            // Trigram metrics
            System.Console.WriteLine("\nTrigrams:");
            foreach (var kvp in _trigramMetricCounts.OrderByDescending(x => x.Value))
            {
                long trigramTotal = _corpus.GetTrigramTotal();
                double frequency = trigramTotal > 0 ? (double)kvp.Value / trigramTotal * 100.0 : 0.0;
                System.Console.WriteLine($"  {kvp.Key}: {kvp.Value:N0} ({frequency:F3}%)");
            }
            
            System.Console.WriteLine("========================\n");
        }

        /// <summary>
        /// Gets the metric result for a specific monogram sequence.
        /// </summary>
        public NgramMetricResult? GetMonogramResult(string sequence)
        {
            return _monogramResults.TryGetValue(sequence, out var result) ? result : null;
        }

        /// <summary>
        /// Gets the metric result for a specific bigram sequence.
        /// </summary>
        public NgramMetricResult? GetBigramResult(string sequence)
        {
            return _bigramResults.TryGetValue(sequence, out var result) ? result : null;
        }

        /// <summary>
        /// Gets the metric result for a specific skipgram sequence.
        /// </summary>
        public NgramMetricResult? GetSkipgramResult(string sequence)
        {
            return _skipgramResults.TryGetValue(sequence, out var result) ? result : null;
        }

        /// <summary>
        /// Gets the metric result for a specific trigram sequence.
        /// </summary>
        public NgramMetricResult? GetTrigramResult(string sequence)
        {
            return _trigramResults.TryGetValue(sequence, out var result) ? result : null;
        }

        /// <summary>
        /// Gets the metric result for a specific n-gram sequence based on n-gram type.
        /// </summary>
        public NgramMetricResult? GetNgramResult(string sequence, string ngramType)
        {
            return ngramType switch
            {
                "monogram" => GetMonogramResult(sequence),
                "bigram" => GetBigramResult(sequence),
                "skipgram" => GetSkipgramResult(sequence),
                "trigram" => GetTrigramResult(sequence),
                _ => null
            };
        }

        /// <summary>
        /// Gets all bigram results.
        /// </summary>
        public IReadOnlyDictionary<string, NgramMetricResult> GetBigramResults()
        {
            return _bigramResults;
        }

        /// <summary>
        /// Gets all skipgram results.
        /// </summary>
        public IReadOnlyDictionary<string, NgramMetricResult> GetSkipgramResults()
        {
            return _skipgramResults;
        }

        /// <summary>
        /// Gets all trigram results.
        /// </summary>
        public IReadOnlyDictionary<string, NgramMetricResult> GetTrigramResults()
        {
            return _trigramResults;
        }

        /// <summary>
        /// Gets the total count for a specific metric in monograms.
        /// </summary>
        public long GetMonogramMetricCount(string metricName)
        {
            return _monogramMetricCounts.TryGetValue(metricName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the total count for a specific metric in bigrams.
        /// </summary>
        public long GetBigramMetricCount(string metricName)
        {
            return _bigramMetricCounts.TryGetValue(metricName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the total count for a specific metric in skipgrams.
        /// </summary>
        public long GetSkipgramMetricCount(string metricName)
        {
            return _skipgramMetricCounts.TryGetValue(metricName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the total count for a specific metric in trigrams.
        /// </summary>
        public long GetTrigramMetricCount(string metricName)
        {
            return _trigramMetricCounts.TryGetValue(metricName, out var count) ? count : 0;
        }

        /// <summary>
        /// Gets the total count for a specific metric based on n-gram type.
        /// </summary>
        public long GetMetricCount(string metricName, string ngramType)
        {
            return ngramType switch
            {
                "monogram" => GetMonogramMetricCount(metricName),
                "bigram" => GetBigramMetricCount(metricName),
                "skipgram" => GetSkipgramMetricCount(metricName),
                "trigram" => GetTrigramMetricCount(metricName),
                _ => 0
            };
        }

        /// <summary>
        /// Gets all metric counts for monograms.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetMonogramMetricCounts()
        {
            return _monogramMetricCounts;
        }

        /// <summary>
        /// Gets all metric counts for bigrams.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetBigramMetricCounts()
        {
            return _bigramMetricCounts;
        }

        /// <summary>
        /// Gets all metric counts for skipgrams.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetSkipgramMetricCounts()
        {
            return _skipgramMetricCounts;
        }

        /// <summary>
        /// Gets all metric counts for trigrams.
        /// </summary>
        public IReadOnlyDictionary<string, long> GetTrigramMetricCounts()
        {
            return _trigramMetricCounts;
        }

        /// <summary>
        /// Formats a key sequence as a readable string (e.g., "A", "LShift+A", "Space").
        /// Truncates to a maximum length to prevent overflow in the table.
        /// </summary>
        private string FormatKeySequence(List<PhysicalKey> keySequence)
        {
            const int MaxLength = 50; // Maximum characters to display before truncation

            if (keySequence == null || keySequence.Count == 0)
                return "";

            var parts = new List<string>();
            foreach (var key in keySequence)
            {
                // Check if it's a modifier key (LShift)
                if (key.Identifier == "LShift")
                {
                    parts.Add("Shift");
                }
                else
                {
                    // Use identifier if available, otherwise use primary character
                    string keyName = !string.IsNullOrEmpty(key.Identifier) ? key.Identifier : key.PrimaryCharacter ?? "?";
                    parts.Add(keyName);
                }
            }

            string result = string.Join("+", parts);
            
            // Truncate if too long
            if (result.Length > MaxLength)
            {
                result = result.Substring(0, MaxLength - 3) + "...";
            }

            return result;
        }

        /// <summary>
        /// Formats a finger sequence as a readable string with shortened finger names (e.g., "LP+LI", "LM+RM").
        /// </summary>
        private string FormatFingerSequence(List<PhysicalKey> keySequence)
        {
            if (keySequence == null || keySequence.Count == 0)
                return "";

            var fingerNames = keySequence.Select(key => GetShortFingerName(key.Finger)).ToList();
            return string.Join("+", fingerNames);
        }

        /// <summary>
        /// Gets the shortened 2-character name for a finger.
        /// </summary>
        private string GetShortFingerName(Finger finger)
        {
            return finger switch
            {
                Finger.LeftPinky => "LP",
                Finger.LeftRing => "LR",
                Finger.LeftMiddle => "LM",
                Finger.LeftIndex => "LI",
                Finger.LeftThumb => "LT",
                Finger.RightThumb => "RT",
                Finger.RightIndex => "RI",
                Finger.RightMiddle => "RM",
                Finger.RightRing => "RR",
                Finger.RightPinky => "RP",
                _ => "??"
            };
        }

        /// <summary>
        /// Increments the count for a metric in the given dictionary.
        /// </summary>
        private void IncrementMetricCount(Dictionary<string, long> metricCounts, string metricName, long count)
        {
            if (metricCounts.ContainsKey(metricName))
            {
                metricCounts[metricName] += count;
            }
            else
            {
                metricCounts[metricName] = count;
            }
        }
    }
}

