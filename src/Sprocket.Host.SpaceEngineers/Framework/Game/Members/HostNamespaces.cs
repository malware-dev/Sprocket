using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// The namespaces this host's members, signals and types belong to, each registered by one owner with the name and author
    /// a player sees: Sprocket's own, and each a mod registers.
    /// </summary>
    /// <remarks>
    /// Static, as one list for the whole host: a program is read where no world is at hand, in the library and on import.
    /// <see cref="Clear" /> is called as the session unloads, so a namespace never outlives the world whose mod registered it.
    /// </remarks>
    public static class HostNamespaces
    {
        /// <summary>The owner Sprocket's own namespace is registered under.</summary>
        public const string Sprocket = "Sprocket";

        static readonly Dictionary<string, HostNamespace> Registered = new Dictionary<string, HostNamespace>();

        static HostNamespaces()
        {
            Clear();
        }

        /// <summary>
        /// Registers the namespace <paramref name="name" />, of one part or more joined by dots, as <paramref name="owner" />'s,
        /// shown as <paramref name="title" /> by <paramref name="author" />; false where another owner has it already.
        /// </summary>
        public static bool Register(string name, string owner, string title, string author)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            HostNamespace had;
            if (Registered.TryGetValue(name, out had))
                return had.Owner == owner;

            Registered.Add(name, new HostNamespace(name, owner, title ?? "", author ?? ""));
            return true;
        }

        /// <summary>Takes back the namespace <paramref name="name" /> where <paramref name="owner" /> registered it.</summary>
        public static void Unregister(string name, string owner)
        {
            HostNamespace had;
            if (name != BlockKinds.Own && Registered.TryGetValue(name, out had) && had.Owner == owner)
                Registered.Remove(name);
        }

        /// <summary>Whether <paramref name="name" /> is one of the host's namespaces, spelled exactly so.</summary>
        public static bool Has(string name)
        {
            return Registered.ContainsKey(name);
        }

        /// <summary>The namespace <paramref name="name" /> as registered, or null where no one has registered it.</summary>
        public static HostNamespace Find(string name)
        {
            HostNamespace found;
            return Registered.TryGetValue(name, out found) ? found : null;
        }

        /// <summary>Every namespace registered, Sprocket's own among them.</summary>
        public static IEnumerable<HostNamespace> All => Registered.Values;

        /// <summary>Forgets every namespace but Sprocket's own, as a session ends.</summary>
        public static void Clear()
        {
            Registered.Clear();
            Registered.Add(BlockKinds.Own, new HostNamespace(BlockKinds.Own, Sprocket, Sprocket, "Malware"));
        }
    }
}
