namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Takes a logic system out of the program, with its sequences and installations; undone, it stands where it stood.</summary>
    public sealed class TakeSystemOut : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly Change _change;

        int _at = -1;

        /// <summary>Takes <paramref name="system" /> out, offered back as <paramref name="change" />: deleting it, or taking it off the construct.</summary>
        public TakeSystemOut(AutomationSystem system, Change change)
        {
            _system = system;
            _change = change;
        }

        /// <inheritdoc />
        public override Change Change => _change;

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            _at = IndexOf(program, _system);
            program.RemoveSystem(_system);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            program.PutBack(_system, _at);
        }

        /// <summary>Only while it is still gone, nothing has come back with its identity, and none of its buttons has been taken since.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _system) < 0 && program.FindSystem(_system.Identity) == null && !Clashes(program, _system);
        }

        /// <summary>Only while it is back.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _system) >= 0;
        }
    }
}
