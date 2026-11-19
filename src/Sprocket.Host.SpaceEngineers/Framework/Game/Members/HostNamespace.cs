namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>One of the host's namespaces: its name in programs, who registered it, and the mod's name and author a player sees.</summary>
    public sealed class HostNamespace
    {
        /// <summary>The namespace <paramref name="name" />, registered by <paramref name="owner" />, shown as <paramref name="title" /> by <paramref name="author" />.</summary>
        public HostNamespace(string name, string owner, string title, string author)
        {
            Name = name;
            Owner = owner;
            Title = title;
            Author = author;
        }

        /// <summary>The namespace as programs write it, parts joined by dots: <c>malforge.hover_repulsors</c>.</summary>
        public string Name { get; }

        /// <summary>Who registered it, the one owner who may take it back.</summary>
        public string Owner { get; }

        /// <summary>The name of the mod it belongs to, as a player sees it.</summary>
        public string Title { get; }

        /// <summary>Who made the mod, as a player sees it.</summary>
        public string Author { get; }
    }
}
