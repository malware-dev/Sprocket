using System;
using System.Collections.Generic;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>
    /// A host type whose values are a fixed list of words, such as a mode or a unit: one the mod defines, or one defined
    /// elsewhere and named by its full name.
    /// </summary>
    /// <remarks>Its words are stored in players' programs: a word may be added once the mod is released, never removed or renamed.</remarks>
    public class SprocketType
    {
        readonly Dictionary<string, Func<string>> _names = new Dictionary<string, Func<string>>();

        /// <summary>The type <paramref name="id" /> in the namespace <paramref name="space" />, holding <paramref name="words" />; none where it is defined elsewhere.</summary>
        public SprocketType(string space, string id, string[] words)
        {
            Namespace = space;
            Id = id;
            Words = words;
        }

        /// <summary>The namespace the type belongs to.</summary>
        public string Namespace { get; }

        /// <summary>The type's id in its namespace, such as <c>speed_unit</c>.</summary>
        public string Id { get; }

        /// <summary>The type's full name, such as <c>malforge.hover_repulsors.speed_unit</c>.</summary>
        public string FullName => Namespace + "." + Id;

        /// <summary>The type's words as programs write them, in order; empty for a type defined elsewhere.</summary>
        public string[] Words { get; }

        /// <summary>Whether the mod defined this type, rather than naming one defined elsewhere.</summary>
        public bool IsDefined => Words.Length > 0;

        /// <summary>Names the word <paramref name="word" /> <paramref name="name" /> for players.</summary>
        public SprocketType Naming(string word, string name)
        {
            return Naming(word, SprocketText.Of(name));
        }

        /// <summary>Names the word <paramref name="word" /> by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public SprocketType Naming(string word, MyStringId name)
        {
            return Naming(word, SprocketText.Of(name));
        }

        /// <summary>Names the word <paramref name="word" /> by what <paramref name="name" /> says each time.</summary>
        protected SprocketType Naming(string word, Func<string> name)
        {
            if (Array.IndexOf(Words, word) < 0)
                throw new ArgumentException($"Type {FullName} has no word \"{word}\" to name.");

            _names[word] = name;
            return this;
        }

        /// <summary>The type as Sprocket reads it, by the keys of <see cref="SprocketWire" />.</summary>
        /// <remarks>Throws where a word has no name.</remarks>
        public Dictionary<string, object> Wire()
        {
            var names = new Func<string>[Words.Length];
            for (var i = 0; i < Words.Length; i++)
            {
                Func<string> name;
                if (!_names.TryGetValue(Words[i], out name))
                    throw new InvalidOperationException($"Word \"{Words[i]}\" of type {FullName} has no name. Give each word one with Naming(...).");
                names[i] = name;
            }

            return new Dictionary<string, object>
            {
                { SprocketWire.Id, Id },
                { SprocketWire.Words, Words },
                { SprocketWire.WordNames, names }
            };
        }
    }
}
