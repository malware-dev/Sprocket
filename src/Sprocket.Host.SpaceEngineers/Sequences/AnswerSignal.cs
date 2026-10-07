using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Answers a signal part in one blockset with the buttons picked, taking the part where the system has none by that
    /// name, and does what was picked with it.
    /// </summary>
    /// <remarks>
    /// One edit, because picking a button is one answer: undoing it takes back the trigger it made and the part with it,
    /// or puts back the buttons a part picked again had.
    /// </remarks>
    public sealed class AnswerSignal : SystemEdit
    {
        readonly SystemEdit _made;
        readonly SystemInstance _instance;
        readonly string _name;
        readonly string _type;
        readonly List<string> _had;
        readonly List<ToolbarSlot> _hadAt;
        readonly List<string> _now;
        readonly List<ToolbarSlot> _nowAt;
        readonly bool _fresh;

        Part _part;
        int _partAt = -1;

        /// <summary>
        /// Answers part <paramref name="name" /> of <paramref name="instance" /> with <paramref name="now" /> at
        /// <paramref name="nowAt" />, a new part expecting <paramref name="type" /> where there is none, then does
        /// <paramref name="made" /> where there is one.
        /// </summary>
        public AnswerSignal(SystemEdit made, AutomationSystem system, SystemInstance instance, string name, string type,
            IList<string> now, IList<ToolbarSlot> nowAt)
        {
            _made = made;
            _instance = instance;
            _name = name;
            _type = type ?? "";
            _part = system.FindPart(name);
            _fresh = _part == null;
            _had = CopyOf(instance.FilledWith(name) ?? new List<string>());
            _hadAt = new List<ToolbarSlot>(instance.Positions(name) ?? new List<ToolbarSlot>());
            _now = CopyOf(now);
            _nowAt = new List<ToolbarSlot>(nowAt);
        }

        /// <inheritdoc />
        public override Change Change => Change.PickButton;

        /// <inheritdoc />
        public override bool Changes => true;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_part == null)
            {
                _part = system.PartFor(_name);
                _part.IsSignal = true;
                if (_type.Length > 0)
                    _part.Expects.Add(_type);
            }
            else if (_partAt >= 0)
            {
                system.PutPartBack(_partAt, _part);
                _partAt = -1;
            }

            _instance.Fill(_name, _now, _nowAt);

            if (_made != null)
                _made.Apply(system);
        }

        /// <inheritdoc />
        public override void Undo(AutomationSystem system)
        {
            if (_made != null)
                _made.Undo(system);

            if (!_fresh)
            {
                if (_had.Count > 0)
                    _instance.Fill(_name, _had, _hadAt);
                else
                    _instance.Clear(_name);

                return;
            }

            _instance.Clear(_name);
            _partAt = system.ForgetPart(_part);
        }

        /// <summary>Only while the part is still answered as it was left, and what was done with it can be taken back.</summary>
        public override bool Undoable(AutomationSystem system)
        {
            return system.FindPart(_name) == _part && Same(_instance.FilledWith(_name) ?? new List<string>(), _now)
                && (_made == null || _made.Undoable(system));
        }

        /// <summary>Only while nothing else has taken the name, and the part is answered as it was before.</summary>
        public override bool Redoable(AutomationSystem system)
        {
            var named = system.FindPart(_name);
            return (_fresh ? named == null : named == _part && Same(_instance.FilledWith(_name) ?? new List<string>(), _had))
                && (_made == null || _made.Redoable(system));
        }
    }
}
