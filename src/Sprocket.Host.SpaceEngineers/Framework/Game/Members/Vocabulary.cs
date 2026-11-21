using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// Works out what a slot is allowed to contain, given what is already in the
    /// sequence.
    /// </summary>
    public static class Vocabulary
    {
        // What a value read with no arguments is given.
        static readonly Literal[] NoArguments = new Literal[0];

        /// <summary>
        /// Events every one of the given entities can raise.
        /// </summary>
        public static List<EntityEvent> EventsFor(Bindings bindings, IList<string> names)
        {
            return EventsOf(Held(bindings, names));
        }

        /// <summary>
        /// Events every one of the blocks <paramref name="entityIds" /> can raise, asked of blocks somebody is holding.
        /// </summary>
        public static List<EntityEvent> EventsOf(GridWorld world, IList<long> entityIds)
        {
            return EventsOf(Blocks(world, entityIds));
        }

        static List<EntityEvent> EventsOf(List<GridEntity> entities)
        {
            var result = new List<EntityEvent>();
            if (entities.Count == 0)
                return result;

            var candidates = entities[0].Kind.Events;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AllHaveEvent(entities, candidates[i].Key))
                    result.Add(candidates[i]);
            }
            return result;
        }

        /// <summary>
        /// What starts a sequence when somebody picks these blocks and this moment.
        /// </summary>
        /// <remarks>
        /// The block decides whether the moment is answered from a toolbar slot or watched by Sprocket. Asked of all the
        /// blocks, a watched change is its value coming to be so on every one of them.
        /// </remarks>
        public static MomentTrigger Starter(List<EntityEvent> events, MemberKey key, bool all)
        {
            var picked = Find(events, key);

            if (picked != null && picked.Answered)
                return new SignalTrigger(key.Id);

            if (!all || picked == null || picked.Value.Length == 0)
                return SignalTrigger.Written(picked != null ? picked.Namespace : key.Namespace, key.Id);

            var when = new WhenTrigger(picked.Value);
            when.Key = picked.ValueKey;
            when.Quantifier = Quantifier.All;
            when.Value = picked.Becomes;
            return when;
        }

        /// <summary>The moment among <paramref name="events" /> that <paramref name="key" /> names, worded for the one slot it names on a toolbar a person presses; null where none is.</summary>
        public static EntityEvent Find(IList<EntityEvent> events, MemberKey key)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Names(key))
                    return events[i].At(ToolbarSlot.Of(key.Id));
            }

            return null;
        }

        /// <summary>
        /// The signal among <paramref name="events" /> that is the value <paramref name="value" /> names coming to be what
        /// <paramref name="compared" /> says, with whether the comparison asks for the block to be in that state rather than
        /// out of it; null where no signal is, or the comparison is no sameness.
        /// </summary>
        public static EntityEvent Change(IList<EntityEvent> events, MemberKey value, Relation relation, Literal compared,
            out bool inState)
        {
            inState = relation == Relation.Is;
            if (relation != Relation.Is && relation != Relation.IsNot)
                return null;

            for (var i = 0; i < events.Count; i++)
            {
                var signal = events[i];
                if (signal.IsState && signal.Value.Length > 0 && value.Names(signal.ValueKey) && signal.Becomes.Said == compared.Said)
                    return signal;
            }

            return null;
        }

        /// <summary>
        /// The moment <paramref name="moment" /> of <paramref name="system" /> means as <paramref name="through" /> runs it: for a
        /// trigger, the trigger on its block that the first toolbar slot answering it is among; for anything watched, what it
        /// watches for. Empty where there is none, or no program to ask.
        /// </summary>
        public static MemberKey EventOf(Automation program, Bindings bindings, AutomationSystem system, MomentTrigger moment,
            SystemInstance through)
        {
            var when = moment as WhenTrigger;
            if (when != null && bindings != null)
            {
                bool inState;
                var signal = Change(EventsFor(bindings, SystemInstance.Through(through, when.Sources)), when.Key,
                    when.Relation, when.Value, out inState);

                return signal != null && inState ? signal.Key : when.Key;
            }

            var required = moment as SignalTrigger;
            if (required == null || required.IsWatched)
                return moment.Key;

            if (program == null || system == null)
                return default(MemberKey);

            var blocks = new List<string>();
            var positions = new List<ToolbarSlot>();
            program.Answers(system, through, required, blocks, positions);
            if (positions.Count == 0)
                return default(MemberKey);

            GridEntity block = bindings?.Entity(blocks[0]);
            EntityEvent stands = block?.Kind.Signal(positions[0]);
            return stands != null ? stands.Key : MemberKey.Bare(positions[0].EventId);
        }

        /// <summary>
        /// Whether <paramref name="when" /> compares a value on blocks raising <paramref name="events" />, rather than watching
        /// for one of those moments: anything but a host's state coming to be.
        /// </summary>
        public static bool Compares(WhenTrigger when, IList<EntityEvent> events)
        {
            bool inState;
            return Change(events, when.Key, when.Relation, when.Value, out inState) == null || !inState;
        }

        /// <summary>The type the host provides that the member <paramref name="key" /> names on these blocks is a member of; null where it is none's.</summary>
        public static HostType TypeOf(GridWorld world, IList<long> entityIds, MemberKey key)
        {
            if (key.IsEmpty)
                return null;

            foreach (var action in ActionsOf(world, entityIds))
            {
                if (key.Names(action.Key) && action.Type != null)
                    return action.Type;
            }

            return null;
        }

        /// <summary>Actions every one of the given entities accepts.</summary>
        public static List<EntityAction> ActionsFor(Bindings bindings, IList<string> names)
        {
            return ActionsOf(Held(bindings, names));
        }

        /// <summary>Actions every one of the blocks <paramref name="entityIds" /> accepts, asked of blocks somebody is holding.</summary>
        public static List<EntityAction> ActionsOf(GridWorld world, IList<long> entityIds)
        {
            return ActionsOf(Blocks(world, entityIds));
        }

        /// <summary>The value <paramref name="key" /> names that every one of the blocks <paramref name="entityIds" /> has, set or read, or null where they share none.</summary>
        public static EntityAction ValueOf(GridWorld world, IList<long> entityIds, MemberKey key)
        {
            return EntityAction.Value(ActionsOf(world, entityIds), key);
        }

        /// <summary>
        /// What each value the member <paramref name="key" /> names on these blocks wants asks for, one per value.
        /// </summary>
        /// <remarks>
        /// Asked by id and by form, because a switch's value and the act that flips it share one id.
        /// </remarks>
        public static List<Field> Fields(GridWorld world, IList<long> entityIds, MemberKey key, ReachKind form)
        {
            var said = new List<Field>();
            EntityAction.Find(ActionsOf(world, entityIds), key, form == ReachKind.Value)?.Fields(said);
            return said;
        }

        /// <summary>Whether the member these blocks share that <paramref name="key" /> names, reached as <paramref name="form" /> says, takes time to finish.</summary>
        public static bool Lasts(GridWorld world, IList<long> entityIds, MemberKey key, ReachKind form)
        {
            var action = EntityAction.Find(ActionsOf(world, entityIds), key, form == ReachKind.Value);
            return action != null && action.Lasts;
        }

        /// <summary>Whether any of the blocks offers something the others do not, which the shared list leaves out.</summary>
        public static bool Narrowed(GridWorld world, IList<long> entityIds, bool terminal)
        {
            var entities = Blocks(world, entityIds);
            foreach (var entity in entities)
            {
                var actions = entity.Kind.Actions;
                for (var j = 0; j < actions.Count; j++)
                {
                    var listed = terminal ? actions[j].Unaltered : !actions[j].Hidden;
                    if (listed && !AllHaveAction(entities, actions[j], 0))
                        return true;
                }
            }

            return false;
        }

        static List<EntityAction> ActionsOf(List<GridEntity> entities)
        {
            var result = new List<EntityAction>();
            if (entities.Count == 0)
                return result;

            var candidates = entities[0].Kind.Actions;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AllHaveAction(entities, candidates[i], 1))
                    result.Add(Agreed(entities, candidates[i]));
            }
            return result;
        }

        /// <summary><paramref name="action" /> as <paramref name="entities" /> can all say it: by number for a part they name differently, as a cargo container and a refinery do.</summary>
        static EntityAction Agreed(List<GridEntity> entities, EntityAction action)
        {
            if (action.OwnValue == null)
                return action;

            var parameters = action.OwnValue.Parameters;
            for (var i = 0; i < parameters.Count; i++)
            {
                if (!parameters[i].IsPart)
                    continue;

                for (var other = 1; other < entities.Count; other++)
                {
                    var theirs = EntityAction.Value(entities[other].Kind.Actions, action.Key)?.OwnValue;
                    if (theirs != null && !parameters[i].Agrees(theirs.Parameters[i]))
                        return action.WithPartsNumbered();
                }
            }

            return action;
        }

        /// <summary>The blocks a test can read anything of: a state, or one of their values.</summary>
        public static List<GridEntity> Testable(GridWorld world)
        {
            var result = new List<GridEntity>();
            var entities = world.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                if (Tests(entities[i].Kind))
                    result.Add(entities[i]);
            }

            return result;
        }

        static bool Tests(EntityKind kind)
        {
            for (var i = 0; i < kind.Events.Count; i++)
            {
                if (kind.Events[i].IsState)
                    return true;
            }

            for (var i = 0; i < kind.Actions.Count; i++)
            {
                if (kind.Actions[i].Form == ReachKind.Value)
                    return true;
            }

            return false;
        }

        /// <summary>Every block in <paramref name="world" /> with anything to offer: a moment to be watched where <paramref name="forEvents" />, and an action otherwise.</summary>
        /// <remarks>
        /// Takes no account of what is already picked.
        /// </remarks>
        public static List<GridEntity> Capable(GridWorld world, bool forEvents)
        {
            var result = new List<GridEntity>();
            var entities = world.Entities;
            for (var i = 0; i < entities.Count; i++)
            {
                var entity = entities[i];
                var kind = entity.Kind;
                var count = forEvents ? kind.Events.Count : kind.Actions.Count;
                if (count > 0)
                    result.Add(entity);
            }
            return result;
        }

        /// <summary>
        /// What these names currently point at, skipping the ones that point at nothing.
        /// </summary>
        /// <remarks>
        /// An adrift reference is included, because this decides what may be said about a selection.
        /// </remarks>
        static List<GridEntity> Held(Bindings bindings, IList<string> names)
        {
            var result = new List<GridEntity>();
            for (var i = 0; i < names.Count; i++)
            {
                var entity = bindings.Entity(names[i]);
                if (entity != null)
                    result.Add(entity);
            }
            return result;
        }

        /// <summary>
        /// Whether <paramref name="arguments" />, given to a member reached as <paramref name="form" />, choose toggle on a switch:
        /// the game's toggle beside it, which takes nothing, rather than a value.
        /// </summary>
        public static bool Toggles(ReachKind form, IList<Literal> arguments)
        {
            return form == ReachKind.Value && arguments.Count == 1 && arguments[0].Kind == ValueKind.Text && arguments[0].Text == EntityAction.Toggle;
        }

        /// <summary>
        /// <paramref name="act" />, which acts on the blocks <paramref name="entityIds" />, given <paramref name="values" /> in
        /// place of its own, as a step of its own: the game's toggle beside its switch, taking nothing, where the values choose
        /// toggle.
        /// </summary>
        public static Act Given(GridWorld world, IList<long> entityIds, Act act, IList<Literal> values)
        {
            var given = (Act)Automation.Copied(act);
            given.Arguments.Clear();
            if (!Toggles(act.Form, values))
            {
                for (var i = 0; i < values.Count; i++)
                    given.Arguments.Add(values[i]);

                return given;
            }

            var toggle = Toggle(world, entityIds, act.Key);
            given.Key = toggle != null ? MemberKey.Bare(toggle) : act.Key;
            given.Form = ReachKind.Action;
            return given;
        }

        /// <summary>The id of the game's toggle that flips the switch <paramref name="value" /> names on the blocks, or null where none does.</summary>
        /// <remarks>A toggle is the game's own, so its id alone is its key.</remarks>
        public static string Toggle(GridWorld world, IList<long> entityIds, MemberKey value)
        {
            var entities = Blocks(world, entityIds);
            foreach (var entity in entities)
            {
                var one = EntityAction.Value(entity.Kind.Actions, value);
                if (one != null && one.Flips)
                    return one.FlippedBy;
            }

            return null;
        }

        /// <summary>The switch the game's toggle <paramref name="toggle" /> names flips on the blocks, or null where it flips none listed.</summary>
        public static MemberKey? Flipped(GridWorld world, IList<long> entityIds, MemberKey toggle)
        {
            var entities = Blocks(world, entityIds);
            foreach (var entity in entities)
            {
                var actions = entity.Kind.Actions;
                for (var a = 0; a < actions.Count; a++)
                {
                    if (actions[a].Form == ReachKind.Value && (toggle.IsBare || toggle.Namespace == BlockKinds.Game) && actions[a].FlippedBy == toggle.Id)
                        return actions[a].Key;
                }
            }

            return null;
        }

        /// <summary>What any of the blocks is to offer that is not built yet, each once, in the order the blocks say them.</summary>
        public static List<PlannedMember> Planned(GridWorld world, IList<long> entityIds)
        {
            var planned = new List<PlannedMember>();
            var entities = Blocks(world, entityIds);
            foreach (var entity in entities)
            {
                var plans = entity.Kind.Planned;
                for (var p = 0; p < plans.Count; p++)
                {
                    if (!planned.Contains(plans[p]))
                        planned.Add(plans[p]);
                }
            }

            return planned;
        }

        /// <summary>
        /// What the first of the blocks that has the value <paramref name="key" /> names holds in it now, read off the facet called
        /// <paramref name="facet" /> where the value is of a type the host provides, or null where none can say.
        /// </summary>
        /// <remarks>
        /// A value worked out from what was seen before, or one that wants arguments to be read, gives null. A block with no
        /// facet by that name is read off its first.
        /// </remarks>
        public static Literal? Current(GridWorld world, IList<long> entityIds, MemberKey key, string facet)
        {
            var entities = Blocks(world, entityIds);
            foreach (var entity in entities)
            {
                var value = EntityAction.Value(entity.Kind.Actions, key);
                if (value == null)
                    continue;

                var own = value.OwnValue;
                var given = value.Type == null ? (IList<Literal>)NoArguments : HostType.Arguments(Number(entity, value.Type, facet), NoArguments);
                var gauge = own == null
                    ? entity.Gauge(value)
                    : !own.Remembers && own.Missing(0) == null ? own.Gauge(entity.Block, given, null) : null;
                if (gauge != null)
                    return own != null && own.Kind == ValueKind.Name ? Named(own, gauge.Read()) : gauge.Read();
            }

            return null;
        }

        /// <summary>The name <paramref name="read" /> stands for among <paramref name="own" />'s, which it holds as a position, or null where it stands for none.</summary>
        static Literal? Named(OwnValue own, Literal read)
        {
            var at = read.Kind == ValueKind.Integer ? read.Whole : -1;
            return at >= 0 && at < own.Names.Count ? Literal.Name(own.Names[(int)at]) : (Literal?)null;
        }

        /// <summary>The number of the facet of <paramref name="type" /> called <paramref name="name" /> on <paramref name="entity" />, or 0 where it has none by that name.</summary>
        static int Number(GridEntity entity, HostType type, string name)
        {
            foreach (var facet in entity.Kind.Facets(type))
            {
                if (facet.Name == name)
                    return facet.Number;
            }

            return 0;
        }

        static List<GridEntity> Blocks(GridWorld world, IList<long> entityIds)
        {
            var result = new List<GridEntity>();
            for (var i = 0; i < entityIds.Count; i++)
            {
                var entity = world.FindEntity(entityIds[i]);
                if (entity != null)
                    result.Add(entity);
            }
            return result;
        }

        static bool AllHaveEvent(List<GridEntity> entities, MemberKey key)
        {
            for (var i = 1; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindEvent(key) == null)
                    return false;
            }
            return true;
        }

        static bool AllHaveAction(List<GridEntity> entities, EntityAction action, int from)
        {
            for (var i = from; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindAction(action.Key, action.Form) == null)
                    return false;
            }
            return true;
        }
    }
}
