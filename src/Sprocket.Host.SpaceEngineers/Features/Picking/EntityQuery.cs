using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// Narrows a list of entities by free text and kind.
    /// </summary>
    /// <remarks>
    /// A real construct has hundreds of blocks, so every list that can contain
    /// entities has to be searchable - the ship browser and the slot pickers alike.
    /// This lives in core so all of them narrow identically: a target that invented
    /// its own matching would quietly behave differently from the others.
    ///
    /// Matching is deliberately forgiving. Someone hunting for a door types "door",
    /// not a prefix, and case is never their problem. Each word typed is looked for anywhere,
    /// but in the order typed, so "airl 1" finds "Airlock 1", "Airlock 10" and "Airlock Door 1",
    /// and leaves out "1 Airlock".
    /// </remarks>
    public static class EntityQuery
    {
        /// <summary>Whether a plain label matches a search, just as forgivingly.</summary>
        public static bool MatchesText(string text, string search)
        {
            return string.IsNullOrEmpty(search) || EveryWord(search, text);
        }

        /// <summary>
        /// Whether <paramref name="entity" /> is of <paramref name="family" />, or of the kind <paramref name="kindId" /> where it
        /// names no family, with every word of <paramref name="search" /> in its name.
        /// </summary>
        public static bool Matches(GridEntity entity, string search, string kindId, Family family)
        {
            if (entity == null)
                return false;

            if (family != Family.None)
            {
                if (entity.Kind.Family != family)
                    return false;
            }
            else if (!string.IsNullOrEmpty(kindId) && entity.Kind.Id != kindId)
                return false;

            if (string.IsNullOrEmpty(search))
                return true;

            return EveryWord(search, entity.Name);
        }

        /// <summary>
        /// The distinct kinds present among the candidates, so the kind filter offers
        /// only what is actually on this construct rather than a fixed catalogue.
        /// </summary>
        public static void KindsPresent(IList<GridEntity> candidates, List<EntityKind> result)
        {
            for (var i = 0; i < candidates.Count; i++)
            {
                var kind = candidates[i].Kind;
                if (!Contains(result, kind))
                    result.Add(kind);
            }
        }

        static bool Contains(List<EntityKind> kinds, EntityKind kind)
        {
            for (var i = 0; i < kinds.Count; i++)
            {
                if (kinds[i].Id == kind.Id)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Whether every word of <paramref name="search" /> is found in <paramref name="text" /> in the order typed, each after
        /// the one before it.
        /// </summary>
        /// <remarks>
        /// Asked of every block on every keystroke, so the words are walked where they stand in the search rather than split
        /// out of it, which would make a string for each.
        /// </remarks>
        static bool EveryWord(string search, string text)
        {
            var at = 0;
            var next = 0;
            while (at < search.Length)
            {
                while (at < search.Length && char.IsWhiteSpace(search[at]))
                    at++;

                var start = at;
                while (at < search.Length && !char.IsWhiteSpace(search[at]))
                    at++;

                var length = at - start;
                if (length == 0)
                    break;

                var found = IndexIgnoreCase(text, next, search, start, length);
                if (found < 0)
                    return false;

                next = found + length;
            }

            return true;
        }

        /// <summary>
        /// Where the word at <paramref name="from" /> in <paramref name="needle" /> first stands in <paramref name="haystack" />,
        /// at or after <paramref name="after" />; -1 where it does not.
        /// </summary>
        /// <remarks>
        /// Written out rather than using string.IndexOf with a relation enum, which is one of the overloads that is easy to lose
        /// on an older target framework.
        /// </remarks>
        static int IndexIgnoreCase(string haystack, int after, string needle, int from, int length)
        {
            if (string.IsNullOrEmpty(haystack))
                return -1;

            var limit = haystack.Length - length;
            for (var start = after; start <= limit; start++)
            {
                var matched = true;
                for (var offset = 0; offset < length; offset++)
                {
                    if (!SameLetter(haystack[start + offset], needle[from + offset]))
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                    return start;
            }
            return -1;
        }

        static bool SameLetter(char a, char b)
        {
            if (a == b)
                return true;
            return Lower(a) == Lower(b);
        }

        static char Lower(char c)
        {
            return c >= 'A' && c <= 'Z' ? (char)(c + 32) : char.ToLowerInvariant(c);
        }
    }
}
