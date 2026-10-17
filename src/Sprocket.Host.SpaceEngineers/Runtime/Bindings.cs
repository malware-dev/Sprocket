using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// What each declared reference currently points at, and how well.
    /// </summary>
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
        /// <remarks>Anything that resolves a name on every tick holds what it found and compares this instead.</remarks>
        public int Revision { get; private set; }

        /// <summary>
        /// Works these out again only if something has changed since last time, and says whether it did.
        /// </summary>
        /// <remarks>Called on every tick; two integer comparisons where nothing has changed.</remarks>
        public bool Refresh(GridWorld world, Automation program)
        {
            if (world.Revision == _world && program.Revision == _program)
                return false;

            _world = world.Revision;
            _program = program.Revision;
            Resolve(world, program.Refs, program.SignalRefs);
            return true;
        }

        /// <summary>Resolves every declaration against the world as it is now, unconditionally.</summary>
        public void Resolve(GridWorld world, IReadOnlyList<EntityRef> refs)
        {
            Resolve(world, refs, NoSignals);
        }

        /// <summary>
        /// Resolves every declaration against the world as it is now, and each signal variable to the first block its slot
        /// is on, as bound as the least bound of its blocks.
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

                // The kind refuses what a key found rather than finding anything: a name landing on a light where a door
                // was declared is a miss.
                if (found != null && declared.KindId.Length > 0 && found.Kind.Id != declared.KindId)
                    found = null;

                if (found == null)
                {
                    _states[declared.Name] = RefState.Lost;
                    continue;
                }

                _bound[declared.Name] = found;

                // The keys are tried in the order the program lists them, and the first to find a block decides. An id or
                // a path found further down still says which block was meant; a name further down only proposes one,
                // since two blocks can share it. Found by its path alone, it is a copy's block, and its id key takes the
                // new id in the place it already has.
                var key = declared.Keys[matched];
                var sure = matched == 0 || key.Cells != null || key.Kind == ValueKind.Integer;
                _states[declared.Name] = sure ? RefState.Bound : RefState.Adrift;
                if (key.Cells != null)
                    world.Renumber(found, declared.Keys);
            }

            for (var i = 0; i < signals.Count; i++)
            {
                var blocks = signals[i].Blocks;
                GridEntity panel;
                if (blocks.Count > 0 && _bound.TryGetValue(blocks[0], out panel))
                    _bound[signals[i].Name] = panel;

                var state = RefState.Bound;
                for (var n = 0; n < blocks.Count; n++)
                {
                    var one = State(blocks[n]);
                    if (one == RefState.Lost || (one == RefState.Adrift && state == RefState.Bound))
                        state = one;
                }

                _states[signals[i].Name] = state;
            }
        }

        /// <summary>
        /// What this reference points at, or null.
        /// </summary>
        /// <remarks>Answers for an adrift reference too; whether it may be acted on is <see cref="Holds" />.</remarks>
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

        /// <summary>Whether the reference <paramref name="name" /> may be acted on, which only a bound one may.</summary>
        public bool Holds(string name)
        {
            return State(name) == RefState.Bound;
        }

        /// <summary>Leaves each block in <paramref name="names" /> once: a name repeated, or a second name for a block already there, is taken out.</summary>
        /// <remarks>Quadratic in the length of the list, so called where a list of names is worked out, never per tick.</remarks>
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
