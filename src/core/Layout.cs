using System;
using System.Collections.Generic;

namespace Keysharp.Core
{
    /// <summary>
    /// Represents a keyboard layout that maps strings to sequences of physical key presses.
    /// A layout defines how strings are produced using physical key presses, where each
    /// physical key describes its position, size, and which finger is used to press it.
    /// </summary>
    public class Layout
    {
        /// <summary>
        /// Maps strings to the sequence of physical keys needed to produce them.
        /// The list represents the keys that must be pressed in order to produce the string.
        /// </summary>
        private Dictionary<string, List<PhysicalKey>> _stringToKeys;

        /// <summary>
        /// All physical keys defined in this layout.
        /// </summary>
        private List<PhysicalKey> _physicalKeys;

        public Layout()
        {
            _stringToKeys = new Dictionary<string, List<PhysicalKey>>();
            _physicalKeys = new List<PhysicalKey>();
        }

        /// <summary>
        /// Adds a mapping from a string to a sequence of physical key presses.
        /// </summary>
        /// <param name="str">The string to map</param>
        /// <param name="keys">The sequence of physical keys needed to produce the string</param>
        public void AddMapping(string str, List<PhysicalKey> keys)
        {
            if (str == null)
                throw new ArgumentNullException(nameof(str));
            if (keys == null)
                throw new ArgumentNullException(nameof(keys));

            _stringToKeys[str] = new List<PhysicalKey>(keys);
        }

        /// <summary>
        /// Gets the sequence of physical key presses needed to produce a given string.
        /// </summary>
        /// <param name="str">The string to look up</param>
        /// <returns>The sequence of physical keys, or null if the string is not mapped</returns>
        public List<PhysicalKey>? GetKeySequence(string str)
        {
            if (str == null)
                return null;

            if (_stringToKeys.TryGetValue(str, out var keys))
            {
                return new List<PhysicalKey>(keys); // Return a copy to prevent external modification
            }

            return null;
        }

        /// <summary>
        /// Checks if a string has a mapping in this layout.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>True if the string is mapped, false otherwise</returns>
        public bool HasMapping(string str)
        {
            if (str == null)
                return false;

            return _stringToKeys.ContainsKey(str);
        }

        /// <summary>
        /// Adds a physical key to the layout's collection.
        /// </summary>
        /// <param name="key">The physical key to add</param>
        public void AddPhysicalKey(PhysicalKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            _physicalKeys.Add(key);
        }

        /// <summary>
        /// Gets all physical keys in this layout.
        /// </summary>
        /// <returns>A read-only list of all physical keys</returns>
        public IReadOnlyList<PhysicalKey> GetPhysicalKeys()
        {
            return _physicalKeys.AsReadOnly();
        }

        /// <summary>
        /// Removes a string mapping from the layout.
        /// </summary>
        /// <param name="str">The string to remove</param>
        /// <returns>True if the mapping was removed, false if it didn't exist</returns>
        public bool RemoveMapping(string str)
        {
            if (str == null)
                return false;

            return _stringToKeys.Remove(str);
        }

        /// <summary>
        /// Gets the number of string mappings in this layout.
        /// </summary>
        public int MappingCount => _stringToKeys.Count;

        /// <summary>
        /// Gets the number of physical keys in this layout.
        /// </summary>
        public int PhysicalKeyCount => _physicalKeys.Count;

        /// <summary>
        /// Creates a standard 60% QWERTY keyboard layout.
        /// </summary>
        /// <returns>A Layout instance configured with a standard 60% QWERTY layout</returns>
        public static Layout CreateStandard60PercentQwerty()
        {
            var layout = new Layout();
            var keys = new Dictionary<string, PhysicalKey>();

            // Standard key dimensions
            const float standardWidth = 1.0f;
            const float standardHeight = 1.0f;

            // Row positions (Y coordinates in U units)
            float row0 = 0.0f; // Top row (number row)
            float row1 = 1.0f; // QWERTY row
            float row2 = 2.0f; // ASDF row
            float row3 = 3.0f; // ZXCV row
            float row4 = 4.0f; // Bottom row (modifiers/space)

            // Top row: ` 1 2 3 4 5 6 7 8 9 0 - =
            float x = 0.0f;
            keys["`"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftPinky, "`");
            x += standardWidth;
            keys["1"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftPinky, "1");
            x += standardWidth;
            keys["2"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftRing, "2");
            x += standardWidth;
            keys["3"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftMiddle, "3");
            x += standardWidth;
            keys["4"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftIndex, "4");
            x += standardWidth;
            keys["5"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.LeftIndex, "5");
            x += standardWidth;
            keys["6"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightIndex, "6");
            x += standardWidth;
            keys["7"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightIndex, "7");
            x += standardWidth;
            keys["8"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightMiddle, "8");
            x += standardWidth;
            keys["9"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightRing, "9");
            x += standardWidth;
            keys["0"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightPinky, "0");
            x += standardWidth;
            keys["-"] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightPinky, "-");
            x += standardWidth;
            keys["="] = new PhysicalKey(x, row0, standardWidth, standardHeight, Finger.RightPinky, "=");
            x += standardWidth;
            keys["Backspace"] = new PhysicalKey(x, row0, 2.0f, standardHeight, Finger.RightPinky, "Backspace");

