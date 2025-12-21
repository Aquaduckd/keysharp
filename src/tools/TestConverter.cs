using System;
using System.IO;
using Keysharp.Tools;

namespace Keysharp.Tools
{
    /// <summary>
    /// Simple test program to convert layouts to Keysharp format.
    /// This can be called from Program.cs or run as a separate test.
    /// </summary>
    public static class TestConverter
    {
        public static void RunTest()
        {
            string sourcePath = Path.Combine(Directory.GetCurrentDirectory(), "colemak.json");
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "layouts", "qwerty.json");
            string outputPath = Path.Combine(Directory.GetCurrentDirectory(), "layouts", "colemak_converted.json");
            string startKey = "Key16";

            if (!File.Exists(sourcePath))
            {
                Console.WriteLine($"Error: Source file not found: {sourcePath}");
                return;
            }

            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"Error: Template file not found: {templatePath}");
                return;
            }

            try
            {
                Console.WriteLine($"Converting layout from {sourcePath}");
                Console.WriteLine($"Using template: {templatePath}");
                Console.WriteLine($"Starting key: {startKey}");
                Console.WriteLine($"Output: {outputPath}");

                LayoutConverter.ConvertFromColemakFormat(sourcePath, templatePath, outputPath, startKey);

                Console.WriteLine($"Successfully converted layout to {outputPath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error converting layout: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
            }
        }

        public static void RunBatchConversion()
        {
            string sourceDirectory = Path.Combine(Directory.GetCurrentDirectory(), "cmini layouts");
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "layouts", "qwerty.json");
            string outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "layouts");
            string startKey = "Key16";

            BatchConverter.ConvertAll(sourceDirectory, templatePath, outputDirectory, startKey);
        }
    }
}

