namespace Sprocket.Host.SpaceEngineers.Memory
{
    /// <summary>Turns what Sprocket keeps into text a world or a block can hold, and back, through the game's binary serialization.</summary>
    /// <remarks>Only the game may serialize; the harness answers it with the library the game uses.</remarks>
    public interface IBinary
    {
        /// <summary><paramref name="kept" />, one of the kept types, as text.</summary>
        string Pack<T>(T kept);

        /// <summary>What <paramref name="text" /> holds; throws where it holds nothing that reads as one.</summary>
        T Unpack<T>(string text);
    }
}
