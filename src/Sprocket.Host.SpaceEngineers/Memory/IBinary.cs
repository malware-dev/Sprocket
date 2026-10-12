namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>Turns what the world remembers into text a world can keep, and back, through the game's binary serialization.</summary>
    /// <remarks>Asked of the game, which alone may serialize: the harness answers it with the same library the game uses.</remarks>
    public interface IBinary
    {
        /// <summary><paramref name="memory" /> as text.</summary>
        string Pack(WorldMemory memory);

        /// <summary>What <paramref name="text" /> holds; throws where it holds nothing that reads as one.</summary>
        WorldMemory Unpack(string text);
    }
}
