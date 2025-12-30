// Analysis results display (tables for n-grams and words)

import type { AnalysisResults, NGramData, NGramType, SearchSettings, FilterSettings } from '../types';
import { applyTableStyles } from '../styles';
import { matchesSearch } from '../search/matcher';
import { createSearchPanel } from './search';

/**
 * Get display name for n-gram type.
 */
function getNGramTypeLabel(type: NGramType): string {
  switch (type) {
    case 'monograms':
      return 'Monograms';
    case 'bigrams':
      return 'Bigrams';
    case 'trigrams':
      return 'Trigrams';
    case 'words':
      return 'Words';
  }
}

/**
 * Get column header name for sequence column based on type.
 */
function getSequenceColumnLabel(type: NGramType): string {
  switch (type) {
    case 'monograms':
      return 'Monogram';
    case 'bigrams':
      return 'Bigram';
    case 'trigrams':
      return 'Trigram';
    case 'words':
      return 'Word';
  }
}

/**
 * Create a single table row for an n-gram.
 */
function createTableRow(
  ngram: NGramData,
  type: NGramType,
  total: number
): HTMLTableRowElement {
  const row = document.createElement('tr');
  row.style.borderBottom = '1px solid #eee';
  row.style.height = '40px';

  // Rank cell
  const rankCell = document.createElement('td');
  rankCell.textContent = ngram.rank.toString();
  rankCell.style.padding = '8px';
  rankCell.style.textAlign = 'left';
  row.appendChild(rankCell);

  // Sequence cell
  const sequenceCell = document.createElement('td');
  // Add quotes around all sequences for consistency
  const displaySequence = `"${ngram.sequence}"`;
  sequenceCell.textContent = displaySequence;
  sequenceCell.style.padding = '8px';
  sequenceCell.style.fontFamily = 'monospace';
  row.appendChild(sequenceCell);

  // Count cell
  const countCell = document.createElement('td');
  countCell.textContent = ngram.frequency.toLocaleString();
  countCell.style.padding = '8px';
  countCell.style.textAlign = 'right';
  row.appendChild(countCell);

  // Frequency cell (percentage) - use pre-calculated total
  const frequencyCell = document.createElement('td');
  const frequencyPercent = total > 0 
    ? ((ngram.frequency / total) * 100).toFixed(3)
    : '0.000';
  frequencyCell.textContent = `${frequencyPercent}%`;
  frequencyCell.style.padding = '8px';
  frequencyCell.style.textAlign = 'right';
  row.appendChild(frequencyCell);

  return row;
}

/**
 * Create a spacer row to maintain scroll height in virtual scrolling.
 */
function createSpacerRow(height: number): HTMLTableRowElement {
  const spacer = document.createElement('tr');
  spacer.style.height = `${height}px`;
  spacer.style.display = 'table-row';
  const spacerCell = document.createElement('td');
  spacerCell.colSpan = 4;
  spacerCell.style.height = `${height}px`;
  spacerCell.style.padding = '0';
  spacerCell.style.margin = '0';
  spacer.appendChild(spacerCell);
  return spacer;
}

/**
 * Create a scrollable n-gram table with virtual scrolling.
 * Only renders visible rows to improve performance with large datasets.
 */
