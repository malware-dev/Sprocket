using Sprocket.Host.SpaceEngineers.Memory;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>Keeps the chosen one of two versions of a logic system, in their place; undone, both wait again.</summary>
    public sealed class ChooseVersion : ProgramEdit
    {
        readonly AutomationSystem _here;
        readonly AutomationSystem _waiting;
        readonly AutomationSystem _chosen;
        readonly ConflictMemory _conflict;

        int _at = -1;
        int _waitingAt = -1;

        /// <summary>Puts <paramref name="chosen" /> where <paramref name="here" /> and <paramref name="waiting" /> stand.</summary>
        public ChooseVersion(AutomationSystem here, AutomationSystem waiting, AutomationSystem chosen, ConflictMemory conflict)
        {
            _here = here;
            _waiting = waiting;
            _chosen = chosen;
            _conflict = conflict;
        }

        /// <inheritdoc />
        public override Change Change => Change.ChooseVersion;

        /// <inheritdoc />
        public override void Apply(Automation program)
        {
            // Taken in this order, so undo puts each back where it stood.
            _waitingAt = IndexOf(program, _waiting);
            program.RemoveSystem(_waiting);
            _at = IndexOf(program, _here);
            program.RemoveSystem(_here);
            _chosen.Enabled = false;
            program.PutBack(_chosen, _at);
            program.Memory.Conflicts.Remove(_conflict);
        }

        /// <inheritdoc />
        public override void Undo(Automation program)
        {
            program.RemoveSystem(_chosen);
            _here.Enabled = false;
            _waiting.Enabled = false;
            program.PutBack(_here, _at);
            program.PutBack(_waiting, _waitingAt);
            program.Memory.Conflicts.Add(_conflict);
        }

        /// <summary>Waits while the one standing there runs.</summary>
        public override bool Held(Automation program, bool back)
        {
            return back ? _chosen.Enabled : _here.Enabled || _waiting.Enabled;
        }

        /// <summary>Only while the one chosen stands there.</summary>
        public override bool Undoable(Automation program)
        {
            return IndexOf(program, _chosen) >= 0;
        }

        /// <summary>Only while both versions are back.</summary>
        public override bool Redoable(Automation program)
        {
            return IndexOf(program, _here) >= 0 && IndexOf(program, _waiting) >= 0;
        }
    }
}
