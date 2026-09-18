using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Terminal;
using VRage.Game.ModAPI;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a construct's own buttons do, which is nothing of Reactor's.
    /// </summary>
    /// <remarks>
    /// A trigger that does one plain thing is written into a toolbar slot as the game's own action,
    /// and nothing of it is kept in the program: the slot is the record. So this is how such a
    /// trigger is shown at all, and it is the same reading that shows the wiring somebody did by hand
    /// before Reactor was ever on the ship.
    /// <para>
    /// <b>Read when somebody asks, never on a tick.</b> Reading a toolbar is the dearest thing Reactor
    /// does, and what it is read for is a screen.
    /// </para>
    /// </remarks>
    public sealed class Vanilla
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

        public IReadOnlyList<Entry> Entries => _entries;

        /// <summary>
        /// Looks at every toolbar on the construct and writes down what is in it.
        /// </summary>
        /// <remarks>
        /// Reactor's own slots are left out. They are the program, which the screens already have,
        /// and listing them here would show every sequence twice.
        /// </remarks>
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
                    if (!toolbar.TryGetItem(slot, out held) || TriggerAction.IsOurs(held))
                        continue;

                    if (held.Kind != MyToolbarItemKind.BlockAction
                        && held.Kind != MyToolbarItemKind.GroupAction)
                    {
                        // A tool, a weapon, a paint colour. Not automation, and not something a list
                        // of what the buttons do should claim to explain.
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
