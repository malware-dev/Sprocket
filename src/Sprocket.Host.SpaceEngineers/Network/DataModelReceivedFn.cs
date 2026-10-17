
namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>What is called when a message of one type arrives.</summary>
    /// <typeparam name="TModel">The message type.</typeparam>
    /// <param name="channelId">The channel it arrived on.</param>
    /// <param name="msg">The message.</param>
    /// <param name="senderId">The sender's Steam ID.</param>
    /// <param name="fromServer">Whether the server sent it.</param>
    public delegate void DataModelReceivedFn<in TModel>(ushort channelId, TModel msg, ulong senderId, bool fromServer) where TModel : struct;
}
