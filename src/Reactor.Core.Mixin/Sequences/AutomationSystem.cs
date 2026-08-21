using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// One system's worth of automation: an airlock, a hangar, a drill rig.
    /// </summary>
    /// <remarks>
    /// The player's word for this is "system", and that is what the editor says. The type
    /// cannot be called that - a type named <c>System</c> shadows the namespace every file
    /// in the project depends on - so the name is longer here and nowhere else.
    ///
    /// A system is what a system <em>is</em>; a <see cref="SystemInstance"/> is one somebody
    /// actually has. Every system has at least one, made with it, because a system nobody has
    /// fitted anywhere is not a thing this editor can show.
    ///
    /// Nothing is declared before it is used. A system exists because somebody built
    /// something, never because they made an empty one first.
    /// </remarks>
    public sealed class AutomationSystem
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<SystemInstance> _instances = new List<SystemInstance>();
        readonly List<SystemRole> _roles = new List<SystemRole>();

        public AutomationSystem(int id)
        {
            Id = id;
            Name = "";
            Description = "";
            Enabled = true;
        }

        public int Id { get; private set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// Empty is the ordinary state rather than an oversight. Naming happens when there
        /// is something worth naming, and until then <see cref="HasName"/> is false and the
        /// editor shows whatever it can work out instead.
        /// </remarks>
        public string Name { get; set; }

        public bool HasName { get { return !string.IsNullOrEmpty(Name); } }

        /// <summary>
        /// What this thing is, in the words of whoever built it.
        /// </summary>
        /// <remarks>
        /// A label to its author and the only explanation anybody who receives it will ever
        /// get, so it travels with the system when it is shared.
        /// </remarks>
        public string Description { get; set; }

        /// <summary>
        /// Whether this system may run at all.
        /// </summary>
        /// <remarks>
        /// Separate from each sequence's own switch rather than a shortcut for setting them
        /// all, so switching an airlock off to rewire it and switching it back on afterwards
        /// leaves the one sequence somebody had deliberately parked still parked.
        ///
        /// Rewiring means stopping the system, not stopping three sequences one at a time -
        /// and a half-stopped airlock, with the inner door's rule off and the outer door's
        /// rule still live, is worse than either state.
        /// </remarks>
        public bool Enabled { get; set; }

        public IReadOnlyList<Sequence> Sequences { get { return _sequences; } }

        /// <summary>
        /// What the author wrote about the parts this system needs.
        /// </summary>
        /// <remarks>
        /// Not the list of parts - that is derived by asking the sequences what they act on,
        /// so it cannot drift from what the system does. This is only what somebody chose to
        /// say about them, and it is kept whether or not a part is currently used: deleting
        /// the last step that mentioned a door must not quietly discard the name and the note
        /// its author wrote for it.
        /// </remarks>
        public IReadOnlyList<SystemRole> Roles { get { return _roles; } }

        public SystemRole FindRole(long id)
        {
            for (var i = 0; i < _roles.Count; i++)
            {
                if (_roles[i].Id == id)
                    return _roles[i];
            }
            return null;
        }

        /// <summary>What the author said about a part, making a blank record if they said nothing.</summary>
        public SystemRole RoleFor(long id)
        {
            var role = FindRole(id);
            if (role != null)
                return role;

            role = new SystemRole(id);
            _roles.Add(role);
            return role;
        }

        /// <summary>
        /// The parts this system needs, worked out from what its sequences act on.
        /// </summary>
        /// <remarks>
        /// Derived on the spot rather than kept, because a kept list is a second thing to
        /// maintain and goes stale the moment somebody adds a step. The same thing acted on in
        /// five places is one part, listed once, so whoever sets this up answers "which is the
        /// inner door" exactly once.
        /// </remarks>
        public void Parts(List<long> into)
        {
            into.Clear();

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];

                var when = sequence.Trigger as WhenTrigger;
                if (when != null)
                    Gather(when.Sources, into);

                for (var s = 0; s < sequence.Steps.Count; s++)
                {
                    var step = sequence.Steps[s];
                    Gather(step.Targets, into);

                    var once = step.Gate as OnceGate;
                    if (once != null && once.Condition != null)
                        Gather(once.Condition.Entities, into);

                    var onlyIf = step.Gate as OnlyIfGate;
                    if (onlyIf != null && onlyIf.Condition != null)
                        Gather(onlyIf.Condition.Entities, into);
                }
            }
        }

        static void Gather(IList<long> ids, List<long> into)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (!into.Contains(ids[i]))
                    into.Add(ids[i]);
            }
        }

        /// <summary>The ones on this ship. Never empty.</summary>
        public IReadOnlyList<SystemInstance> Instances { get { return _instances; } }

        /// <summary>
        /// Adds another of this system, named after the system and its number.
        /// </summary>
        /// <remarks>
        /// It is not given a name. What it is called - Airlock 1, Airlock 2, the way the game
        /// says Interior Light 4 - is worked out from the system and the number, so a system
        /// that only learns its own name later takes its children with it.
        /// </remarks>
        public SystemInstance AddInstance()
        {
            var instance = new SystemInstance(_instances.Count + 1);
            _instances.Add(instance);
            return instance;
        }

        /// <summary>Puts back one of this system that was saved, number and name intact.</summary>
        public SystemInstance Restore(int number, string name)
        {
            var instance = new SystemInstance(number);
            instance.Name = name;
            _instances.Add(instance);
            return instance;
        }

        internal void Add(Sequence sequence)
        {
            _sequences.Add(sequence);
            sequence.System = this;
        }

        internal bool Remove(Sequence sequence)
        {
            return _sequences.Remove(sequence);
        }
    }
}
