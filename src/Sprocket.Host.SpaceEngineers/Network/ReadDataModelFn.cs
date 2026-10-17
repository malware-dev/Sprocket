
namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>What reads a message of one type from a byte array.</summary>
    /// <typeparam name="TModel">The message type.</typeparam>
    /// <param name="data">The source.</param>
    /// <param name="offset">Where to read; moved on past the message.</param>
    /// <param name="msg">The message read.</param>
    /// <returns>How many bytes were read.</returns>
    public delegate int ReadDataModelFn<TModel>(byte[] data, ref int offset, out TModel msg) where TModel : struct;
}