            // Second row: Tab Q W E R T Y U I O P [ ] \
            x = 0.0f;
            keys["Tab"] = new PhysicalKey(x, row1, 1.5f, standardHeight, Finger.LeftPinky, "Tab");
            x += 1.5f;
            keys["Q"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.LeftPinky, "Q");
            x += standardWidth;
            keys["W"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.LeftRing, "W");
            x += standardWidth;
            keys["E"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.LeftMiddle, "E");
            x += standardWidth;
            keys["R"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.LeftIndex, "R");
            x += standardWidth;
            keys["T"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.LeftIndex, "T");
            x += standardWidth;
            keys["Y"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightIndex, "Y");
            x += standardWidth;
            keys["U"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightIndex, "U");
            x += standardWidth;
            keys["I"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightMiddle, "I");
            x += standardWidth;
            keys["O"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightRing, "O");
            x += standardWidth;
            keys["P"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightPinky, "P");
            x += standardWidth;
            keys["["] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightPinky, "[");
            x += standardWidth;
            keys["]"] = new PhysicalKey(x, row1, standardWidth, standardHeight, Finger.RightPinky, "]");
            x += standardWidth;
            keys["\\"] = new PhysicalKey(x, row1, 1.5f, standardHeight, Finger.RightPinky, "\\");

            // Third row: Caps A S D F G H J K L ; '
            x = 0.0f;
            keys["Caps"] = new PhysicalKey(x, row2, 1.75f, standardHeight, Finger.LeftPinky, "Caps");
            x += 1.75f;
            keys["A"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.LeftPinky, "A");
            x += standardWidth;
            keys["S"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.LeftRing, "S");
            x += standardWidth;
            keys["D"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.LeftMiddle, "D");
            x += standardWidth;
            keys["F"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.LeftIndex, "F");
            x += standardWidth;
            keys["G"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.LeftIndex, "G");
            x += standardWidth;
            keys["H"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightIndex, "H");
            x += standardWidth;
            keys["J"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightIndex, "J");
            x += standardWidth;
            keys["K"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightMiddle, "K");
            x += standardWidth;
            keys["L"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightRing, "L");
            x += standardWidth;
            keys[";"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightPinky, ";");
            x += standardWidth;
            keys["'"] = new PhysicalKey(x, row2, standardWidth, standardHeight, Finger.RightPinky, "'");
            x += standardWidth;
            keys["Enter"] = new PhysicalKey(x, row2, 2.25f, standardHeight, Finger.RightPinky, "Enter");

