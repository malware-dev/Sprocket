namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A logic system just made, in the program already; undone, it goes, and redone, it comes back where it stood.</summary>
    sealed class MakeSystem : ProgramEdit
    {
        readonly AutomationSystem _system;

        int _at = -1;

        /// <summary>The making of <paramref name="system" />, which is in the program already.</summary>
        public MakeSystem(AutomationSystem system)
        {
            _system = system;
        }

        /// <inheritdoc />
        public override Change Change => Change.AddSystem;

        /// <remarks>Puts nothing back the first time: it was made in the program, and this only records that.</remarks>
        public override void Apply(Automation program)
        {
            program.PutBack(_system, _at);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _at = IndexOf(program, _system);
            program.RemoveSystem(_system);
        }

        /// <summary>Only while it is here; one made and left untouched has gone already.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _system) >= 0;
        }

        /// <summary>Only while it is gone, nothing has come back with its identity, and none of its buttons has been taken since.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _system) < 0 && program.FindSystem(_system.Identity) == null && !Clashes(program, _system);
        }
    }
}
