using System;
using System.Collections.Generic;
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

    /// <summary>
    /// Whether this block is switched on, and working, which the rig treats as one thing.
    /// </summary>
    /// <remarks>
    /// Setting it raises <c>IsWorkingChanged</c>, as the game does when a block loses power or is
    /// switched off. Reactor listens for that rather than asking every tick, so a rig that changed
    /// quietly would leave the listening untested.
    /// </remarks>
    public bool Enabled
    {
        get { return _enabled; }
        set
        {
            if (_enabled == value)
                return;

            _enabled = value;

            for (var i = 0; i < _watching.Count; i++)
                _watching[i](Block);
        }
    }

    bool _enabled = true;

    /// <summary>Whoever is listening for this block starting or stopping working.</summary>
    readonly List<Action<IMyCubeBlock>> _watching = new List<Action<IMyCubeBlock>>();

    /// <summary>What the mod holds: the block, as the game's own interface.</summary>
    public IMyTerminalBlock Block { get; private set; }

    /// <summary>The toolbar this block carries, or null where it carries none.</summary>
    public Toolbar Toolbar { get; protected set; }

    public IMyCubeGrid Grid { get; set; }

    /// <summary>
    /// Where what this block is told to do gets written down, which is the rig's own journal.
    /// </summary>
    /// <remarks>
    /// One list for the whole construct, in the order things happened. Both halves are asked about:
    /// which block got an action - the port airlock's inner door must shut and the front one must
    /// not - and in what order, because a step that ran before the one above it finished is the
    /// failure the whole model is shaped to prevent.
    /// </remarks>
    public List<string> Journal { get; set; }

    protected void Told(string action)
    {
        if (Journal != null)
            Journal.Add(Name + ":" + action);
    }

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
            .Does("add_IsWorkingChanged", args => Watch((Action<IMyCubeBlock>)args[0], true))
            .Does("remove_IsWorkingChanged", args => Watch((Action<IMyCubeBlock>)args[0], false))
            .Gives("Enabled", () => Enabled)
            .Takes("Enabled", value =>
            {
                Enabled = (bool)value;
                Told(Enabled ? "on" : "off");
            })
            .Gives("CubeGrid", () => Grid)
            .Gives("Storage", () => Storage)
            .Takes("Storage", value => Storage = (MyModStorageComponentBase)value)
            .Gives("Components", () => container);
    }

    /// <summary>
    /// Whether this block has an action of the game's own by that name.
    /// </summary>
    /// <remarks>
    /// Every functional block has the on/off pair; a block with a switch of its own adds that pair,
    /// which is the game's convention - a switch called <c>X</c> offers <c>X_On</c> and <c>X_Off</c>.
    /// The rig answers this because Reactor asks it before writing one into a slot.
    /// </remarks>
    public virtual bool Offers(string actionId)
    {
        return actionId == "OnOff_On" || actionId == "OnOff_Off";
    }

    /// <summary>Does what one of those actions does, as the game would when a slot invokes it.</summary>
    public virtual void Perform(string actionId)
    {
        if (actionId == "OnOff_On")
            Enabled = true;
        else if (actionId == "OnOff_Off")
            Enabled = false;
    }

    /// <summary>Takes a listener on or off, answering nothing, as an event accessor does.</summary>
    object Watch(Action<IMyCubeBlock> listener, bool on)
    {
        if (on)
            _watching.Add(listener);
        else
            _watching.Remove(listener);

        return null;
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
