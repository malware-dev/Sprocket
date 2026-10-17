
namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>What says how many bytes a message of one type takes written.</summary>
    /// <typeparam name="TModel">The message type.</typeparam>
    /// <param name="msg">The message, by reference so it is not copied.</param>
    /// <returns>How many bytes it takes.</returns>
    public delegate int SizeOfDataModelFn<TModel>(ref TModel msg) where TModel : struct;
}
