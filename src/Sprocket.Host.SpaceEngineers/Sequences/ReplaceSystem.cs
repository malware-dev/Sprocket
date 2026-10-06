namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Puts one logic system in the place of another of the same identity; undone, the first stands there again.</summary>
    public sealed class ReplaceSystem : ProgramEdit
    {
        readonly AutomationSystem _was;
        readonly AutomationSystem _now;
        readonly Change _change;

        int _at = -1;

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

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            _at = IndexOf(program, _was);
            program.RemoveSystem(_was);
            program.PutBack(_now, _at);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            program.RemoveSystem(_now);
            program.PutBack(_was, _at);
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
