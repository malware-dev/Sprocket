using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// A class of entity and the whole vocabulary it brings with it - what can happen
    /// to it, what situations it can be in, and what it can be told to do.
    /// </summary>
    /// <remarks>
    /// The host owns this catalogue. Space Engineers builds kinds from real block types; a made-up
    /// machine writes a handful by hand. Core never hardcodes a door or a light, which is what keeps
    /// the editor driven by whatever machine it happens to be pointed at.
    /// </remarks>
    public sealed class EntityKind
    {
        static readonly HostTypeFacet[] None = new HostTypeFacet[0];

        readonly List<EntityEvent> _events = new List<EntityEvent>();
        readonly List<EntityAction> _actions = new List<EntityAction>();
        readonly Dictionary<HostType, List<HostTypeFacet>> _facets = new Dictionary<HostType, List<HostTypeFacet>>();
        readonly List<string> _hidden = new List<string>();
        readonly List<string> _lacking = new List<string>();
        readonly List<Plan> _planned = new List<Plan>();
        readonly Dictionary<string, string> _flippers = new Dictionary<string, string>();
        readonly List<Capability> _capabilities = new List<Capability>();
        readonly List<string> _lineage = new List<string>();

        EntityAction _value;

        /// <summary>A kind of block, <paramref name="id" />, called <paramref name="displayName" />, with nothing described yet.</summary>
        public EntityKind(string id, string displayName)
        {
            Id = id;
            DisplayName = displayName;
            Icon = "";
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>What this kind is called in the UI, e.g. "Door".</summary>
        public string DisplayName { get; private set; }

        /// <summary>
        /// Names a picture of this kind, or empty for none.
        /// </summary>
        /// <remarks>
        /// Opaque to core, which never learns what a picture is or how one is drawn. It is a
        /// word the host hands back to itself: Space Engineers has artwork for every block
        /// type and will point this at it, another game will point it somewhere else, and a
        /// host with no artwork at all leaves it empty.
        ///
        /// On the kind rather than the entity because that is where the game puts it. Every
        /// interior light looks like an interior light; what tells two of them apart is the
        /// name their owner gave them, which is a different thing and already handled.
        /// </remarks>
        public string Icon { get; private set; }

        /// <summary>The node this kind hangs under in the list of kinds, or <see cref="World.Family.None" />.</summary>
        public Family Family { get; private set; }

        /// <summary>
        /// What a slot may expect of a block of this kind, nearest first: the block itself as <c>Type/Subtype</c>, its type, and
        /// each type the game builds that one on, up to and short of the terminal block every block is; empty for what is no block.
        /// </summary>
        /// <remarks>Each written as <see cref="BlockKinds.Sort" /> writes one, the spelling of a slot's <c>expects</c>.</remarks>
        public IList<string> Lineage => _lineage;

        /// <summary>Moments. These start sequences, and nothing else.</summary>
        public IList<EntityEvent> Events => _events;

        /// <summary>What a block of this kind can be told to do.</summary>
        public IList<EntityAction> Actions => _actions;

        /// <summary>The types the host provides that this kind of block has anything of.</summary>
        public IEnumerable<HostType> Types => _facets.Keys;

        /// <summary>What knows this kind of block, in the order each described it: the block's own first, the floor every block stands on last.</summary>
        public IList<Capability> Capabilities => _capabilities;

        /// <summary>Says the game's toggle <paramref name="toggle" /> flips Sprocket's own switch <paramref name="value" />.</summary>
        /// <remarks>A switch of the game's own is flipped by the toggle that shares its id, which needs no saying.</remarks>
        public EntityKind Flipping(string value, string toggle)
        {
            _flippers[value] = toggle;
            return this;
        }

        /// <summary>The game's toggle said to flip Sprocket's own switch <paramref name="value" />, or null.</summary>
        public string Flipper(string value)
        {
            string toggle;
            return _flippers.TryGetValue(value, out toggle) ? toggle : null;
        }

        /// <summary>What this kind is to offer that is not built yet, in the order it was said.</summary>
        public IList<Plan> Planned => _planned;

        /// <summary>Says this kind is to offer <paramref name="plan" />, which is not built yet.</summary>
        public EntityKind Planning(Plan plan)
        {
            if (!_planned.Contains(plan))
                _planned.Add(plan);

            return this;
        }

        /// <summary>Says <paramref name="capability" /> knows this kind of block.</summary>
        public void Known(Capability capability)
        {
            _capabilities.Add(capability);
        }

        /// <summary>Hangs the kind under <paramref name="family" /> in the list of kinds.</summary>
        public EntityKind WithFamily(Family family)
        {
            Family = family;
            return this;
        }

        /// <summary>Gives the kind the picture <paramref name="icon" />.</summary>
        public EntityKind WithIcon(string icon)
        {
            Icon = icon;
            return this;
        }

        /// <summary>Adds the moment <paramref name="id" />: <paramref name="flag" /> crossing <paramref name="edge" />, read as <paramref name="text" />.</summary>
        public EntityKind WithEvent(string id, Reading flag, Edge edge, string text)
        {
            _events.Add(new EntityEvent(id, flag, edge, text));
            _value = null;
            return this;
        }

        /// <summary>
        /// Adds the signal <paramref name="id" />, read as <paramref name="text" />: Sprocket's own value <paramref name="value" />,
        /// added before it, coming to be <paramref name="becomes" />.
        /// </summary>
        public EntityKind WithChange(string id, string text, string value, Literal becomes)
        {
            var changed = FindAction(value, Nature.Value);
            if (changed == null || changed.Being == null)
                throw new ArgumentException($"The signal {id} is a change in {value}, which is no value of Sprocket's own declared before it.");

            var signal = new EntityEvent(id, changed.Being.Is(becomes), Edge.Rising, text);
            signal.Changes(value, becomes);
            _events.Add(signal);
            _value = null;
            return this;
        }

        /// <summary>Adds Sprocket's own value <paramref name="id" />, named <paramref name="named" />, read by <paramref name="being" />.</summary>
        /// <remarks>Read, never set. What it is called in a list is what the field beside a test asks for.</remarks>
        public EntityKind WithValue(string id, string named, Being being)
        {
            var value = new EntityAction(id, named, null).Reads(being, Asked(named, being));
            value.List(named);
            _actions.Add(value);
            _value = value;
            return this;
        }


        /// <summary>Says the value of Sprocket's own added last is the status of the block, which a check reads as the block itself.</summary>
        public EntityKind Status()
        {
            if (_value == null)
                throw new InvalidOperationException("A status is a value of Sprocket's own, and none was added last.");

            _value.IsStatus = true;
            return this;
        }

        /// <summary>Has a step set the value of Sprocket's own added last as text over several lines, up to <paramref name="longest" /> characters.</summary>
        public EntityKind Lines(int longest)
        {
            if (_value == null)
                throw new InvalidOperationException("Lines belong to a value of Sprocket's own, and none was added last.");

            _value.Lines(longest);
            return this;
        }

        /// <summary>
        /// Holds what a step sets the value of Sprocket's own added last to between <paramref name="least" /> and
        /// <paramref name="most" />, each written as a number or empty for none.
        /// </summary>
        /// <remarks>Only what the block itself takes, which is what a player could set it to; a test is not held to them.</remarks>
        public EntityKind Between(string least, string most)
        {
            if (_value == null)
                throw new InvalidOperationException("Bounds belong to a value of Sprocket's own, and none was added last.");

            _value.Between(least, most);
            return this;
        }

        /// <summary>
        /// Says the value of Sprocket's own added last is in <paramref name="unit" />, said after its number, and shown
        /// <paramref name="scale" /> times what it holds.
        /// </summary>
        /// <remarks>A unit as a sentence says it, with its space before it where it takes one: " kg", "°".</remarks>
        public EntityKind Measured(string unit, double scale = 1)
        {
            if (_value == null)
                throw new InvalidOperationException("A unit belongs to a value of Sprocket's own, and none was added last.");

            _value.Measured(unit, scale, 0);
            return this;
        }

        /// <summary>Says the value of Sprocket's own added last is a percent, 0 to 100, which a slider sets and checks.</summary>
        public EntityKind Percent()
        {
            return Between("0", "100").Measured(" %");
        }

        /// <summary>Says how a test reads the moment added last: the block in it, and not.</summary>
        public EntityKind Reading(string state, string unstate)
        {
            _events[_events.Count - 1].Read(state, unstate);
            return this;
        }

        /// <summary>Says that the block announces when the value or signal added last may have changed - see <see cref="Capability.Listen" />.</summary>
        public EntityKind Announced()
        {
            if (_value != null)
                _value.Announce();
            else
                _events[_events.Count - 1].Announce();

            return this;
        }

        /// <summary>Says the value or signal added last is looked for on a cadence, and <paramref name="reason" /> why: the game announces neither it nor anything that rules it out.</summary>
        public EntityKind Looked(string reason)
        {
            if (_value != null)
                _value.Look(reason);
            else
                _events[_events.Count - 1].Look(reason);

            return this;
        }

        /// <summary>Says the value or signal added last cannot change while <paramref name="flag" /> reads true, and the block announces when that changes - see <see cref="EntityEvent.RuledOutBy" />.</summary>
        public EntityKind RuledOutBy(Reading flag)
        {
            if (_value != null)
                _value.RuleOutBy(flag);
            else
                _events[_events.Count - 1].RuleOutBy(flag);

            return this;
        }

        /// <summary>
        /// Adds a signal, a moment the construct sends from the toolbar slot <paramref name="at" /> - see
        /// <see cref="EntityEvent.Answered" />.
        /// </summary>
        /// <remarks>
        /// No property, because there is nothing to read: what makes it happen is a slot on this
        /// block's own toolbar, and what it runs is what that slot was given.
        /// </remarks>
        public EntityKind WithSignal(Sequences.ToolbarSlot at, string text)
        {
            _events.Add(new EntityEvent(at, text));
            _value = null;
            return this;
        }

        /// <summary>
        /// Adds the toolbar <paramref name="toolbar" /> a person presses as one signal, read as <paramref name="text" />:
        /// <paramref name="pages" /> pages of <paramref name="slots" /> slots, the one chosen worded by <paramref name="says" />.
        /// </summary>
        /// <remarks>See <see cref="EntityEvent.IsPressed" />.</remarks>
        public EntityKind WithPressed(int toolbar, string text, int pages, int slots, Func<Sequences.ToolbarSlot, string> says)
        {
            _events.Add(new EntityEvent(new Sequences.ToolbarSlot(toolbar, 0, 0), text, pages, slots, says));
            _value = null;
            return this;
        }

        /// <summary>Adds one of the block's own actions, <paramref name="gameActionId" />, as <paramref name="id" /> said as <paramref name="text" />.</summary>
        public EntityKind WithAction(string id, string text, string gameActionId)
        {
            _actions.Add(new EntityAction(id, text, gameActionId));
            return this;
        }

        /// <summary>
        /// Adds one of Sprocket's own actions, doing <paramref name="doing" />, which is the game's member
        /// <paramref name="gameActionId" /> said another way.
        /// </summary>
        public EntityKind WithAction(string id, string text, Doing doing, string gameActionId)
        {
            _actions.Add(new EntityAction(id, text, gameActionId).Performing(doing));
            return this;
        }

        /// <summary>Adds the game's action <paramref name="gameActionId" /> as <paramref name="id" />, said as <paramref name="text" />, which the game will not perform until it is given <paramref name="wants" /> values of the types <paramref name="takes" />, described by <paramref name="described" />.</summary>
        public EntityKind WithAction(string id, string text, string gameActionId, int wants,
            Description described, IList<Primitive> takes)
        {
            _actions.Add(new EntityAction(id, text, gameActionId, wants)
                .Described(described)
                .Taking(takes));

            return this;
        }

        /// <summary>
        /// Adds one of Sprocket's own actions, doing <paramref name="doing" />, given values each asked for by its field.
        /// </summary>
        public EntityKind WithAction(string id, string text, Doing doing, params Field[] asks)
        {
            _actions.Add(new EntityAction(id, text, null).Performing(doing).Asking(asks));
            return this;
        }

        /// <summary>Says what the member added last is about, for one the game did not name.</summary>
        public EntityKind In(Category category)
        {
            _actions[_actions.Count - 1].Category = category;
            return this;
        }

        /// <summary>Says the action or value added last is a member of <paramref name="type" /> - see <see cref="EntityAction.Type" />.</summary>
        public EntityKind Of(HostType type)
        {
            _actions[_actions.Count - 1].Type = type;
            return this;
        }

        /// <summary>Keeps <paramref name="facets" /> as what this kind of block has of <paramref name="type" />.</summary>
        public EntityKind Offering(HostType type, IList<HostTypeFacet> facets)
        {
            _facets[type] = new List<HostTypeFacet>(facets);
            return this;
        }

        /// <summary>What this kind of block has of <paramref name="type" />, in order; none where it has nothing of it.</summary>
        public IList<HostTypeFacet> Facets(HostType type)
        {
            List<HostTypeFacet> facets;
            if (_facets.TryGetValue(type, out facets))
                return facets;

            return None;
        }

        /// <summary>
        /// Keeps the block's own members out of the list somebody chooses from, by the game's ids.
        /// </summary>
        /// <remarks>
        /// For what this kind says better elsewhere, and for what the game offers that no sequence
        /// can use. A velocity to set makes the game's increase and decrease worse than useless, and
        /// growing a rotor a head is building the machine rather than running it. Said as a set
        /// because one entry standing in for one other is the rare case, not the rule.
        /// <para>
        /// The game's members are added last, so this is read once the list is complete rather than
        /// applied where it is declared.
        /// </para>
        /// </remarks>
        public EntityKind Hiding(params string[] gameIds)
        {
            foreach (var gameId in gameIds)
                _hidden.Add(gameId);

            return this;
        }

        /// <summary>Leaves out the settings the game's terminal never shows on this kind of block, by their ids.</summary>
        /// <remarks>
        /// A setting left out is in neither view: the terminal view lists what the game's terminal does. Each takes its value,
        /// its toggle and the pair a checkbox adds with it.
        /// </remarks>
        public EntityKind Lacking(params string[] settingIds)
        {
            foreach (var settingId in settingIds)
                _lacking.Add(settingId);

            return this;
        }

        /// <summary>Whether the game's member <paramref name="gameId" /> belongs to a setting this kind lacks.</summary>
        public bool Lacks(string gameId)
        {
            foreach (var setting in _lacking)
            {
                if (gameId == setting || gameId == $"{setting}_On" || gameId == $"{setting}_Off")
                    return true;
            }

            return false;
        }

        /// <summary>Whether this kind keeps the block's own member out of that list.</summary>
        public bool Hides(string gameId)
        {
            foreach (var hiddenId in _hidden)
            {
                if (hiddenId == gameId)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Names the action added last in a list of things to choose from, where a sentence says it
        /// differently.
        /// </summary>
        /// <remarks>
        /// A turn to an angle is "rotate to" in a list, and "rotate" in a sentence whose angle says "to".
        /// </remarks>
        public EntityKind Listed(string words)
        {
            _actions[_actions.Count - 1].List(words);
            return this;
        }

        /// <summary>Gives the action added last the values it asks for, each asked for by its field.</summary>
        /// <remarks>For one that stands in for a member of the game's own and is given values the game's member takes none of.</remarks>
        public EntityKind Asking(params Field[] asks)
        {
            _actions[_actions.Count - 1].Asking(asks);
            return this;
        }

        /// <summary>Says that reaching the state of the action added last takes time.</summary>
        public EntityKind Lasting()
        {
            _actions[_actions.Count - 1].Lasting();
            return this;
        }

        /// <summary>
        /// Gives one of the block's own members Sprocket's word for it.
        /// </summary>
        /// <remarks>
        /// A rename and nothing more: the member keeps the block's id and is performed as the block's.
        /// Taken up where the block's own members are read, so a block that does not declare it gets
        /// nothing - see <c>BlockKinds</c>.
        /// </remarks>
        public EntityKind Renaming(string gameId, bool sets, string word)
        {
            _renamed.Add(new Alias(gameId, sets, word));
            return this;
        }

        /// <summary>
        /// Says the block's own value <paramref name="gameId" /> is in <paramref name="unit" />, said after its number, and shown
        /// <paramref name="scale" /> times what the game holds, moving in <paramref name="step" /> as shown where that is more
        /// than 0, and shown no higher than <paramref name="most" /> where that is given.
        /// </summary>
        /// <remarks>
        /// The game gives a value's bounds and never its unit, so the unit is Sprocket's to say. The script keeps the game's
        /// number; the scale is how a person reads it, a strength the game holds as 0 to 1 typed as a percent, and the step what
        /// the game snaps it to, a screen's rotation to quarter turns. Taken up where the block's own members are read, as a
        /// rename is.
        /// </remarks>
        public EntityKind Measuring(string gameId, string unit, double scale = 1, double step = 0, string most = null)
        {
            _measured.Add(new Measure(gameId, unit, scale, step, most));
            return this;
        }

        /// <summary>The unit and scale one of the block's own values is said in, where Sprocket says one.</summary>
        public bool Measures(string gameId, out string unit, out double scale, out double step, out string most)
        {
            foreach (var measure in _measured)
            {
                if (measure.GameId == gameId)
                {
                    unit = measure.Unit;
                    scale = measure.Scale;
                    step = measure.Step;
                    most = measure.Most;
                    return true;
                }
            }

            unit = null;
            scale = 1;
            step = 0;
            most = null;
            return false;
        }

        /// <summary>Vouches for the block's own members <paramref name="gameIds" /> in the game's own words: values where <paramref name="sets" /> says so, actions otherwise.</summary>
        /// <remarks>
        /// What puts one of the block's own members in the organized view without a word of Sprocket's for it. Taken up where
        /// the block's own members are read, as a rename is.
        /// </remarks>
        public EntityKind Vetting(bool sets, params string[] gameIds)
        {
            foreach (var gameId in gameIds)
                _vetted.Add(new Alias(gameId, sets, null));

            return this;
        }

        /// <summary>Says the block's own switches <paramref name="gameIds" /> are states of the block, which a check reads as the block itself.</summary>
        /// <remarks>
        /// The game's switch for what the block is or is doing, "Tank is stockpiling", rather than a setting of how it behaves,
        /// "Suspension invert steering is on". Taken up where the block's own members are read, as a rename is.
        /// </remarks>
        public EntityKind Stating(params string[] gameIds)
        {
            foreach (var gameId in gameIds)
                _states.Add(gameId);

            return this;
        }

        /// <summary>Whether one of the block's own switches is a state of the block.</summary>
        public bool States(string gameId)
        {
            return _states.Contains(gameId);
        }

        /// <summary>Whether one of the block's own members is vouched for in the game's own words.</summary>
        public bool Vets(string gameId, bool sets)
        {
            for (var i = 0; i < _vetted.Count; i++)
            {
                if (_vetted[i].GameId == gameId && _vetted[i].Sets == sets)
                    return true;
            }

            return false;
        }

        /// <summary>Sprocket's word for one of the block's own members, where it has one.</summary>
        public bool Renames(string gameId, bool sets, out string word)
        {
            for (var i = 0; i < _renamed.Count; i++)
            {
                if (_renamed[i].GameId == gameId && _renamed[i].Sets == sets)
                {
                    word = _renamed[i].Word;
                    return true;
                }
            }

            word = null;
            return false;
        }

        /// <summary>Sprocket's word for one of the block's own members.</summary>
        struct Alias
        {
            public readonly string GameId;
            public readonly bool Sets;
            public readonly string Word;

            public Alias(string gameId, bool sets, string word)
            {
                GameId = gameId;
                Sets = sets;
                Word = word;
            }
        }

        readonly List<Alias> _renamed = new List<Alias>();

        readonly List<Measure> _measured = new List<Measure>();

        readonly List<Alias> _vetted = new List<Alias>();

        readonly List<string> _states = new List<string>();

        /// <summary>Adds the block's own value <paramref name="propertyId" /> as <paramref name="id" />, said as <paramref name="named" />, set through <paramref name="field" /> and holding <paramref name="holds" />.</summary>
        public EntityKind WithValue(string id, string named, string propertyId, Field field, ValueKind holds)
        {
            _actions.Add(new EntityAction(id, named, null, 1)
                .Writes(propertyId, field, named, holds));

            return this;
        }

        /// <summary>The moment <paramref name="eventId" />, or null where the kind has none.</summary>
        public EntityEvent FindEvent(string eventId)
        {
            foreach (var entityEvent in _events)
            {
                if (entityEvent.Id == eventId)
                    return entityEvent;
            }
            return null;
        }

        /// <summary>The signal the one toolbar slot <paramref name="at" /> is among, or null where none of this kind's is.</summary>
        public EntityEvent Signal(Sequences.ToolbarSlot at)
        {
            foreach (var entityEvent in _events)
            {
                if (!entityEvent.Answered)
                    continue;

                var stands = Sequences.ToolbarSlot.Of(entityEvent.Id);
                if (stands != null && stands.Covers(at))
                    return entityEvent.At(at);
            }

            return null;
        }

        /// <summary>The entry a step names, by its id and by whether it sets a value.</summary>
        /// <remarks>
        /// Both, because the id alone is not unique: a switch's value and the game's bare toggle for
        /// it are both called <c>OnOff</c>, and a program tells them apart by writing one as a
        /// <c>set</c>.
        /// </remarks>
        public EntityAction FindAction(string actionId, Nature form)
        {
            foreach (var action in _actions)
            {
                if (action.Id == actionId && action.Form == form)
                    return action;
            }
            return null;
        }

        /// <summary>The field a test of a value of Sprocket's own asks for what it is compared with.</summary>
        static Field Asked(string named, Being being)
        {
            switch (being.Kind)
            {
                case ValueKind.Name:
                    var words = new string[being.Names.Count];
                    var means = new string[being.Names.Count];
                    for (var i = 0; i < words.Length; i++)
                    {
                        means[i] = being.Names[i];
                        words[i] = being.Said != null && i < being.Said.Count ? being.Said[i] : means[i].Replace('-', ' ');
                    }

                    var among = Field.Among(named, words, means);
                    among.Entry = Entry.Name;
                    return among;
                case ValueKind.Flag:
                    var either = Field.OneOf(named, Literal.Of(true).Said, Literal.Of(false).Said);
                    either.Edits = Editor.Chosen;
                    return either;
                case ValueKind.Color:
                    return Field.Holding(named, Editor.Colour, 1);
            }

            var box = Field.Called(named);
            box.Entry = EntityAction.Entered(being.Kind);
            return box;
        }

        /// <summary>The unit and scale one of the block's own values is said in.</summary>
        sealed class Measure
        {
            public Measure(string gameId, string unit, double scale, double step, string most)
            {
                GameId = gameId;
                Unit = unit;
                Scale = scale;
                Step = step;
                Most = most;
            }

            public string GameId { get; }

            public string Unit { get; }

            public double Scale { get; }

            public double Step { get; }

            public string Most { get; }
        }
    }
}
