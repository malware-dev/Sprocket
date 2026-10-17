using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Takes a sequence out of its logic system, with every trigger that runs it.</summary>
    public sealed class TakeSequenceOut : SystemEdit
    {
        readonly Automation _program;
        readonly Sequence _going;
        readonly List<Trigger> _triggers = new List<Trigger>();
        readonly List<int> _triggersAt = new List<int>();

        int _at;

        /// <summary>Takes <paramref name="going" /> out of <paramref name="program" />, with the triggers that run it.</summary>
        public TakeSequenceOut(Automation program, Sequence going)
        {
            _program = program;
            _going = going;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.DeleteSequence;

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _going);
        }

        /// <inheritdoc />
        public override bool ChangesShape => true;

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return !Holds(system);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _at = -1;
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count && _at < 0; i++)
            {
                if (sequences[i] == _going)
                    _at = i;
            }

            _triggers.Clear();
            _triggersAt.Clear();
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (triggers[i].Runs != _going.Number)
                    continue;

                _triggers.Add(triggers[i]);
                _triggersAt.Add(i);
            }

            _program.RemoveSequence(_going);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            _program.PutBack(system, _going, _at);

            // In the order they stood, so each lands in the place it had among those already back.
            for (var i = 0; i < _triggers.Count; i++)
                system.PutTriggerBack(_triggersAt[i], _triggers[i]);
        }

        /// <summary>Whether <paramref name="system" /> holds the sequence.</summary>
        bool Holds(AutomationSystem system)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (sequences[i] == _going)
                    return true;
            }

            return false;
        }
    }
}
