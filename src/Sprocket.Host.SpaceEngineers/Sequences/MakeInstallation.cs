namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>An installation just made, in its logic system already; undone, it goes, and redone, it comes back where it stood.</summary>
    sealed class MakeInstallation : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _instance;

        int _at = -1;

        /// <summary>The making of <paramref name="instance" />, which is in <paramref name="system" /> already.</summary>
        public MakeInstallation(AutomationSystem system, SystemInstance instance)
        {
            _system = system;
            _instance = instance;
        }

        /// <inheritdoc />
        public override Change Change => Change.AddInstallation;

        /// <remarks>Puts nothing back the first time: it was made in its logic system, and this only records that.</remarks>
        public override void Apply(Automation program)
        {
            _system.PutBack(_instance, _at);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _at = IndexOf(_system, _instance);
            _system.RemoveInstance(_instance);
        }

        /// <summary>Only while it is here and is not the only one: a logic system keeps at least one installation.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _system) >= 0 && IndexOf(_system, _instance) >= 0 && _system.Instances.Count > 1;
        }

        /// <summary>Only while its logic system is here, it is gone, no installation has its number, and none of its buttons has been taken since.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _system) >= 0 && IndexOf(_system, _instance) < 0 && !Numbered(_system, _instance)
                   && !Clashes(program, _system, _instance);
        }
    }
}
