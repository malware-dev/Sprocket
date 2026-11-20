using System;

namespace Sprocket.ModSupport
{
    /// <summary>The naming convention every id a mod gives Sprocket keeps: lowercase words joined by underscores.</summary>
    public static class SprocketIds
    {
        /// <summary>Throws where <paramref name="id" />, given as <paramref name="what" />, breaks the convention.</summary>
        /// <remarks>Sees only the form: <c>speedunit</c> passes as readily as <c>speed_unit</c>.</remarks>
        public static void Check(string id, string what)
        {
            if (!Follows(id))
                throw new ArgumentException($"{what} \"{id}\" breaks Sprocket's naming convention. Please keep to it: lowercase words joined by single underscores, starting with a letter, such as speed_unit.");
        }

        /// <summary>Throws where <paramref name="space" />, a namespace, breaks the convention in any of its dot-joined parts.</summary>
        public static void CheckNamespace(string space)
        {
            if (string.IsNullOrEmpty(space))
                throw new ArgumentException("SprocketMod.Namespace is missing. Give your mod a namespace unique to it, such as your_name.your_mod.");

            foreach (var part in space.Split('.'))
            {
                if (!Follows(part))
                    throw new ArgumentException($"Namespace \"{space}\" breaks Sprocket's naming convention. Please keep to it: parts joined by dots, each lowercase words joined by single underscores, such as your_name.your_mod.");
            }
        }

        /// <summary>Whether <paramref name="id" /> keeps the convention.</summary>
        public static bool Follows(string id)
        {
            if (string.IsNullOrEmpty(id) || id[0] < 'a' || id[0] > 'z' || id[id.Length - 1] == '_')
                return false;

            for (var i = 0; i < id.Length; i++)
            {
                var c = id[i];
                if (c == '_')
                {
                    if (id[i - 1] == '_')
                        return false;

                    continue;
                }

                if ((c < 'a' || c > 'z') && (c < '0' || c > '9'))
                    return false;
            }

            return true;
        }

        /// <summary>The id an enum member's C# name stands for: <c>KilometersPerHour</c> is <c>kilometers_per_hour</c>.</summary>
        /// <remarks>Splits where the name has a capital; the result is checked like any other id.</remarks>
        public static string FromName(string name)
        {
            var id = new System.Text.StringBuilder(name.Length + 8);
            for (var i = 0; i < name.Length; i++)
            {
                var c = name[i];
                if (char.IsUpper(c))
                {
                    if (i > 0)
                        id.Append('_');
                    id.Append(char.ToLowerInvariant(c));
                }
                else
                    id.Append(c);
            }

            return id.ToString();
        }
    }
}
