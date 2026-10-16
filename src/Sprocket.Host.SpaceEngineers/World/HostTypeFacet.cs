namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One thing of a type the host provides that a kind of block has: its number, from 1, and its name there.</summary>
    /// <remarks>Screen 2 of a cockpit, called Keyboard: the number is what a program writes, the name what a person reads.</remarks>
    public sealed class HostTypeFacet
    {
        /// <summary>The facet numbered <paramref name="number" />, called <paramref name="name" />.</summary>
        public HostTypeFacet(int number, string name)
        {
            Number = number;
            Name = name;
        }

        /// <summary>Its number among the block's of its type, from 1, as it is passed to the type's function.</summary>
        public int Number { get; }

        /// <summary>What the game calls it on the block.</summary>
        public string Name { get; }
    }
}
