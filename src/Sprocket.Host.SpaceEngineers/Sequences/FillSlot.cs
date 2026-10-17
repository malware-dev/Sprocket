using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Fills one slot of one installation with blocks, and what that settles in the installations' other slots.</summary>
    /// <remarks>What filling the slot settles is worked out by the caller and handed in, so a redo settles it the same way.</remarks>
    public sealed class FillSlot : SystemEdit, IInstallationEdit
    {
        readonly string _slot;
        readonly List<string> _blocks;
        readonly List<ToolbarSlot> _at;
        readonly List<int> _facets;
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

        /// <summary>
        /// Fills <paramref name="slot" /> of <paramref name="instance" />, a slot of a type the host provides, with
        /// <paramref name="blocks" />, each holding the facet <paramref name="facets" /> numbers, from 1, then runs <paramref name="settle" />.
        /// </summary>
        public FillSlot(SystemInstance instance, string slot, IList<string> blocks, IList<int> facets, Action<AutomationSystem> settle)
        {
            Instance = instance;
            _slot = slot;
            _blocks = CopyOf(blocks);
            _facets = new List<int>(facets);
            _settle = settle;
        }

        /// <inheritdoc />
        public SystemInstance Instance { get; }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.FillSlot;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, Instance);
            foreach (var kept in _was)
                Touch(screens, kept.Instance);
        }

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
                    var facets = instances[i].Facets(slot);
                    _was.Add(new Kept(instances[i], slot, CopyOf(instances[i].FilledWith(slot)), at == null ? null : new List<ToolbarSlot>(at),
                        facets == null ? null : new List<int>(facets)));
                }
            }

            if (_at != null)
                Instance.Fill(_slot, _blocks, _at);
            else if (_facets != null)
                Instance.Fill(_slot, _blocks, _facets);
            else
                Instance.Fill(_slot, _blocks);

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

                foreach (var slotName in slots)
                {
                    if (!Had(instances[i], slotName))
                        instances[i].Clear(slotName);
                }
            }

            foreach (var kept in _was)
            {
                if (kept.At != null)
                    kept.Instance.Fill(kept.Slot, kept.Blocks, kept.At);
                else if (kept.Facets != null)
                    kept.Instance.Fill(kept.Slot, kept.Blocks, kept.Facets);
                else
                    kept.Instance.Fill(kept.Slot, kept.Blocks);
            }
        }

        /// <summary>Whether <paramref name="instance" /> filled <paramref name="slot" /> before this was made.</summary>
        bool Had(SystemInstance instance, string slot)
        {
            foreach (var kept in _was)
            {
                if (kept.Instance == instance && kept.Slot == slot)
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
            public readonly List<int> Facets;

            public Kept(SystemInstance instance, string slot, List<string> blocks, List<ToolbarSlot> at, List<int> facets)
            {
                Instance = instance;
                Slot = slot;
                Blocks = blocks;
                At = at;
                Facets = facets;
            }
        }
    }
}
