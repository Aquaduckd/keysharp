// Internal types for the corpus-explorer feature

export interface CorpusData {
  name: string;
  text: string;
  isCustom: boolean;
}

export interface FilterSettings {
  filterWhitespace: boolean;
  filterPunctuation: boolean;
  caseSensitive: boolean;
}

export interface SearchSettings {
  query: string;
  useRegex: boolean;
  filters: FilterSettings;
  limit: number; // Limit number of results displayed (0 = no limit)
}

export interface NGramData {
  sequence: string;
  frequency: number;
  rank: number;
}

export interface AnalysisResults {
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
}

export type NGramType = 'monograms' | 'bigrams' | 'trigrams' | 'skipgrams' | 'words';

export interface CorpusExplorerState {
  corpus: CorpusData | null;
  search: SearchSettings;
  analysis: AnalysisResults | null;
  selectedNGramType: NGramType;
}

