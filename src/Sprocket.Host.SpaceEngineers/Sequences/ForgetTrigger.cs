using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Takes a trigger out of a system, with the trigger part it is and what each instance answered it with.
    /// </summary>
    /// <remarks>
    /// It comes back where it was. A trigger put back on the end would reorder the list somebody is looking at, which
    /// reads as a second change rather than as the first one being undone.
    /// </remarks>
    sealed class ForgetTrigger : SystemEdit
    {
        readonly Trigger _going;
        readonly Part _part;

        int _at;
        int _partAt;

        readonly List<Answer> _answers = new List<Answer>();

        readonly List<string> _sources;
        readonly string _eventId;

        public ForgetTrigger(AutomationSystem system, Trigger going)
        {
            _going = going;

            var needed = going as RequiredTrigger;
            _part = needed == null ? null : system.FindPart(needed.Part);

            var moment = going as MomentTrigger;
            _sources = moment == null ? null : CopyOf(moment.Sources);
            _eventId = moment == null ? null : moment.EventId;
        }

        public override string Label => "deleting a trigger";

        public override bool Undoable(AutomationSystem system)
        {
            return Gone(system, _going) && (_part == null || system.FindPart(_part.Id) == null);
        }

        public override bool Redoable(AutomationSystem system)
        {
            return Holds(system, _going, _sources, _eventId);
        }

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

        public override void Undo(AutomationSystem system)
        {
            if (_part != null)
            {
                system.PutPartBack(_partAt, _part);

                for (var i = 0; i < _answers.Count; i++)
                {
                    var answer = _answers[i];
                    if (answer.At == null)
                        answer.Instance.Fill(_part.Id, answer.Blocks);
                    else
                        answer.Instance.Fill(_part.Id, answer.Blocks, answer.At);
                }
            }

            system.PutTriggerBack(_at, _going);
        }

        /// <summary>What one instance answered the trigger part with.</summary>
        sealed class Answer
        {
            public SystemInstance Instance;
            public List<string> Blocks;
            public List<ToolbarSlot> At;
        }
    }
}
