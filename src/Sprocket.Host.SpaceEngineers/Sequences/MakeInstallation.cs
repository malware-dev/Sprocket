namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>An installation just made, in its logic system already; undone, it goes, and redone, it comes back where it stood, switched off.</summary>
    public sealed class MakeInstallation : ProgramEdit
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

        /// <summary>The installation made.</summary>
        public SystemInstance Instance => _instance;

        /// <inheritdoc />
        public override ChangeKind Change => ChangeKind.AddInstallation;

        /// <remarks>Puts nothing back the first time: it was made in its logic system, and this only records that.</remarks>
        public override void Apply(Automation program)
        {
            if (IndexOf(_system, _instance) >= 0)
                return;

            _instance.Enabled = false;
            _system.PutBack(_instance, _at);
        }

        /// <summary>Taking it away waits while it may run; putting it back, switched off, waits for nothing.</summary>
        public override bool Held(Automation program, bool back)
        {
            return back && _system.Enabled && _instance.Enabled;
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
