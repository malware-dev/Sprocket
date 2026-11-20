using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>How a reader takes the attributes of a statement: the ones Sprocket uses by name, and the rest held as written.</summary>
    public static class ScriptAttributes
    {
        /// <summary>A system's highest sequence number handed out, an integer: <c>#@ lastSequence: 7</c>.</summary>
        public const string LastSequence = "lastSequence";

        /// <summary>The block a system lists on alone, text naming a declared block: <c>#@ listedOn: "Back Office PB"</c>.</summary>
        public const string ListedOn = "listedOn";

        /// <summary>A signal slot's few words shown with each toolbar slot that answers it, text: <c>#@ short: "Call"</c>.</summary>
        public const string Short = "short";

        /// <summary>A system's slot categories in the order its installation panel shows them, a list of text read to stand the slots and never written: <c>#@ categories: [ "Doors", "Lights" ]</c>.</summary>
        public const string Categories = "categories";

        /// <summary>The category a slot is shown under with the slots beside it under the same one, text: <c>#@ category: "Doors"</c>.</summary>
        public const string Category = "category";

        /// <summary>How a setting's value is edited and shown beyond its type, text naming the editor: <c>#@ editor: "percent"</c>.</summary>
        public const string Editor = "editor";

        /// <summary>The attributes of <paramref name="statement" /> other than those named in <paramref name="used" />, held as written, or null where there are none.</summary>
        public static UnusedAttributes Unused(Statement statement, params string[] used)
        {
            UnusedAttributes found = null;
            foreach (var attribute in statement.Attributes)
            {
                if (System.Array.IndexOf(used, attribute.Name) >= 0)
                    continue;

                if (found == null)
                    found = new UnusedAttributes();

                found.Add(attribute.Name, attribute.Written);
            }

            return found;
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
