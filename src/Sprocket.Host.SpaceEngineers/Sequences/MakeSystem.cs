namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A logic system just made, or put on from the library, in the program already; undone, it goes, and redone, it comes back
    /// where it stood, switched off.
    /// </summary>
    public sealed class MakeSystem : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly Change _change;

        int _at = -1;

        /// <summary>The making of <paramref name="system" />, which is in the program already.</summary>
        public MakeSystem(AutomationSystem system) : this(system, Change.AddSystem)
        {
        }

        /// <summary>The making of <paramref name="system" />, which is in the program already, offered back as <paramref name="change" />.</summary>
        public MakeSystem(AutomationSystem system, Change change)
        {
            _system = system;
            _change = change;
        }

        /// <summary>The logic system made.</summary>
        public AutomationSystem System => _system;

        /// <inheritdoc />
        public override Change Change => _change;

        /// <summary>Taking it away waits while it runs; putting it back, switched off, waits for nothing.</summary>
        public override bool Held(Automation program, bool back)
        {
            return back && _system.Enabled;
        }

        /// <remarks>Puts nothing back the first time: it was made in the program, and this only records that.</remarks>
        public override void Apply(Automation program)
        {
            if (IndexOf(program, _system) >= 0)
                return;

            _system.Enabled = false;
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
