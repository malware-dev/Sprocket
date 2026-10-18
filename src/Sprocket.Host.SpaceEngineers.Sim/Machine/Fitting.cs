using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Sim.Fakes;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Components.Interfaces;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Sim.Machine;

/// <summary>
/// One block fitted to the rig: what the game would hand the mod, and what it does over time.
/// </summary>
/// <remarks>
/// The mod reads blocks through the game's own interfaces, so the rig answers those interfaces
/// rather than a smaller set of its own. What is behind them is made up - a door that takes a
/// second to open, a vent that fills - which is the part a real ship supplies by being a ship.
/// </remarks>
public abstract class Fitting
{
    readonly SerializableDefinitionId _definition;
    readonly string _display;
    readonly List<TerminalActionParameter> _held = new List<TerminalActionParameter>();

    /// <summary>
    /// What the game does when a slot of this block's toolbar is invoked: run whatever it holds, with what it holds it with.
    /// </summary>
    /// <remarks>
    /// Installed by whoever is running the rig, because what a slot's payload means is Sprocket's
    /// business and not the block's.
    /// <para>
    /// Handed the block the slot names as well as the payload, because the game runs an action
    /// <em>on a block</em>: a slot naming one that is gone does nothing at all. Without that, a
    /// slot here reached Sprocket however broken the wiring was, and the rig answered a slot the
    /// game would have left silent.
    /// </para>
    /// </remarks>
    public Action<long, string>? Invokes;

    /// <summary>
    /// What the game does when a slot holds a block's own action: perform it on that block.
    /// </summary>
    /// <remarks>
    /// Installed by whoever is running the rig, because finding a block by its id is the construct's
    /// business. A trigger that does one plain thing is wired this way, and Sprocket is not part of
    /// it at all - which is the property worth being able to test.
    /// </remarks>
    public Action<long, string>? Performs;

    /// <summary>A block, <paramref name="id" />, called <paramref name="name" />, of the block type <paramref name="type" /> and <paramref name="subtype" />, shown as <paramref name="display" />.</summary>
    protected Fitting(long id, string name, MyObjectBuilderType type, string subtype, string display)
    {
        Id = id;
        Name = name;
        _definition = new SerializableDefinitionId(type, subtype);
        _display = display;
    }

    /// <summary>The block's id.</summary>
    public long Id { get; private set; }

    /// <summary>The block's type and subtype.</summary>
    public SerializableDefinitionId Definition => _definition;

    /// <summary>The block's name, raising <c>CustomNameChanged</c> as it changes, as the game does.</summary>
    public string Name
    {
        get { return _name; }
        set
        {
            if (_name == value)
                return;

            _name = value;
            for (var i = 0; i < _renaming.Count; i++)
                _renaming[i](Block);
        }
    }

    string _name = "";

    readonly List<Action<IMyTerminalBlock>> _renaming = new List<Action<IMyTerminalBlock>>();

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

