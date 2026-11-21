using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Game.Wiring;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;
using System.Globalization;
using SpaceEngineers.Game.ModAPI;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Features.Selector.Toolbar
{
    /// <summary>What every slot of a toolbar a person presses holds on the blocks picked, for the slots offered under it.</summary>
    public sealed class SlotOccupancy
    {
        readonly SprocketEditor _editor;
        readonly List<SlotHeld> _held = new List<SlotHeld>();
        string _heldFor;

        /// <summary>The slots read in <paramref name="editor" />.</summary>
        public SlotOccupancy(SprocketEditor editor)
        {
            _editor = editor;
        }

        /// <summary>What each slot of the toolbar last read holds, page by page.</summary>
        public List<SlotHeld> Held => _held;

        /// <summary>The first slot of <paramref name="pressed" /> that holds nothing on any of <paramref name="blocks" />, or its first slot where every one holds something.</summary>
        public ToolbarSlot Free(List<long> blocks, EntityEvent pressed)
        {
            Occupy(blocks, pressed);
            var toolbar = ToolbarSlot.Of(pressed.Id).Toolbar;
            for (var i = 0; i < _held.Count; i++)
            {
                if (!_held[i].Holds)
                    return new ToolbarSlot(toolbar, i / pressed.Slots + 1, i % pressed.Slots + 1);
            }

            return new ToolbarSlot(toolbar, 1, 1);
        }

        /// <summary>Reads what the slots of <paramref name="pressed" /> hold on <paramref name="blocks" />, where the toolbar or the blocks have changed since they were read.</summary>
        public void Occupy(List<long> blocks, EntityEvent pressed)
        {
            var reading = pressed.Id + Joined(blocks);
            if (reading == _heldFor)
                return;

            _heldFor = reading;
            Read(blocks, pressed, _held);
        }

        /// <summary>
        /// The toolbar a person presses among what <paramref name="targets" /> share that <paramref name="eventId" /> names,
        /// the toolbar itself or one of its slots; null where it names none.
        /// </summary>
        public static EntityEvent Pressing(GridWorld world, IList<long> targets, MemberKey eventId)
        {
            if (eventId.IsEmpty || ToolbarSlot.Of(eventId.Id) == null)
                return null;

            foreach (var entityEvent in Vocabulary.EventsOf(world, targets))
            {
                if (entityEvent.IsPressed && entityEvent.Names(eventId))
                    return entityEvent;
            }

            return null;
        }

        static string Joined(List<long> blocks)
        {
            var joined = "";
            foreach (var block in blocks)
                joined += $" {block.ToString(CultureInfo.InvariantCulture)}";
            return joined;
        }

        /// <summary>
        /// Reads into <paramref name="into" /> what each slot of <paramref name="pressed" /> holds on <paramref name="blocks" />,
        /// page by page: a slot holds something where it does on any of them, says what on each, and answers a trigger where
        /// it does on any of them.
        /// </summary>
        /// <remarks>Read as the toolbar is chosen or the blocks change, never on a tick.</remarks>
        void Read(IList<long> blocks, EntityEvent pressed, List<SlotHeld> into)
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
                GridEntity block = _editor.World.FindEntity(id);
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
                        says = prefix + _editor.Claims.Answering(block, at);
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
