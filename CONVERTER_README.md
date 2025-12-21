# Layout Converter

This tool converts keyboard layouts from external formats (like `colemak.json`) to Keysharp format.

## Usage

The converter can be used programmatically via the `LayoutConverter` class:

```csharp
using Keysharp.Tools;

LayoutConverter.ConvertFromColemakFormat(
    sourcePath: "colemak.json",
    templatePath: "templates/Ansi_60%.json",
    outputPath: "layouts/colemak_converted.json",
    startKeyIdentifier: "Key16"  // Optional, defaults to "Key16"
);
```

## Example

To convert the `colemak.json` file to Keysharp format using the `Ansi_60%` template:

```csharp
// See src/tools/TestConverter.cs for a complete example
TestConverter.RunTest();
```

## How It Works

1. **Loads the source layout** (e.g., `colemak.json`) which contains:
   - Character mappings with row/col positions
   - Finger assignments (LP, LR, LM, LI, RI, RM, RR, RP)

2. **Loads the template** (e.g., `templates/Ansi_60%.json`) which contains:
   - Physical key positions and sizes
   - Key identifiers

3. **Maps the layout** by:
   - Finding the starting key (default: "Key16") in the template
   - Building a grid of template keys starting from that key
   - Mapping colemak's row/col coordinates to template keys sequentially
   - Setting `primaryCharacter` from the source layout
   - Converting finger assignments (e.g., "LP" → `Finger.LeftPinky`)

4. **Saves the result** as a new layout file in Keysharp format

## Source Format

The converter expects source files in this format:

```json
{
  "keys": {
    "q": {
      "row": 0,
      "col": 0,
      "finger": "LP"
    },
    "w": {
      "row": 0,
      "col": 1,
      "finger": "LR"
    }
  }
}
```

## Finger Mappings

- `LP` → LeftPinky
- `LR` → LeftRing
- `LM` → LeftMiddle
- `LI` → LeftIndex
- `LT` → LeftThumb
- `RT` → RightThumb
- `RI` → RightIndex
- `RM` → RightMiddle
- `RR` → RightRing
- `RP` → RightPinky

