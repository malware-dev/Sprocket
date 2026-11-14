using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>The blocks a slot's references found by name alone, taken as the blocks they mean.</summary>
    public sealed class AcceptBlocks : ProgramEdit
    {
        readonly SystemInstance _instance;
        readonly string _slot;
        readonly List<EntityRef> _refs = new List<EntityRef>();
        readonly List<List<EntityKey>> _was = new List<List<EntityKey>>();
        readonly List<List<EntityKey>> _now = new List<List<EntityKey>>();

        /// <summary>
        /// The taking of <paramref name="blocks" /> as what the declarations <paramref name="refs" /> of the slot
        /// <paramref name="slot" /> of <paramref name="instance" /> mean, the two lists in step, identified in <paramref name="world" />.
        /// </summary>
        public AcceptBlocks(SystemInstance instance, string slot, IList<EntityRef> refs, IList<GridEntity> blocks, GridWorld world)
        {
            _instance = instance;
            _slot = slot;
            for (var i = 0; i < refs.Count; i++)
            {
                _refs.Add(refs[i]);
                _was.Add(new List<EntityKey>(refs[i].Keys));

                var keys = new List<EntityKey>();
                world.Keys.Identify(blocks[i], keys);
                _now.Add(keys);
            }
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.FillSlot;

        /// <inheritdoc />
        public override void Name(Automation program, bool done, ChangeName name)
        {
            name.Thing = _slot;
            name.Place = _instance;
            name.Count = _refs.Count;
        }

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            for (var i = 0; i < _refs.Count; i++)
                program.Rekey(_refs[i], _now[i]);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            for (var i = 0; i < _refs.Count; i++)
                program.Rekey(_refs[i], _was[i]);
        }
    }
}
