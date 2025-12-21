using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Keysharp.Core;
using System.Text.Json.Serialization;

namespace Keysharp.Tools
{
    /// <summary>
    /// Converts keyboard layouts from external formats (like colemak.json) to Keysharp format.
    /// </summary>
    public class LayoutConverter
    {
        /// <summary>
        /// Represents a key in the source format (e.g., colemak.json)
        /// </summary>
        private class SourceKey
        {
            public int Row { get; set; }
            public int Col { get; set; }
            public string Finger { get; set; } = "";
            public string Character { get; set; } = "";
        }

        /// <summary>
        /// Maps finger codes from source format to Finger enum
        /// </summary>
        private static Dictionary<string, Finger> FingerMap = new Dictionary<string, Finger>
        {
            { "LP", Finger.LeftPinky },
            { "LR", Finger.LeftRing },
            { "LM", Finger.LeftMiddle },
            { "LI", Finger.LeftIndex },
            { "LT", Finger.LeftThumb },
            { "RT", Finger.RightThumb },
            { "RI", Finger.RightIndex },
            { "RM", Finger.RightMiddle },
            { "RR", Finger.RightRing },
            { "RP", Finger.RightPinky }
        };

        /// <summary>
        /// Converts a colemak-style layout to Keysharp format, mapping it onto a template.
        /// </summary>
        /// <param name="sourcePath">Path to source layout file (e.g., colemak.json)</param>
        /// <param name="templatePath">Path to template file (e.g., Ansi_60%.json)</param>
        /// <param name="outputPath">Path where the converted layout will be saved</param>
        /// <param name="startKeyIdentifier">Identifier of the key in the template where row 0, col 0 should map (e.g., "Key16")</param>
        public static void ConvertFromColemakFormat(string sourcePath, string templatePath, string outputPath, string startKeyIdentifier = "Key16")
        {
            // Load source layout and extract name
            var (sourceLayout, layoutName) = LoadSourceLayout(sourcePath);
            
            // Load template
            var templateJson = LoadTemplate(templatePath);
            
            // Find the starting key in the template
            var startKey = templateJson.Keys.FirstOrDefault(k => k.Identifier == startKeyIdentifier);
            if (startKey == null)
            {
                throw new ArgumentException($"Starting key '{startKeyIdentifier}' not found in template");
            }
            
            // Create a grid mapping of template keys by their logical position
            // We'll map colemak's row/col to template keys starting from the start key
            var keyGrid = BuildKeyGrid(templateJson.Keys, startKey);
            
            // Map source layout to template keys
            foreach (var kvp in sourceLayout)
            {
                var charStr = kvp.Key;
                var sourceKey = kvp.Value;
                
                // Find corresponding template key using row/col position
                // colemak (0,0) maps to startKey (first key in first row of grid)
                var templateKey = FindTemplateKeyAtPosition(keyGrid, startKey, sourceKey.Row, sourceKey.Col);
                
                if (templateKey != null)
                {
                    // Set the character
                    templateKey.PrimaryCharacter = charStr;
                    
                    // Set the finger assignment
                    if (FingerMap.TryGetValue(sourceKey.Finger, out var finger))
                    {
                        templateKey.Finger = finger;
                    }
                    
                    // Set shift character: if it's a lowercase letter, use uppercase version
                    // For uppercase letters or non-letter keys, clear shift character
                    // (symbol keys should keep their template shift characters, but we'll clear to avoid confusion)
                    if (charStr.Length == 1 && char.IsLetter(charStr[0]) && char.IsLower(charStr[0]))
                    {
                        templateKey.ShiftCharacter = charStr.ToUpperInvariant();
                    }
                    else if (charStr.Length == 1 && char.IsLetter(charStr[0]) && char.IsUpper(charStr[0]))
                    {
                        // If source has uppercase letter, it should probably be the shift character
                        // But we'll clear it to avoid confusion - the source format should use lowercase
                        templateKey.ShiftCharacter = null;
                    }
                    else
                    {
                        // For non-letter keys, clear shift character to avoid incorrect mappings from template
                        templateKey.ShiftCharacter = null;
                    }
                }
                else
                {
                    System.Console.WriteLine($"Warning: Could not find template key for colemak position ({sourceKey.Row}, {sourceKey.Col}) - character '{charStr}'");
                }
            }
            
            // Clear mappings so they get regenerated from PrimaryCharacter/ShiftCharacter when loaded
            // The template has QWERTY mappings which would be incorrect for the converted layout
            templateJson.Mappings.Clear();
            
            // Update metadata
            templateJson.Metadata = new LayoutMetadataJson
            {
                DisplayName = layoutName ?? "Converted Layout",
                Description = "autoconverted by keysharp",
                Authors = new List<string>(),
                CreationDate = null
            };
            
            // Save the converted layout
            SaveLayout(templateJson, outputPath);
        }

