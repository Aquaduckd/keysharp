// N-gram analysis (monograms, bigrams, trigrams, and words)

import type { NGramData, FilterSettings } from '../types';

interface RawNGramCounts {
  monograms: Map<string, number>;
  bigrams: Map<string, number>;
  trigrams: Map<string, number>;
  skipgrams: Map<string, number>;
  words: Map<string, number>;
}

/**
 * Extended Map structure that includes the total count.
 * This allows totals to be calculated once during filtering/reaggregation.
 */
interface CountMap {
  map: Map<string, number>;
  total: number;
}

/**
 * Check if a character is whitespace (word boundary).
 */
function isWhitespace(char: string): boolean {
  return /\s/.test(char);
}

/**
 * Calculate all n-grams and words in a single pass through the text.
 * Returns raw counts before filtering.
 */
export function calculateNGrams(text: string): RawNGramCounts {
  const monogramCounts = new Map<string, number>();
  const bigramCounts = new Map<string, number>();
  const trigramCounts = new Map<string, number>();
  const skipgramCounts = new Map<string, number>();
  const wordCounts = new Map<string, number>();

  let currentWord = '';

  // Single pass through the text
  for (let i = 0; i < text.length; i++) {
    const char = text[i];

    // Count monogram
    monogramCounts.set(char, (monogramCounts.get(char) || 0) + 1);

    // Count bigram if available
    if (i + 1 < text.length) {
      const bigram = text[i] + text[i + 1];
      bigramCounts.set(bigram, (bigramCounts.get(bigram) || 0) + 1);
    }

    // Count trigram if available
    if (i + 2 < text.length) {
      const trigram = text[i] + text[i + 1] + text[i + 2];
      trigramCounts.set(trigram, (trigramCounts.get(trigram) || 0) + 1);
      
      // Count skipgram (first and last character of trigram)
      const skipgram = text[i] + text[i + 2];
      skipgramCounts.set(skipgram, (skipgramCounts.get(skipgram) || 0) + 1);
    }

    // Extract words: split by whitespace (words can contain punctuation)
    if (isWhitespace(char)) {
      // Hit whitespace, add current word if it's not empty
      if (currentWord.length > 0) {
        wordCounts.set(currentWord, (wordCounts.get(currentWord) || 0) + 1);
        currentWord = '';
      }
    } else {
      // Add any non-whitespace character to current word
      currentWord += char;
    }
  }

  // Add final word if text doesn't end with whitespace
  if (currentWord.length > 0) {
    wordCounts.set(currentWord, (wordCounts.get(currentWord) || 0) + 1);
  }

  return {
    monograms: monogramCounts,
    bigrams: bigramCounts,
    trigrams: trigramCounts,
    skipgrams: skipgramCounts,
    words: wordCounts,
  };
}

/**
 * Check if a sequence contains whitespace characters.
 */
function containsWhitespace(sequence: string): boolean {
  return /\s/.test(sequence);
}

/**
 * Check if a sequence contains punctuation characters.
 */
function containsPunctuation(sequence: string): boolean {
  return /[^\w\s]/.test(sequence);
}

/**
 * Apply case sensitivity normalization if needed.
 */
function normalizeSequence(sequence: string, caseSensitive: boolean): string {
  return caseSensitive ? sequence : sequence.toLowerCase();
}

/**
 * Filter and reaggregate n-gram counts.
 * Discards sequences with whitespace if filterWhitespace is enabled.
 * Discards sequences with punctuation if filterPunctuation is enabled.
 * Returns a CountMap with both the filtered map and the total count.
 */
function filterAndReaggregate(
  counts: Map<string, number>,
  filters: FilterSettings
): CountMap {
  const filtered = new Map<string, number>();
  let total = 0;

  for (const [sequence, frequency] of counts.entries()) {
    // Discard sequences containing whitespace if filterWhitespace is enabled
    if (filters.filterWhitespace && containsWhitespace(sequence)) {
      continue;
    }

    // Discard sequences containing punctuation if filterPunctuation is enabled
    if (filters.filterPunctuation && containsPunctuation(sequence)) {
      continue;
    }

    // Normalize case if needed
    const normalized = normalizeSequence(sequence, filters.caseSensitive);

    // Reaggregate (combine counts for sequences that normalize to the same value)
    const existingCount = filtered.get(normalized) || 0;
    filtered.set(normalized, existingCount + frequency);
    total += frequency;
  }

  return { map: filtered, total };
}

/**
 * Convert CountMap to sorted array with ranks.
 */
function mapToRankedArray(countMap: CountMap): NGramData[] {
  const entries = Array.from(countMap.map.entries());
  
  // Sort by frequency (descending), then by sequence (ascending) for ties
  entries.sort((a, b) => {
    if (b[1] !== a[1]) {
      return b[1] - a[1]; // Higher frequency first
    }
    return a[0].localeCompare(b[0]); // Alphabetical for ties
  });

  // Add ranks
  return entries.map(([sequence, frequency], index) => ({
    sequence,
    frequency,
    rank: index + 1,
  }));
}


/**
 * Calculate all n-grams and words, apply filters, and return ranked results.
 */
export function calculateFilteredNGrams(
  text: string,
  filters: FilterSettings
): {
  monograms: NGramData[];
  bigrams: NGramData[];
  trigrams: NGramData[];
  skipgrams: NGramData[];
  words: NGramData[];
  totals: {
    monograms: number;
    bigrams: number;
    trigrams: number;
    skipgrams: number;
    words: number;
  };
} {
  // First, calculate all n-grams and words in one pass
  const rawCounts = calculateNGrams(text);

  // Then filter and reaggregate (returns CountMap with totals)
  const filteredMonograms = filterAndReaggregate(rawCounts.monograms, filters);
  const filteredBigrams = filterAndReaggregate(rawCounts.bigrams, filters);
  const filteredTrigrams = filterAndReaggregate(rawCounts.trigrams, filters);
  const filteredSkipgrams = filterAndReaggregate(rawCounts.skipgrams, filters);
  const filteredWords = filterAndReaggregate(rawCounts.words, filters);

  // Extract totals from CountMap structures
  const totals = {
    monograms: filteredMonograms.total,
    bigrams: filteredBigrams.total,
    trigrams: filteredTrigrams.total,
    skipgrams: filteredSkipgrams.total,
    words: filteredWords.total,
  };

  // Convert to ranked arrays
  return {
    monograms: mapToRankedArray(filteredMonograms),
    bigrams: mapToRankedArray(filteredBigrams),
    trigrams: mapToRankedArray(filteredTrigrams),
    skipgrams: mapToRankedArray(filteredSkipgrams),
    words: mapToRankedArray(filteredWords),
    totals,
  };
}

