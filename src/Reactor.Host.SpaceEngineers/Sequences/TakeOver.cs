using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Points a trigger at a button another trigger holds, and takes the button off that one.
    /// </summary>
    /// <remarks>
    /// A toolbar slot holds one thing, so two triggers naming one button is a state the construct
    /// cannot be put into - and Reactor, asked to arrange it, ends up arranging neither: the slot is
    /// left empty and both triggers stand there naming a button that answers nothing.
    /// <para>
    /// So the person's answer decides it. Taking a button over means the trigger that had it is left
    /// without one - the same state the host leaves behind when somebody clears that button in the
    /// game, which says so on its row and is mended by pressing it.
    /// </para>
    /// <para>
    /// One edit rather than two, because it is one answer to one question. What it carries out is
    /// whatever was being done - pointing an existing trigger, usually - and the blocks it took away
    /// come back with it.
    /// </para>
    /// </remarks>
    sealed class TakeOver : SystemEdit
    {
        readonly SystemEdit _made;
        readonly MomentTrigger _from;
        readonly List<string> _had;

        public TakeOver(SystemEdit made, MomentTrigger from)
        {
            _made = made;
            _from = from;
            _had = CopyOf(from.Sources);
        }

        public override string Label => "taking a button over";

        public override bool Changes => _made.Changes;

        public override void Apply(AutomationSystem system)
        {
            _made.Apply(system);
            _from.Sources.Clear();
        }

        public override void Undo(AutomationSystem system)
        {
            Refill(_from.Sources, _had);
            _made.Undo(system);
        }

        /// <summary>Only while what it did is still what is there, and what it took is still gone.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return _from.Sources.Count == 0 && _made.Undoable(system);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return Same(_from.Sources, _had) && _made.Redoable(system);
        }
    }
}
