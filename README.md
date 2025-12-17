# Keysharp

A keyboard layout analysis tool for analyzing text corpora and computing n-gram statistics.

## Features

- **N-gram Analysis**: Analyze character monograms, bigrams, trigrams, and words
- **Corpus Loading**: Load and analyze text files (.txt format)
- **Interactive Table View**: View n-gram frequencies, counts, and rankings in a sortable table
- **Filtering**: Search and filter n-grams with regular expression support
- **Result Limiting**: Limit the number of displayed results
- **Export**: Export analysis results to CSV format
- **Modern UI**: Built with Raylib for a clean, responsive interface

## Requirements

- .NET 6.0 SDK or later
- DejaVu Sans font (optional, falls back to default font if not found)

## Building

```bash
dotnet build
```

## Running

```bash
dotnet run
```

## Usage

1. **Load a Corpus**: Click "Load Corpus" or select a corpus file from the dropdown (files should be placed in the `corpus/` directory)

2. **Select N-gram Type**: Choose between Monogram, Bigram, Trigram, or Words from the dropdown

3. **Filter Results** (optional):
   - Enter text in the search box to filter n-grams
   - Toggle "Regex" mode for regular expression matching
   - Use the "Limit" field to restrict the number of results displayed

4. **View Statistics**: The table displays:
   - **Rank**: Position in the filtered results
   - **N-gram**: The sequence being analyzed
   - **Frequency**: Percentage of total occurrences
   - **Count**: Total number of occurrences
   - **Global Rank** (when filtered): Rank across all n-grams
   - **Relative Frequency** (when filtered): Frequency within the filtered set

5. **Export Results**: Click "Save to CSV" to export the current table data to a CSV file

## Keyboard Shortcuts

- **F3**: Toggle debug overlay (shows UI element boundaries)

## Project Structure

```
src/
├── Components/          # UI components (Button, Table, Dropdown, etc.)
├── UI/                  # UI panels and layout management
├── Corpus.cs           # Corpus loading and n-gram computation
├── Grams.cs            # N-gram count and frequency calculations
├── Program.cs          # Main entry point
└── ...
```

## Corpus Files

Place `.txt` files in the `corpus/` directory to make them available for analysis. The application will automatically detect and list available corpus files.

## License

[Add your license here]

