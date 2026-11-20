namespace Sprocket.Host.SpaceEngineers.Framework.Storage.Memory
{
    /// <summary>Turns what Sprocket stores into text a world or a block can hold, and back, through the game's binary serialization.</summary>
    /// <remarks>Only the game may serialize; the harness answers it with the library the game uses.</remarks>
    public interface IBinary
    {
        /// <summary><paramref name="stored" />, one of the stored types, as text.</summary>
        string Pack<T>(T stored);

        /// <summary>What <paramref name="text" /> holds; throws where it holds nothing that reads as one.</summary>
        T Unpack<T>(string text);
    }
}
