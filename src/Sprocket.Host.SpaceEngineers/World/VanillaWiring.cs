using System.Collections.Generic;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Terminal;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a construct's own buttons do, which is nothing of Sprocket's.
    /// </summary>
    /// <remarks>Read when a screen asks, never on a tick: reading a toolbar is the costliest thing Sprocket does.</remarks>
    public sealed class VanillaWiring
    {
        /// <summary>One slot that does something, and what it does.</summary>
        public struct Entry
        {
            /// <summary>The block whose toolbar holds it.</summary>
            public long PanelId;

            /// <summary>Which slot of that toolbar, counted from zero.</summary>
            public int Slot;

            /// <summary>The block it acts on, or zero where it acts on a group.</summary>
            public long BlockId;

            /// <summary>The group it acts on, or empty where it acts on one block.</summary>
            public string GroupName;

            /// <summary>The game's own name for the action.</summary>
            public string ActionId;
        }

        readonly List<Entry> _entries = new List<Entry>();

        /// <summary>Every slot read that does something, in the order they were read.</summary>
        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Looks at every toolbar on the construct and writes down what is in it.
        /// </summary>
        /// <remarks>Sprocket's own slots are left out.</remarks>
        public void Read(GridWorld world)
        {
            _entries.Clear();
            if (world == null)
                return;

            var blocks = world.Entities;
            for (var i = 0; i < blocks.Count; i++)
            {
                GridEntity block = blocks[i];
                IMyToolbar toolbar = Toolbars.Of(block.Block);
                if (toolbar == null)
                    continue;

                for (var slot = 0; slot < toolbar.SlotCount; slot++)
                {
                    MyToolbarItemInfo held;
                    if (!toolbar.TryGetItem(slot, out held) || SignalAction.IsOurs(held))
                        continue;

                    if (held.Kind != MyToolbarItemKind.BlockAction
                        && held.Kind != MyToolbarItemKind.GroupAction)
                    {
                        // A tool, a weapon, a paint color: not automation.
                        continue;
                    }

                    var entry = new Entry();
                    entry.PanelId = block.Block.EntityId;
                    entry.Slot = slot;
                    entry.BlockId = held.BlockEntityId;
                    entry.GroupName = held.GroupName ?? "";
                    entry.ActionId = held.ActionId ?? "";
                    _entries.Add(entry);
                }
            }
        }
    }
}
