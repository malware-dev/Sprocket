using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Text
{
    /// <summary>Every text a program says, read in the player's language once, and said on request.</summary>
    /// <remarks>A text the source lacks is said as it was written.</remarks>
    public sealed class Phrasebook
    {
        readonly IList<Phrase> _phrases;

        readonly string[] _said;

        Dictionary<string, Phrase> _keyed;

        /// <summary>The texts in <paramref name="phrases" />, each at its own index, said as written until <see cref="Load" /> is given a language.</summary>
        public Phrasebook(IList<Phrase> phrases)
        {
            _phrases = phrases;
            _said = new string[phrases.Count];

            for (var i = 0; i < phrases.Count; i++)
            {
                if (phrases[i].Index != i)
                    throw new ArgumentException($"The text keyed {phrases[i].Key} is at {i} but says it is at {phrases[i].Index}.");
            }

            Load(null);
        }

        /// <summary>Reads every text from <paramref name="source" />, or goes back to the texts as written where it is null.</summary>
        public void Load(ITextSource source)
        {
            for (var i = 0; i < _phrases.Count; i++)
            {
                var found = source?.Find(_phrases[i].Key);
                _said[i] = found ?? _phrases[i].Written;
            }
        }

        /// <summary>The text <paramref name="phrase" /> in the language loaded.</summary>
        public string Say(Phrase phrase)
        {
            return _said[phrase.Index];
        }

        /// <summary>The text keyed <paramref name="key" /> in the language loaded, or null where the book has no such text.</summary>
        /// <remarks>The keys are gathered the first time this is asked, for a text found by a name written in other text, such as a link's.</remarks>
        public string Find(string key)
        {
            if (_keyed == null)
            {
                _keyed = new Dictionary<string, Phrase>(_phrases.Count, StringComparer.Ordinal);
                foreach (var phrase in _phrases)
                    _keyed[phrase.Key] = phrase;
            }

            Phrase found;
            return key != null && _keyed.TryGetValue(key, out found) ? _said[found.Index] : null;
        }
    }
}
