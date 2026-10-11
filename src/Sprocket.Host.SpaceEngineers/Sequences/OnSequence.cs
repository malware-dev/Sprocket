namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A change to one of a logic system's sequences, carried on the logic system's own history.</summary>
    /// <remarks>
    /// Everything done to a logic system is one timeline, whichever of its screens it was done on, because a sequence's steps
    /// name the logic system's slots and a pick reshapes them: two timelines touching the same things cannot be walked in a safe
    /// order.
    /// </remarks>
    public sealed class OnSequence : SystemEdit
    {
        readonly Edit<Sequence> _edit;

        /// <summary><paramref name="edit" />, made to <paramref name="sequence" />.</summary>
        public OnSequence(Sequence sequence, Edit<Sequence> edit)
        {
            Sequence = sequence;
            _edit = edit;
        }

        /// <summary>The sequence the change was made to.</summary>
        public Sequence Sequence { get; }

        /// <inheritdoc />
        public override Change Change => _edit.Change;

        /// <inheritdoc />
        public override bool Changes => _edit.Changes;

        /// <inheritdoc />
        public override bool ChangesShape => _edit.ChangesShape;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _edit.Apply(Sequence);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _edit.Undo(Sequence);
        }

        /// <summary>Whether the sequence is still the logic system's, and the change is still what is there.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return Holds(system) && _edit.Undoable(Sequence);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system) && _edit.Redoable(Sequence);
        }

        /// <summary>What the change itself waits for.</summary>
        public override bool Held(AutomationSystem system, bool back)
        {
            return _edit.Held(Sequence, back);
        }

        /// <summary>Whether <paramref name="system" /> holds the sequence.</summary>
        bool Holds(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i] == Sequence)
                    return true;
            }

            return false;
        }
    }
}
