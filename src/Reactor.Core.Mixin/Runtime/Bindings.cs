using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Runtime
{
    /// <summary>
    /// What each declared reference currently points at, and how well.
    /// </summary>
    /// <remarks>
    /// Worked out against a world rather than stored, because the answer changes when somebody welds
    /// a block on or shoots one off, and a stored answer would be a stale one at exactly the moment
    /// it mattered.
    /// <para>
    /// Nothing here reads a key. The host is asked to find something and to say which key found it;
    /// core turns "the first one" into <see cref="RefState.Bound" /> and anything later into
    /// <see cref="RefState.Adrift" />, which is the whole of what it knows about identity.
    /// </para>
    /// </remarks>
    public sealed class Bindings
    {
        readonly Dictionary<string, IEntity> _bound = new Dictionary<string, IEntity>();
        readonly Dictionary<string, RefState> _states = new Dictionary<string, RefState>();

        /// <summary>Resolves every declaration against the world as it is now.</summary>
        public void Resolve(IWorld world, IReadOnlyList<EntityRef> refs)
        {
            _bound.Clear();
            _states.Clear();

            for (var i = 0; i < refs.Count; i++)
            {
                var declared = refs[i];

                int matched;
                var found = world.Match(declared.Keys, out matched);

                // The kind is a constraint rather than a key: it never finds anything, it refuses
                // what a key found. A name landing on a light where a door was declared is a miss.
                if (found != null && declared.KindId.Length > 0 && found.Kind.Id != declared.KindId)
                    found = null;

                if (found == null)
                {
                    _states[declared.Name] = RefState.Lost;
                    continue;
                }

                _bound[declared.Name] = found;
                _states[declared.Name] = matched == 0 ? RefState.Bound : RefState.Adrift;
            }
        }

        /// <summary>
        /// What this reference points at, or null.
        /// </summary>
        /// <remarks>
        /// Answers for an adrift reference too, because the editor has to show the candidate it is
        /// proposing. Whether it may be <i>acted</i> on is <see cref="Holds" />.
        /// </remarks>
        public IEntity Entity(string name)
        {
            IEntity entity;
            return _bound.TryGetValue(name, out entity) ? entity : null;
        }

        public RefState State(string name)
        {
            RefState state;
            return _states.TryGetValue(name, out state) ? state : RefState.Lost;
        }

        /// <summary>Whether this may be acted on. Only a bound reference may.</summary>
        public bool Holds(string name)
        {
            return State(name) == RefState.Bound;
        }
    }
}
