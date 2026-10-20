using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// One system's worth of automation: an airlock, a hangar, a drill rig.
    /// </summary>
    public sealed class AutomationSystem
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<SystemInstance> _instances = new List<SystemInstance>();
        readonly List<Part> _parts = new List<Part>();
        readonly List<string> _categories = new List<string>();
        readonly List<Trigger> _triggers = new List<Trigger>();
        readonly SystemInstance[] _self;
        DateTime? _changed;
        DateTime? _originChanged;
        int _lastSequence;
        bool _enabled = true;
        bool _blueprint;

        /// <summary>Told whenever a sequence or an instance of this system comes or goes.</summary>
        public Action Reshaped;

        /// <summary>Told whenever the system, or one of its installations, is switched on.</summary>
        public Action Started;

        /// <summary>Told whenever the system, or one of its installations, is switched on or off.</summary>
        public Action Switched;

        void Reshape()
        {
            if (Reshaped != null)
                Reshaped();
        }

        /// <summary>An empty logic system numbered <paramref name="id" />, known everywhere as <paramref name="identity" />.</summary>
        public AutomationSystem(int id, Guid identity)
        {
            Id = id;
            Identity = identity;
            Namespace = "";
            Name = "";
            Annotation = "";
            _self = new[] { new SystemInstance(0, this) };
        }

        /// <summary>Whether the system may run.</summary>
        /// <remarks>Switching it on clears <see cref="Halt" /> and switches back on each instance Sprocket switched off.</remarks>
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value)
                {
                    Halt = null;
                    foreach (var instance in _instances)
                    {
                        if (instance.Halt != null)
                            instance.Enabled = true;
                    }
                }

                var started = value && !_enabled;
                var switched = value != _enabled;
                _enabled = value;

                if (started && Started != null)
                    Started();

                if (switched && Switched != null)
                    Switched();
            }
        }

        /// <summary>Why Sprocket switched the system off because it could not work, or null where it did not.</summary>
        public Halt Halt { get; set; }

        /// <summary>
        /// A handle for this session only.
        /// </summary>
        /// <remarks>Never saved, and meaningless after a reload.</remarks>
        public int Id { get; private set; }

        /// <summary>
        /// What this system is, everywhere: in a save, a button's key, and whatever it is exported to.
        /// </summary>
        public Guid Identity { get; set; }

        /// <summary>
        /// Who a system is by - MalTek, a faction, a player's name - or empty.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// What the player called it, or empty if they have not said.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// When this system was last changed, in UTC to the second, or null if nobody has said.
        /// </summary>
        /// <remarks>Held to the second, as the script writes it.</remarks>
        public DateTime? Changed
        {
            get { return _changed; }
            set { _changed = value.HasValue ? ToSecond(value.Value) : (DateTime?)null; }
        }

        /// <summary>The identity of the stored blueprint this logic system was made from, or empty where it has none.</summary>
        public Guid Origin { get; set; }

        /// <summary>The version of the blueprint, by its <see cref="Changed" />, it was made from or last brought up to date with; null where it has none.</summary>
        public DateTime? OriginChanged
        {
            get { return _originChanged; }
            set { _originChanged = value.HasValue ? ToSecond(value.Value) : (DateTime?)null; }
        }

        /// <summary>Whether it was made from a stored blueprint it still names.</summary>
        public bool HasOrigin => Origin != Guid.Empty;

        /// <summary>Whether somebody has named it.</summary>
        public bool HasName => !string.IsNullOrEmpty(Name);

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        public string Annotation { get; set; }

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>Its sequences, in the order they were made.</summary>
        public IReadOnlyList<Sequence> Sequences => _sequences;

        /// <summary>
        /// What starts this system's sequences.
        /// </summary>
        public IReadOnlyList<Trigger> Triggers => _triggers;

        /// <summary>Adds <paramref name="trigger" /> to its triggers, where there is one.</summary>
        public void AddTrigger(Trigger trigger)
        {
            if (trigger != null)
                _triggers.Add(trigger);
        }

        /// <summary>
        /// Swaps what starts a sequence, keeping its place among the others.
        /// </summary>
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
        public bool Called(int number)
        {
            foreach (var sequence in _sequences)
            {
                var steps = sequence.Steps;
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
        public IReadOnlyList<Part> Parts => _parts;

        /// <summary>The names of the categories its slots are shown under on the installation panel, in the order they are shown.</summary>
        public IList<string> Categories => _categories;

        /// <summary>Puts its slots in the order of <paramref name="order" />, which holds every one of them.</summary>
        public void Arrange(IList<Part> order)
        {
            _parts.Clear();
            for (var i = 0; i < order.Count; i++)
                _parts.Add(order[i]);
        }

        /// <summary>Renames <paramref name="part" /> to <paramref name="name" />, everywhere the system names it.</summary>
        public void RenamePart(Part part, string name)
        {
            RenameMentions(part.Id, name);
            part.Rename(name);
        }

        /// <summary>Has everything that names the slot <paramref name="was" /> - triggers, steps, installations - name <paramref name="name" /> instead.</summary>
        public void RenameMentions(string was, string name)
        {
            foreach (var trigger in _triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null)
                    Rename(moment.Sources, was, name);

                var own = trigger.Acts;
                if (own != null)
                    Rename(own.Targets, was, name);
            }

            foreach (var sequence in _sequences)
            {
                var steps = sequence.Steps;
                for (var i = 0; i < steps.Count; i++)
                    Rename(steps[i].Targets, was, name);
            }

            foreach (var instance in _instances)
                instance.RenameSlot(was, name);
        }

        static void Rename(IList<string> names, string was, string now)
        {
            for (var i = 0; i < names.Count; i++)
            {
                if (names[i] == was)
                    names[i] = now;
            }
        }

        /// <summary>The slot called <paramref name="name" />, or null where it has none.</summary>
        public Part FindPart(string name)
        {
            foreach (var part in _parts)
            {
                if (part.Id == name)
                    return part;
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
        /// <remarks>Cannot be undone.</remarks>
        public void DropPart(Part part)
        {
            ForgetPart(part);
            foreach (var instance in _instances)
                instance.Clear(part.Id);
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
        /// A new signal part, answered in <paramref name="instance" /> by the slot <paramref name="at" /> on each block of
        /// <paramref name="declared" />, and called <paramref name="called" /> as a picked block's part is.
        /// </summary>
        public string TakeTrigger(SystemInstance instance, IList<string> declared, IList<ToolbarSlot> at, string called, string type)
        {
            var name = Free(called);
            var part = PartFor(name);
            part.IsSignal = true;
            part.Expects = type ?? "";

            instance.Fill(name, declared, at);
            return name;
        }

        /// <summary><paramref name="called" />, numbered where a part already has it: Door, Door 2.</summary>
        public string Free(string called)
        {
            var name = called;
            for (var n = 2; FindPart(name) != null; n++)
                name = $"{called} {n}";

            return name;
        }

        /// <summary>How many times its triggers and steps name <paramref name="slot" />.</summary>
        public int Mentions(string slot)
        {
            var uses = 0;
            foreach (var trigger in _triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null)
                    uses += Count(moment.Sources, slot);

                var own = trigger.Acts;
                if (own != null)
                    uses += Count(own.Targets, slot);
            }

            foreach (var sequence in _sequences)
            {
                var steps = sequence.Steps;
                for (var s = 0; s < steps.Count; s++)
                    uses += Count(steps[s].Targets, slot);
            }

            return uses;
        }

        /// <summary>The first action of its triggers and steps that acts on <paramref name="slot" />, or null where none does.</summary>
        public Act ActingOn(string slot)
        {
            foreach (var trigger in _triggers)
            {
                var own = trigger.Acts;
                if (own != null && own.Targets.Contains(slot))
                    return own;
            }

            foreach (var sequence in _sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var act = step as Act;
                    if (act != null && act.Targets.Contains(slot))
                        return act;
                }
            }

            return null;
        }

        /// <summary>How many of <paramref name="ids" /> are <paramref name="id" />.</summary>
        public static int Count(IList<string> ids, string id)
        {
            var count = 0;
            for (var i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                    count++;
            }

            return count;
        }

        /// <summary>Puts into <paramref name="into" /> every slot its triggers and steps name, each once.</summary>
        public void Acted(List<string> into)
        {
            into.Clear();

            foreach (var trigger in _triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null)
                    Gather(moment.Sources, into);

                var own = trigger.Acts;
                if (own != null)
                    Gather(own.Targets, into);
            }

            foreach (var sequence in _sequences)
            {
                for (var s = 0; s < sequence.Steps.Count; s++)
                    Gather(sequence.Steps[s].Targets, into);
            }
        }

        /// <summary>Puts into <paramref name="into" /> every list of names its triggers and steps hold, the lists themselves, leaving out empty ones.</summary>
        public void Lists(List<IList<string>> into)
        {
            into.Clear();

            foreach (var trigger in _triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null && moment.Sources.Count > 0)
                    into.Add(moment.Sources);

                var own = trigger.Acts;
                if (own != null && own.Targets.Count > 0)
                    into.Add(own.Targets);
            }

            foreach (var sequence in _sequences)
            {
                var steps = sequence.Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    if (steps[s].Targets.Count > 0)
                        into.Add(steps[s].Targets);
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

        /// <summary>The instances of this system blueprint on this ship, which a system never has.</summary>
        public IReadOnlyList<SystemInstance> Instances => _instances;

        /// <summary>The first way it runs: a blueprint's first instance, or a system's own run; null for a blueprint with no instances.</summary>
        public SystemInstance First => Uses.Count > 0 ? Uses[0] : null;

        /// <summary>Whether this is a system blueprint, used by instances, rather than a system.</summary>
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
        /// <remarks>A system's own run is numbered 0, is never saved, and answers no slots.</remarks>
        public IReadOnlyList<SystemInstance> Uses => IsBlueprint ? (IReadOnlyList<SystemInstance>)_instances : _self;

        /// <summary>
        /// Adds another installation of this system, numbered one past the highest.
        /// </summary>
        public SystemInstance AddInstance()
        {
            var highest = 0;
            foreach (var earlier in _instances)
            {
                if (earlier.Number > highest)
                    highest = earlier.Number;
            }

            var instance = new SystemInstance(highest + 1, this);
            _instances.Add(instance);
            Reshape();
            return instance;
        }

        /// <summary>Takes <paramref name="instance" /> out of this system, with the blocks it was filled with, and says whether it was here.</summary>
        public bool RemoveInstance(SystemInstance instance)
        {
            if (!_instances.Remove(instance))
                return false;

            Reshape();
            return true;
        }

        /// <summary>Puts a removed sequence back where it stood, <paramref name="at" />, as the sequence it was.</summary>
        public void PutBack(Sequence sequence, int at)
        {
            if (sequence == null || _sequences.Contains(sequence))
                return;

            _sequences.Insert(at < 0 || at > _sequences.Count ? _sequences.Count : at, sequence);
            sequence.System = this;
            Reshape();
            Used(sequence.Number);
        }

        /// <summary>Puts a removed installation back where it stood, <paramref name="at" />, as the installation it was.</summary>
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
            foreach (var instance in _instances)
            {
                if (string.Equals(instance.Name, name, StringComparison.Ordinal))
                    return instance;
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

        /// <summary>Makes <paramref name="sequence" /> one of its own.</summary>
        public void Add(Sequence sequence)
        {
            _sequences.Add(sequence);
            sequence.System = this;
            Reshape();
            Used(sequence.Number);
        }

        /// <summary>Takes <paramref name="sequence" /> out of it, and says whether it was there.</summary>
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
        /// <remarks>Saved only where it is higher than <see cref="HighestSequenceNumber" />.</remarks>
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
            foreach (var sequence in _sequences)
                if (sequence.Number > highest)
                    highest = sequence.Number;

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
            foreach (var sequence in _sequences)
                if (sequence.Number == number)
                    return sequence;

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
        /// <remarks>A time with no stated zone is taken as UTC.</remarks>
        static DateTime ToSecond(DateTime time)
        {
            var utc = time.Kind == DateTimeKind.Local
                ? time.ToUniversalTime()
                : DateTime.SpecifyKind(time, DateTimeKind.Utc);

            return new DateTime(utc.Ticks - utc.Ticks % TimeSpan.TicksPerSecond, DateTimeKind.Utc);
        }
    }
}
