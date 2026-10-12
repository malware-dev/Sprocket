using System;
using System.IO;
using ProtoBuf;
using ProtoBuf.Meta;
using Sprocket.Host.SpaceEngineers.Memory;

namespace Sprocket.Sim.Machine;

/// <summary>The game's binary serialization, as the game does it: the library's serializer, in base-64.</summary>
/// <remarks>
/// The game installs the library's model as it starts; outside it there is none until one is made, and making one replaces
/// the library's default for the whole process, so it is made once.
/// </remarks>
public sealed class ProtoBinary : IBinary
{
    static readonly Lazy<TypeModel> Model = new Lazy<TypeModel>(() => RuntimeTypeModel.Create(true));

    /// <inheritdoc />
    public string Pack(WorldMemory memory)
    {
        _ = Model.Value;
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, memory);
        return Convert.ToBase64String(stream.GetBuffer(), 0, (int)stream.Length);
    }

    /// <inheritdoc />
    public WorldMemory Unpack(string text)
    {
        _ = Model.Value;
        using var stream = new MemoryStream(Convert.FromBase64String(text));
        return Serializer.Deserialize<WorldMemory>(stream);
    }
}
