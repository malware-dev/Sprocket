using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.Components.Interfaces;
using VRage.Game.ModAPI;
using VRage.ObjectBuilders;

namespace Sprocket.Sim.Machine;

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
    /// switched off. Sprocket listens for that rather than asking every tick, so a rig that changed
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
        // Nothing, rather than a refusal. A block on the rig carries no resource components, and
        // the mod reads "no components to ask" as "not moving anything" - which is what a block
        // that models no gas at all should look like.
        var container = Pretend.To<IMyEntityComponentContainer>()
            .Answers("TryGet", Component)
            .Answers("Get", _ => null)
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
    /// The rig answers this because Sprocket asks it before writing one into a slot.
    /// </remarks>
    public virtual bool Offers(string actionId)
    {
        return actionId == "OnOff" || actionId == "OnOff_On" || actionId == "OnOff_Off"
            || Named(actionId);
    }

    /// <summary>
    /// Everything this block declares it can be told to do, as the terminal lists them.
    /// </summary>
    /// <remarks>
    /// Including the bare toggle, which is what makes it worth having on the rig: Sprocket says open
    /// and close in its own words and must not then offer the toggle they came from a third time.
    /// </remarks>
    public virtual void Offered(List<Offer> into)
    {
        into.Add(new Offer("OnOff", "Toggle block On/Off"));
        into.Add(new Offer("OnOff_On", "Turn block On"));
        into.Add(new Offer("OnOff_Off", "Turn block Off"));

        for (var i = 0; i < _offers.Count; i++)
            into.Add(_offers[i]);
    }

    /// <summary>
    /// Gives this block one more of the game's own actions, which Sprocket knows nothing about.
    /// </summary>
    /// <remarks>
    /// Most of a ship is blocks like this: a piston that extends, a connector that connects, a tank
    /// that stockpiles. Nobody has written an extension for any of them and nobody ever will write
    /// one for a block a mod adds next year, so what Sprocket can say about them is whatever they
    /// declare - and the rig has to be able to declare something for that to be tested at all.
    /// </remarks>
    public Fitting Offering(string id, string text)
    {
        return Offering(id, text, 0);
    }

    /// <summary>The same, for one that has to be given values before it can do anything.</summary>
    /// <remarks>
    /// Most of the game's value-taking actions want text and parse it themselves - a radius, a
    /// colour as three numbers. Sprocket leaves them out until a step can carry a value, and the
    /// rig has to be able to declare one for that to be tested.
    /// </remarks>
    public Fitting Offering(string id, string text, int wants)
    {
        _offers.Add(new Offer(id, text, wants));
        return this;
    }

    /// <summary>
    /// Every value this block carries that can be told what to be.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="Offered" />. A real block answers this out of its own terminal
    /// controls, which describe themselves; the rig says what it carries so a test can watch a value
    /// being told something.
    /// </remarks>
    /// <summary>
    /// Every value this block carries, the name every block carries first.
    /// </summary>
    /// <remarks>
    /// The game gives every terminal block a name that can be written to, which is the floor under
    /// the values the way the switch is the floor under the actions.
    /// </remarks>
    public virtual void Carried(List<Offer> into)
    {
        into.Add(new Offer("CustomName", "Name", ValueKind.Text, "", ""));

        for (var i = 0; i < _carries.Count; i++)
            into.Add(_carries[i]);
    }

    /// <summary>Gives this block one of the block's own values, with what it will accept.</summary>
    public Fitting Carrying(string id, string text, ValueKind kind, string least, string most)
    {
        _carries.Add(new Offer(id, text, kind, least, most));
        return this;
    }

    /// <summary>Gives it one with no bounds, which is what a value that is not a number has.</summary>
    public Fitting Carrying(string id, string text, ValueKind kind)
    {
        return Carrying(id, text, kind, "", "");
    }

    /// <summary>
    /// Gives it one that may only be a listed few, each word standing for what the block is told.
    /// </summary>
    /// <remarks>
    /// What a combo box on the terminal amounts to. The game reports one as a whole number and keeps
    /// the words on the control, so the two lists are written out here the same way they arrive.
    /// </remarks>
    public Fitting Carrying(string id, string text, params string[] wordsAndMeanings)
    {
        var choices = new List<Choice>();
        for (var i = 0; i + 1 < wordsAndMeanings.Length; i += 2)
            choices.Add(new Choice(wordsAndMeanings[i], wordsAndMeanings[i + 1]));

        _carries.Add(new Offer(id, text, ValueKind.Integer).Between(choices));
        return this;
    }

    /// <summary>
    /// Gives this block a value that is one of a listed few, where the game will not hand over the list.
    /// </summary>
    /// <remarks>
    /// What a real block does wherever it fills a drop-down per block: the list a mod is handed throws
    /// when it is read, and all that comes through is the number.
    /// </remarks>
    public Fitting CarryingUnread(string id, string text)
    {
        _carries.Add(new Offer(id, text, ValueKind.Integer).Unread());
        return this;
    }

    /// <summary>
    /// Tells one of those values what to be, and writes down that it was told.
    /// </summary>
    /// <remarks>
    /// Written down rather than modelled, because what a test about values needs to know is which
    /// value arrived and how it was written.
    /// </remarks>
    public virtual bool Set(string propertyId, Literal value)
    {
        if (!Carries(propertyId))
            return false;

        Told(propertyId + " " + value.Said);
        return true;
    }

    bool Carries(string propertyId)
    {
        for (var i = 0; i < _carries.Count; i++)
        {
            if (_carries[i].Id == propertyId)
                return true;
        }

        return false;
    }

    readonly List<Offer> _carries = new List<Offer>();

    bool Named(string actionId)
    {
        for (var i = 0; i < _offers.Count; i++)
        {
            if (_offers[i].Id == actionId)
                return true;
        }

        return false;
    }

    readonly List<Offer> _offers = new List<Offer>();

    /// <summary>Does what one of those actions does, as the game would when a slot invokes it.</summary>
    public void Perform(string actionId)
    {
        Perform(actionId, null);
    }

    /// <summary>
    /// Does one of the game's own actions, with whatever values it was given.
    /// </summary>
    /// <remarks>
    /// An action the rig declares but does not model writes itself down instead, with its values,
    /// because what a test about values needs to know is which values arrived and in what order.
    /// </remarks>
    public virtual void Perform(string actionId, IList<Literal> values)
    {
        if (actionId == "OnOff_On")
            Enabled = true;
        else if (actionId == "OnOff_Off")
            Enabled = false;
        else if (actionId == "OnOff")
            Enabled = !Enabled;
        else if (Named(actionId))
            Told(Said(actionId, values));
    }

    static string Said(string actionId, IList<Literal> values)
    {
        if (values == null || values.Count == 0)
            return actionId;

        var said = actionId;
        for (var i = 0; i < values.Count; i++)
            said += " " + values[i].Said;

        return said;
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
