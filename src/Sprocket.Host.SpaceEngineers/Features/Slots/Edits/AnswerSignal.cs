using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Slots.Edits
{
    /// <summary>
    /// Answers a signal part in one blockset with the buttons picked, taking the part where the system has none by that
    /// name, and does what was picked with it.
    /// </summary>
    public sealed class AnswerSignal : SystemEdit
    {
        readonly SystemEdit _made;
        readonly SystemInstance _instance;
        readonly string _name;
        readonly List<string> _had;
        readonly List<ToolbarSlot> _hadAt;
        readonly List<string> _now;
        readonly List<ToolbarSlot> _nowAt;
        readonly bool _fresh;

        Part _part;
        int _partAt = -1;

        /// <summary>
        /// Answers part <paramref name="name" /> of <paramref name="instance" /> with <paramref name="now" /> at
        /// <paramref name="nowAt" />, a new signal part where there is none, then does <paramref name="made" /> where there is
        /// one.
        /// </summary>
        public AnswerSignal(SystemEdit made, AutomationSystem system, SystemInstance instance, string name,
            IList<string> now, IList<ToolbarSlot> nowAt)
        {
            _made = made;
            _instance = instance;
            _name = name;
            _part = system.FindPart(name);
            _fresh = _part == null;
            _had = CopyOf(instance.FilledWith(name) ?? new List<string>());
            _hadAt = new List<ToolbarSlot>(instance.Positions(name) ?? new List<ToolbarSlot>());
            _now = CopyOf(now);
            _nowAt = new List<ToolbarSlot>(nowAt);
        }

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.PickButton;

        /// <inheritdoc />
        public override void Name(AutomationSystem system, bool done, ChangeName name)
        {
            name.Thing = _name;
            name.Place = _instance;
            name.Now = _now;
            name.Count = _now.Count;
        }

        /// <inheritdoc />
        public override void Touches(AutomationSystem system, IList<object> screens)
        {
            Touch(screens, _instance);
            if (_fresh)
            {
                Touch(screens, system);
                Touch(screens, _part);
            }

            if (_made != null)
                _made.Touches(system, screens);
        }

        /// <inheritdoc />
        public override bool Changes => true;

        /// <inheritdoc />
        public override void Apply(AutomationSystem system)
        {
            if (_part == null)
            {
                _part = system.PartFor(_name);
                _part.IsSignal = true;
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
