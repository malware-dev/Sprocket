using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>How a reader takes the attributes of a statement: the ones Sprocket uses by name, and the rest kept as written.</summary>
    public static class ScriptAttributes
    {
        /// <summary>A system's highest sequence number handed out, a whole number: <c>#@ lastSequence: 7</c>.</summary>
        public const string LastSequence = "lastSequence";

        /// <summary>A signal slot's few words shown with each toolbar slot that answers it, text: <c>#@ short: "Call"</c>.</summary>
        public const string Short = "short";

        /// <summary>The attributes of <paramref name="statement" /> other than <paramref name="used" />, kept as written, or null where there are none.</summary>
        public static KeptAttributes Unused(Statement statement, string used = null)
        {
            KeptAttributes kept = null;
            foreach (var attribute in statement.Attributes)
            {
                if (attribute.Name == used)
                    continue;

                if (kept == null)
                    kept = new KeptAttributes();

                kept.Add(attribute.Name, attribute.Written);
            }

            return kept;
        }

        /// <summary>Whether <paramref name="statement" /> has the attribute <paramref name="name" />, and which.</summary>
        public static bool Find(Statement statement, string name, out ScriptAttribute found)
        {
            foreach (var attribute in statement.Attributes)
            {
                if (attribute.Name == name)
                {
                    found = attribute;
                    return true;
                }
            }

            found = default(ScriptAttribute);
            return false;
        }
    }
}
