// CSV export functionality

import type { NGramData, NGramType } from '../types';

/**
 * Get the column header name for sequence column based on type.
 */
function getSequenceColumnLabel(type: NGramType): string {
  switch (type) {
    case 'monograms':
      return 'Monogram';
    case 'bigrams':
      return 'Bigram';
    case 'trigrams':
      return 'Trigram';
    case 'skipgrams':
      return 'Skipgram';
    case 'words':
      return 'Word';
  }
}

/**
 * Escape a CSV field value (handles quotes and commas).
 */
function escapeCSVField(value: string): string {
  // If value contains comma, quote, or newline, wrap in quotes and escape quotes
  if (value.includes(',') || value.includes('"') || value.includes('\n')) {
    return `"${value.replace(/"/g, '""')}"`;
  }
  return value;
}

/**
 * Export n-gram data to CSV file.
 */
export function exportToCSV(
  ngramData: NGramData[],
  type: NGramType,
  total: number
): void {
  // Create CSV content
  const lines: string[] = [];
  
  // Header row
  const sequenceLabel = getSequenceColumnLabel(type);
  lines.push(`Rank,${sequenceLabel},Counts,Frequency`);
  
  // Data rows
  for (const ngram of ngramData) {
    const sequence = escapeCSVField(`"${ngram.sequence}"`);
    const counts = ngram.frequency.toString();
    const frequency = total > 0 
      ? ((ngram.frequency / total) * 100).toFixed(3)
      : '0.000';
    
    lines.push(`${ngram.rank},${sequence},${counts},${frequency}%`);
  }
  
  // Create CSV content string
  const csvContent = lines.join('\n');
  
  // Create blob and download
  const blob = new Blob([csvContent], { type: 'text/csv;charset=utf-8;' });
  const link = document.createElement('a');
  const url = URL.createObjectURL(blob);
  
  link.setAttribute('href', url);
  link.setAttribute('download', `${type}_export.csv`);
  link.style.visibility = 'hidden';
  document.body.appendChild(link);
  link.click();
  document.body.removeChild(link);
  
  // Clean up
  URL.revokeObjectURL(url);
}

