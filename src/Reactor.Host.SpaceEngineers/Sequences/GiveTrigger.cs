namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Gives a sequence that nothing starts something that does.
    /// </summary>
    /// <remarks>
    /// Separate from <see cref="ChangeTrigger" />, which swaps one moment for another and needs the
    /// old one to put back. Here there was nothing, so undoing means taking the trigger away again
    /// rather than restoring anything.
    /// <para>
    /// Any sort of trigger, not only a moment: a sequence with none may be told to repeat as easily
    /// as to listen, and which of those it is was asked before this was made.
    /// </para>
    /// </remarks>
    sealed class GiveTrigger : SequenceEdit
    {
        readonly Trigger _made;

        public GiveTrigger(Trigger made)
        {
            _made = made;
        }

        public override string Label => "choosing what starts it";

        public override void Apply(Sequence sequence)
        {
            // Which sequence it runs is not known until it is applied, and it is the number rather
            // than the handle, because that is what a trigger survives a save by.
            _made.Runs = sequence.Number;

            if (sequence.System != null)
                sequence.System.AddTrigger(_made);
        }

        public override void Undo(Sequence sequence)
        {
            if (sequence.System != null)
                sequence.System.ReplaceTrigger(_made, null);
        }
    }
}
