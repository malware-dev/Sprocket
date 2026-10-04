using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One system's worth of automation: an airlock, a hangar, a drill rig.
    /// </summary>
    /// <remarks>
    /// The player's word for this is "system", and that is what the editor says. The type
    /// cannot be called that - a type named <c>System</c> shadows the namespace every file
    /// in the project depends on - so the name is longer here and nowhere else.
    ///
    /// A system runs on the program's own construct and names its blocks directly. A system blueprint is
    /// written against slots instead, and each <see cref="SystemInstance"/> of it answers them on blocks of
    /// its own.
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
        readonly SystemInstance[] _self;
        DateTime? _changed;
        int _lastSequence;
        bool _enabled = true;
        bool _blueprint;

        /// <summary>Told whenever a sequence or one of this system comes or goes. See <see cref="Automation.Reshaped" />.</summary>
        public Action Reshaped;

        /// <summary>Told whenever the system is switched on, so the program is checked for the start it is given. See <see cref="Automation.Starts" />.</summary>
        public Action Started;

        void Reshape()
        {
            if (Reshaped != null)
                Reshaped();
        }

        public AutomationSystem(int id, Guid identity)
        {
            Id = id;
            Identity = identity;
            Namespace = "";
            Name = "";
            Annotation = "";
            _self = new[] { new SystemInstance(0, this) };
        }

        /// <summary>Whether the system may run, which is also what keeps what it does from being changed while it does.</summary>
        /// <remarks>
        /// One switch for everything the system does, and switching it on is starting the machine again: it forgets why
        /// Sprocket switched it off, and switches back on each one of it Sprocket switched off. One somebody switched off
        /// stays off, because starting the machine must not start what a person stopped.
        /// </remarks>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                {
                    Halt = null;
                    for (var i = 0; i < _instances.Count; i++)
                    {
                        if (_instances[i].Halt != null)
                            _instances[i].Enabled = true;
                    }
                }

                var started = value && !_enabled;
                _enabled = value;

                if (started && Started != null)
                    Started();
            }
        }

        /// <summary>Why Sprocket switched the system off because it could not work, or null where it did not.</summary>
        public Halt Halt { get; set; }

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
        /// For people, not for Sprocket: it tells two players' airlocks apart in a list, and says who made one.
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
        /// A hint for a person, never something Sprocket decides by. When two copies of the same system meet
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
        /// door means Sprocket watches it, and pointing at a button means the construct starts it.
        /// Those are different triggers, and an edit that could only change fields could not
        /// answer the question the player was asked.
        /// </remarks>
        public void ReplaceTrigger(Trigger was, Trigger now)
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

        /// <summary>Where a trigger sits among the others, or -1.</summary>
        public int IndexOfTrigger(Trigger trigger)
        {
            for (var i = 0; i < _triggers.Count; i++)
            {
                if (_triggers[i] == trigger)
                    return i;
            }

            return -1;
        }

        /// <summary>
        /// Puts a trigger back where it was.
        /// </summary>
        /// <remarks>
        /// For undo. One that came back on the end would reorder the list somebody is looking at,
        /// which reads as a second change rather than as the first one being taken back.
        /// </remarks>
        public void PutTriggerBack(int at, Trigger trigger)
        {
            if (trigger == null)
                return;

            if (at < 0 || at > _triggers.Count)
                _triggers.Add(trigger);
            else
                _triggers.Insert(at, trigger);
        }

        /// <summary>The first thing that starts that sequence, or null where nothing does.</summary>
        public Trigger TriggerFor(int number)
        {
            for (var i = 0; i < _triggers.Count && number > 0; i++)
            {
                if (_triggers[i].Runs == number && !_triggers[i].Stops)
                    return _triggers[i];
            }

            return null;
        }

        /// <summary>Whether any sequence of this system has a step that runs that one; a step that stops it does not count.</summary>
        /// <remarks>
        /// Asked of the whole system rather than of one sequence, because what is wanted is whether
        /// anybody at all calls it: a sequence with no trigger and no caller is one nothing can ever
        /// start, which is a fault rather than a way of building.
        /// </remarks>
        public bool Called(int number)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                var steps = _sequences[i].Steps;
                for (var q = 0; q < steps.Count; q++)
                {
                    var run = steps[q] as Run;
                    if (run != null && !run.IsOff && !run.Stops && run.Number == number)
                        return true;
                }
            }

            return false;
        }

        /// <summary>Forgets everything that started a sequence, for when the sequence itself goes.</summary>
        public void DropTriggersFor(int number)
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

        /// <summary>Renames <paramref name="part" /> to <paramref name="name" />, everywhere the system names it.</summary>
        public void RenamePart(Part part, string name)
        {
            var was = part.Id;

            for (var i = 0; i < _triggers.Count; i++)
            {
                var moment = _triggers[i] as MomentTrigger;
                if (moment != null)
                    Rename(moment.Sources, was, name);

                var own = _triggers[i].Acts;
                if (own != null)
                    Rename(own.Targets, was, name);
            }

            for (var s = 0; s < _sequences.Count; s++)
            {
                var steps = _sequences[s].Steps;
                for (var i = 0; i < steps.Count; i++)
                    Rename(steps[i].Targets, was, name);
            }

            for (var i = 0; i < _instances.Count; i++)
                _instances[i].RenameSlot(was, name);

            part.Rename(name);
        }

        static void Rename(IList<string> names, string was, string now)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == was)
                    names[i] = now;
            }
        }

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

        /// <summary>Takes <paramref name="part" /> out of the system, and answers where it was, or -1 where it was not there.</summary>
        public int ForgetPart(Part part)
        {
            var at = _parts.IndexOf(part);
            if (at >= 0)
                _parts.RemoveAt(at);

            return at;
        }

        /// <summary>Takes <paramref name="part" /> out of the system, and every instance's answer to it.</summary>
        /// <remarks>For a part nothing uses, which would not be saved; there is no way back.</remarks>
        public void DropPart(Part part)
        {
            ForgetPart(part);
            for (var i = 0; i < _instances.Count; i++)
                _instances[i].Clear(part.Id);
        }

        /// <summary>Puts <paramref name="part" /> back at <paramref name="at" />, as it was before it was forgotten.</summary>
        public void PutPartBack(int at, Part part)
        {
            if (at < 0 || at > _parts.Count)
                _parts.Add(part);
            else
                _parts.Insert(at, part);
        }

        /// <summary>
        /// Which of the system's parts its sequences actually act on.
        /// </summary>
        /// <remarks>
        /// Worked out on the spot rather than kept, because a kept list goes stale the moment
        /// somebody adds a step. The same thing acted on in five places is one part, listed once, so
        /// whoever fits this system answers "which is the inner door" exactly once.
        /// </remarks>
        /// <summary>
        /// The part <paramref name="instance" /> fills with <paramref name="declared" /> and nothing
        /// else, made where there is none.
        /// </summary>
        /// <remarks>
        /// A new part takes <paramref name="called" />, numbered the way the game numbers blocks where
        /// another part has it - Door, Door 2 - and expects that type. The name is given once and kept:
        /// deriving it again would let renaming a block rewrite the system's own vocabulary.
        /// </remarks>
        /// <param name="instance">The one of the system the block was picked through.</param>
        /// <param name="declared">The block, as the construct declares it.</param>
        /// <param name="called">What a new part is called, as a person reads it.</param>
        /// <param name="type">The block's type, as a part expects one.</param>
        public string Take(SystemInstance instance, string declared, string called, string type)
        {
            for (var i = 0; i < _parts.Count; i++)
            {
                var filled = instance.FilledWith(_parts[i].Id);
                if (filled != null && filled.Count == 1 && filled[0] == declared)
                    return _parts[i].Id;
            }

            var name = Free(called);
            var part = PartFor(name);
            if (type.Length > 0)
                part.Expects.Add(type);

            instance.Fill(name, new[] { declared });
            return name;
        }

        /// <summary>
        /// A new trigger part, answered in <paramref name="instance" /> by the slot <paramref name="at" /> on each block of
        /// <paramref name="declared" />, and called <paramref name="called" /> as a picked block's part is.
        /// </summary>
        public string TakeTrigger(SystemInstance instance, IList<string> declared, IList<ToolbarSlot> at, string called, string type)
        {
            var name = Free(called);
            var part = PartFor(name);
            part.IsTrigger = true;
            if (type.Length > 0)
                part.Expects.Add(type);

            instance.Fill(name, declared, at);
            return name;
        }

        /// <summary><paramref name="called" />, numbered where a part already has it: Door, Door 2.</summary>
        public string Free(string called)
        {
            var name = called;
            for (var n = 2; FindPart(name) != null; n++)
                name = called + " " + n;

            return name;
        }

        public void Acted(List<string> into)
        {
            into.Clear();

            for (var i = 0; i < _triggers.Count; i++)
            {
                var moment = _triggers[i] as MomentTrigger;
                if (moment != null)
                    Gather(moment.Sources, into);

                var own = _triggers[i].Acts;
                if (own != null)
                    Gather(own.Targets, into);
            }

            for (var i = 0; i < _sequences.Count; i++)
            {
                var sequence = _sequences[i];

                for (var s = 0; s < sequence.Steps.Count; s++)
                    Gather(sequence.Steps[s].Targets, into);
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

        /// <summary>The instances of this system blueprint on this ship, which a system never has.</summary>
        public IReadOnlyList<SystemInstance> Instances => _instances;

        /// <summary>The first way it runs: a blueprint's first instance, or a system's own run; null for a blueprint with no instances.</summary>
        public SystemInstance First => Uses.Count > 0 ? Uses[0] : null;

        /// <summary>Whether this is a system blueprint, used by instances, rather than a system.</summary>
        /// <remarks>Changes which runs it has, so whatever arranges triggers and runners is told, as for an instance coming or going.</remarks>
        public bool IsBlueprint
        {
            get { return _blueprint; }
            set
            {
                if (_blueprint == value)
                    return;

                _blueprint = value;
                Reshape();
            }
        }

        /// <summary>Each way this system runs: one run for each instance of a blueprint, and one of a system's own.</summary>
        /// <remarks>
        /// A system's own run is numbered 0, is never saved, and answers no slots, so the names its steps use come through
        /// as the program's variables. Every runner, poller and toolbar slot is kept per run, so a system and a blueprint
        /// are worked the same way.
        /// </remarks>
        public IReadOnlyList<SystemInstance> Uses => IsBlueprint ? (IReadOnlyList<SystemInstance>)_instances : _self;

        /// <summary>
        /// Adds another installation of this system, numbered one past the highest.
        /// </summary>
        /// <remarks>
        /// The number is what tells it from the others within the system, and nothing more: what it is called is given by
        /// whoever adds it.
        /// </remarks>
        public SystemInstance AddInstance()
        {
            var highest = 0;
            for (var i = 0; i < _instances.Count; i++)
            {
                if (_instances[i].Number > highest)
                    highest = _instances[i].Number;
            }

            var instance = new SystemInstance(highest + 1, this);
            _instances.Add(instance);
            Reshape();
            return instance;
        }

        /// <summary>Takes <paramref name="instance" /> out of this system, with the blocks it was filled with, and says whether it was here.</summary>
        /// <remarks>What it was running stops, and the toolbar slots it claimed are let go of, once the program is looked at again.</remarks>
        public bool RemoveInstance(SystemInstance instance)
        {
            if (!_instances.Remove(instance))
                return false;

            Reshape();
            return true;
        }

        /// <summary>Puts a removed installation back where it stood, <paramref name="at" />, as the installation it was.</summary>
        /// <remarks>For undo: the blocks it was filled with, and its number, come back with it.</remarks>
        public void PutBack(SystemInstance instance, int at)
        {
            if (instance == null || _instances.Contains(instance))
                return;

            _instances.Insert(at < 0 || at > _instances.Count ? _instances.Count : at, instance);
            Reshape();
        }

        /// <summary>The instance of this system called <paramref name="name" />, or null.</summary>
        public SystemInstance FindInstance(string name)
        {
            for (var i = 0; i < _instances.Count; i++)
            {
                if (string.Equals(_instances[i].Name, name, StringComparison.Ordinal))
                    return _instances[i];
            }

            return null;
        }

        /// <summary>The run of this system carrying a number, a blueprint's instance or a system's own numbered 0, or null.</summary>
        public SystemInstance FindInstance(int number)
        {
            var uses = Uses;
            for (var i = 0; i < uses.Count; i++)
            {
                if (uses[i].Number == number)
                    return uses[i];
            }

            return null;
        }

        /// <summary>Puts back one of this system that was saved, number and name intact.</summary>
        public SystemInstance Restore(int number, string name)
        {
            var instance = new SystemInstance(number, this);
            instance.Name = name;
            _instances.Add(instance);
            Reshape();
            return instance;
        }

        public void Add(Sequence sequence)
        {
            _sequences.Add(sequence);
            sequence.System = this;
            Reshape();
            Used(sequence.Number);
        }

        public bool Remove(Sequence sequence)
        {
            if (!_sequences.Remove(sequence))
                return false;

            Reshape();
            return true;
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
        public void Used(int number)
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