            // Fourth row: Shift Z X C V B N M , . /
            x = 0.0f;
            keys["LShift"] = new PhysicalKey(x, row3, 2.25f, standardHeight, Finger.LeftPinky, "LShift");
            x += 2.25f;
            keys["Z"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.LeftPinky, "Z");
            x += standardWidth;
            keys["X"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.LeftRing, "X");
            x += standardWidth;
            keys["C"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.LeftMiddle, "C");
            x += standardWidth;
            keys["V"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.LeftIndex, "V");
            x += standardWidth;
            keys["B"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.LeftIndex, "B");
            x += standardWidth;
            keys["N"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.RightIndex, "N");
            x += standardWidth;
            keys["M"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.RightIndex, "M");
            x += standardWidth;
            keys[","] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.RightMiddle, ",");
            x += standardWidth;
            keys["."] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.RightRing, ".");
            x += standardWidth;
            keys["/"] = new PhysicalKey(x, row3, standardWidth, standardHeight, Finger.RightPinky, "/");
            x += standardWidth;
            keys["RShift"] = new PhysicalKey(x, row3, 2.75f, standardHeight, Finger.RightPinky, "RShift");

            // Bottom row: Ctrl Win Alt Space Alt Fn Ctrl (or similar)
            x = 0.0f;
            keys["LCtrl"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.LeftPinky, "LCtrl");
            x += 1.25f;
            keys["LWin"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.LeftRing, "LWin");
            x += 1.25f;
            keys["LAlt"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.LeftThumb, "LAlt");
            x += 1.25f;
            keys["Space"] = new PhysicalKey(x, row4, 6.25f, standardHeight, Finger.RightThumb, "Space");
            x += 6.25f;
            keys["RAlt"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.RightThumb, "RAlt");
            x += 1.25f;
            keys["Menu"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.RightRing, "Menu");
            x += 1.25f;
            keys["Fn"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.RightRing, "Fn");
            x += 1.25f;
            keys["RCtrl"] = new PhysicalKey(x, row4, 1.25f, standardHeight, Finger.RightPinky, "RCtrl");

            // Add all physical keys to the layout
            foreach (var key in keys.Values)
            {
                layout.AddPhysicalKey(key);
            }

            // Map characters to their physical keys
            // Uppercase letters (using Shift modifier)
            foreach (char c in "ABCDEFGHIJKLMNOPQRSTUVWXYZ")
            {
                string lower = c.ToString().ToLowerInvariant();
                if (keys.ContainsKey(lower))
                {
                    layout.AddMapping(c.ToString(), new List<PhysicalKey> { keys["LShift"], keys[lower] });
                }
            }

            // Lowercase letters
            foreach (char c in "abcdefghijklmnopqrstuvwxyz")
            {
                if (keys.ContainsKey(c.ToString()))
                {
                    layout.AddMapping(c.ToString(), new List<PhysicalKey> { keys[c.ToString()] });
                }
            }

            // Numbers and symbols (top row)
            var numberSymbols = new Dictionary<string, string>
            {
                ["1"] = "1", ["2"] = "2", ["3"] = "3", ["4"] = "4", ["5"] = "5",
                ["6"] = "6", ["7"] = "7", ["8"] = "8", ["9"] = "9", ["0"] = "0",
                ["-"] = "-", ["="] = "="
            };
            foreach (var kvp in numberSymbols)
            {
                if (keys.ContainsKey(kvp.Value))
                {
                    layout.AddMapping(kvp.Key, new List<PhysicalKey> { keys[kvp.Value] });
                }
            }

            // Shifted number row symbols (typically: ! @ # $ % ^ & * ( ) _ +)
            var shiftedSymbols = new Dictionary<string, List<PhysicalKey>>
            {
                ["!"] = new List<PhysicalKey> { keys["LShift"], keys["1"] },
                ["@"] = new List<PhysicalKey> { keys["LShift"], keys["2"] },
                ["#"] = new List<PhysicalKey> { keys["LShift"], keys["3"] },
                ["$"] = new List<PhysicalKey> { keys["LShift"], keys["4"] },
                ["%"] = new List<PhysicalKey> { keys["LShift"], keys["5"] },
                ["^"] = new List<PhysicalKey> { keys["LShift"], keys["6"] },
                ["&"] = new List<PhysicalKey> { keys["LShift"], keys["7"] },
                ["*"] = new List<PhysicalKey> { keys["LShift"], keys["8"] },
                ["("] = new List<PhysicalKey> { keys["LShift"], keys["9"] },
                [")"] = new List<PhysicalKey> { keys["LShift"], keys["0"] },
                ["_"] = new List<PhysicalKey> { keys["LShift"], keys["-"] },
                ["+"] = new List<PhysicalKey> { keys["LShift"], keys["="] }
            };
            foreach (var kvp in shiftedSymbols)
            {
                layout.AddMapping(kvp.Key, kvp.Value);
            }

            // Punctuation and special characters
            layout.AddMapping(";", new List<PhysicalKey> { keys[";"] });
            layout.AddMapping(":", new List<PhysicalKey> { keys["LShift"], keys[";"] });
            layout.AddMapping("'", new List<PhysicalKey> { keys["'"] });
            layout.AddMapping("\"", new List<PhysicalKey> { keys["LShift"], keys["'"] });
            layout.AddMapping(",", new List<PhysicalKey> { keys[","] });
            layout.AddMapping("<", new List<PhysicalKey> { keys["LShift"], keys[","] });
            layout.AddMapping(".", new List<PhysicalKey> { keys["."] });
            layout.AddMapping(">", new List<PhysicalKey> { keys["LShift"], keys["."] });
            layout.AddMapping("/", new List<PhysicalKey> { keys["/"] });
            layout.AddMapping("?", new List<PhysicalKey> { keys["LShift"], keys["/"] });
            layout.AddMapping("[", new List<PhysicalKey> { keys["["] });
            layout.AddMapping("{", new List<PhysicalKey> { keys["LShift"], keys["["] });
            layout.AddMapping("]", new List<PhysicalKey> { keys["]"] });
            layout.AddMapping("}", new List<PhysicalKey> { keys["LShift"], keys["]"] });
            layout.AddMapping("\\", new List<PhysicalKey> { keys["\\"] });
            layout.AddMapping("|", new List<PhysicalKey> { keys["LShift"], keys["\\"] });
            layout.AddMapping("`", new List<PhysicalKey> { keys["`"] });
            layout.AddMapping("~", new List<PhysicalKey> { keys["LShift"], keys["`"] });

            // Space
            layout.AddMapping(" ", new List<PhysicalKey> { keys["Space"] });

            return layout;
        }
    }
}

