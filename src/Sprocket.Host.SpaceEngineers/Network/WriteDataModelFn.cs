
namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>What writes a message of one type to a byte array.</summary>
    /// <typeparam name="TModel">The message type.</typeparam>
    /// <param name="msg">The message, by reference so it is not copied.</param>
    /// <param name="data">The destination.</param>
    /// <param name="offset">Where to write; moved on past the message.</param>
    /// <returns>How many bytes were written.</returns>
    public delegate int WriteDataModelFn<TModel>(ref TModel msg, byte[] data, ref int offset);
}
