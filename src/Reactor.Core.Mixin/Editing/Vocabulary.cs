using System.Collections.Generic;
using Reactor.Runtime;
using Reactor.World;

namespace Reactor.Editing
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
        public static List<EntityEvent> EventsOf(IWorld world, IList<long> entityIds)
        {
            return EventsOf(Blocks(world, entityIds));
        }

        static List<EntityEvent> EventsOf(List<IEntity> entities)
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

        /// <summary>Actions every one of the given entities accepts.</summary>
        public static List<EntityAction> ActionsFor(Bindings bindings, IList<string> names)
        {
            return ActionsOf(Held(bindings, names));
        }

        /// <summary>The same, asked about blocks somebody is holding.</summary>
        public static List<EntityAction> ActionsOf(IWorld world, IList<long> entityIds)
        {
            return ActionsOf(Blocks(world, entityIds));
        }

        static List<EntityAction> ActionsOf(List<IEntity> entities)
        {
            var result = new List<EntityAction>();
            if (entities.Count == 0)
                return result;

            var candidates = entities[0].Kind.Actions;
            for (var i = 0; i < candidates.Count; i++)
            {
                if (AllHaveAction(entities, candidates[i].Id))
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
        /// as you tick them reads as the ship shrinking, and a block somebody knows
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
        public static List<IEntity> Capable(IWorld world, bool forEvents)
        {
            var result = new List<IEntity>();
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
        static List<IEntity> Held(Bindings bindings, IList<string> names)
        {
            var result = new List<IEntity>();
            for (var i = 0; i < names.Count; i++)
            {
                var entity = bindings.Entity(names[i]);
                if (entity != null)
                    result.Add(entity);
            }
            return result;
        }

        static List<IEntity> Blocks(IWorld world, IList<long> entityIds)
        {
            var result = new List<IEntity>();
            for (var i = 0; i < entityIds.Count; i++)
            {
                var entity = world.FindEntity(entityIds[i]);
                if (entity != null)
                    result.Add(entity);
            }
            return result;
        }

        static bool AllHaveEvent(List<IEntity> entities, string eventId)
        {
            for (var i = 1; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindEvent(eventId) == null)
                    return false;
            }
            return true;
        }

        static bool AllHaveAction(List<IEntity> entities, string actionId)
        {
            for (var i = 1; i < entities.Count; i++)
            {
                if (entities[i].Kind.FindAction(actionId) == null)
                    return false;
            }
            return true;
        }

        static bool Contains(IList<string> names, string name)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == name)
                    return true;
            }
            return false;
        }
    }
}
