using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Fills one slot of one installation with blocks, and what that settles in the installations' other slots.</summary>
    /// <remarks>
    /// What filling a slot settles, such as blocks set aside that are no longer needed, is worked out by whoever fills it and
    /// handed in, so it runs again the same way when the step is made again. Every installation's slots are noted before it
    /// runs, so taking it back puts them all back as they were.
    /// </remarks>
    public sealed class FillSlot : SystemEdit, IInstallationEdit
    {
        readonly string _slot;
        readonly List<string> _blocks;
        readonly List<ToolbarSlot> _at;
        readonly Action<AutomationSystem> _settle;
        readonly List<Kept> _was = new List<Kept>();

        /// <summary>
        /// Fills <paramref name="slot" /> of <paramref name="instance" /> with <paramref name="blocks" />, each answered at
        /// <paramref name="at" /> where it is a signal slot, then runs <paramref name="settle" />.
        /// </summary>
        public FillSlot(SystemInstance instance, string slot, IList<string> blocks, IList<ToolbarSlot> at, Action<AutomationSystem> settle)
        {
            Instance = instance;
            _slot = slot;
            _blocks = CopyOf(blocks);
            _at = at == null ? null : new List<ToolbarSlot>(at);
            _settle = settle;
        }

        /// <inheritdoc />
        public SystemInstance Instance { get; }

        /// <inheritdoc />
        public override Change Change => Change.FillSlot;

        /// <inheritdoc />
        public override bool ChangesShape => true;

        /// <summary>Waits only while the installation may run: switched on, in a logic system switched on.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return system.Enabled && Instance.Enabled;
        }

        /// <summary>While the installation is still the logic system's and its slot holds what this filled it with.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            var filled = Instance.FilledWith(_slot);
            return Has(system, Instance) && filled != null && Same(filled, _blocks);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Has(system, Instance);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _was.Clear();
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                foreach (var slot in instances[i].Filled)
                {
                    var at = instances[i].Positions(slot);
                    _was.Add(new Kept(instances[i], slot, CopyOf(instances[i].FilledWith(slot)), at == null ? null : new List<ToolbarSlot>(at)));
                }
            }

            if (_at == null)
                Instance.Fill(_slot, _blocks);
            else
                Instance.Fill(_slot, _blocks, _at);

            if (_settle != null)
                _settle(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            var instances = system.Instances;
            var slots = new List<string>();
            for (var i = 0; i < instances.Count; i++)
            {
                slots.Clear();
                foreach (var slot in instances[i].Filled)
                    slots.Add(slot);

                for (var s = 0; s < slots.Count; s++)
                {
                    if (!Had(instances[i], slots[s]))
                        instances[i].Clear(slots[s]);
                }
            }

            for (var i = 0; i < _was.Count; i++)
            {
                var kept = _was[i];
                if (kept.At == null)
                    kept.Instance.Fill(kept.Slot, kept.Blocks);
                else
                    kept.Instance.Fill(kept.Slot, kept.Blocks, kept.At);
            }
        }

        /// <summary>Whether <paramref name="instance" /> filled <paramref name="slot" /> before this was made.</summary>
        bool Had(SystemInstance instance, string slot)
        {
            for (var i = 0; i < _was.Count; i++)
            {
                if (_was[i].Instance == instance && _was[i].Slot == slot)
                    return true;
            }

            return false;
        }

        /// <summary>What one installation filled one slot with.</summary>
        sealed class Kept
        {
            public readonly SystemInstance Instance;
            public readonly string Slot;
            public readonly List<string> Blocks;
            public readonly List<ToolbarSlot> At;

            public Kept(SystemInstance instance, string slot, List<string> blocks, List<ToolbarSlot> at)
            {
                Instance = instance;
                Slot = slot;
                Blocks = blocks;
                At = at;
            }
        }
    }
}
