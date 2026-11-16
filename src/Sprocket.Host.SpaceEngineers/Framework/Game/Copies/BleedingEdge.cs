namespace Sprocket.Host.SpaceEngineers.Framework.Game.Copies
{
    /// <summary>
    /// Which mod in a world is Sprocket's Bleeding Edge copy, the development build that the stable copy stands down for when
    /// a world holds both. Other mods' Bleeding Edge builds are not Sprocket and change nothing.
    /// </summary>
    public static class BleedingEdge
    {
        /// <summary>The Bleeding Edge copy's name, as its Workshop item and its local deploy folder both give it.</summary>
        public const string Name = "Sprocket Automation (Bleeding Edge)";

        /// <summary>The Bleeding Edge copy's Steam Workshop item.</summary>
        public const ulong ItemId = 3807234771;

        /// <summary>
        /// Whether the mod a world lists as <paramref name="name" />, from Workshop item <paramref name="itemId" /> (0 for a local
        /// mod), is Sprocket's Bleeding Edge copy.
        /// </summary>
        public static bool Is(string name, ulong itemId)
        {
            return itemId == ItemId || name == Name;
        }

        /// <summary>
        /// Whether the running copy, known to the game as <paramref name="modName" /> with id <paramref name="modId" /> (a
        /// Workshop copy's id is its item number and ".sbm"), is Sprocket's Bleeding Edge copy.
        /// </summary>
        public static bool IsRunning(string modName, string modId)
        {
            return modName == Name || modId == ItemId + ".sbm";
        }
    }
}