export function createNGramTable(
  container: HTMLElement,
  ngrams: NGramData[],
  type: NGramType,
  total: number
): void {
  // Clear container
  container.innerHTML = '';

  const ROW_HEIGHT = 40;
  const BUFFER = 10; // Render 10 extra rows above/below visible area

  // Create scrollable wrapper
  const scrollWrapper = document.createElement('div');
  scrollWrapper.style.overflowY = 'auto';
  scrollWrapper.style.height = '100%';
  scrollWrapper.style.maxHeight = '100%';
  scrollWrapper.style.width = '100%';
  scrollWrapper.style.position = 'relative';
  scrollWrapper.id = 'ngram-table-scroll-wrapper';

  // Create table
  const table = document.createElement('table');
  applyTableStyles(table);
  table.style.tableLayout = 'fixed';
  table.style.width = '100%';

  // Create header
  const thead = document.createElement('thead');
  const headerRow = document.createElement('tr');
  
  const rankHeader = document.createElement('th');
  rankHeader.textContent = 'Rank';
  rankHeader.style.textAlign = 'left';
  rankHeader.style.padding = '8px';
  rankHeader.style.borderBottom = '2px solid #ccc';
  rankHeader.style.width = '80px';
  rankHeader.style.position = 'sticky';
  rankHeader.style.top = '0';
  rankHeader.style.backgroundColor = '#f8f8f8';
  rankHeader.style.zIndex = '10';
  
  const sequenceHeader = document.createElement('th');
  sequenceHeader.textContent = getSequenceColumnLabel(type);
  sequenceHeader.style.textAlign = 'left';
  sequenceHeader.style.padding = '8px';
  sequenceHeader.style.borderBottom = '2px solid #ccc';
  sequenceHeader.style.position = 'sticky';
  sequenceHeader.style.top = '0';
  sequenceHeader.style.backgroundColor = '#f8f8f8';
  sequenceHeader.style.zIndex = '10';
  
  const countHeader = document.createElement('th');
  countHeader.textContent = 'Counts';
  countHeader.style.textAlign = 'right';
  countHeader.style.padding = '8px';
  countHeader.style.borderBottom = '2px solid #ccc';
  countHeader.style.width = '120px';
  countHeader.style.position = 'sticky';
  countHeader.style.top = '0';
  countHeader.style.backgroundColor = '#f8f8f8';
  countHeader.style.zIndex = '10';

  const frequencyHeader = document.createElement('th');
  frequencyHeader.textContent = 'Frequency';
  frequencyHeader.style.textAlign = 'right';
  frequencyHeader.style.padding = '8px';
  frequencyHeader.style.borderBottom = '2px solid #ccc';
  frequencyHeader.style.width = '120px';
  frequencyHeader.style.position = 'sticky';
  frequencyHeader.style.top = '0';
  frequencyHeader.style.backgroundColor = '#f8f8f8';
  frequencyHeader.style.zIndex = '10';

  headerRow.appendChild(rankHeader);
  headerRow.appendChild(sequenceHeader);
  headerRow.appendChild(countHeader);
  headerRow.appendChild(frequencyHeader);
  thead.appendChild(headerRow);
  table.appendChild(thead);

  // Create tbody for virtual scrolling
  const tbody = document.createElement('tbody');
  tbody.id = 'ngram-table-body';
  table.appendChild(tbody);

  scrollWrapper.appendChild(table);
  container.appendChild(scrollWrapper);

  // Function to update visible rows
  let scrollTimeout: number | null = null;
  const updateVisibleRows = () => {
    const scrollTop = scrollWrapper.scrollTop;
    let containerHeight = scrollWrapper.clientHeight;
    
    // Fallback: if clientHeight is 0, try to get it from parent or use a default
    if (containerHeight === 0) {
      containerHeight = scrollWrapper.parentElement?.clientHeight || 400;
      // Set explicit height if we had to use fallback
      if (scrollWrapper.parentElement) {
        scrollWrapper.style.height = `${containerHeight}px`;
      }
    }
    
    const visibleCount = Math.ceil(containerHeight / ROW_HEIGHT);
    
    // Calculate visible range with buffer
    const startIndex = Math.max(0, Math.floor(scrollTop / ROW_HEIGHT) - BUFFER);
    const endIndex = Math.min(ngrams.length, startIndex + visibleCount + BUFFER * 2);
    
    // Clear tbody
    tbody.innerHTML = '';
    
    // Add top spacer
    if (startIndex > 0) {
      const topSpacer = createSpacerRow(startIndex * ROW_HEIGHT);
      tbody.appendChild(topSpacer);
    }
    
    // Add visible rows
    for (let i = startIndex; i < endIndex; i++) {
      const row = createTableRow(ngrams[i], type, total);
      tbody.appendChild(row);
    }
    
    // Add bottom spacer
    if (endIndex < ngrams.length) {
      const bottomSpacer = createSpacerRow((ngrams.length - endIndex) * ROW_HEIGHT);
      tbody.appendChild(bottomSpacer);
    }
  };

  // Throttled scroll handler
  const handleScroll = () => {
    if (scrollTimeout !== null) {
      cancelAnimationFrame(scrollTimeout);
    }
    scrollTimeout = requestAnimationFrame(updateVisibleRows);
  };

  scrollWrapper.addEventListener('scroll', handleScroll);

  // Initial render
  updateVisibleRows();
}


