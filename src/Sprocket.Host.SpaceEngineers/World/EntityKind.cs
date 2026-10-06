using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Values;

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
        readonly List<EntityProperty> _properties = new List<EntityProperty>();
        readonly List<EntityEvent> _events = new List<EntityEvent>();
        readonly List<EntityState> _states = new List<EntityState>();
        readonly List<EntityAction> _actions = new List<EntityAction>();
        readonly List<string> _hidden = new List<string>();

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

        /// <summary>How far Sprocket has checked this kind, which is pending until somebody says otherwise.</summary>
        public Evaluation Evaluation { get; set; }

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

        /// <summary>Gives the kind the picture <paramref name="icon" />.</summary>
        public EntityKind WithIcon(string icon)
        {
            Icon = icon;
            return this;
        }

        /// <summary>Everything that can be read off one of these.</summary>
        public IList<EntityProperty> Properties => _properties;

        /// <summary>Moments. These start sequences, and nothing else.</summary>
        public IList<EntityEvent> Events => _events;

        /// <summary>Situations. These gate steps and guard interlocks, and nothing else.</summary>
        public IList<EntityState> States => _states;

        /// <summary>What a block of this kind can be told to do.</summary>
        public IList<EntityAction> Actions => _actions;

        /// <summary>Declares something that can be read off this kind.</summary>
        public EntityKind WithProperty(string id, ValueKind kind, string label)
        {
            _properties.Add(new EntityProperty(id, kind, label));
            return this;
        }

        /// <summary>A flag, which is the common case.</summary>
        public EntityKind WithProperty(string id, string label)
        {
            return WithProperty(id, ValueKind.Flag, label);
        }

        /// <summary>The reading <paramref name="propertyId" />, or null where the kind has none.</summary>
        public EntityProperty FindProperty(string propertyId)
        {
            for (var i = 0; i < _properties.Count; i++)
            {
                if (_properties[i].Id == propertyId)
                    return _properties[i];
            }
            return null;
        }

        /// <summary>Adds the moment <paramref name="id" />: <paramref name="propertyId" /> crossing <paramref name="edge" />, read as <paramref name="text" />.</summary>
        public EntityKind WithEvent(string id, string propertyId, Edge edge, string text)
        {
            _events.Add(new EntityEvent(id, propertyId, edge, text));
            return this;
        }

        /// <summary>Says how a test reads the moment added last: the block in it, and not.</summary>
        public EntityKind Reading(string state, string unstate)
        {
            _events[_events.Count - 1].Read(state, unstate);
            return this;
        }

        /// <summary>Says that the block announces when the moment added last may have happened - see <see cref="IAnnounces" />.</summary>
        public EntityKind Announced()
        {
            _events[_events.Count - 1].Announce();
            return this;
        }

        /// <summary>Says the moment added last is looked for on a cadence, and <paramref name="reason" /> why: the game announces neither it nor anything that rules it out.</summary>
        public EntityKind Looked(string reason)
        {
            _events[_events.Count - 1].Look(reason);
            return this;
        }

        /// <summary>Says the moment added last cannot happen while <paramref name="propertyId" /> reads true, and the block announces when that changes - see <see cref="EntityEvent.RuledOutBy" />.</summary>
        public EntityKind RuledOutBy(string propertyId)
        {
            _events[_events.Count - 1].RuleOutBy(propertyId);
            return this;
        }

        /// <summary>
        /// Adds a trigger, a moment the construct answers through the toolbar slot <paramref name="at" /> - see
        /// <see cref="EntityEvent.Answered" />.
        /// </summary>
        /// <remarks>
        /// No property, because there is nothing to read: what makes it happen is a slot on this
        /// block's own toolbar, and what it runs is what that slot was given.
        /// </remarks>
        public EntityKind WithTrigger(Sequences.ToolbarSlot at, string text)
        {
            _events.Add(new EntityEvent(at, text));
            return this;
        }

        /// <summary>
        /// Adds a situation: a relation against one of this kind's properties.
        /// </summary>
        /// <remarks>
        /// Two of these over one property describe both sides of it - "is open" and "is shut"
        /// off a single reading - which is why a host is never asked to keep a pair of
        /// opposites in step by hand.
        /// </remarks>
        public EntityKind WithState(string id, string propertyId, Relation relation, EntityValue operand, string text)
        {
            var property = FindProperty(propertyId);
            var kind = property == null ? ValueKind.Flag : property.Kind;

            _states.Add(new EntityState(id, propertyId, kind, relation, operand, text));
            return this;
        }

        /// <summary>A flag being one way or the other, which is the common case.</summary>
        public EntityKind WithState(string id, string propertyId, bool whenTrue, string text)
        {
            return WithState(id, propertyId, Relation.Is, EntityValue.Of(whenTrue), text);
        }

        /// <summary>
        /// Adds an action. <paramref name="leadsToStateId"/> names the state the
        /// action is trying to reach, which is what allows an interlock to stop it.
        /// </summary>
        public EntityKind WithAction(string id, string text, string leadsToStateId = null,
            string gameActionId = null)
        {
            _actions.Add(new EntityAction(id, text, leadsToStateId, gameActionId));
            return this;
        }

        /// <summary>The same, for one the game will not perform until it is given values.</summary>
        public EntityKind WithAction(string id, string text, string gameActionId, int wants,
            Description described, IList<Primitive> takes)
        {
            _actions.Add(new EntityAction(id, text, null, gameActionId, wants)
                .Described(described)
                .Taking(takes));

            return this;
        }

        /// <summary>
        /// The same, for one of Sprocket's own that is given values, each asked for by its field.
        /// </summary>
        public EntityKind WithAction(string id, string text, string leadsToStateId, params Field[] asks)
        {
            _actions.Add(new EntityAction(id, text, leadsToStateId, null).Asking(asks));
            return this;
        }

        /// <summary>Says what the member added last is about, for one the game did not name.</summary>
        public EntityKind In(Category category)
        {
            _actions[_actions.Count - 1].Category = category;
            return this;
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
            for (var i = 0; i < gameIds.Length; i++)
                _hidden.Add(gameIds[i]);

            return this;
        }

        /// <summary>Whether this kind keeps the block's own member out of that list.</summary>
        public bool Hides(string gameId)
        {
            for (var i = 0; i < _hidden.Count; i++)
            {
                if (_hidden[i] == gameId)
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

        /// <summary>The same, for one of the block's own values rather than one of its actions.</summary>
        public EntityKind WithValue(string id, string named, string propertyId, Field field, ValueKind holds)
        {
            _actions.Add(new EntityAction(id, "set " + named, null, null, 1)
                .Writes(propertyId, field, named, holds));

            return this;
        }

        /// <summary>The moment <paramref name="eventId" />, or null where the kind has none.</summary>
        public EntityEvent FindEvent(string eventId)
        {
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].Id == eventId)
                    return _events[i];
            }
            return null;
        }

        /// <summary>The trigger the one toolbar slot <paramref name="at" /> is among, or null where none of this kind's is.</summary>
        public EntityEvent Trigger(Sequences.ToolbarSlot at)
        {
            for (var i = 0; i < _events.Count; i++)
            {
                if (!_events[i].Answered)
                    continue;

                var stands = Sequences.ToolbarSlot.Of(_events[i].Id);
                if (stands != null && stands.Covers(at))
                    return _events[i];
            }

            return null;
        }

        /// <summary>The state <paramref name="stateId" />, or null where the kind has none.</summary>
        public EntityState FindState(string stateId)
        {
            for (var i = 0; i < _states.Count; i++)
            {
                if (_states[i].Id == stateId)
                    return _states[i];
            }
            return null;
        }

        /// <summary>The entry a step names, by its id and by whether it sets a value.</summary>
        /// <remarks>
        /// Both, because the id alone is not unique: a switch's value and the game's bare toggle for
        /// it are both called <c>OnOff</c>, and a program tells them apart by writing one as a
        /// <c>set</c>.
        /// </remarks>
        public EntityAction FindAction(string actionId, Form form)
        {
            for (var i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Id == actionId && _actions[i].Form == form)
                    return _actions[i];
            }
            return null;
        }
    }
}
