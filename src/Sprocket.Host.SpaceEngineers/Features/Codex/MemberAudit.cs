using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Picking;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>Finds what the organized lists offer that the codex says nothing of, kind by kind of block on the construct.</summary>
    public sealed class MemberAudit
    {
        readonly SprocketEditor _editor;

        /// <summary>An audit of the construct <paramref name="editor" /> is open on.</summary>
        public MemberAudit(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>Each row with no text, as its kind of block and the name it is listed under, in the order the kinds are met.</summary>
        /// <remarks>Goes through every kind of block once, in all three lists: use it from a press, never on a repeating path.</remarks>
        public List<string> Undescribed()
        {
            var missing = new List<string>();
            var seen = new HashSet<EntityKind>();
            var listedTerminal = _editor.ShowsTerminal;
            _editor.ShowsTerminal = false;

            foreach (var block in _editor.World.Entities)
            {
                if (!seen.Add(block.Kind))
                    continue;

                var selection = new List<long> { block.Id };
                var rows = new HashSet<string>();
                Look(block.Kind, selection, false, false, rows, missing);
                Look(block.Kind, selection, false, true, rows, missing);
                Look(block.Kind, selection, true, false, rows, missing);
            }

            _editor.ShowsTerminal = listedTerminal;
            return missing;
        }

        void Look(EntityKind kind, List<long> selection, bool forEvents, bool forTests, HashSet<string> rows, List<string> missing)
        {
            var offers = new MemberOffers(_editor, forEvents, forTests, false);
            offers.Fill(selection, false, null);

            for (var at = 0; at < offers.Names.Count; at++)
            {
                var name = offers.Names[at];
                var isStatus = offers.IsStatusWord(at);
                if (!rows.Add($"{name} {isStatus}"))
                    continue;

                if (_editor.Codex.Member(kind, name, isStatus).Length == 0)
                    missing.Add($"{kind.DisplayName}: {name}");

                if (forEvents || forTests)
                    continue;

                foreach (var field in _editor.Fields(selection, offers.Ids[at], offers.Forms[at]))
                {
                    // A value's box for its new value goes under the value's own name, and the value's text covers it.
                    if (!string.Equals(field.Name, name, StringComparison.OrdinalIgnoreCase) && _editor.Codex.Option(kind, field.Name) == null)
                        missing.Add($"{kind.DisplayName}: option {field.Name} of {name}");
                }
            }
        }
    }
}