/**
 * Create n-gram type selector dropdown with inline filter checkboxes.
 */
export function createNGramSelector(
  container: HTMLElement,
  currentType: NGramType,
  onTypeChange: (type: NGramType) => void,
  filters?: FilterSettings,
  onFilterChange?: (filters: FilterSettings) => void
): void {
  const selectorContainer = document.createElement('div');
  selectorContainer.style.display = 'flex';
  selectorContainer.style.alignItems = 'center';
  selectorContainer.style.gap = '10px';
  selectorContainer.style.marginBottom = '15px';
  selectorContainer.style.justifyContent = 'space-between';

  const leftSection = document.createElement('div');
  leftSection.style.display = 'flex';
  leftSection.style.alignItems = 'center';
  leftSection.style.gap = '10px';

  const label = document.createElement('label');
  label.textContent = 'N-Gram Type:';
  label.style.fontWeight = 'bold';
  leftSection.appendChild(label);

  const select = document.createElement('select');
  select.id = 'ngram-type-selector';
  select.style.padding = '6px 12px';
  select.style.minWidth = '150px';

  const options: { value: NGramType; label: string }[] = [
    { value: 'monograms', label: 'Monograms' },
    { value: 'bigrams', label: 'Bigrams' },
    { value: 'trigrams', label: 'Trigrams' },
    { value: 'words', label: 'Words' },
  ];

  for (const option of options) {
    const optionElement = document.createElement('option');
    optionElement.value = option.value;
    optionElement.textContent = option.label;
    select.appendChild(optionElement);
  }

  // Set the value AFTER options are added
  select.value = currentType;

  select.addEventListener('change', () => {
    onTypeChange(select.value as NGramType);
  });

  leftSection.appendChild(select);
  selectorContainer.appendChild(leftSection);

  // Add filter checkboxes on the right if filters are provided
  if (filters && onFilterChange) {
    const rightSection = document.createElement('div');
    rightSection.style.display = 'flex';
    rightSection.style.alignItems = 'center';
    rightSection.style.gap = '15px';

    // Filter Whitespace checkbox
    const whitespaceCheckbox = document.createElement('input');
    whitespaceCheckbox.type = 'checkbox';
    whitespaceCheckbox.id = 'filter-whitespace';
    whitespaceCheckbox.checked = filters.filterWhitespace;
    whitespaceCheckbox.addEventListener('change', () => {
      onFilterChange({
        ...filters,
        filterWhitespace: whitespaceCheckbox.checked,
      });
    });

    const whitespaceLabel = document.createElement('label');
    whitespaceLabel.htmlFor = 'filter-whitespace';
    whitespaceLabel.textContent = 'Filter Whitespace';
    whitespaceLabel.style.cursor = 'pointer';

    // Filter Punctuation checkbox
    const punctuationCheckbox = document.createElement('input');
    punctuationCheckbox.type = 'checkbox';
    punctuationCheckbox.id = 'filter-punctuation';
    punctuationCheckbox.checked = filters.filterPunctuation;
    punctuationCheckbox.addEventListener('change', () => {
      onFilterChange({
        ...filters,
        filterPunctuation: punctuationCheckbox.checked,
      });
    });

    const punctuationLabel = document.createElement('label');
    punctuationLabel.htmlFor = 'filter-punctuation';
    punctuationLabel.textContent = 'Filter Punctuation';
    punctuationLabel.style.cursor = 'pointer';

    // Collapse Case checkbox (inverted: checked = caseSensitive: false)
    const caseCheckbox = document.createElement('input');
    caseCheckbox.type = 'checkbox';
    caseCheckbox.id = 'collapse-case';
    caseCheckbox.checked = !filters.caseSensitive; // Inverted: checked means collapse (caseSensitive: false)
    caseCheckbox.addEventListener('change', () => {
      onFilterChange({
        ...filters,
        caseSensitive: !caseCheckbox.checked, // Inverted: checked = false, unchecked = true
      });
    });

    const caseLabel = document.createElement('label');
    caseLabel.htmlFor = 'collapse-case';
    caseLabel.textContent = 'Collapse Case';
    caseLabel.style.cursor = 'pointer';

    rightSection.appendChild(whitespaceCheckbox);
    rightSection.appendChild(whitespaceLabel);
    rightSection.appendChild(punctuationCheckbox);
    rightSection.appendChild(punctuationLabel);
    rightSection.appendChild(caseCheckbox);
    rightSection.appendChild(caseLabel);

    selectorContainer.appendChild(rightSection);
  }

  container.appendChild(selectorContainer);
}

