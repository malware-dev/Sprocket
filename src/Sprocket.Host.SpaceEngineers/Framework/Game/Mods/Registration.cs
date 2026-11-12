using System;
using System.Collections.Generic;
using Sprocket.ModApi;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>Reads the parts of a member another mod has registered through Sprocket's mod API.</summary>
    public static class Registration
    {
        /// <summary>The part <paramref name="key" /> of <paramref name="member" /> as a <typeparamref name="T" />, or the default where it is missing or of another type.</summary>
        public static T Part<T>(Dictionary<string, object> member, string key)
        {
            object part;
            return member.TryGetValue(key, out part) && part is T ? (T)part : default(T);
        }

        /// <summary>The ids of the choice <paramref name="member" /> registers, as a stored program keeps them; empty where it gives none.</summary>
        public static string[] Choices(Dictionary<string, object> member)
        {
            return Part<string[]>(member, ModMemberKeys.Choices) ?? new string[0];
        }

        /// <summary>The words a player reads for the choice <paramref name="member" /> registers, one for each id; the ids themselves where the mod names them otherwise than one each.</summary>
        public static string[] ChoiceWords(Dictionary<string, object> member)
        {
            var ids = Choices(member);
            var names = Part<Func<string[]>>(member, ModMemberKeys.ChoiceNames);
            var words = names == null ? null : names();
            return words != null && words.Length == ids.Length ? words : ids;
        }

        /// <summary>The number <paramref name="key" /> of <paramref name="member" /> as a field writes a bound, or empty where none is given.</summary>
        public static string Bound(Dictionary<string, object> member, string key)
        {
            object part;
            return member.TryGetValue(key, out part) && part is double
                ? ((double)part).ToString("0.######", System.Globalization.CultureInfo.InvariantCulture)
                : "";
        }
    }
}
