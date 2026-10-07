using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
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
        static readonly SignalRef[] NoSignals = new SignalRef[0];

        readonly Dictionary<string, GridEntity> _bound = new Dictionary<string, GridEntity>();
        readonly Dictionary<string, RefState> _states = new Dictionary<string, RefState>();

        int _world = -1;
        int _program = -1;

        /// <summary>
        /// Bumped whenever these were worked out again, so a reader can cache off it.
        /// </summary>
        /// <remarks>
        /// Anything that resolves a name to an entity on every tick should hold what it found and
        /// compare this instead. The chain is the whole point: the world says when its blocks
        /// changed, the program says when its declarations did, this says when either mattered, and
        /// the tick reads a cached answer.
        /// </remarks>
        public int Revision { get; private set; }

        /// <summary>
        /// Works these out again only if something has changed since last time.
        /// </summary>
        /// <remarks>
        /// <b>Called on every tick, and does nothing on almost all of them.</b> Two integer
        /// comparisons in the ordinary case, because matching walks every key against every entity
        /// and that cost belongs at the moment a block is welded on rather than sixty times a second
        /// forever after.
        /// </remarks>
        public void Refresh(GridWorld world, Automation program)
        {
            if (world.Revision == _world && program.Revision == _program)
                return;

            _world = world.Revision;
            _program = program.Revision;
            Resolve(world, program.Refs, program.SignalRefs);
        }

        /// <summary>Resolves every declaration against the world as it is now, unconditionally.</summary>
        public void Resolve(GridWorld world, IReadOnlyList<EntityRef> refs)
        {
            Resolve(world, refs, NoSignals);
        }

        /// <summary>
        /// The same, and each signal variable to the block its slot is on, so a signal names its block wherever blocks are
        /// asked after.
        /// </summary>
        public void Resolve(GridWorld world, IReadOnlyList<EntityRef> refs, IReadOnlyList<SignalRef> signals)
        {
            Revision++;
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

                // The keys are tried in the order the program lists them, and the first to find a block
                // decides. Found by the first of them, it is the block the author put first. An id or a
                // path found further down still says which block was meant; a name further down only
                // proposes one, since two blocks can share it.
                //
                // Found by its path alone, it is a copy's block, and its id key takes the new id where it
                // has one - in the place it already has, so the order the program was written in stands.
                var key = declared.Keys[matched];
                var sure = matched == 0 || key.Cells != null || key.Kind == ValueKind.Integer;
                _states[declared.Name] = sure ? RefState.Bound : RefState.Adrift;
                if (key.Cells != null)
                    world.Renumber(found, declared.Keys);
            }

            for (var i = 0; i < signals.Count; i++)
            {
                GridEntity panel;
                if (_bound.TryGetValue(signals[i].Block, out panel))
                    _bound[signals[i].Name] = panel;

                _states[signals[i].Name] = State(signals[i].Block);
            }
        }

        /// <summary>
        /// What this reference points at, or null.
        /// </summary>
        /// <remarks>
        /// Answers for an adrift reference too, because the editor has to show the candidate it is
        /// proposing. Whether it may be <i>acted</i> on is <see cref="Holds" />.
        /// </remarks>
        public GridEntity Entity(string name)
        {
            GridEntity entity;
            return _bound.TryGetValue(name, out entity) ? entity : null;
        }

        /// <summary>How surely the variable <paramref name="name" /> was found on the construct, or lost where it was not.</summary>
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

        /// <summary>Leaves each block in <paramref name="names" /> once: a name repeated, or a second name for a block already there, is taken out.</summary>
        /// <remarks>
        /// The language's names stand for one set of blocks. Done where a list of names is worked out, never per tick: the walk
        /// is quadratic in the length of one list, which is a handful of blocks.
        /// </remarks>
        public void Distinct(List<string> names)
        {
            var kept = 0;
            for (var i = 0; i < names.Count; i++)
            {
                var entity = Entity(names[i]);
                var seen = false;
                for (var k = 0; k < kept && !seen; k++)
                    seen = names[k] == names[i] || (entity != null && Entity(names[k]) == entity);

                if (!seen)
                    names[kept++] = names[i];
            }

            names.RemoveRange(kept, names.Count - kept);
        }
    }
}
