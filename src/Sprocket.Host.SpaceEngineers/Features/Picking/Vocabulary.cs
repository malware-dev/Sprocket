using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Fields;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// Works out what a slot is allowed to contain, given what is already in the
    /// sequence.
    /// </summary>
    /// <remarks>
    /// Nothing invalid is ever offered. An illegal sequence is not rejected with a
    /// message - it is simply unreachable, which is worth more than any error text
    /// because the user never has to recover from anything.
    /// </remarks>
    public static class Vocabulary
    {
        // What a value read with no arguments is given.
        static readonly Literal[] NoArguments = new Literal[0];

        /// <summary>
        /// Events every one of the given entities can raise.
        /// </summary>
        /// <remarks>
        /// The intersection, not the union. "When any of these happens" has to mean
        /// something for every entity in the selection, or the sentence stops being
        /// true of the things it names.
        /// </remarks>
        public static List<EntityEvent> EventsFor(Bindings bindings, IList<string> names)
        {
            return EventsOf(Held(bindings, names));
        }

        /// <summary>
        /// The same, asked about blocks somebody is holding rather than about a sequence.
        /// </summary>
        /// <remarks>
        /// A picker has blocks and no declarations yet - it is the thing that turns one into the
        /// other. Everything else asks the first form.
        /// </remarks>
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
                if (AllHaveEvent(entities, candidates[i].Id))
                    result.Add(candidates[i]);
            }
            return result;
        }

        /// <summary>
        /// What starts a sequence when somebody picks these blocks and this moment.
        /// </summary>
        /// <remarks>
        /// <b>The player is never asked this.</b> "When the airlock button is pressed" and "when the
        /// inner door finishes closing" are one sentence to write and two different arrangements
        /// underneath: the game already starts things from a button, through a toolbar slot, and it
        /// announces nothing at all about a door finishing. Which of those it is belongs to the
        /// block, so it is read off the block rather than put to the person at the screen.
        /// <para>
        /// Asked of the whole selection, because the moment has to arrive the same way for all of
        /// them: a trigger that was half watched and half answered would fire twice for some of its
        /// blocks and not at all for the rest. A mixed selection is treated as watched, which is the
        /// one that reports itself honestly - see <c>SignalTrigger</c>.
        /// </para>
        /// <para>
        /// A change Sprocket watches for is a signal written on the blocks, sent from any of them. Asked of all of them, it is
        /// the value it is a change in, coming to be so on the last of them: "when all of these doors finish closing" is the
        /// doors' status coming to be closed on all of them.
        /// </para>
        /// </remarks>
        public static MomentTrigger Starter(List<EntityEvent> events, string eventId, bool all)
        {
            EntityEvent picked = null;
            for (var i = 0; i < events.Count && picked == null; i++)
            {
                if (events[i].Id == eventId)
                    picked = events[i];
            }

            if (picked != null && picked.Answered)
                return new SignalTrigger(eventId);

            if (!all || picked == null || picked.Value.Length == 0)
                return SignalTrigger.Written(BlockKinds.Own, eventId);

            var when = new WhenTrigger(picked.Value);
            when.Provider = BlockKinds.Own;
            when.Quantifier = Quantifier.All;
            when.Value = picked.Becomes;
            return when;
        }

        /// <summary>
        /// The signal among <paramref name="events" /> that is the value <paramref name="value" /> coming to be what
        /// <paramref name="compared" /> says, with whether the comparison asks for the block to be in that state rather than
        /// out of it; null where no signal is, or the comparison is no sameness.
        /// </summary>
        /// <remarks>How a test or a watch on one of Sprocket's own values reads in a sentence: by the words of its signal.</remarks>
        public static EntityEvent Change(IList<EntityEvent> events, string value, Relation relation, Literal compared,
            out bool inState)
        {
            inState = relation == Relation.Is;
            if (relation != Relation.Is && relation != Relation.IsNot)
                return null;

            for (var i = 0; i < events.Count; i++)
            {
                var signal = events[i];
                if (signal.Value == value && signal.IsState && signal.Becomes.Said == compared.Said)
                    return signal;
            }

            return null;
        }

        /// <summary>
        /// The moment <paramref name="moment" /> of <paramref name="system" /> means as <paramref name="through" /> runs it: for a
        /// trigger, the trigger on its block that the first toolbar slot answering it is among; for anything watched, what it
        /// watches for. Empty where there is none, or no program to ask.
        /// </summary>
        public static string EventOf(Automation program, Bindings bindings, AutomationSystem system, MomentTrigger moment,
            SystemInstance through)
        {
            var when = moment as WhenTrigger;
            if (when != null && when.Provider == BlockKinds.Own && bindings != null)
            {
                bool inState;
                var signal = Change(EventsFor(bindings, SystemInstance.Through(through, when.Sources)), when.EventId,
                    when.Relation, when.Value, out inState);

                return signal != null && inState ? signal.Id : when.EventId;
            }

            var required = moment as SignalTrigger;
            if (required == null || required.IsWatched)
                return moment.EventId;

            if (program == null || system == null)
                return "";

            var blocks = new List<string>();
            var positions = new List<ToolbarSlot>();
            program.Answers(system, through, required, blocks, positions);
            if (positions.Count == 0)
                return "";

            GridEntity block = bindings == null ? null : bindings.Entity(blocks[0]);
            EntityEvent stands = block == null ? null : block.Kind.Signal(positions[0]);
            return stands != null ? stands.Id : positions[0].EventId;
        }

        /// <summary>
        /// Whether <paramref name="when" /> compares a value on the blocks <paramref name="sources" /> stand for, rather than
        /// watching for one of the moments they say: anything but Sprocket's own state coming to be.
        /// </summary>
        public static bool Compares(Bindings bindings, WhenTrigger when, IList<string> sources)
        {
            if (when.Provider != BlockKinds.Own || bindings == null)
                return true;

            bool inState;
            return Change(EventsFor(bindings, sources), when.EventId, when.Relation, when.Value, out inState) == null || !inState;
        }

        /// <summary>
        /// The namespace a step doing <paramref name="id" /> as <paramref name="form" /> on these blocks is qualified by:
        /// Sprocket's own for its own actions and values, empty for the game's.
        /// </summary>
        public static string Provider(GridWorld world, IList<long> entityIds, string id, Nature form)
        {
            if (form == Nature.Native)
                return BlockKinds.Own;

            if (form != Nature.Value)
                return "";

            return EntityAction.Value(ActionsOf(world, entityIds), BlockKinds.Own, id) != null ? BlockKinds.Own : "";
        }

        /// <summary>Actions every one of the given entities accepts.</summary>
        public static List<EntityAction> ActionsFor(Bindings bindings, IList<string> names)
        {
            return ActionsOf(Held(bindings, names));
        }

        /// <summary>The same, asked about blocks somebody is holding.</summary>
        public static List<EntityAction> ActionsOf(GridWorld world, IList<long> entityIds)
        {
            return ActionsOf(Blocks(world, entityIds));
        }

        /// <summary>
        /// What each value one of those actions wants asks for, one per value.
        /// </summary>
        /// <remarks>
        /// Empty where the action wants nothing, so its length is how many are wanted and whether
        /// to ask at all. Asked of the blocks somebody has just picked, because that is where the
        /// answer is: the action is the block's own.
        /// <para>
        /// Asked by id and by which of the three ways it reaches the block, because the id alone can
        /// name more than one: a switch's value and the act that flips it share one.
        /// </para>
        /// </remarks>
        public static List<Field> Fields(GridWorld world, IList<long> entityIds, string actionId, Nature form)
        {
            var said = new List<Field>();

            var actions = ActionsOf(world, entityIds);
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == actionId && actions[i].Form == form)
                    actions[i].Fields(said);
            }

            return said;
        }

        /// <summary>Whether the action these blocks share by that id and form takes time to finish.</summary>
        /// <remarks>Which is whether a step doing it is followed by a wait for it.</remarks>
        public static bool Lasts(GridWorld world, IList<long> entityIds, string actionId, Nature form)
        {
            var actions = ActionsOf(world, entityIds);
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == actionId && actions[i].Form == form)
                    return actions[i].Lasts;
            }

            return false;
        }

        /// <summary>Whether any of the blocks offers something the others do not, which the shared list leaves out.</summary>
        /// <remarks>Asked of the view showing, because each lists a different set.</remarks>
        public static bool Narrowed(GridWorld world, IList<long> entityIds, bool terminal)
        {
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                var actions = entities[i].Kind.Actions;
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
                    result.Add(candidates[i]);
            }
            return result;
        }

        /// <summary>
        /// Entities that have anything to offer at all: something that can happen to
        /// them, or something that can be done to them.
        /// </summary>
        /// <remarks>
        /// Deliberately blind to what is already picked. Picking blocks decides what
        /// can be said about them, never the other way round: a list that loses rows
        /// as they are ticked reads as the ship shrinking, and a block somebody knows
        /// they have going missing is the one thing a picker must never do.
        ///
        /// So an impossible pair is allowed to be made, and answered where the answer
        /// belongs - the next stage has nothing in it and says so. That is a sentence
        /// the user can read, which is worth more than a row that silently was not
        /// there.
        ///
        /// A block with no events at all still never appears in an event picker. That
        /// is not the selection talking, it is the question: there is nothing to ask
        /// about a block that nothing can happen to.
        /// </remarks>
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
                if (kind.Actions[i].Form == Nature.Value)
                    return true;
            }

            return false;
        }

        /// <summary>Every block in <paramref name="world" /> with anything to offer: a moment to be watched where <paramref name="forEvents" />, and an action otherwise.</summary>
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
        /// An adrift reference is included, because this decides what may be <i>said</i> about a
        /// selection rather than what may be done to it. Refusing to offer "open" for a door whose
        /// id died would leave somebody unable to describe the thing they are about to re-point.
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

        /// <summary>The game's toggle that flips the switch <paramref name="value" /> on the blocks, or null where none does.</summary>
        public static string Toggle(GridWorld world, IList<long> entityIds, string value)
        {
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                var one = entities[i].Kind.FindAction(value, Nature.Value);
                if (one != null && one.Flips)
                    return one.FlippedBy;
            }

            return null;
        }

        /// <summary>The switch the game's toggle <paramref name="toggle" /> flips on the blocks, or null where it flips none listed.</summary>
        public static string Flipped(GridWorld world, IList<long> entityIds, string toggle)
        {
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                var actions = entities[i].Kind.Actions;
                for (var a = 0; a < actions.Count; a++)
                {
                    if (actions[a].Form == Nature.Value && actions[a].FlippedBy == toggle)
                        return actions[a].Id;
                }
            }

            return null;
        }

        /// <summary>What any of the blocks is to offer that is not built yet, each once, in the order the blocks say them.</summary>
        public static List<Plan> Planned(GridWorld world, IList<long> entityIds)
        {
            var planned = new List<Plan>();
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                var plans = entities[i].Kind.Planned;
                for (var p = 0; p < plans.Count; p++)
                {
                    if (!planned.Contains(plans[p]))
                        planned.Add(plans[p]);
                }
            }

            return planned;
        }

        /// <summary>What the first of the blocks that has the value <paramref name="id" /> holds in it now, or null where none can say.</summary>
        /// <remarks>
        /// Read the way a check reads it, so a step setting the value opens on what the block holds. A value that is worked
        /// out from what was seen before, or that wants arguments to be read, has nothing to say here.
        /// </remarks>
        public static Literal? Current(GridWorld world, IList<long> entityIds, string id)
        {
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                var value = entities[i].Kind.FindAction(id, Nature.Value);
                if (value == null)
                    continue;

                var being = value.Being;
                var gauge = being == null
                    ? entities[i].Gauge(value)
                    : !being.Remembers && being.Missing(0) == null ? being.Gauge(entities[i].Block, NoArguments, null) : null;
                if (gauge != null)
                    return gauge.Read();
            }

            return null;
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

        static bool AllHaveEvent(List<GridEntity> entities, string eventId)
        {
            for (var i = 1; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindEvent(eventId) == null)
                    return false;
            }
            return true;
        }

        static bool AllHaveAction(List<GridEntity> entities, EntityAction action, int from)
        {
            for (var i = from; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindAction(action.Id, action.Form) == null)
                    return false;
            }
            return true;
        }
    }
}
