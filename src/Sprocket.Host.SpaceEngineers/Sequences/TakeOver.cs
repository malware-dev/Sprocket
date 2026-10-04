using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Does what was picked, and takes the button it was given out of the trigger slot of the blockset that had it.
    /// </summary>
    /// <remarks>
    /// A toolbar slot holds one thing, so two triggers naming one button is a state the construct cannot be put into. The
    /// person's answer decides it: the blockset that had the button is left needing another, the same state somebody
    /// clearing that button in the game leaves behind, said on its row and mended by picking one.
    /// <para>
    /// One edit rather than two, because it is one answer to one question; undoing it puts the other blockset's slot back.
    /// </para>
    /// </remarks>
    sealed class TakeOver : SystemEdit
    {
        readonly SystemEdit _made;
        readonly SystemInstance _from;
        readonly string _part;
        readonly List<string> _had;
        readonly List<ToolbarSlot> _hadAt;
        readonly List<string> _kept;
        readonly List<ToolbarSlot> _keptAt;

        /// <summary>Does <paramref name="made" />, leaving trigger part <paramref name="part" /> of <paramref name="from" /> answered by <paramref name="kept" /> at <paramref name="keptAt" /> alone.</summary>
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
        public override Change Change => Change.TakeOver;

        /// <inheritdoc />
        public override bool Changes => true;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _made.Apply(system);
            Put(_kept, _keptAt);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            Put(_had, _hadAt);
            _made.Undo(system);
        }

        /// <summary>Only while what it did is still what is there, and what it took is still gone.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return Holding(_kept) && _made.Undoable(system);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holding(_had) && _made.Redoable(system);
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
