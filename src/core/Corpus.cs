using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Keysharp.Core
{
    /// <summary>
    /// Represents a loaded corpus (text file) with n-gram analysis capabilities.
    /// </summary>
    public class Corpus
    {
        public string FilePath { get; private set; }
        public string FileName { get; private set; }
        public string Content { get; private set; }
        public long CharacterCount { get; private set; }
        public bool IsLoaded { get; private set; }

        private Grams? _monograms;
        private Grams? _bigrams;
        private Grams? _trigrams;
        private Grams? _skipgrams;
        private Grams? _words;

        public Corpus(string filePath)
        {
            FilePath = filePath;
            FileName = Path.GetFileName(filePath);
            Content = "";
            CharacterCount = 0;
            IsLoaded = false;
        }

        /// <summary>
        /// Loads the corpus from the file path.
        /// </summary>
        public void Load()
        {
            if (!File.Exists(FilePath))
            {
                throw new FileNotFoundException($"Corpus file not found: {FilePath}");
            }

            Content = File.ReadAllText(FilePath, Encoding.UTF8);
            CharacterCount = Content.Length;
            IsLoaded = true;

            // Clear cached n-grams so they'll be recalculated
            _monograms = null;
            _bigrams = null;
            _trigrams = null;
            _skipgrams = null;
            _words = null;

            // Pre-compute all n-grams and words in a single pass
            ComputeAllGrams();
            ComputeSkipgrams();
        }

        /// <summary>
        /// Computes monograms, bigrams, trigrams, and words in a single pass through the content.
        /// </summary>
        private void ComputeAllGrams()
        {
            _monograms = new Grams();
            _bigrams = new Grams();
            _trigrams = new Grams();
            _words = new Grams();

            int wordStart = -1; // Track the start of the current word (-1 means not in a word)

            for (int i = 0; i < Content.Length; i++)
            {
                char currentChar = Content[i];
                bool isWhitespace = char.IsWhiteSpace(currentChar);

                // Handle word boundaries
                if (isWhitespace)
                {
                    // If we were in a word, add it
                    if (wordStart >= 0)
                    {
                        int wordLength = i - wordStart;
                        if (wordLength > 0)
                        {
                            _words.Add(Content.Substring(wordStart, wordLength));
                        }
                        wordStart = -1; // Reset word start
                    }
                }
                else
                {
                    // Start a new word if we're not already in one
                    if (wordStart < 0)
                    {
                        wordStart = i;
                    }
                }

                // Add monogram
                _monograms.Add(currentChar.ToString());

                // Add bigram (if we have at least one more character)
                if (i < Content.Length - 1)
                {
                    _bigrams.Add(Content.Substring(i, 2));
                }

                // Add trigram (if we have at least two more characters)
                if (i < Content.Length - 2)
                {
                    _trigrams.Add(Content.Substring(i, 3));
                }
            }

            // Handle word at end of content (if content doesn't end with whitespace)
            if (wordStart >= 0)
            {
                int wordLength = Content.Length - wordStart;
                if (wordLength > 0)
                {
                    _words.Add(Content.Substring(wordStart, wordLength));
                }
            }
        }

        /// <summary>
        /// Gets monogram (single character) counts from the corpus.
        /// </summary>
        public Grams GetMonograms()
        {
            if (_monograms == null && IsLoaded)
            {
                ComputeAllGrams();
            }
            return _monograms ?? new Grams();
        }

        /// <summary>
        /// Gets bigram (two character) counts from the corpus.
        /// </summary>
        public Grams GetBigrams()
        {
            if (_bigrams == null && IsLoaded)
            {
                ComputeAllGrams();
            }
            return _bigrams ?? new Grams();
        }

        /// <summary>
        /// Gets trigram (three character) counts from the corpus.
        /// </summary>
        public Grams GetTrigrams()
        {
            if (_trigrams == null && IsLoaded)
            {
                ComputeAllGrams();
            }
            return _trigrams ?? new Grams();
        }

        /// <summary>
        /// Computes skipgrams from trigrams by taking the first and last character of each trigram.
        /// </summary>
        private void ComputeSkipgrams()
        {
            _skipgrams = new Grams();
            
            // Generate skipgrams from existing trigrams
            if (_trigrams != null)
            {
                foreach (var kvp in _trigrams.Counts)
                {
                    string trigram = kvp.Key;
                    long count = kvp.Value;
                    
                    // Skipgram is first and last character of trigram
                    if (trigram.Length == 3)
                    {
                        string skipgram = trigram[0].ToString() + trigram[2].ToString();
                        _skipgrams.Add(skipgram, count);
                    }
                }
            }
        }

        /// <summary>
        /// Gets skipgram (first and last character of trigram) counts from the corpus.
        /// </summary>
        public Grams GetSkipgrams()
        {
            if (_skipgrams == null && IsLoaded)
            {
                if (_trigrams == null)
                {
                    ComputeAllGrams();
                }
                ComputeSkipgrams();
            }
            return _skipgrams ?? new Grams();
        }

        /// <summary>
        /// Gets word counts from the corpus.
        /// </summary>
        public Grams GetWords()
        {
            if (_words == null && IsLoaded)
            {
                ComputeAllGrams();
            }
            return _words ?? new Grams();
        }

        /// <summary>
        /// Gets the total count of monograms (number of characters).
        /// </summary>
        public long GetMonogramTotal()
        {
            return GetMonograms().Total;
        }

        /// <summary>
        /// Gets the total count of bigrams.
        /// </summary>
        public long GetBigramTotal()
        {
            return GetBigrams().Total;
        }

        /// <summary>
        /// Gets the total count of trigrams.
        /// </summary>
        public long GetTrigramTotal()
        {
            return GetTrigrams().Total;
        }

        /// <summary>
        /// Gets the total count of skipgrams.
        /// </summary>
        public long GetSkipgramTotal()
        {
            return GetSkipgrams().Total;
        }

        /// <summary>
        /// Gets the total count of words.
        /// </summary>
        public long GetWordTotal()
        {
            return GetWords().Total;
        }

        /// <summary>
        /// Clears cached n-gram data to free memory.
        /// </summary>
        public void ClearCache()
        {
            _monograms = null;
            _bigrams = null;
            _trigrams = null;
            _skipgrams = null;
            _words = null;
        }
    }
}

