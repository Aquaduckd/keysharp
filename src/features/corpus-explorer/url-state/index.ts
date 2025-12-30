// Public API for URL state management
// This is the only entry point for consumers

import { serializeState } from './serializer';
import { deserializeState } from './deserializer';
import type { CorpusExplorerState } from '../types';

export interface URLState {
  corpus?: string; // Preset corpus name or 'custom'
  type?: string; // N-gram type
  limit?: string; // Limit number
  search?: string; // Search query
  regex?: string; // 'true' or 'false'
  whitespace?: string; // 'true' or 'false'
  punct?: string; // 'true' or 'false'
  case?: string; // 'true' or 'false' (caseSensitive)
}

/**
 * Serialize corpus explorer state to URL query parameters.
 * Only includes non-default values to keep URLs shorter.
 */
export function serializeToURL(state: Partial<CorpusExplorerState>): URLSearchParams {
  return serializeState(state);
}

/**
 * Deserialize URL query parameters to corpus explorer state.
 * Returns partial state that can be merged with defaults.
 */
export function deserializeFromURL(params: URLSearchParams): Partial<CorpusExplorerState> {
  return deserializeState(params);
}

/**
 * Update the browser URL with the current state without reloading the page.
 */
export function updateURL(state: Partial<CorpusExplorerState>): void {
  const params = serializeToURL(state);
  const newURL = params.toString() 
    ? `${window.location.pathname}?${params.toString()}`
    : window.location.pathname;
  window.history.pushState({}, '', newURL);
}

/**
 * Get current URL state from the browser.
 */
export function getCurrentURLState(): Partial<CorpusExplorerState> {
  const params = new URLSearchParams(window.location.search);
  return deserializeFromURL(params);
}

