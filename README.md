# Keysharp

A comprehensive keyboard layout analysis and design tool for analyzing text corpora, computing n-gram statistics, and designing optimal keyboard layouts with detailed metric analysis.

## Features

### Layout Editor

- **Visual Layout Designer**: Interactive keyboard layout editor with drag-and-drop key positioning
- **Load/Save Layouts**: Import and export keyboard layouts in JSON format
- **Key Management**:
  - Add new keys with default properties
  - Delete existing keys
  - Edit key properties: characters, colors, position, size, rotation, finger assignment
  - Key size constraints (minimum 0.25U)
  - Key rotation support (-90° to +90°)
- **Multi-Key Selection**:
  - Shift+Click to select multiple keys
  - Shift+Drag for continuous selection/deselection
  - Bulk editing of selected keys (position, size, finger, disable state)
- **Advanced Key Properties**:
  - Primary and shift characters
  - Auto Shift: Automatically converts primary characters to shifted equivalents (US QWERTY standard)
  - HSV color picker with scroll wheel increment/decrement
  - Key identifiers (custom names for each key)
  - Disable/enable keys
  - Finger assignment (Left/Right Pinky, Ring, Middle, Index, Thumb)
- **Visual Feedback**:
  - Selected keys rendered above unselected keys
  - Rotation support with visual rotation indicators
  - Color-coded keys based on finger assignment

### Corpus Analysis

- **N-gram Analysis**: Analyze character monograms, bigrams, trigrams, skipgrams, and words
- **Corpus Loading**: Load and analyze text files (.txt format)
- **Interactive Table View**: View n-gram frequencies, counts, rankings, key sequences, finger sequences, and metric matches
- **Advanced Filtering**:
  - Search and filter n-grams with regular expression support
  - Ignore whitespace (spaces, tabs, newlines, etc.)
  - Ignore modifier keys (Shift, Ctrl, Alt, etc.)
  - Ignore numbers and symbols
  - Ignore punctuation
  - Ignore disabled keys
  - Result limiting
- **Export**: Export analysis results to CSV format

### Metrics Analysis

- **Bigram Metrics**:
  - **SFB** (Same Finger Bigram): Two characters typed with the same finger on different keys
  - **LSB** (Lateral Stretch Bigram): Same hand, adjacent fingers, horizontal distance ≥ 2U
  - **HSB** (Half Scissor Bigram): Same hand, adjacent fingers, vertical distance 1U-2U
  - **FSB** (Full Scissor Bigram): Same hand, different fingers, vertical distance ≥ 2U

- **Skipgram Metrics** (derived from trigrams):
  - **SFS**, **LSS**, **HSS**, **FSS**: Same metrics as bigrams, applied to skipgrams

- **Trigram Metrics**:
  - **InHand**: Same hand, fingers rolling towards center
  - **OutHand**: Same hand, fingers rolling towards outside
  - **Redirect**: Same hand, opposite rolling directions
  - **Alternate**: First and third keys on same hand, middle key on other hand
  - **InRoll**: Different hands, rolling towards center
  - **OutRoll**: Different hands, rolling towards outside

- **Monogram Metrics**: Finger usage analysis for individual characters

### Statistics Tab

- **Metric Frequency Tables**: Comprehensive tables showing metric frequencies for all n-gram types
- **Change Tracking**: 
  - Baseline comparison system
  - Frequency change column showing percentage changes
  - Color-coded changes:
    - **Cyan**: Positive frequency changes (improvements)
    - **Orange**: Negative frequency changes (regressions)
- **Filtering Options**: Apply the same filters as corpus analysis to metric calculations

### User Interface

- **Modern UI**: Built with Raylib for a clean, responsive interface
- **Resizable Panels**: Adjustable side panel and bottom panel sizes
- **Tabbed Interface**: Layout, Corpus, Stats, and Settings tabs
- **Horizontal Text Scrolling**: Long text in input fields scrolls horizontally
- **Dropdown Click Blocking**: Prevents clicks from passing through open dropdowns

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

### Layout Design

1. **Load a Layout**: Use "Load Layout" from the File menu or toolbar. Default layout (`layouts/pinev4.json`) loads automatically on startup.

