// Public API for the corpus-explorer feature
// This is the only entry point for consumers

import { createCorpusLoader } from './corpus/loader';
import { createControlsPanel } from './ui/controls';
import { createAnalysisDisplay, updateAnalysisDisplay } from './ui/analysis';
import { calculateFilteredNGrams } from './analysis/ngrams';
import { applyContainerStyles, applySectionStyles } from './styles';
import { exportToCSV } from './export/csv';
import { matchesSearch } from './search/matcher';
import { getCurrentURLState, updateURL } from './url-state';
import type { CorpusExplorerState, SearchSettings, AnalysisResults, NGramType, FilterSettings, NGramData } from './types';

let currentState: CorpusExplorerState = {
  corpus: null,
  search: {
    query: '',
    useRegex: false,
    filters: {
      filterWhitespace: true, // Checked by default
      filterPunctuation: false,
      caseSensitive: false, // Collapse Case checked by default (caseSensitive: false)
    },
    limit: 0, // 0 = no limit
  },
  analysis: null,
  selectedNGramType: 'bigrams',
};

let analysisSection: HTMLElement | null = null;
let controlsSection: HTMLElement | null = null;
let corpusLoader = createCorpusLoader();

/**
 * Handler for n-gram type changes.
 */
function handleNGramTypeChange(type: NGramType): void {
  currentState.selectedNGramType = type;
  updateURL(currentState);
  if (currentState.analysis && analysisSection) {
    updateAnalysisDisplay(analysisSection, currentState.analysis, currentState.selectedNGramType, handleNGramTypeChange, currentState.search, currentState.search.filters, handleFilterChange, handleSearchChange);
  }
}

/**
 * Handler for filter changes.
 */
function handleFilterChange(filters: FilterSettings): void {
  currentState.search.filters = filters;
  updateURL(currentState);
  // Filters affect n-gram calculation, so we need to recalculate
  performAnalysis();
}

/**
 * Handler for search changes.
 */
function handleSearchChange(settings: SearchSettings): void {
  currentState.search = {
    query: settings.query,
    useRegex: settings.useRegex,
    limit: settings.limit,
    filters: currentState.search.filters, // Preserve existing filters
  };
  updateURL(currentState);
  // If we have existing analysis, just update the display with search filter
  if (currentState.analysis && analysisSection) {
    updateAnalysisDisplay(analysisSection, currentState.analysis, currentState.selectedNGramType, handleNGramTypeChange, currentState.search, currentState.search.filters, handleFilterChange, handleSearchChange);
  }
}

/**
 * Function to handle CSV export.
 */
function handleExportCSV(): void {
  if (!currentState.analysis || !analysisSection) {
    return;
  }
  
  // Get the current displayed data (with search/limit filters applied)
  let ngramData: NGramData[];
  let total: number;
  
  if (currentState.selectedNGramType === 'words') {
    ngramData = currentState.analysis.words;
    total = currentState.analysis.totals.words;
  } else {
    ngramData = currentState.analysis[currentState.selectedNGramType];
    total = currentState.analysis.totals[currentState.selectedNGramType];
  }
  
  // Apply search filter if present
  if (currentState.search.query) {
    ngramData = ngramData.filter(ngram => 
      matchesSearch(ngram.sequence, currentState.search)
    );
    ngramData = ngramData.map((ngram, index) => ({
      ...ngram,
      rank: index + 1,
    }));
  }
  
  // Apply limit if present
  if (currentState.search.limit > 0) {
    ngramData = ngramData.slice(0, currentState.search.limit);
  }
  
  // Export to CSV
  exportToCSV(ngramData, currentState.selectedNGramType, total);
}

/**
 * Update the controls panel (to show/hide export button).
 */
