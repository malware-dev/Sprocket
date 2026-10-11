namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A sequence made in a logic system, new or as a copy of another, as a step its undo takes back.</summary>
    /// <remarks>Made by the editor before this is recorded, so the first time it is applied it finds the sequence there already.</remarks>
    public sealed class MakeSequence : SystemEdit
    {
        readonly Automation _program;
        readonly Change _change;

        int _at = -1;

        /// <summary>Records <paramref name="made" />, just made in its logic system of <paramref name="program" />, as <paramref name="change" />.</summary>
        public MakeSequence(Automation program, Sequence made, Change change)
        {
            _program = program;
            Sequence = made;
            _change = change;
        }

        /// <summary>The sequence made.</summary>
        public Sequence Sequence { get; }

        /// <inheritdoc />
        public override Change Change => _change;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return IndexIn(system) >= 0;
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return IndexIn(system) < 0;
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (IndexIn(system) < 0)
                _program.PutBack(system, Sequence, _at);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _at = IndexIn(system);
            _program.RemoveSequence(Sequence);
        }

        /// <summary>Where the sequence stands among <paramref name="system" />'s, or -1.</summary>
        int IndexIn(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i] == Sequence)
                    return i;
            }

            return -1;
        }
    }
}