export function createAnalysisDisplay(
  container: HTMLElement,
  results: AnalysisResults | null,
  selectedType: NGramType,
  onTypeChange: (type: NGramType) => void,
  searchSettings?: SearchSettings,
  filters?: FilterSettings,
  onFilterChange?: (filters: FilterSettings) => void,
  onSearchChange?: (settings: SearchSettings) => void
): void {
  // Preserve focus information before clearing
  const activeElement = document.activeElement;
  let focusInfo: { id?: string; selectionStart?: number; selectionEnd?: number } | null = null;
  
  if (activeElement && container.contains(activeElement)) {
    if (activeElement instanceof HTMLInputElement) {
      focusInfo = {
        id: activeElement.id || undefined,
        selectionStart: activeElement.selectionStart || undefined,
        selectionEnd: activeElement.selectionEnd || undefined,
      };
    } else if (activeElement.id) {
      focusInfo = { id: activeElement.id };
    }
  }

  container.innerHTML = '';
  container.style.display = 'flex';
  container.style.flexDirection = 'column';
  container.style.height = '100%';
  container.style.overflow = 'hidden';
  
  if (!results) {
    container.innerHTML = '<p>No analysis results available. Load a corpus to begin.</p>';
    return;
  }

  // Create n-gram type selector with inline filter checkboxes
  createNGramSelector(container, selectedType, onTypeChange, filters, onFilterChange);

  // Create search panel if search settings are provided
  // We'll update it later with stats after calculating them
  if (searchSettings) {
    const searchContainer = document.createElement('div');
    searchContainer.id = 'search-container';
    searchContainer.style.marginBottom = '15px';
    // Create initially without stats (will be updated after data processing)
    createSearchPanel(searchContainer, searchSettings, (settings: SearchSettings) => {
      if (onSearchChange) {
        onSearchChange(settings);
      }
    });
    container.appendChild(searchContainer);
  }

  // Create table section
  const tableSection = document.createElement('div');
  tableSection.style.flex = '1';
  tableSection.style.display = 'flex';
  tableSection.style.flexDirection = 'column';
  tableSection.style.minHeight = '0'; // Important for flex children to shrink

  const tableContainer = document.createElement('div');
  tableContainer.style.flex = '1';
  tableContainer.style.minHeight = '0'; // Important for flex children to shrink
  tableContainer.style.display = 'flex';
  tableContainer.style.flexDirection = 'column';
  tableContainer.style.overflow = 'hidden';
  
  // Get the appropriate data and total based on selected type
  let ngramData: NGramData[];
  let total: number;
  if (selectedType === 'words') {
    ngramData = results.words;
    total = results.totals.words;
  } else {
    ngramData = results[selectedType];
    total = results.totals[selectedType];
  }

  // Calculate pre-search/limit sum of counts (after whitespace/punctuation/case filters, before search/limit)
  const preFilterCount = ngramData.reduce((sum, ngram) => sum + ngram.frequency, 0);

  // Apply search filter if search settings are provided
  if (searchSettings && searchSettings.query) {
    ngramData = ngramData.filter(ngram => 
      matchesSearch(ngram.sequence, searchSettings!)
    );
    // Recalculate ranks after filtering
    ngramData = ngramData.map((ngram, index) => ({
      ...ngram,
      rank: index + 1,
    }));
  }

  // Apply limit if search settings are provided and limit is set
  if (searchSettings && searchSettings.limit > 0) {
    ngramData = ngramData.slice(0, searchSettings.limit);
  }
  
  // Calculate displayed sum of counts (after search and limit)
  const displayedCount = ngramData.reduce((sum, ngram) => sum + ngram.frequency, 0);
  const percentage = preFilterCount > 0 ? (displayedCount / preFilterCount) * 100 : 0;

  // Update stats display in search panel if it exists
  // Total is always shown, Displayed and Percentage are only shown when filters are applied
  const hasFilters = searchSettings && (searchSettings.query || (searchSettings.limit > 0));
  
  if (searchSettings) {
    const searchInputContainer = container.querySelector('#search-input-container') as HTMLElement;
    if (searchInputContainer) {
      let statsContainer = container.querySelector('#stats-container') as HTMLElement;
      
      if (!statsContainer) {
        // Create stats container if it doesn't exist
        statsContainer = document.createElement('div');
        statsContainer.id = 'stats-container';
        statsContainer.style.display = 'flex';
        statsContainer.style.alignItems = 'center';
        statsContainer.style.gap = '15px';

        const finalCountSpan = document.createElement('span');
        finalCountSpan.id = 'final-count';
        finalCountSpan.style.fontSize = '14px';
        statsContainer.appendChild(finalCountSpan);

        const preFilterCountSpan = document.createElement('span');
        preFilterCountSpan.id = 'pre-filter-count';
        preFilterCountSpan.style.fontSize = '14px';
        statsContainer.appendChild(preFilterCountSpan);

        const percentageSpan = document.createElement('span');
        percentageSpan.id = 'percentage';
        percentageSpan.style.fontSize = '14px';
        percentageSpan.style.fontWeight = 'bold';
        statsContainer.appendChild(percentageSpan);

        searchInputContainer.appendChild(statsContainer);
      }
      
      // Always show stats container and update Total
      statsContainer.style.display = 'flex';
      const finalCountSpan = statsContainer.querySelector('#final-count') as HTMLElement;
      const preFilterCountSpan = statsContainer.querySelector('#pre-filter-count') as HTMLElement;
      const percentageSpan = statsContainer.querySelector('#percentage') as HTMLElement;
      
      // Always update and show Total
      if (preFilterCountSpan) {
        preFilterCountSpan.textContent = `Total: ${preFilterCount.toLocaleString()}`;
        preFilterCountSpan.style.display = 'inline';
      }
      
      // Conditionally show/hide Displayed and Percentage
      if (hasFilters) {
        if (finalCountSpan) {
          finalCountSpan.textContent = `Displayed: ${displayedCount.toLocaleString()}`;
          finalCountSpan.style.display = 'inline';
        }
        if (percentageSpan) {
          percentageSpan.textContent = `${percentage.toFixed(3)}%`;
          percentageSpan.style.display = 'inline';
        }
      } else {
        if (finalCountSpan) {
          finalCountSpan.style.display = 'none';
        }
        if (percentageSpan) {
          percentageSpan.style.display = 'none';
        }
      }
    }
  }
  
  createNGramTable(tableContainer, ngramData, selectedType, total);
  tableSection.appendChild(tableContainer);

  container.appendChild(tableSection);

  // Restore focus after DOM is recreated
  if (focusInfo) {
    // Use requestAnimationFrame to ensure DOM is fully rendered
    requestAnimationFrame(() => {
      let elementToFocus: HTMLElement | null = null;
      
      if (focusInfo!.id) {
        elementToFocus = document.getElementById(focusInfo!.id) || 
                        container.querySelector(`#${focusInfo!.id}`) as HTMLElement;
      }
      
      if (elementToFocus instanceof HTMLInputElement) {
        elementToFocus.focus();
        if (focusInfo!.selectionStart !== undefined && focusInfo!.selectionEnd !== undefined) {
          elementToFocus.setSelectionRange(focusInfo!.selectionStart, focusInfo!.selectionEnd);
        }
      } else if (elementToFocus) {
        elementToFocus.focus();
      }
    });
  }
}

export function updateAnalysisDisplay(
  container: HTMLElement,
  results: AnalysisResults,
  selectedType: NGramType,
  onTypeChange: (type: NGramType) => void,
  searchSettings?: SearchSettings,
  filters?: FilterSettings,
  onFilterChange?: (filters: FilterSettings) => void,
  onSearchChange?: (settings: SearchSettings) => void
): void {
  createAnalysisDisplay(container, results, selectedType, onTypeChange, searchSettings, filters, onFilterChange, onSearchChange);
}

