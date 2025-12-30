// Serialize state to URL query parameters

import type { CorpusExplorerState, NGramType } from '../types';

/**
 * Serialize corpus explorer state to URL query parameters.
 * Only includes non-default values to keep URLs shorter.
 */
export function serializeState(state: Partial<CorpusExplorerState>): URLSearchParams {
  const params = new URLSearchParams();

  // Corpus: only include if a corpus is loaded
  if (state.corpus) {
    if (state.corpus.isCustom) {
      params.set('corpus', 'custom');
    } else {
      params.set('corpus', state.corpus.name);
    }
  }

  // N-gram type: only include if not default ('bigrams')
  if (state.selectedNGramType && state.selectedNGramType !== 'bigrams') {
    params.set('type', state.selectedNGramType);
  }

  // Search settings
  if (state.search) {
    // Limit: only include if > 0
    if (state.search.limit > 0) {
      params.set('limit', state.search.limit.toString());
    }

    // Search query: only include if not empty
    if (state.search.query) {
      params.set('search', state.search.query);
    }

    // Regex: only include if true (default is false)
    if (state.search.useRegex) {
      params.set('regex', 'true');
    }

    // Filters
    if (state.search.filters) {
      // Whitespace: only include if false (default is true)
      if (!state.search.filters.filterWhitespace) {
        params.set('whitespace', 'false');
      }

      // Punctuation: only include if true (default is false)
      if (state.search.filters.filterPunctuation) {
        params.set('punct', 'true');
      }

      // Case: only include if true (default is false, meaning case-insensitive)
      if (state.search.filters.caseSensitive) {
        params.set('case', 'true');
      }
    }
  }

  return params;
}