            for (var i = 0; i < _switching.Count; i++)
                _switching[i]((IMyTerminalBlock)Block);
        }
    }

    bool _enabled = true;

    /// <summary>The free text every block carries, raising <c>CustomDataChanged</c> as it changes, as the game does.</summary>
    public string CustomData
    {
        get { return _customData; }
        set
        {
            if (_customData == value)
                return;

            _customData = value;
            for (var i = 0; i < _writing.Count; i++)
                _writing[i](Block);
        }
    }

    string _customData = "";

    readonly List<Action<IMyTerminalBlock>> _writing = new List<Action<IMyTerminalBlock>>();

    /// <summary>The block as its grid holds it, which the grid gives it as it is welded; null before.</summary>
    public IMySlimBlock? Slim { get; set; }

    /// <summary>The identity of the player who owns the block, or 0 where nobody does; setting it tells its grid, as the game does.</summary>
    public long Owner
    {
        get { return _owner; }
        set
        {
            if (_owner == value)
                return;

            _owner = value;
            if (Owning != null)
                Owning();
        }
    }

    long _owner;

    /// <summary>Who the owner shares the block with.</summary>
    public Sharing Share { get; set; }

    /// <summary>Which faction each player is in, which the rig hands every block it welds; null for none.</summary>
    public Factions? Factions { get; set; }

    /// <summary>The players using every terminal as admins, whom the game's own access test lets in to any block; null for none.</summary>
    public HashSet<long>? Admins { get; set; }

    /// <summary>What the grid does when the block changes hands, which the grid installs as it welds the block.</summary>
    public Action? Owning { get; set; }

    /// <summary>
    /// Whether the player <paramref name="identity" /> may use the block, as the game's own access test answers it.
    /// </summary>
    /// <remarks>A block nobody owns is anybody's; otherwise its owner, and whoever its share setting reaches.</remarks>
    public bool Lets(long identity)
    {
        if (_owner == 0 || identity == _owner || Share == Sharing.All)
            return true;

        return Share == Sharing.Faction && Factions != null && Factions.Same(_owner, identity);
    }

    /// <summary>How the player <paramref name="identity" /> stands to the block's owner, by its owner and share setting.</summary>
    public MyRelationsBetweenPlayerAndBlock Relation(long identity)
    {
        if (_owner == 0)
            return MyRelationsBetweenPlayerAndBlock.NoOwnership;
        if (identity == _owner)
            return MyRelationsBetweenPlayerAndBlock.Owner;

        return Lets(identity) ? MyRelationsBetweenPlayerAndBlock.FactionShare : MyRelationsBetweenPlayerAndBlock.Enemies;
    }

    readonly List<Action<IMyCubeBlock>> _watching = new List<Action<IMyCubeBlock>>();
    readonly List<Action<IMyTerminalBlock>> _switching = new List<Action<IMyTerminalBlock>>();
    readonly List<Action<IMyEntity>> _closing = new List<Action<IMyEntity>>();

    /// <summary>What the mod holds: the block, as the game's own interface.</summary>
    public IMyTerminalBlock Block { get; private set; }

    /// <summary>The toolbar this block carries, or null where it carries none.</summary>
    public Toolbar Toolbar { get; protected set; }

    /// <summary>The grid the block is on, or null before it is welded to one.</summary>
    public IMyCubeGrid Grid { get; set; }

    /// <summary>Whether the block is shown in toolbar config, the game's setting that keeps a block out of toolbar lists.</summary>
    public bool InToolbars { get; set; } = true;

    /// <summary>Whether the block has been closed, by being ground off or by its grid being deleted.</summary>
    public bool Closed { get; private set; }

    /// <summary>Where the block stands on its grid, which the grid gives it as it is welded where nothing set one.</summary>
    public Vector3I? Cell { get; set; }

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

    /// <summary>Writes <paramref name="action" /> into the journal, as the block being told to do it.</summary>
    protected void Told(string action)
    {
        if (Journal != null)
            Journal.Add(Name + ":" + action);
    }

    /// <summary>What mods keep on the block.</summary>
    public MyModStorageComponentBase Storage { get; set; }

    /// <summary>Closes the block as the game does, telling whoever listens for it closing.</summary>
    public void Close()
    {
        if (Closed)
            return;

        Closed = true;
        for (var i = _closing.Count - 1; i >= 0; i--)
            _closing[i](Block);
    }

    /// <summary>A moment of the rig running, which for most blocks is nothing at all.</summary>
    public virtual void Tick()
    {
    }

    /// <summary>Makes the block answer as <typeparamref name="T" />, with <paramref name="stand" /> saying how, on top of what every block answers.</summary>
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
            .Gives("Closed", () => Closed)
            .Gives("MarkedForClose", () => Closed)
            .Does("add_OnMarkForClose", args => { _closing.Add((Action<IMyEntity>)args[0]); })
            .Does("remove_OnMarkForClose", args => { _closing.Remove((Action<IMyEntity>)args[0]); })
            .Gives("CustomName", () => Name)
            .Takes("CustomName", value => Name = (string)value)
            .Does("add_CustomNameChanged", args => { _renaming.Add((Action<IMyTerminalBlock>)args[0]); })
            .Does("remove_CustomNameChanged", args => { _renaming.Remove((Action<IMyTerminalBlock>)args[0]); })
            .Gives("DisplayNameText", () => Name)
            .Gives("BlockDefinition", () => _definition)
            .Gives("DefinitionDisplayNameText", () => _display)
            .Gives("IsFunctional", () => true)
            .Gives("IsWorking", () => Enabled)
            .Does("add_IsWorkingChanged", args => Watch((Action<IMyCubeBlock>)args[0], true))
            .Does("remove_IsWorkingChanged", args => Watch((Action<IMyCubeBlock>)args[0], false))
            .Does("add_EnabledChanged", args => { _switching.Add((Action<IMyTerminalBlock>)args[0]); })
            .Does("remove_EnabledChanged", args => { _switching.Remove((Action<IMyTerminalBlock>)args[0]); })
            .Gives("Enabled", () => Enabled)
            .Takes("Enabled", value =>
            {
                Enabled = (bool)value;
                Told(Enabled ? "on" : "off");
            })
            .Gives("CubeGrid", () => Grid)
            .Does("add_CubeGridChanged", () => null)
            .Does("remove_CubeGridChanged", () => null)
            .Gives("SlimBlock", () => Slim)
            .Gives("IsBeingHacked", () => false)
            .Gives("ResourceSink", () => null)
            .Gives("InventoryCount", () => 0)
            .Gives("ShowInToolbarConfig", () => InToolbars)
            .Otherwise("UseGenericLcd", () => false)
            .Otherwise("SurfaceCount", () => 0)
            .Gives("CustomData", () => CustomData)
            .Takes("CustomData", value => CustomData = (string)value)
            .Does("add_CustomDataChanged", args => { _writing.Add((Action<IMyTerminalBlock>)args[0]); })
            .Does("remove_CustomDataChanged", args => { _writing.Remove((Action<IMyTerminalBlock>)args[0]); })
            .Gives("Position", () => Cell ?? Vector3I.Zero)
            .Gives("OwnerId", () => Owner)
            .Does("HasPlayerAccess", args => Lets((long)args[0]) || (Admins != null && Admins.Contains((long)args[0])))
            .Does("GetUserRelationToOwner", args => Relation((long)args[0]))
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

        foreach (var offer in _offers)
            into.Add(offer);
    }

    /// <summary>
    /// Gives this block one more of the game's own actions, which Sprocket knows nothing about.
    /// </summary>
    /// <remarks>
    /// Most of a ship is blocks like this: a piston that extends, a connector that connects, a tank
    /// that stockpiles. Nobody has written a capability for any of them and nobody ever will write
    /// one for a block a mod adds next year, so what Sprocket can say about them is whatever they
    /// declare - and the rig has to be able to declare something for that to be tested at all.
    /// </remarks>
    public Fitting Offering(string id, string text)
    {
        return Offering(id, text, 0);
    }

    /// <summary>Gives this block one more of the game's own actions, which has to be given <paramref name="wants" /> values before it can do anything.</summary>
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

    /// <summary>Gives this block one more of the game's own actions, declaring with <paramref name="declares" /> what type each of its values is, as the game's own actions do.</summary>
    public Fitting Offering(string id, string text, params Primitive[] declares)
    {
        _offers.Add(new Offer(id, text, declares.Length).Declaring(declares));
        return this;
    }

    /// <summary>What the last of its actions to be given values was handed, each as the type it was built as.</summary>
    public IList<object> Handed { get; set; }

    /// <summary>
    /// Every value this block carries that can be told what to be, into <paramref name="into" />, the name every block
    /// carries first.
    /// </summary>
    /// <remarks>
    /// The other half of <see cref="Offered" />. A real block answers this out of its own terminal
    /// controls, which describe themselves; the rig says what it carries so a test can watch a value
    /// being told something.
    /// <para>
    /// The game gives every terminal block a name that can be written to, which is the floor under
    /// the values the way the switch is the floor under the actions.
    /// </para>
    /// </remarks>
    public virtual void Carried(List<Offer> into)
    {
        into.Add(new Offer("CustomName", "Name", ValueKind.Text, "", ""));

        foreach (var carry in _carries)
            into.Add(carry);
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

        if (propertyId == "OnOff" && value.Kind == ValueKind.Flag)
            Enabled = value.Flag;

        _values[propertyId] = value;
        Told(propertyId + " " + value.Said);
        return true;
    }

    /// <summary>
    /// What one of its values reads: whether it is on, for OnOff, the last it was told for the rest, and nought of the kind it
    /// carries before it is told anything.
    /// </summary>
    public virtual Literal Read(string propertyId)
    {
        if (propertyId == "OnOff")
            return Literal.Of(Enabled);

        Literal had;
        return _values.TryGetValue(propertyId, out had) ? had : Zero(propertyId);
    }

    /// <summary>One of its values made ready to be read, or null where it carries none by that name.</summary>
    public Gauge Gauge(string propertyId)
    {
        return propertyId == "OnOff" || Carries(propertyId) ? new Reading(this, propertyId) : null;
    }

    Literal Zero(string propertyId)
    {
        foreach (var carry in _carries)
        {
            if (carry.Id != propertyId)
                continue;

            switch (carry.Sets)
            {
                case ValueKind.Integer:
                    return Literal.Of(0L);
                case ValueKind.Real:
                    return Literal.Of(0.0);
                case ValueKind.Text:
                    return Literal.Of("");
                case ValueKind.Color:
                    return Literal.Colour(0, 0, 0, 0);
            }
        }

        return Literal.Of(false);
    }

    bool Carries(string propertyId)
    {
        foreach (var carry in _carries)
        {
            if (carry.Id == propertyId)
                return true;
        }

        return false;
    }

    readonly List<Offer> _carries = new List<Offer>();

    readonly Dictionary<string, Literal> _values = new Dictionary<string, Literal>();

    /// <summary>One of a fitting's values, read as it reads now.</summary>
    sealed class Reading : Gauge
    {
        readonly Fitting _fitting;
        readonly string _id;

        public Reading(Fitting fitting, string id)
        {
            _fitting = fitting;
            _id = id;
        }

        public override ValueKind Kind => _fitting.Read(_id).Kind;

        public override Literal Read()
        {
            return _fitting.Read(_id);
        }
    }

    bool Named(string actionId)
    {
        foreach (var offer in _offers)
        {
            if (offer.Id == actionId)
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
    public virtual void Perform(string actionId, IList<Literal> arguments)
    {
        if (actionId == "OnOff_On")
            Enabled = true;
        else if (actionId == "OnOff_Off")
            Enabled = false;
        else if (actionId == "OnOff")
            Enabled = !Enabled;
        else if (Named(actionId))
            Told(Said(actionId, arguments));
    }

    static string Said(string actionId, IList<Literal> arguments)
    {
        if (arguments == null || arguments.Count == 0)
            return actionId;

        var said = actionId;
        for (var i = 0; i < arguments.Count; i++)
            said += " " + arguments[i].Said;

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

    /// <summary>A component of the game's own this block carries besides its toolbar, of the type <paramref name="wanted" />, or null where it carries none.</summary>
    protected virtual object? Part(Type wanted)
    {
        return null;
    }

    /// <summary>Gives this block a component of the game's, found by the mod-side interface <typeparamref name="T" /> as the game finds it.</summary>
    /// <remarks>
    /// What a catch-all functional block is made of: a separation charge is a functional block with a destroy component,
    /// and nothing else says what it is.
    /// </remarks>
    public Fitting Holding<T>(T component) where T : class
    {
        _components[typeof(T)] = component;
        return this;
    }

    readonly Dictionary<Type, object> _components = new Dictionary<Type, object>();

    /// <summary>
    /// A component the mod asks a block for: its toolbar, one a test gave it, or one <see cref="Part" /> gives.
    /// </summary>
    /// <remarks>
    /// A block does not implement the provider, it carries one - so a block with no toolbar answers
    /// no, which is the ordinary case for a door.
    /// </remarks>
    object Component(Castle.DynamicProxy.IInvocation invocation)
    {
        var wanted = invocation.GenericArguments.Length == 1 ? invocation.GenericArguments[0] : null;
        object held;
        var carried = wanted == null ? null : _components.TryGetValue(wanted, out held) ? held : Part(wanted);
        if (carried != null)
        {
            invocation.Arguments[0] = carried;
            return true;
        }

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

    /// <summary>Runs what slot <paramref name="slot" /> of this block's toolbar holds, counted from zero, as the game does when the slot is used.</summary>
    protected void Run(int slot)
    {
        MyToolbarItemInfo held;
        if (Toolbar == null || !Toolbar.TryGetItem(slot, out held))
            return;

        // A block's own action, which the game performs itself. Nothing of Sprocket's is involved and
        // the slot carries no argument.
        if (held.ActionId != SignalAction.ActionId)
        {
            if (Performs != null)
                Performs(held.BlockEntityId, held.ActionId);

            return;
        }

        if (Invokes == null)
            return;

        _held.Clear();
        Toolbar.Parameters(slot, _held);

        if (_held.Count > 0)
        {
            var key = _held[0].Value as string;
            if (!string.IsNullOrEmpty(key))
                Invokes(held.BlockEntityId, key);
        }
    }
}