2. **Edit Keys**:
   - Click a key to select it and edit properties in the sidebar
   - Use the HSV color controls to adjust key colors
   - Edit primary and shift characters directly
   - Toggle "Auto Shift" to automatically populate shift characters
   - Adjust key position, size, and rotation
   - Assign fingers for metric analysis
   - Disable keys to exclude them from analysis

3. **Multi-Key Selection**:
   - Hold Shift and click multiple keys to select them
   - Hold Shift and drag across keys for continuous selection
   - Edit common properties (position, size, finger, disable state) for all selected keys
   - Rotate multiple keys around a center key to preserve layout shape

4. **Add/Delete Keys**:
   - Click "Add Key" in the layout controls to add a new key
   - Click "Delete Key" in the sidebar when a key is selected

5. **Save Layout**: Click "Save Layout" to export your layout to a JSON file

### Corpus Analysis

1. **Load a Corpus**: Click "Load Corpus" or select a corpus file from the dropdown (files should be placed in the `corpus/` directory)

2. **Select N-gram Type**: Choose between Monogram, Bigram, Skipgram, Trigram, or Words from the dropdown

3. **Filter Results** (optional):
   - Enter text in the search box to filter n-grams
   - Toggle "Regex" mode for regular expression matching
   - Toggle filter checkboxes (Ignore Whitespace, Ignore Modifiers, etc.)
   - Use the "Limit" field to restrict the number of results displayed

4. **View Statistics**: The table displays:
   - **Rank**: Position in the filtered results
   - **N-gram**: The sequence being analyzed
   - **Frequency**: Percentage of total occurrences
   - **Count**: Total number of occurrences
   - **Key Sequence** (bigrams/trigrams only): Sequence of key presses needed
   - **Finger Sequence** (bigrams/trigrams only): Fingers used for the sequence
   - **Metric Matches** (bigrams/trigrams only): Metrics detected (SFB, LSB, etc.)
   - **Global Rank** (when filtered): Rank across all n-grams
   - **Relative Frequency** (when filtered): Frequency within the filtered set

5. **Export Results**: Click "Save to CSV" to export the current table data to a CSV file

### Metrics Analysis

1. **Navigate to Stats Tab**: Switch to the Stats tab to view metric frequencies

2. **View Metric Frequencies**: Tables show frequency percentages for:
   - Monograms: Finger usage breakdown
   - Bigrams & Skipgrams: SFB, LSB, HSB, FSB metrics
   - Trigrams: InRoll, OutRoll, InHand, OutHand, Redirect, Alternate metrics

3. **Track Changes**: 
   - Make changes to your layout (swap keys, change characters, etc.)
   - The Change column shows frequency differences from the baseline
   - Colors indicate improvements (cyan) or regressions (orange)

4. **Apply Filters**: Use filter checkboxes to exclude whitespace, modifiers, numbers, symbols, punctuation, or disabled keys from metric calculations

## Keyboard Shortcuts

- **F3**: Toggle debug overlay (shows UI element boundaries)

## Project Structure

```
src/
├── Components/          # UI components (Button, Table, Dropdown, TextInput, etc.)
├── UI/                  # UI panels and layout management
│   ├── LayoutTab.cs    # Layout editor functionality
│   ├── CorpusTab.cs    # Corpus analysis and metrics display
│   ├── StatsTab.cs     # Statistics and metric frequency tables
│   └── SidePanel.cs    # Key properties sidebar
├── core/               # Core functionality
│   ├── Layout.cs       # Layout management and key mapping
│   ├── PhysicalKey.cs  # Key data structure
│   ├── Corpus.cs       # Corpus loading and n-gram computation
│   ├── MetricAnalyzer.cs  # Metric computation engine
│   └── Metrics.cs      # Metric detection algorithms
├── Program.cs          # Main entry point
└── ...
```

## File Formats

### Layout Files (JSON)

Layout files are stored in the `layouts/` directory and contain:
- Key definitions with properties (position, size, characters, colors, rotation, finger assignment, etc.)
- Layout metadata

### Corpus Files

Place `.txt` files in the `corpus/` directory to make them available for analysis. The application will automatically detect and list available corpus files.

## License

[Add your license here]