        private static (Dictionary<string, SourceKey> keys, string? layoutName) LoadSourceLayout(string path)
        {
            var json = File.ReadAllText(path);
            var doc = JsonDocument.Parse(json);
            
            var keys = new Dictionary<string, SourceKey>();
            string? layoutName = null;
            
            // Extract layout name if present
            if (doc.RootElement.TryGetProperty("name", out var nameElement))
            {
                layoutName = nameElement.GetString();
            }
            
            if (doc.RootElement.TryGetProperty("keys", out var keysElement))
            {
                foreach (var prop in keysElement.EnumerateObject())
                {
                    var charStr = prop.Name;
                    var keyObj = prop.Value;
                    
                    var sourceKey = new SourceKey
                    {
                        Character = charStr,
                        Row = keyObj.GetProperty("row").GetInt32(),
                        Col = keyObj.GetProperty("col").GetInt32(),
                        Finger = keyObj.GetProperty("finger").GetString() ?? ""
                    };
                    
                    keys[charStr] = sourceKey;
                }
            }
            
            return (keys, layoutName);
        }

        private static LayoutJson LoadTemplate(string path)
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<LayoutJson>(json) ?? throw new Exception("Failed to deserialize template");
        }

        private static void SaveLayout(LayoutJson layout, string path)
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
                // Use default naming policy (respects JsonPropertyName attributes)
            };
            
            var json = JsonSerializer.Serialize(layout, options);
            File.WriteAllText(path, json);
        }

        /// <summary>
        /// Removes mappings from a layout file so they get regenerated from PrimaryCharacter/ShiftCharacter when loaded.
        /// </summary>
        public static void RemoveMappingsFromLayout(string layoutPath)
        {
            var layoutJson = LoadTemplate(layoutPath);
            layoutJson.Mappings.Clear();
            SaveLayout(layoutJson, layoutPath);
        }

        /// <summary>
        /// Removes mappings from all layout files in a directory.
        /// </summary>
        public static void RemoveMappingsFromAllLayouts(string layoutDirectory)
        {
            if (!Directory.Exists(layoutDirectory))
            {
                System.Console.WriteLine($"Error: Layout directory not found: {layoutDirectory}");
                return;
            }

            var layoutFiles = Directory.GetFiles(layoutDirectory, "*.json");
            
            if (layoutFiles.Length == 0)
            {
                System.Console.WriteLine($"No JSON files found in {layoutDirectory}");
                return;
            }

            System.Console.WriteLine($"Found {layoutFiles.Length} layout files");
            System.Console.WriteLine();

            int successCount = 0;
            int failCount = 0;

            foreach (var layoutFile in layoutFiles.OrderBy(f => f))
            {
                string fileName = Path.GetFileName(layoutFile);
                try
                {
                    System.Console.Write($"Removing mappings from {fileName}... ");
                    RemoveMappingsFromLayout(layoutFile);
                    System.Console.WriteLine("✓");
                    successCount++;
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"✗ Error: {ex.Message}");
                    failCount++;
                }
            }

            System.Console.WriteLine();
            System.Console.WriteLine($"Complete: {successCount} succeeded, {failCount} failed");
        }

        /// <summary>
        /// Finds template keys in order starting from the start key, organized by rows and columns.
        /// Returns a list of keys organized as [row][col]
        /// Filters out special keys (like Caps, Tab, Shift, etc.) that shouldn't be part of the alphanumeric grid.
        /// </summary>
        private static List<List<PhysicalKeyJson>> BuildKeyGrid(List<PhysicalKeyJson> keys, PhysicalKeyJson startKey)
        {
            // Filter out special keys - only include keys with identifiers like "Key" + number
            // or keys that are standard width (between 0.9 and 1.25U)
            bool IsStandardKey(PhysicalKeyJson k)
            {
                if (k.Identifier == null)
                    return false;
                
                // Include keys that start with "Key" followed by digits
                if (k.Identifier.StartsWith("Key") && k.Identifier.Length > 3 && 
                    System.Text.RegularExpressions.Regex.IsMatch(k.Identifier.Substring(3), @"^\d+$"))
                {
                    return true;
                }
                
                // Also include keys that are standard width (to catch any we might have missed)
                // But exclude very wide keys (like Caps at 1.75U) or very narrow keys
                return k.Width >= 0.9f && k.Width <= 1.25f;
            }

            // Group keys by row (y position), rounding to nearest integer
            var rows = keys
                .Where(k => (Math.Abs(k.Y - startKey.Y) < 0.1 || k.Y > startKey.Y) && IsStandardKey(k))
                .GroupBy(k => (int)Math.Round(k.Y))
                .OrderBy(g => g.Key)
                .ToList();
            
            var grid = new List<List<PhysicalKeyJson>>();
            
            foreach (var rowGroup in rows)
            {
                bool isFirstRow = rowGroup.Key == (int)Math.Round(startKey.Y);
                
                // Sort keys in this row by x position
                var sortedKeys = rowGroup
                    .OrderBy(k => k.X)
                    .Where(k => !isFirstRow || k.X >= startKey.X - 0.1) // On first row, only keys at or to the right of start key
                    .ToList();
                
                grid.Add(sortedKeys);
            }
            
            return grid;
        }

        /// <summary>
        /// Finds the template key at a specific row/col position relative to the start key.
        /// </summary>
        private static PhysicalKeyJson? FindTemplateKeyAtPosition(
            List<List<PhysicalKeyJson>> grid,
            PhysicalKeyJson startKey,
            int sourceRow,
            int sourceCol)
        {
            if (sourceRow < 0 || sourceRow >= grid.Count)
                return null;
            
            var row = grid[sourceRow];
            if (sourceCol < 0 || sourceCol >= row.Count)
                return null;
            
            return row[sourceCol];
        }
    }
}

