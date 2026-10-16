using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Features.Terminal;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What every slot of a toolbar a person presses holds on the blocks picked, for the slots offered under it.</summary>
    public static class Occupancy
    {
        /// <summary>
        /// Reads into <paramref name="into" /> what each slot of <paramref name="pressed" /> holds on <paramref name="blocks" />,
        /// page by page: a slot holds something where it does on any of them, says what on each, and answers a trigger where
        /// it does on any of them.
        /// </summary>
        /// <remarks>Read as the toolbar is chosen or the blocks change, never on a tick.</remarks>
        public static void Read(SprocketEditor editor, IList<long> blocks, EntityEvent pressed, List<SlotHeld> into)
        {
            var count = pressed.Pages * pressed.Slots;
            while (into.Count < count)
                into.Add(new SlotHeld());
            while (into.Count > count)
                into.RemoveAt(into.Count - 1);

            foreach (var held in into)
            {
                held.Taken = false;
                held.Answering = false;
                held.Picture = "";
                held.Says = "";
                held.Lines.Clear();
                held.Label = "";
            }

            var whole = ToolbarSlot.Of(pressed.Id);
            if (whole == null)
                return;

            foreach (var id in blocks)
            {
                GridEntity block = editor.World.FindEntity(id);
                IMyToolbar toolbar = block == null ? null : Toolbars.Of(block.Block, whole.Toolbar);
                if (toolbar == null)
                    continue;

                var panel = block.Block as IMyButtonPanel;
                var prefix = blocks.Count > 1 ? $"{block.Name}: " : "";
                for (var i = 0; i < count; i++)
                {
                    var held = into[i];
                    var at = new ToolbarSlot(whole.Toolbar, i / pressed.Slots + 1, i % pressed.Slots + 1);
                    MyToolbarItemInfo present;
                    var index = Toolbars.Index(toolbar, at);
                    if (index < 0 || !toolbar.TryGetItem(index, out present))
                        continue;

                    string picture;
                    string says;
                    if (SignalAction.IsOurs(present))
                    {
                        held.Answering = true;
                        says = prefix + editor.Answering(block, at);
                    }
                    else
                    {
                        held.Taken = true;
                        says = prefix + Toolbars.Described(present, out picture);
                        if (held.Picture.Length == 0)
                            held.Picture = picture;
                    }

                    held.Lines.Add(says);
                    if (held.Lines.Count > 1)
                        continue;

                    held.Says = says;
                    held.Label = panel != null && panel.HasCustomButtonName(index) ? panel.GetButtonName(index) : present.Label ?? "";
                }
            }
        }
    }
}
