using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Keysharp.Core
{
    /// <summary>
    /// Represents a mapping from one keyboard layout to another, mapping key identifiers
    /// from a source layout to key identifiers in a target layout.
    /// </summary>
    public class KeyMappings
    {
        /// <summary>
        /// Maps source key identifiers to target key identifiers.
        /// </summary>
        [JsonPropertyName("mappings")]
        public Dictionary<string, string> Mappings { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Optional metadata about the source layout (for reference).
        /// </summary>
        [JsonPropertyName("sourceLayout")]
        public string? SourceLayout { get; set; }

        /// <summary>
        /// Optional metadata about the target layout (for reference).
        /// </summary>
        [JsonPropertyName("targetLayout")]
        public string? TargetLayout { get; set; }

        /// <summary>
        /// Adds or updates a mapping from a source identifier to a target identifier.
        /// </summary>
        public void AddMapping(string sourceIdentifier, string targetIdentifier)
        {
            if (string.IsNullOrEmpty(sourceIdentifier))
                throw new ArgumentException("Source identifier cannot be null or empty", nameof(sourceIdentifier));
            if (string.IsNullOrEmpty(targetIdentifier))
                throw new ArgumentException("Target identifier cannot be null or empty", nameof(targetIdentifier));

            Mappings[sourceIdentifier] = targetIdentifier;
        }

        /// <summary>
        /// Removes a mapping for the given source identifier.
        /// </summary>
        public bool RemoveMapping(string sourceIdentifier)
        {
            return Mappings.Remove(sourceIdentifier);
        }

        /// <summary>
        /// Clears all mappings.
        /// </summary>
        public void Clear()
        {
            Mappings.Clear();
        }

        /// <summary>
        /// Gets the target identifier for a given source identifier, or null if not mapped.
        /// </summary>
        public string? GetTargetIdentifier(string sourceIdentifier)
        {
            Mappings.TryGetValue(sourceIdentifier, out string? targetId);
            return targetId;
        }

        /// <summary>
        /// Applies the mappings to convert a source layout onto a target layout.
        /// For each mapped key, copies the PrimaryCharacter and ShiftCharacter from the source
        /// layout's key to the corresponding key in the target layout (identified by the mapping).
        /// </summary>
        /// <param name="sourceLayout">The source layout to copy characters from</param>
        /// <param name="targetLayout">The target layout to apply characters to</param>
        public void ApplyMappings(Layout sourceLayout, Layout targetLayout)
        {
            if (sourceLayout == null)
                throw new ArgumentNullException(nameof(sourceLayout));
            if (targetLayout == null)
                throw new ArgumentNullException(nameof(targetLayout));

            // Build a dictionary of target layout keys by identifier for fast lookup
            var targetKeysByIdentifier = new Dictionary<string, PhysicalKey>();
            foreach (var key in targetLayout.GetPhysicalKeys())
            {
                if (!string.IsNullOrEmpty(key.Identifier))
                {
                    targetKeysByIdentifier[key.Identifier] = key;
                }
            }

            // Build a dictionary of source layout keys by identifier for fast lookup
            var sourceKeysByIdentifier = new Dictionary<string, PhysicalKey>();
            foreach (var key in sourceLayout.GetPhysicalKeys())
            {
                if (!string.IsNullOrEmpty(key.Identifier))
                {
                    sourceKeysByIdentifier[key.Identifier] = key;
                }
            }

            // Apply each mapping
            foreach (var mapping in Mappings)
            {
                string sourceId = mapping.Key;
                string targetId = mapping.Value;

                // Find the source key
                if (!sourceKeysByIdentifier.TryGetValue(sourceId, out PhysicalKey? sourceKey))
                {
                    // Source key not found - skip this mapping
                    continue;
                }

                // Find the target key
                if (!targetKeysByIdentifier.TryGetValue(targetId, out PhysicalKey? targetKey))
                {
                    // Target key not found - skip this mapping
                    continue;
                }

                // Copy primary and shift characters from source to target
                targetKey.PrimaryCharacter = sourceKey.PrimaryCharacter;
                targetKey.ShiftCharacter = sourceKey.ShiftCharacter;
            }

            // Rebuild mappings in the target layout to reflect the new character assignments
            targetLayout.RebuildMappings();
        }

        /// <summary>
        /// Saves the mappings to a JSON file.
        /// </summary>
        /// <param name="filePath">The path to save the file to</param>
        public void SaveToFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            string json = JsonSerializer.Serialize(this, options);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// Loads mappings from a JSON file.
        /// </summary>
        /// <param name="filePath">The path to load the file from</param>
        /// <returns>A new KeyMappings instance loaded from the file</returns>
        public static KeyMappings LoadFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty", nameof(filePath));
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Mapping file not found", filePath);

            string json = File.ReadAllText(filePath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            var mappings = JsonSerializer.Deserialize<KeyMappings>(json, options);
            if (mappings == null)
                throw new InvalidOperationException("Failed to deserialize mapping file");

            // Ensure Mappings dictionary is initialized
            if (mappings.Mappings == null)
            {
                mappings.Mappings = new Dictionary<string, string>();
            }

            return mappings;
        }
    }
}


