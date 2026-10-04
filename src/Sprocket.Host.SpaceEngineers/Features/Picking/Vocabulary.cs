using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Features.Values;

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
        /// one that reports itself honestly - see <c>RequiredTrigger</c>.
        /// </para>
        /// </remarks>
        public static MomentTrigger Starter(List<EntityEvent> events, string eventId, bool all)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == eventId && events[i].Answered)
                    return new RequiredTrigger(eventId);
            }

            var when = new WhenTrigger(eventId);
            when.RequiresAll = all;
            return when;
        }

        /// <summary>
        /// The moment <paramref name="moment" /> of <paramref name="system" /> means as <paramref name="through" /> runs it: for a
        /// trigger, the trigger on its block that the first toolbar slot answering it is among; for anything watched, what it
        /// watches for. Empty where there is none, or no program to ask.
        /// </summary>
        public static string EventOf(Automation program, Bindings bindings, AutomationSystem system, MomentTrigger moment,
            SystemInstance through)
        {
            var required = moment as RequiredTrigger;
            if (required == null)
                return moment.EventId;

            if (program == null || system == null)
                return "";

            var blocks = new List<string>();
            var positions = new List<ToolbarSlot>();
            program.Answers(system, through, required.Part, blocks, positions);
            if (positions.Count == 0)
                return "";

            GridEntity block = bindings == null ? null : bindings.Entity(blocks[0]);
            EntityEvent stands = block == null ? null : block.Kind.Trigger(positions[0]);
            return stands != null ? stands.Id : positions[0].EventId;
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
        public static List<Field> Fields(GridWorld world, IList<long> entityIds, string actionId, Form form)
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
        public static bool Lasts(GridWorld world, IList<long> entityIds, string actionId, Form form)
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
                if (kind.Actions[i].Sets)
                    return true;
            }

            return false;
        }

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

        /// <summary>How far the least checked of the blocks has been checked, or completed where none are picked.</summary>
        public static Evaluation Least(GridWorld world, IList<long> entityIds)
        {
            var least = Evaluation.Completed;
            var entities = Blocks(world, entityIds);
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].Kind.Evaluation < least)
                    least = entities[i].Kind.Evaluation;
            }

            return least;
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
