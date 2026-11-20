using System;
using VRage.Utils;

namespace Sprocket.ModSupport
{
    /// <summary>A host type whose words stand for the members of the enum <typeparamref name="T" />, so the mod reads and writes the enum itself.</summary>
    /// <remarks>Each word is an enum member's name made an id, unless the words were written out: renaming a member then renames a stored word.</remarks>
    public sealed class SprocketEnumType<T> : SprocketType where T : struct
    {
        readonly T[] _values;

        /// <summary>The type <paramref name="id" /> of a mod registered through <paramref name="registry" />, which only the wrapper holds, in <paramref name="space" />, its words <paramref name="words" /> standing for <paramref name="values" /> in order.</summary>
        public SprocketEnumType(ISprocketRegistry registry, string space, string id, string[] words, T[] values)
            : base(registry, space, id, words)
        {
            _values = values;
        }

        /// <summary>Names the word standing for <paramref name="value" /> <paramref name="name" /> for players.</summary>
        public SprocketEnumType<T> Naming(T value, string name)
        {
            Naming(Word(value), SprocketText.Of(name));
            return this;
        }

        /// <summary>Names the word standing for <paramref name="value" /> by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public SprocketEnumType<T> Naming(T value, MyStringId name)
        {
            Naming(Word(value), SprocketText.Of(name));
            return this;
        }

        /// <summary>The word standing for <paramref name="value" />.</summary>
        public string Word(T value)
        {
            var at = Array.IndexOf(_values, value);
            if (at < 0)
                throw new ArgumentException($"{value} has no word in type {FullName}.");

            return Words[at];
        }

        /// <summary>The value <paramref name="word" /> stands for, or the enum's default where it stands for none.</summary>
        public T Value(string word)
        {
            var at = Array.IndexOf(Words, word);
            return at < 0 ? default(T) : _values[at];
        }
    }
}
