namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Takes a logic system out of the program, with its sequences and installations; undone, it stands where it stood, switched off.</summary>
    public sealed class TakeSystemOut : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly ChangeKind _change;

        int _at = -1;

        /// <summary>Takes <paramref name="system" /> out, offered back as <paramref name="change" />: deleting it, or taking it off the construct.</summary>
        public TakeSystemOut(AutomationSystem system, ChangeKind change)
        {
            _system = system;
            _change = change;
        }

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(Automation program, bool done, ChangeName name)
        {
            name.Thing = _system;
        }

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            _at = IndexOf(program, _system);
            program.RemoveSystem(_system);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _system.Enabled = false;
            program.PutBack(_system, _at);
        }

        /// <summary>Taking it out again waits while it runs; putting it back, switched off, waits for nothing.</summary>
        public override bool Held(Automation program, bool back)
        {
            return !back && _system.Enabled;
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
