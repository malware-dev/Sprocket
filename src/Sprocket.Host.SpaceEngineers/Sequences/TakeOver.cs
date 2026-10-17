using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Does what was picked, and takes the button it was given out of the signal slot of the blockset that had it.
    /// </summary>
    public sealed class TakeOver : SystemEdit
    {
        readonly SystemEdit _made;
        readonly SystemInstance _from;
        readonly string _part;
        readonly List<string> _had;
        readonly List<ToolbarSlot> _hadAt;
        readonly List<string> _kept;
        readonly List<ToolbarSlot> _keptAt;

        /// <summary>Does <paramref name="made" />, where there is anything, leaving signal part <paramref name="part" /> of <paramref name="from" /> answered by <paramref name="kept" /> at <paramref name="keptAt" /> alone.</summary>
        public TakeOver(SystemEdit made, SystemInstance from, string part, IList<string> kept, IList<ToolbarSlot> keptAt)
        {
            _made = made;
            _from = from;
            _part = part;
            _had = CopyOf(from.FilledWith(part) ?? new List<string>());
            _hadAt = new List<ToolbarSlot>(from.Positions(part) ?? new List<ToolbarSlot>());
            _kept = CopyOf(kept);
            _keptAt = new List<ToolbarSlot>(keptAt);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.TakeOver;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _from);
            if (_made != null)
                _made.Touches(system, screens);
        }

        /// <inheritdoc />
        public override bool Changes => true;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_made != null)
                _made.Apply(system);
            Put(_kept, _keptAt);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Put(_had, _hadAt);
            if (_made != null)
                _made.Undo(system);
        }

        /// <summary>Waits for what it did, and for the installation it took the button from while that may run.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return _made != null && _made.Held(system, back) || _from.Enabled;
        }

        /// <summary>Only while what it did is still what is there, and what it took is still gone.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return Holding(_kept) && (_made == null || _made.Undoable(system));
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holding(_had) && (_made == null || _made.Redoable(system));
        }

        bool Holding(IList<string> blocks)
        {
            return Same(_from.FilledWith(_part) ?? new List<string>(), blocks);
        }

        void Put(IList<string> blocks, IList<ToolbarSlot> at)
        {
            if (blocks.Count == 0)
                _from.Clear(_part);
            else
                _from.Fill(_part, blocks, at);
        }
    }
}
