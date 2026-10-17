
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// What a name may be, in one place.
    /// </summary>
    public static class NameRules
    {
        /// <summary>
        /// How long a name may be.
        /// </summary>
        public const int MaxLength = 32;

        /// <summary>
        /// A name as it should be stored: one line, trimmed, and cut to <see cref="MaxLength" />.
        /// </summary>
        /// <remarks>
        /// Empty comes back empty. Anything below a space, a line break among them, folds to a space, because a statement in
        /// the saved script ends at the end of its line.
        /// </remarks>
        public static string Clean(string name)
        {
            var wanted = Flatten(name ?? "").Trim();
            return wanted.Length <= MaxLength ? wanted : wanted.Substring(0, MaxLength).TrimEnd();
        }

        static string Flatten(string name)
        {
            var found = -1;
            for (var i = 0; i < name.Length; i++)
            {
                if (name[i] >= ' ')
                    continue;

                found = i;
                break;
            }

            // The ordinary case keeps the string that arrived, allocating nothing.
            if (found < 0)
                return name;

            var flat = name.ToCharArray();
            for (var i = found; i < flat.Length; i++)
                if (flat[i] < ' ')
                    flat[i] = ' ';

            return new string(flat);
        }
    }
}
