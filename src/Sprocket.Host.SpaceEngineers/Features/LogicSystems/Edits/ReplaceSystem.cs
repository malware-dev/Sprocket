using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits
{
    /// <summary>Puts one logic system in the place of another of the same identity; undone, the first stands there again, switched off.</summary>
    public sealed class ReplaceSystem : ProgramEdit
    {
        readonly AutomationSystem _was;
        readonly AutomationSystem _now;
        readonly ChangeKind _change;

        int _at = -1;
        bool _again;

        /// <summary>Puts <paramref name="now" /> where <paramref name="was" /> stands, offered back as <paramref name="change" />.</summary>
        public ReplaceSystem(AutomationSystem was, AutomationSystem now, ChangeKind change)
        {
            _was = was;
            _now = now;
            _change = change;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(Automation program, bool done, ChangeName name)
        {
            name.Thing = done ? _now : _was;
        }

        /// <summary>The identity the two share.</summary>
        public System.Guid Identity => _now.Identity;

        /// <inheritdoc />
        /// <remarks>The first time, what is put in keeps the switch it was given; made again after being taken back, it comes back switched off.</remarks>
        public override void Apply(Automation program)
        {
            if (_again)
                _now.Enabled = false;

            _at = IndexOf(program, _was);
            program.RemoveSystem(_was);
            program.PutBack(_now, _at);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _again = true;
            _was.Enabled = false;
            program.RemoveSystem(_now);
            program.PutBack(_was, _at);
        }

        /// <summary>Waits while the one standing there runs.</summary>
        public override bool Held(Automation program, bool back)
        {
            return back ? _now.Enabled : _was.Enabled;
        }

        /// <summary>Only while the one put in stands there.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _now) >= 0;
        }

        /// <summary>Only while the one taken out is back.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _was) >= 0;
        }
    }
}
