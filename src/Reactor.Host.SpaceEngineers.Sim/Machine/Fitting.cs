using Reactor.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Components.Interfaces;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Reactor.Sim.Machine;

/// <summary>
/// One block fitted to the rig: what the game would hand the mod, and what it does over time.
/// </summary>
/// <remarks>
/// The mod reads blocks through the game's own interfaces, so the rig answers those interfaces
/// rather than a smaller set of its own. What is behind them is made up - a door that takes a
/// second to open, a vent that fills - which is the part a real ship supplies by being a ship.
/// </remarks>
abstract class Fitting
{
    readonly SerializableDefinitionId _definition;
    readonly string _display;

    protected Fitting(long id, string name, MyObjectBuilderType type, string subtype, string display)
    {
        Id = id;
        Name = name;
        _definition = new SerializableDefinitionId(type, subtype);
        _display = display;
    }

    public long Id { get; private set; }

    public string Name { get; set; }

    public bool Enabled { get; set; } = true;

    /// <summary>What the mod holds: the block, as the game's own interface.</summary>
    public IMyTerminalBlock Block { get; private set; }

    /// <summary>The toolbar this block carries, or null where it carries none.</summary>
    public Toolbar Toolbar { get; protected set; }

    public IMyCubeGrid Grid { get; set; }

    public MyModStorageComponentBase Storage { get; set; }

    /// <summary>A moment of the rig running, which for most blocks is nothing at all.</summary>
    public virtual void Tick()
    {
    }

    protected void Fit<T>(Stand<T> stand) where T : class, IMyTerminalBlock
    {
        Block = Shared(stand).Done();
    }

    /// <summary>What every block answers, whatever else it is.</summary>
    Stand<T> Shared<T>(Stand<T> stand) where T : class, IMyTerminalBlock
    {
        var container = Pretend.To<IMyEntityComponentContainer>()
            .Answers("TryGet", Component)
            .Done();

        return stand
            .Gives("EntityId", () => Id)
            .Gives("CustomName", () => Name)
            .Takes("CustomName", value => Name = (string)value)
            .Gives("DisplayNameText", () => Name)
            .Gives("BlockDefinition", () => _definition)
            .Gives("DefinitionDisplayNameText", () => _display)
            .Gives("IsFunctional", () => true)
            .Gives("IsWorking", () => Enabled)
            .Gives("Enabled", () => Enabled)
            .Takes("Enabled", value => Enabled = (bool)value)
            .Gives("CubeGrid", () => Grid)
            .Gives("Storage", () => Storage)
            .Takes("Storage", value => Storage = (MyModStorageComponentBase)value)
            .Gives("Components", () => container);
    }

    /// <summary>
    /// The one component the mod ever asks a block for, which is its toolbar.
    /// </summary>
    /// <remarks>
    /// A block does not implement the provider, it carries one - so a block with no toolbar answers
    /// no, which is the ordinary case for a door.
    /// </remarks>
    object Component(Castle.DynamicProxy.IInvocation invocation)
    {
        var wanted = invocation.GenericArguments.Length == 1 ? invocation.GenericArguments[0] : null;
        if (wanted != typeof(IMyToolbarProvider) || Toolbar == null)
        {
            invocation.Arguments[0] = null;
            return false;
        }

        invocation.Arguments[0] = Pretend.To<IMyToolbarProvider>()
            .Gives("ToolbarCount", () => 1)
            .Does("GetToolbar", () => Toolbar)
            .Done();

        return true;
    }
}
