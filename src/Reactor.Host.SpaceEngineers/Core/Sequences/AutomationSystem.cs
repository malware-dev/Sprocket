using System;
using System.Collections.Generic;

namespace Reactor.Host.SpaceEngineers.Core.Sequences
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
        readonly List<Part> _parts = new List<Part>();
        readonly List<Trigger> _triggers = new List<Trigger>();
        DateTime? _changed;
        int _lastSequence;

        public AutomationSystem(int id, Guid identity)
        {
            Id = id;
            Identity = identity;
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
        /// <see cref="Identity" />: a number cannot be unique once systems are exported, imported, or on
        /// constructs that join.
        /// </remarks>
        public int Id { get; private set; }

        /// <summary>
        /// What this system is, everywhere: in a save, a button's key, and whatever it is exported to.
        /// </summary>
        /// <remarks>
        /// Made once, with the system, and never changed by anything a player does. A name cannot do this
        /// job: a published system can be renamed in one world and not in the copies elsewhere, and a copy
        /// meeting a renamed one still has to be recognised as the same system.
        /// </remarks>
        public Guid Identity { get; set; }

        /// <summary>
        /// Who a system is by - MalTek, a faction, a player's name - or empty.
        /// </summary>
        /// <remarks>
        /// For people, not for Reactor: it tells two players' airlocks apart in a list, and says who made one.
        /// It comes from the player making the system and is theirs to choose; how a target presents it, as
        /// authorship or otherwise, is the target's business.
        /// </remarks>
        public string Namespace { get; set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        /// <remarks>
        /// Free to change, since <see cref="Identity" /> is what the system is. Empty is a system nobody has
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
        /// What starts this system's sequences.
        /// </summary>
        /// <remarks>
        /// Held here rather than on the sequences, because more than one trigger may run one sequence and
        /// a sequence is a procedure rather than a reason.
        /// </remarks>
        public IReadOnlyList<Trigger> Triggers => _triggers;

        public void AddTrigger(Trigger trigger)
        {
            if (trigger != null)
                _triggers.Add(trigger);
        }

        /// <summary>
        /// Swaps what starts a sequence, keeping its place among the others.
        /// </summary>
        /// <remarks>
        /// Because one trip to the picker can change which sort of trigger it is: pointing at a
        /// door means Reactor watches it, and pointing at a button means the construct starts it.
        /// Those are different triggers, and an edit that could only change fields could not
        /// answer the question the player was asked.
        /// </remarks>
        internal void ReplaceTrigger(Trigger was, Trigger now)
        {
            for (var i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i] != was)
                    continue;

                if (now == null)
                    _triggers.RemoveAt(i);
                else
                    _triggers[i] = now;

                return;
            }

            AddTrigger(now);
        }

        /// <summary>The first thing that starts that sequence, or null where nothing does.</summary>
        public Trigger TriggerFor(int number)
        {
            for (var i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i].Runs == number)
                    return _triggers[i];
            }

            return null;
        }

        /// <summary>Forgets everything that started a sequence, for when the sequence itself goes.</summary>
        internal void DropTriggersFor(int number)
        {
            for (var i = _triggers.Count - 1; i >= 0; i--)
            {
                if (_triggers[i].Runs == number)
                    _triggers.RemoveAt(i);
            }
        }

        /// <summary>
        /// What this system needs a construct to provide.
        /// </summary>
        /// <remarks>
        /// Declared rather than derived. Which parts the sequences happen to act on can be worked out
        /// from the sequences, but what a part expects and what its author said about it cannot, and
        /// both have to survive deleting the last step that mentioned it.
        /// </remarks>
        public IReadOnlyList<Part> Parts => _parts;

        public Part FindPart(string name)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                if (_parts[i].Id == name)
                    return _parts[i];
            }
            return null;
        }

        /// <summary>The part by that name, declaring a blank one where the system has none.</summary>
        public Part PartFor(string name)
        {
            var part = FindPart(name);
            if (part != null)
                return part;

            part = new Part(name);
            _parts.Add(part);
            return part;
        }

        /// <summary>
        /// Which of the system's parts its sequences actually act on.
        /// </summary>
        /// <remarks>
        /// Worked out on the spot rather than kept, because a kept list goes stale the moment
        /// somebody adds a step. The same thing acted on in five places is one part, listed once, so
        /// whoever fits this system answers "which is the inner door" exactly once.
        /// </remarks>
        public void Acted(List<string> into)
        {
            into.Clear();

            for (var i = 0; i < _triggers.Count; i++)
            {
                var moment = _triggers[i] as MomentTrigger;
                if (moment != null)
                    Gather(moment.Sources, into);
            }

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];

                for (var s = 0; s < sequence.Steps.Count; s++)
                {
                    var act = sequence.Steps[s] as Act;
                    if (act != null)
                        Gather(act.Targets, into);
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

        /// <summary>The ones on this ship, which is none until it is fitted to something.</summary>
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
            var highest = 0;
            for (var i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Number > highest)
                    highest = _instances[i].Number;
            }

            var instance = new SystemInstance(highest + 1);
            _instances.Add(instance);
            return instance;
        }

        /// <summary>The one of this system carrying a number, or null.</summary>
        public SystemInstance FindInstance(int number)
        {
            for (var i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Number == number)
                    return _instances[i];
            }

            return null;
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
            Used(sequence.Number);
        }

        internal bool Remove(Sequence sequence)
        {
            return _sequences.Remove(sequence);
        }

        /// <summary>
        /// The highest number any sequence of this system has had, including ones since deleted.
        /// </summary>
        /// <remarks>
        /// A failsafe, so a number is never handed out twice: a button still holding a deleted sequence's
        /// number must not start running whatever is made next. It only needs saving when it is higher than
        /// <see cref="HighestSequenceNumber" />, which is only after the highest was deleted.
        /// </remarks>
        public int LastSequenceNumber => _lastSequence;

        /// <summary>The number a new sequence of this system gets: one past the last it had.</summary>
        public int NextSequenceNumber()
        {
            return _lastSequence + 1;
        }

        /// <summary>The highest number a sequence of this system carries now, or 0 for none.</summary>
        public int HighestSequenceNumber()
        {
            var highest = 0;
            for (var i = 0; i < _sequences.Count; i++)
                if (_sequences[i].Number > highest)
                    highest = _sequences[i].Number;

            return highest;
        }

        /// <summary>Records that a sequence number has been used, so it is never handed out again.</summary>
        internal void Used(int number)
        {
            if (number > _lastSequence)
                _lastSequence = number;
        }

        /// <summary>The sequence of this system with that number, or null.</summary>
        public Sequence FindSequence(int number)
        {
            for (var i = 0; i < _sequences.Count; i++)
                if (_sequences[i].Number == number)
                    return _sequences[i];

            return null;
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
