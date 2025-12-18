using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Keysharp.Core
{
    /// <summary>
    /// Data transfer object for serializing/deserializing Layout to/from JSON.
    /// </summary>
    public class LayoutJson
    {
        [JsonPropertyName("metadata")]
        public LayoutMetadataJson? Metadata { get; set; }

        [JsonPropertyName("keys")]
        public List<PhysicalKeyJson> Keys { get; set; } = new List<PhysicalKeyJson>();

        [JsonPropertyName("mappings")]
        public Dictionary<string, List<string>> Mappings { get; set; } = new Dictionary<string, List<string>>();

        /// <summary>
        /// Converts a Layout instance to a LayoutJson DTO.
        /// </summary>
        public static LayoutJson FromLayout(Layout layout, LayoutMetadataJson? metadata = null)
        {
            var json = new LayoutJson
            {
                Metadata = metadata
            };

            // Create a mapping from PhysicalKey to its identifier for serialization
            var keyToIdentifier = new Dictionary<PhysicalKey, string>();
            foreach (var key in layout.GetPhysicalKeys())
            {
                if (key.Identifier != null)
                {
                    keyToIdentifier[key] = key.Identifier;
                    json.Keys.Add(PhysicalKeyJson.FromPhysicalKey(key, key.Identifier));
                }
            }

            // Serialize string-to-keys mappings using key identifiers
            var allMappings = layout.GetAllMappings();
            foreach (var kvp in allMappings)
            {
                var identifierList = new List<string>();
                foreach (var key in kvp.Value)
                {
                    if (keyToIdentifier.TryGetValue(key, out var identifier))
                    {
                        identifierList.Add(identifier);
                    }
                }
                if (identifierList.Count > 0)
                {
                    json.Mappings[kvp.Key] = identifierList;
                }
            }

            return json;
        }

        /// <summary>
        /// Converts a LayoutJson DTO to a Layout instance.
        /// </summary>
        public Layout ToLayout()
        {
            var layout = new Layout();
            var identifierToKey = new Dictionary<string, PhysicalKey>();

            // First, create all physical keys
            foreach (var keyJson in Keys)
            {
                var key = keyJson.ToPhysicalKey();
                if (key.Identifier != null)
                {
                    identifierToKey[key.Identifier] = key;
                    layout.AddPhysicalKey(key);
                }
            }

            // Then, add string-to-keys mappings
            foreach (var kvp in Mappings)
            {
                var keyList = new List<PhysicalKey>();
                foreach (var identifier in kvp.Value)
                {
                    if (identifierToKey.TryGetValue(identifier, out var key))
                    {
                        keyList.Add(key);
                    }
                }
                if (keyList.Count > 0)
                {
                    layout.AddMapping(kvp.Key, keyList);
                }
            }

            // Rebuild mappings from key properties if no mappings were loaded (for backward compatibility)
            // If mappings exist, we trust them; otherwise rebuild from PrimaryCharacter/ShiftCharacter
            if (Mappings.Count == 0)
            {
                layout.RebuildMappings();
            }

            return layout;
        }
    }

    /// <summary>
    /// Data transfer object for layout metadata.
    /// </summary>
    public class LayoutMetadataJson
    {
        [JsonPropertyName("displayName")]
        public string? DisplayName { get; set; }

        [JsonPropertyName("authors")]
        public List<string> Authors { get; set; } = new List<string>();

        [JsonPropertyName("creationDate")]
        public string? CreationDate { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Data transfer object for serializing/deserializing PhysicalKey to/from JSON.
    /// </summary>
    public class PhysicalKeyJson
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("width")]
        public float Width { get; set; }

        [JsonPropertyName("height")]
        public float Height { get; set; }

        [JsonPropertyName("finger")]
        public Finger Finger { get; set; }

        [JsonPropertyName("identifier")]
        public string? Identifier { get; set; }

        [JsonPropertyName("primaryCharacter")]
        public string? PrimaryCharacter { get; set; }

        [JsonPropertyName("shiftCharacter")]
        public string? ShiftCharacter { get; set; }

        [JsonPropertyName("disabled")]
        public bool Disabled { get; set; }

        public static PhysicalKeyJson FromPhysicalKey(PhysicalKey key, string? identifier)
        {
            return new PhysicalKeyJson
            {
                X = key.X,
                Y = key.Y,
                Width = key.Width,
                Height = key.Height,
                Finger = key.Finger,
                Identifier = identifier ?? key.Identifier,
                PrimaryCharacter = key.PrimaryCharacter,
                ShiftCharacter = key.ShiftCharacter,
                Disabled = key.Disabled
            };
        }

        public PhysicalKey ToPhysicalKey()
        {
            return new PhysicalKey(X, Y, Width, Height, Finger, Identifier)
            {
                PrimaryCharacter = PrimaryCharacter,
                ShiftCharacter = ShiftCharacter,
                Disabled = Disabled
            };
        }
    }
}

