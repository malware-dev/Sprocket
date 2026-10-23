using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blocks;
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

        /// <summary>
        /// The screens the installation shown holds of <paramref name="blocks" />, by entity id as the slots <paramref name="names" />
        /// of <paramref name="system" /> give them, each by name in the blocks' order and several of one block joined by commas;
        /// null where none of the slots holds screens.
        /// </summary>
        /// <remarks>Walks the slots' blocks, so it is for building a screen, never for a tick.</remarks>
        public List<string> Screens(AutomationSystem system, IList<string> names, IList<long> blocks)
        {
            var shown = system != null && system.IsBlueprint ? _editor.StandIn(system) : null;
            if (shown == null)
                return null;

            var held = new List<long>();
            var said = new List<string>();
            foreach (var name in names)
            {
                var part = system.FindPart(name);
                var type = part == null ? null : HostTypes.Named(part.Holds);
                var filled = type == null ? null : shown.FilledWith(name);
                var facets = shown.Facets(name);
                for (var i = 0; filled != null && i < filled.Count; i++)
                {
                    GridEntity entity = _editor.Runner.Bindings.Entity(filled[i]);
                    var facet = entity == null || facets == null || i >= facets.Count ? null : type.Find(entity.Kind, facets[i]);
                    if (facet == null)
                        continue;

                    held.Add(entity.Id);
                    said.Add(facet.Name);
                }
            }

            if (held.Count == 0)
                return null;

            var screens = new List<string>(blocks.Count);
            var joined = new List<string>();
            foreach (var id in blocks)
            {
                joined.Clear();
                for (var i = 0; i < held.Count; i++)
                {
                    if (held[i] == id && !joined.Contains(said[i]))
                        joined.Add(said[i]);
                }

                screens.Add(string.Join(", ", joined.ToArray()));
            }

            return screens;
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
