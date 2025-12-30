// Deserialize URL query parameters to state

import type { CorpusExplorerState, NGramType, SearchSettings, FilterSettings } from '../types';

/**
 * Deserialize URL query parameters to corpus explorer state.
 * Returns partial state that can be merged with defaults.
 */
export function deserializeState(params: URLSearchParams): Partial<CorpusExplorerState> {
  const partialState: Partial<CorpusExplorerState> = {};

  // Corpus: if present, we'll need to load it (but can't store the text in URL)
  const corpusParam = params.get('corpus');
  if (corpusParam) {
    if (corpusParam === 'custom') {
      // Custom files can't be restored from URL, but we can mark it
      // The user will need to upload again
      partialState.corpus = null; // Will be handled by the caller
    } else {
      // Preset corpus name
      partialState.corpus = {
        name: corpusParam,
        text: '', // Will be loaded by the caller
        isCustom: false,
      };
    }
  }

  // N-gram type
  const typeParam = params.get('type');
  if (typeParam) {
    const validTypes: NGramType[] = ['monograms', 'bigrams', 'trigrams', 'words'];
    if (validTypes.includes(typeParam as NGramType)) {
      partialState.selectedNGramType = typeParam as NGramType;
    }
  }

  // Search settings
  const searchSettings: Partial<SearchSettings> = {};
  const filters: Partial<FilterSettings> = {};

  // Limit
  const limitParam = params.get('limit');
  if (limitParam) {
    const limit = parseInt(limitParam, 10);
    if (!isNaN(limit) && limit > 0) {
      searchSettings.limit = limit;
    }
  }

  // Search query
  const searchParam = params.get('search');
  if (searchParam !== null) {
    searchSettings.query = searchParam;
  }

  // Regex
  const regexParam = params.get('regex');
  if (regexParam === 'true') {
    searchSettings.useRegex = true;
  }

  // Filters
  // Whitespace: default is true, so 'false' means filterWhitespace: false
  const whitespaceParam = params.get('whitespace');
  if (whitespaceParam === 'false') {
    filters.filterWhitespace = false;
  }

  // Punctuation: default is false, so 'true' means filterPunctuation: true
  const punctParam = params.get('punct');
  if (punctParam === 'true') {
    filters.filterPunctuation = true;
  }

  // Case: default is false (case-insensitive), so 'true' means caseSensitive: true
  const caseParam = params.get('case');
  if (caseParam === 'true') {
    filters.caseSensitive = true;
  }

  // Only include search settings if there are any
  if (Object.keys(searchSettings).length > 0 || Object.keys(filters).length > 0) {
    partialState.search = {
      query: searchSettings.query || '',
      useRegex: searchSettings.useRegex || false,
      limit: searchSettings.limit || 0,
      filters: {
        filterWhitespace: filters.filterWhitespace !== undefined ? filters.filterWhitespace : true,
        filterPunctuation: filters.filterPunctuation !== undefined ? filters.filterPunctuation : false,
        caseSensitive: filters.caseSensitive !== undefined ? filters.caseSensitive : false,
      },
    } as SearchSettings;
  }

  return partialState;
}

