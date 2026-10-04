namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Takes one installation out of its logic system; undone, it stands where it stood, with the blocks it had.</summary>
    public sealed class TakeInstallationOut : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _instance;

        int _at = -1;

        /// <summary>Takes <paramref name="instance" /> out of <paramref name="system" />.</summary>
        public TakeInstallationOut(AutomationSystem system, SystemInstance instance)
        {
            _system = system;
            _instance = instance;
        }

        /// <inheritdoc />
        public override Change Change => Change.DeleteInstallation;

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            _at = IndexOf(_system, _instance);
            _system.RemoveInstance(_instance);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            _system.PutBack(_instance, _at);
        }

        /// <summary>Only while its logic system is here, it is still gone, no installation has its number, and none of its buttons has been taken since.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _system) >= 0 && IndexOf(_system, _instance) < 0 && !Numbered(_system, _instance)
                   && !Clashes(program, _system, _instance);
        }

        /// <summary>Only while it is back, and is not the only one: a logic system keeps at least one installation.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _system) >= 0 && IndexOf(_system, _instance) >= 0 && _system.Instances.Count > 1;
        }
    }
}
