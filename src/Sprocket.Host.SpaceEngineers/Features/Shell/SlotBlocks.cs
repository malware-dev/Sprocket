using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The blocks that stand for the slots a logic system names, as the editor shows it, and what those blocks offer.</summary>
    public sealed class SlotBlocks
    {
        readonly SprocketEditor _editor;
        readonly Dictionary<string, List<long>> _ofType = new Dictionary<string, List<long>>();
        int _builtAt = -1;

        /// <summary>The blocks standing for slots in <paramref name="editor" />.</summary>
        public SlotBlocks(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>The actions every block that can stand for these names in <paramref name="system" /> offers, as <see cref="Standing" /> finds them.</summary>
        public List<EntityAction> Actions(AutomationSystem system, IList<string> names)
        {
            return Vocabulary.ActionsOf(_editor.World, Standing(system, names));
        }

        /// <summary>The moments every block that can stand for these names in <paramref name="system" /> raises, as <see cref="Standing" /> finds them.</summary>
        public List<EntityEvent> Events(AutomationSystem system, IList<string> names)
        {
            return Vocabulary.EventsOf(_editor.World, Standing(system, names));
        }

        /// <summary>
        /// The blocks that can stand for these names in <paramref name="system" />, by entity id: what the installation shown
        /// fills a slot with, and for a block slot it leaves empty, one block on the construct of each kind the slot's type takes.
        /// </summary>
        public List<long> Standing(AutomationSystem system, IList<string> names)
        {
            var standing = new List<long>();
            var one = new string[1];
            foreach (var name in names)
            {
                one[0] = name;
                var found = _editor.Blocks(system, one);
                var part = found.Count == 0 && system != null && system.IsBlueprint ? system.FindPart(name) : null;
                if (part != null && !part.IsSignal && part.Holds.Length == 0)
                    standing.AddRange(OfType(part.Expects));
                else
                    standing.AddRange(found);
            }

            return standing;
        }

        /// <summary>One block on the construct of each kind a slot expecting <paramref name="expects" /> takes, empty for any block, by entity id.</summary>
        /// <remarks>
        /// Worked out once for each type each time the screen is built, as walking the construct's blocks costs with the size of
        /// the construct; the list given back is kept, and is not to be changed.
        /// </remarks>
        public List<long> OfType(string expects)
        {
            if (_builtAt != _editor.Rebuilds)
            {
                _builtAt = _editor.Rebuilds;
                _ofType.Clear();
            }

            List<long> ids;
            if (_ofType.TryGetValue(expects, out ids))
                return ids;

            var seen = new HashSet<EntityKind>();
            ids = new List<long>();
            foreach (var entity in _editor.World.Entities)
            {
                if (seen.Add(entity.Kind) && Part.Takes(expects, entity.Kind.Lineage))
                    ids.Add(entity.Id);
            }

            _ofType[expects] = ids;
            return ids;
        }
    }
}
