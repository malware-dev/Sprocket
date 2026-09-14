using System;
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
        DateTime? _changed;

        public AutomationSystem(int id)
        {
            Id = id;
            Namespace = "";
            Name = "";
            Annotation = "";
            Enabled = true;
        }

        /// <summary>
        /// A handle for this session only.
        /// </summary>
        /// <remarks>
        /// <b>Not the system's identity, and never saved.</b> It exists so the editor can name a system in a
        /// command without holding the object, and it means nothing after a reload. What a system is, is its
        /// <see cref="Namespace" /> and <see cref="Name" />: a number cannot be unique once systems are
        /// exported, imported, or on constructs that join.
        /// </remarks>
        public int Id { get; private set; }

        /// <summary>
        /// Who a system is by - MalTek, a faction, a player's name - or empty.
        /// </summary>
        /// <remarks>
        /// With <see cref="Name" />, what the system is, everywhere: in a save, a button's key, and whatever
        /// it is exported to. Two people's airlocks are different systems because of this. It comes from the
        /// player making the system and is theirs to choose; how a target presents it, as authorship or
        /// otherwise, is the target's business.
        /// </remarks>
        public string Namespace { get; set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// <b>Part of the system's identity, with <see cref="Namespace" />.</b> Renaming a system is making
        /// a different one, which is why a name has to be chosen with some care. Empty is a system nobody has
        /// named, which the editor does not let anybody make.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// When this system was last changed, in UTC to the second, or null if nobody has said.
        /// </summary>
        /// <remarks>
        /// A hint for a person, never something Reactor decides by. When two copies of the same system meet
        /// and differ, the player chooses, and "changed two hours ago" against "changed last Tuesday" is what
        /// helps them. So a clock that disagrees between two servers costs nothing.
        /// <para>
        /// Held to the second because that is what the script writes, so what is held and what reads back
        /// are the same value. Core carries the time and never reads the clock; the host stamps it.
        /// </para>
        /// </remarks>
        public DateTime? Changed
        {
            get { return _changed; }
            set { _changed = value.HasValue ? ToSecond(value.Value) : (DateTime?)null; }
        }

        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// What this thing is, in the words of whoever built it.
        /// </summary>
        /// <remarks>
        /// A label to its author and the only explanation anybody who receives it will ever
        /// get, so it travels with the system when it is shared.
        /// </remarks>
        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }

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

        public IReadOnlyList<Sequence> Sequences => _sequences;

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
        public IReadOnlyList<SystemRole> Roles => _roles;

        public SystemRole FindRole(string name)
        {
            for (var i = 0; i < _roles.Count; i++)
            {
                if (_roles[i].Id == name)
                    return _roles[i];
            }
            return null;
        }

        /// <summary>What the author said about a part, making a blank record if they said nothing.</summary>
        public SystemRole RoleFor(string name)
        {
            var role = FindRole(name);
            if (role != null)
                return role;

            role = new SystemRole(name);
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
        public void Parts(List<string> into)
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

        static void Gather(IList<string> ids, List<string> into)
        {
            for (var i = 0; i < ids.Count; i++)
            {
                if (!into.Contains(ids[i]))
                    into.Add(ids[i]);
            }
        }

        /// <summary>The ones on this ship. Never empty.</summary>
        public IReadOnlyList<SystemInstance> Instances => _instances;

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

        /// <summary>Whether this is the system with that namespace and name.</summary>
        public bool Is(string space, string name)
        {
            return string.Equals(Namespace, space ?? "", StringComparison.Ordinal)
                && string.Equals(Name, name ?? "", StringComparison.Ordinal);
        }

        /// <summary>
        /// A time as UTC, to the second.
        /// </summary>
        /// <remarks>
        /// A time with no stated zone is taken as UTC rather than as local, because the script's rule is UTC
        /// and a local reading would move with whoever's machine loaded it.
        /// </remarks>
        static DateTime ToSecond(DateTime time)
        {
            var utc = time.Kind == DateTimeKind.Local
                ? time.ToUniversalTime()
                : DateTime.SpecifyKind(time, DateTimeKind.Utc);

            return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }
    }
}
