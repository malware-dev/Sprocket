
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A change to a sequence, walked back from the system's screen.
    /// </summary>
    /// <remarks>
    /// For the one sequence that has no screen of its own: the unnamed one holding the single act a
    /// trigger does. Its act is changed from the trigger's row, so the way back has to be the row's
    /// too - a history entry on a sequence nobody can open is one nobody can reach.
    /// <para>
    /// The edit itself is the ordinary one, applied to the ordinary thing. Nothing here knows what it
    /// does; all this carries is which sequence it is about.
    /// </para>
    /// </remarks>
    sealed class Rewrite : SystemEdit
    {
        readonly Sequence _sequence;
        readonly SequenceEdit _edit;

        public Rewrite(Sequence sequence, SequenceEdit edit)
        {
            _sequence = sequence;
            _edit = edit;
        }

        public override Change Change => _edit.Change;

        public override bool Changes => _edit.Changes;

        public override bool ChangesShape => _edit.ChangesShape;

        public override void Apply(AutomationSystem system)
        {
            _edit.Apply(_sequence);
        }

        public override void Undo(AutomationSystem system)
        {
            _edit.Undo(_sequence);
        }
    }
}
