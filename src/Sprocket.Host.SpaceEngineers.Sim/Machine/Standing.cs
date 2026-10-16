using Sandbox.ModAPI;
using Sprocket.Sim.Fakes;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

/// <summary>A block of the game's interface <typeparamref name="T" />, answering what a test gives it and nothing more.</summary>
/// <remarks>For a block the harness has no fitting of its own for, standing as the interface a capability claims it by.</remarks>
public sealed class Standing<T> : Fitting where T : class, IMyTerminalBlock
{
    /// <summary>A block, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />.</summary>
    public Standing(long id, string name, MyObjectBuilderType type, string subtype)
        : base(id, name, type, subtype, name)
    {
        Fit(Pretend.To<T>());
    }
}
