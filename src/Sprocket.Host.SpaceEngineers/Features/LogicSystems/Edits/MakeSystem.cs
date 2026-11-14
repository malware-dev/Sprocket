using Sprocket.Host.SpaceEngineers.Framework.Editing;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems.Edits
{
    /// <summary>
    /// A logic system just made, or put on from the library, in the program already; undone, it goes, and redone, it comes back
    /// where it stood, switched off.
    /// </summary>
    public sealed class MakeSystem : ProgramEdit
    {
        readonly AutomationSystem _system;
        readonly ChangeKind _change;

        int _at = -1;

        /// <summary>The making of <paramref name="system" />, which is in the program already.</summary>
        public MakeSystem(AutomationSystem system) : this(system, ChangeKind.AddSystem)
        {
        }

        /// <summary>The making of <paramref name="system" />, which is in the program already, offered back as <paramref name="change" />.</summary>
        public MakeSystem(AutomationSystem system, ChangeKind change)
        {
            _system = system;
            _change = change;
        }

        /// <summary>The logic system made.</summary>
        public AutomationSystem System => _system;

        /// <inheritdoc />
        public override ChangeKind Change => _change;

        /// <inheritdoc />
        public override void Name(Automation program, bool done, ChangeName name)
        {
            name.Thing = _system;
        }

        /// <summary>Taking it away waits while it runs; putting it back, switched off, waits for nothing.</summary>
        public override bool Held(Automation program, bool back)
        {
            return back && _system.Enabled;
        }

        /// <inheritdoc />
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
