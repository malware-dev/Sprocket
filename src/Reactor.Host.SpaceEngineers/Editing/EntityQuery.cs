using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Editing
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
    /// not a prefix, and case is never their problem.
    /// </remarks>
    public static class EntityQuery
    {
        /// <summary>Whether a plain label matches a search, just as forgivingly.</summary>
        public static bool MatchesText(string text, string search)
        {
            return string.IsNullOrEmpty(search) || ContainsIgnoreCase(text, search);
        }

        public static bool Matches(GridEntity entity, string search, string kindId)
        {
            if (entity == null)
                return false;

            if (!string.IsNullOrEmpty(kindId) && entity.Kind.Id != kindId)
                return false;

            if (string.IsNullOrEmpty(search))
                return true;

            return ContainsIgnoreCase(entity.Name, search)
                || ContainsIgnoreCase(entity.Kind.DisplayName, search);
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

        // Written out rather than using string.IndexOf with a relation enum, which
        // is one of the overloads that is easy to lose on an older target framework.
        static bool ContainsIgnoreCase(string haystack, string needle)
        {
            if (string.IsNullOrEmpty(haystack) || needle.Length > haystack.Length)
                return false;

            var limit = haystack.Length - needle.Length;
            for (var start = 0; start <= limit; start++)
            {
                var matched = true;
                for (var offset = 0; offset < needle.Length; offset++)
                {
                    if (!SameLetter(haystack[start + offset], needle[offset]))
                    {
                        matched = false;
                        break;
                    }
                }
                if (matched)
                    return true;
            }
            return false;
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