function updateControlsPanel(): void {
  if (!controlsSection) return;
  
  // Preserve the selected preset corpus value before recreating
  const presetSelect = controlsSection.querySelector('#preset-corpus-select') as HTMLSelectElement;
  const selectedPreset = presetSelect?.value || '';
  
  // Determine what should be selected based on current corpus state
  let currentPreset: string | undefined;
  if (currentState.corpus) {
    if (currentState.corpus.isCustom) {
      currentPreset = '__custom__';
    } else {
      currentPreset = currentState.corpus.name;
    }
  } else if (selectedPreset) {
    currentPreset = selectedPreset;
  }
  
  createControlsPanel(
    controlsSection,
    corpusLoader,
    (text, name, isCustom) => {
      // Handle corpus load
      currentState.corpus = { name, text, isCustom: isCustom || false };
      updateURL(currentState);
      performAnalysis();
      updateControlsPanel(); // Update to show export button
    },
    handleExportCSV,
    currentState.analysis !== null, // Show button only if corpus is loaded
    currentPreset || undefined // Pass the selected preset to preserve it
  );
}

/**
 * Initializes the corpus explorer in the provided container element.
 * 
 * @param container - The DOM element that will contain the corpus explorer
 * @returns A controller for programmatic control (optional)
 */
export function initializeCorpusExplorer(container: HTMLElement): void {
  // Clear container and apply styles
  container.innerHTML = '';
  applyContainerStyles(container);

  // Create corpus loader
  corpusLoader = createCorpusLoader();

  // Load state from URL if present
  const urlState = getCurrentURLState();
  
  // Apply URL state to current state
  if (urlState.selectedNGramType) {
    currentState.selectedNGramType = urlState.selectedNGramType;
  }
  
  if (urlState.search) {
    currentState.search = {
      ...currentState.search,
      ...urlState.search,
      filters: {
        ...currentState.search.filters,
        ...urlState.search.filters,
      },
    };
  }

  // Create controls section
  controlsSection = document.createElement('div');
  controlsSection.id = 'corpus-controls';
  // No padding or border - removed
  updateControlsPanel();
  container.appendChild(controlsSection);

  // Create analysis section
  analysisSection = document.createElement('div');
  analysisSection.id = 'corpus-analysis';
  analysisSection.style.flex = '1';
  analysisSection.style.overflow = 'hidden'; // Changed from 'auto' - table handles its own scrolling
  analysisSection.style.borderTop = '1px solid #ddd'; // Top border for visual separation
  analysisSection.style.paddingTop = '20px'; // Padding only on top to separate from border
  analysisSection.style.display = 'flex';
  analysisSection.style.flexDirection = 'column';
  createAnalysisDisplay(analysisSection, null, currentState.selectedNGramType, handleNGramTypeChange, currentState.search, currentState.search.filters, handleFilterChange, handleSearchChange);
  container.appendChild(analysisSection);

  // Load corpus from URL if specified
  if (urlState.corpus && !urlState.corpus.isCustom) {
    // Load the preset corpus
    corpusLoader.loadPreset(urlState.corpus.name)
      .then((text) => {
        currentState.corpus = { name: urlState.corpus!.name, text, isCustom: false };
        performAnalysis();
        updateControlsPanel();
      })
      .catch((error) => {
        console.error('Failed to load corpus from URL:', error);
      });
  }
}

/**
 * Perform n-gram analysis on the current corpus with current filter settings.
 */
function performAnalysis(): void {
  if (!currentState.corpus || !analysisSection) {
    return;
  }

  // Calculate n-grams with filters applied
  const ngramResults = calculateFilteredNGrams(
    currentState.corpus.text,
    currentState.search.filters
  );

  // Create analysis results
  const results: AnalysisResults = {
    monograms: ngramResults.monograms,
    bigrams: ngramResults.bigrams,
    trigrams: ngramResults.trigrams,
    words: ngramResults.words,
    totals: ngramResults.totals,
  };

  currentState.analysis = results;
  updateAnalysisDisplay(analysisSection, results, currentState.selectedNGramType, handleNGramTypeChange, currentState.search, currentState.search.filters, handleFilterChange, handleSearchChange);
  
  // Update controls panel to show export button now that corpus is loaded
  updateControlsPanel();
}

// Re-export types that consumers might need
export type { CorpusExplorerState, SearchSettings } from './types';
