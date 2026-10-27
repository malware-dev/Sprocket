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

        /// <summary>A system's slot categories in the order its installation panel shows them, a list of text read to stand the slots and never written: <c>#@ categories: [ "Doors", "Lights" ]</c>.</summary>
        public const string Categories = "categories";

        /// <summary>The category a slot is shown under with the slots beside it under the same one, text: <c>#@ category: "Doors"</c>.</summary>
        public const string Category = "category";

        /// <summary>The attributes of <paramref name="statement" /> other than those named in <paramref name="used" />, kept as written, or null where there are none.</summary>
        public static KeptAttributes Unused(Statement statement, params string[] used)
        {
            KeptAttributes kept = null;
            foreach (var attribute in statement.Attributes)
            {
                if (System.Array.IndexOf(used, attribute.Name) >= 0)
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
