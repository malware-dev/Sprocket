namespace Sprocket.ModSupport
{
    /// <summary>What a mod tells Sprocket about itself as it registers: its namespace, and its name and author as players read them.</summary>
    public struct SprocketMod
    {
        /// <summary>The namespace unique to the mod that its members' ids are written in, such as <c>malforge.hover_repulsors</c>.</summary>
        /// <remarks>Stored in players' programs: never changed once the mod is released.</remarks>
        public string Namespace;

        /// <summary>The mod's name as players read it, such as "Hover Repulsors".</summary>
        public string Name;

        /// <summary>The mod's author as players read it.</summary>
        public string Author;
    }
}
