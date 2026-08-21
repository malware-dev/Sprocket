using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>Reads whether a situation currently holds.</summary>
    public static class Conditions
    {
        /// <summary>
        /// True when every named entity is in the named state.
        /// </summary>
        /// <remarks>
        /// An unfinished condition - no entities picked yet - is false rather than
        /// true. A half-written gate that let everything through would be a sequence
        /// that appears to work while the user is still building it, and then behaves
        /// differently once they finish.
        /// </remarks>
        public static bool IsTrue(IWorld world, Condition condition)
        {
            if (condition == null || condition.Entities.Count == 0)
                return false;

            for (var i = 0; i < condition.Entities.Count; i++)
            {
                if (!Holds(world, condition.Entities[i], condition.StateId))
                    return false;
            }
            return true;
        }

        /// <summary>As <see cref="IsTrue"/>, but ignoring one entity.</summary>
        public static bool IsTrueExcept(IWorld world, Condition condition, long ignoreEntityId)
        {
            for (var i = 0; i < condition.Entities.Count; i++)
            {
                if (condition.Entities[i] == ignoreEntityId)
                    continue;
                if (!Holds(world, condition.Entities[i], condition.StateId))
                    return false;
            }
            return true;
        }

        static bool Holds(IWorld world, long entityId, string stateId)
        {
            var entity = world.FindEntity(entityId);
            if (entity == null)
                return false;

            var state = entity.Kind.FindState(stateId);
            if (state == null)
                return false;

            return state.Holds(entity.GetValue(state.PropertyId));
        }
    }
}
