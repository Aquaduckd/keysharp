using System;
using System.IO;
using System.Linq;
using Keysharp.Tools;

namespace Keysharp.Tools
{
    /// <summary>
    /// Batch converter to convert multiple layouts from a source directory.
    /// </summary>
    public static class BatchConverter
    {
        /// <summary>
        /// Converts all layout files from a source directory to Keysharp format.
        /// </summary>
        /// <param name="sourceDirectory">Directory containing source layout files</param>
        /// <param name="templatePath">Path to template file (e.g., qwerty.json)</param>
        /// <param name="outputDirectory">Directory where converted layouts will be saved</param>
        /// <param name="startKeyIdentifier">Identifier of the key where row 0, col 0 should map (default: "Key16")</param>
        public static void ConvertAll(string sourceDirectory, string templatePath, string outputDirectory, string startKeyIdentifier = "Key16")
        {
            if (!Directory.Exists(sourceDirectory))
            {
                Console.WriteLine($"Error: Source directory not found: {sourceDirectory}");
                return;
            }

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"Error: Template file not found: {templatePath}");
                return;
            }

            // Create output directory if it doesn't exist
            if (!Directory.Exists(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            // Get all JSON files from source directory
            var sourceFiles = Directory.GetFiles(sourceDirectory, "*.json");
            
            if (sourceFiles.Length == 0)
            {
                Console.WriteLine($"No JSON files found in {sourceDirectory}");
                return;
            }

            Console.WriteLine($"Found {sourceFiles.Length} layout files to convert");
            Console.WriteLine($"Template: {templatePath}");
            Console.WriteLine($"Output directory: {outputDirectory}");
            Console.WriteLine();

            int successCount = 0;
            int failCount = 0;

            foreach (var sourceFile in sourceFiles.OrderBy(f => f))
            {
                string fileName = Path.GetFileName(sourceFile);
                string outputPath = Path.Combine(outputDirectory, fileName);

                try
                {
                    Console.Write($"Converting {fileName}... ");
                    LayoutConverter.ConvertFromColemakFormat(sourceFile, templatePath, outputPath, startKeyIdentifier);
                    Console.WriteLine("✓");
                    successCount++;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error: {ex.Message}");
                    failCount++;
                }
            }

            Console.WriteLine();
            Console.WriteLine($"Conversion complete: {successCount} succeeded, {failCount} failed");
        }
    }
}

