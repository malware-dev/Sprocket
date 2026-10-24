using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Takes a trigger out of a system, with the signal slot it names and what each instance answered it with, where no
    /// other trigger names that slot.
    /// </summary>
    public sealed class ForgetTrigger : SystemEdit
    {
        readonly Trigger _going;
        readonly Part _part;

        int _at;
        int _partAt;

        readonly List<Answer> _answers = new List<Answer>();

        readonly List<string> _sources;
        readonly string _eventId;

        /// <summary>Takes <paramref name="going" /> out of <paramref name="system" />, with the signal slot it names where nothing else names it.</summary>
        public ForgetTrigger(AutomationSystem system, Trigger going)
        {
            _going = going;

            var needed = going as SignalTrigger;
            _part = needed == null || Shared(system, needed) ? null : system.FindPart(needed.Part);

            var moment = going as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment?.EventId;
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.ForgetTrigger;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _going;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, system);
            Touch(screens, _part);
            foreach (var answer in _answers)
                Touch(screens, answer.Instance);
        }

        /// <inheritdoc />
        public override bool Undoable(AutomationSystem system)
        {
            return Gone(system, _going) && (_part == null || system.FindPart(_part.Id) == null);
        }

        /// <inheritdoc />
        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system, _going, _sources, _eventId);
        }

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            _at = system.IndexOfTrigger(_going);
            system.ReplaceTrigger(_going, null);

            if (_part == null)
                return;

            _answers.Clear();
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var blocks = instances[i].FilledWith(_part.Id);
                if (blocks == null)
                    continue;

                var answer = new Answer();
                answer.Instance = instances[i];
                answer.Blocks = CopyOf(blocks);
                var at = instances[i].Positions(_part.Id);
                answer.At = at == null ? null : new List<ToolbarSlot>(at);
                _answers.Add(answer);

                instances[i].Clear(_part.Id);
            }

            _partAt = system.ForgetPart(_part);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (_part != null)
            {
                system.PutPartBack(_partAt, _part);

                foreach (var answer in _answers)
                {
                    if (answer.At == null)
                        answer.Instance.Fill(_part.Id, answer.Blocks);
                    else
                        answer.Instance.Fill(_part.Id, answer.Blocks, answer.At);
                }
            }

            system.PutTriggerBack(_at, _going);
        }

        /// <summary>Whether another trigger of <paramref name="system" /> names the slot <paramref name="needed" /> names.</summary>
        static bool Shared(AutomationSystem system, SignalTrigger needed)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var other = triggers[i] as SignalTrigger;
                if (other != null && other != needed && other.Part == needed.Part)
                    return true;
            }

            return false;
        }

        /// <summary>What one instance answered the signal part with.</summary>

        sealed class Answer
        {
            public SystemInstance Instance;
            public List<string> Blocks;
            public List<ToolbarSlot> At;
        }
    }
}
