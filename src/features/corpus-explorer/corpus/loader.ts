// Corpus loading logic (preset and custom files)

import type { CorpusLoader } from './types';

// List of available preset corpora
const PRESET_CORPORA = [
  'e200.txt',
  'e1k.txt',
  'e5k.txt',
  'e10k.txt',
  'e25k.txt',
  'e450k.txt',
  'mr.txt',
  'mt.txt',
  'tr.txt',
];

export function createCorpusLoader(): CorpusLoader {
  return {
    async loadPreset(name: string): Promise<string> {
      try {
        const response = await fetch(`./public/corpus/${name}`);
        if (!response.ok) {
          throw new Error(`Failed to load corpus: ${response.statusText}`);
        }
        return await response.text();
      } catch (error) {
        throw new Error(`Error loading preset corpus "${name}": ${error}`);
      }
    },

    async loadCustom(file: File): Promise<string> {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onload = (e) => {
          const text = e.target?.result;
          if (typeof text === 'string') {
            resolve(text);
          } else {
            reject(new Error('Failed to read file as text'));
          }
        };
        reader.onerror = () => {
          reject(new Error('Error reading file'));
        };
        reader.readAsText(file);
      });
    },

    async listPresets(): Promise<string[]> {
      // Return hardcoded list for now
      return [...PRESET_CORPORA];
    },
  };
}

