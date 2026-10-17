using System;
using System.Collections.Generic;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>How the keyboard chooses among a value's words: an arrow steps to the next word, and a letter to the next word starting with it.</summary>
    public static class KeyboardChoice
    {
        /// <summary>How far <paramref name="key" /> steps through words: one on for right or down, one back for left or up, and none otherwise.</summary>
        public static int By(MyKeys key)
        {
            return key == MyKeys.Right || key == MyKeys.Down ? 1 : key == MyKeys.Left || key == MyKeys.Up ? -1 : 0;
        }

        /// <summary>The word <paramref name="by" /> steps from <paramref name="now" />, or null where there is no step to take.</summary>
        public static string Step(IList<string> words, string now, int by)
        {
            if (by == 0 || words.Count == 0)
                return null;

            var at = words.IndexOf(now);
            var next = at < 0 ? by > 0 ? 0 : words.Count - 1 : Math.Max(0, Math.Min(words.Count - 1, at + by));
            return next == at ? null : words[next];
        }

        /// <summary>The next word after <paramref name="now" /> that starts with <paramref name="letter" />, going round, or null where no other does.</summary>
        public static string Letter(IList<string> words, string now, char letter)
        {
            var at = words.IndexOf(now);
            for (var i = 1; i <= words.Count; i++)
            {
                var word = words[(at + i + words.Count) % words.Count];
                if (word.Length == 0 || char.ToUpperInvariant(word[0]) != char.ToUpperInvariant(letter))
                    continue;

                return word == now ? null : word;
            }

            return null;
        }
    }
}
