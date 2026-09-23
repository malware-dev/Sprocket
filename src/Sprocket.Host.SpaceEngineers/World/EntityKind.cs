using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;

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

        public EntityProperty FindProperty(string propertyId)
        {
            for (var i = 0; i < _properties.Count; i++)
            {
                if (_properties[i].Id == propertyId)
                    return _properties[i];
            }
            return null;
        }

        public EntityKind WithEvent(string id, string propertyId, Edge edge, string text)
        {
            _events.Add(new EntityEvent(id, propertyId, edge, text));
            return this;
        }

        /// <summary>
        /// Adds a moment the construct answers - see <see cref="EntityEvent.Answered" />.
        /// </summary>
        /// <remarks>
        /// No property, because there is nothing to read: what makes it happen is a slot on this
        /// block's own toolbar, and what it runs is what that slot was given.
        /// </remarks>
        public EntityKind WithAnsweredEvent(string id, string text)
        {
            _events.Add(new EntityEvent(id, text));
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
            Description described)
        {
            _actions.Add(new EntityAction(id, text, null, gameActionId, wants)
                .Described(described));

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

        /// <summary>Says which of the block's own actions the action added last takes over.</summary>
        public EntityKind Replacing(string gameActionId)
        {
            _actions[_actions.Count - 1].Replacing(gameActionId);
            return this;
        }

        /// <summary>Says that reaching the state of the action added last takes time.</summary>
        public EntityKind Lasting()
        {
            _actions[_actions.Count - 1].Lasting();
            return this;
        }

        /// <summary>
        /// Offers one of the block's own members under Sprocket's words, in the place of the game's.
        /// </summary>
        /// <remarks>
        /// For a member worth offering as it is, only said better: the block still declares it, with
        /// what it takes, and the entry made from that declaration stands in for the game's, which is
        /// then left out of the list somebody chooses from like any other that is stood in for. Taken
        /// up where the block's own members are read, so a block that does not declare it gets
        /// nothing - see <c>BlockKinds</c>.
        /// </remarks>
        public EntityKind Promoting(string gameId, bool sets, string id, string named)
        {
            _promoted.Add(new Promotion(gameId, sets, id, named));
            return this;
        }

        /// <summary>What one of the block's own members is offered as, where something promoted it.</summary>
        internal bool Promoted(string gameId, bool sets, out string id, out string named)
        {
            for (var i = 0; i < _promoted.Count; i++)
            {
                if (_promoted[i].GameId == gameId && _promoted[i].Sets == sets)
                {
                    id = _promoted[i].Id;
                    named = _promoted[i].Named;
                    return true;
                }
            }

            id = null;
            named = null;
            return false;
        }

        /// <summary>One of the block's own members offered under Sprocket's words.</summary>
        struct Promotion
        {
            public readonly string GameId;
            public readonly bool Sets;
            public readonly string Id;
            public readonly string Named;

            public Promotion(string gameId, bool sets, string id, string named)
            {
                GameId = gameId;
                Sets = sets;
                Id = id;
                Named = named;
            }
        }

        readonly List<Promotion> _promoted = new List<Promotion>();

        /// <summary>The same, for one of the block's own values rather than one of its actions.</summary>
        public EntityKind WithValue(string id, string named, string propertyId, Field field)
        {
            _actions.Add(new EntityAction(id, "set " + named, null, null, 1)
                .Writes(propertyId, field, named));

            return this;
        }

        public EntityEvent FindEvent(string eventId)
        {
            for (var i = 0; i < _events.Count; i++)
            {
                if (_events[i].Id == eventId)
                    return _events[i];
            }
            return null;
        }

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
        public EntityAction FindAction(string actionId, bool sets)
        {
            for (var i = 0; i < _actions.Count; i++)
            {
                if (_actions[i].Id == actionId && _actions[i].Sets == sets)
                    return _actions[i];
            }
            return null;
        }
    }
}
