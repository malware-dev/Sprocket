namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Puts one logic system in the place of another of the same identity; undone, the first stands there again, switched off.</summary>
    public sealed class ReplaceSystem : ProgramEdit
    {
        readonly AutomationSystem _was;
        readonly AutomationSystem _now;
        readonly Change _change;

        int _at = -1;
        bool _again;

        /// <summary>Puts <paramref name="now" /> where <paramref name="was" /> stands, offered back as <paramref name="change" />.</summary>
        public ReplaceSystem(AutomationSystem was, AutomationSystem now, Change change)
        {
            _was = was;
            _now = now;
            _change = change;
        }

        /// <inheritdoc />
        public override Change Change => _change;

        /// <summary>The identity the two share.</summary>
        public System.Guid Identity => _now.Identity;

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
