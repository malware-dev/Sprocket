using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>
    /// Everything one Sprocket unit is running.
    /// </summary>
    public sealed class Automation
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<EntityRef> _refs = new List<EntityRef>();
        readonly List<SignalRef> _signalRefs = new List<SignalRef>();

        // Every name is looked up here while a program is read; a scan of the lists would make reading quadratic.
        readonly Dictionary<string, EntityRef> _refsByName = new Dictionary<string, EntityRef>();
        readonly Dictionary<string, SignalRef> _signalRefsByName = new Dictionary<string, SignalRef>();

        int _nextSequenceId = 1;
        int _nextSystemId = 1;

        /// <summary>
        /// Every sequence, flat, in the order they were made.
        /// </summary>
        /// <remarks>Read by the runtime on every tick, so it is kept rather than built on demand.</remarks>
        public IReadOnlyList<Sequence> Sequences => _sequences;

        /// <summary>The systems, which is what the editor opens onto.</summary>
        public IReadOnlyList<AutomationSystem> Systems => _systems;

        /// <summary>
        /// Whether this is all of the program that was saved.
        /// </summary>
        /// <remarks>While false, nothing may write this program over the save it came from.</remarks>
        public bool Intact { get; private set; } = true;

        /// <summary>
        /// What would not read, in the words of whatever read it, or empty.
        /// </summary>
        /// <remarks>Only the first problem is kept.</remarks>
        public string Trouble { get; private set; } = "";

        /// <summary>
        /// Whether what was saved came from a version of Sprocket this one does not read at all.
        /// </summary>
        public bool Incompatible { get; private set; }

        /// <summary>What somebody wrote above the program's first line, or empty.</summary>
        public string Annotation { get; set; } = "";

        /// <summary>The attributes written above it that Sprocket does not use, kept as written, or null where there are none.</summary>
        public KeptAttributes Attributes { get; set; }

        /// <summary>What the construct's automation is doing, which the world keeps apart from the program; one of its own until a Sprocket hands over the construct's.</summary>
        public ConstructMemory Memory { get; set; } = new ConstructMemory();

        /// <summary>The construct's big owners the automation was last confirmed for, as the game's player identities; empty until the construct had any.</summary>
        public List<long> Owners => Memory.Owners;

        /// <summary>Who the automation belonged to when it changed hands outside sharing and paused, as the game's player identities; empty while it runs.</summary>
        public List<long> PausedFrom => Memory.PausedFrom;

        /// <summary>Whether it is paused, running and firing nothing until a big owner resumes it.</summary>
        public bool Paused => PausedFrom.Count > 0;

        /// <summary>The text that would not read, kept so it can be shown and mended; empty where everything read.</summary>
        public string Unread { get; private set; } = "";

        /// <summary>Empties it, and keeps <paramref name="text" /> as what would not read.</summary>
        public void Unreadable(string text)
        {
            Discard();
            Unread = text ?? "";
        }

        /// <summary>Says this program is not all of what was saved, and cannot be saved over it.</summary>
        public void Damaged(string what)
        {
            Intact = false;

            if (Trouble.Length == 0)
                Trouble = what ?? "";
        }

        /// <summary>Says what was saved was written by a version of Sprocket this one does not read.</summary>
        public void Refused(string what)
        {
            Damaged(what);
            Incompatible = true;
        }

        /// <summary>
        /// Says the unreadable original is safe somewhere else, so this may be saved after all.
        /// </summary>
        public void Kept()
        {
            Intact = true;
            Incompatible = false;
            Trouble = "";
            Unread = "";
        }

        /// <summary>
        /// Every block this program names, declared once and pointed at from wherever it is used.
        /// </summary>
        public IReadOnlyList<EntityRef> Refs => _refs;

        /// <summary>Every trigger this program names as a variable: a slot on a block's toolbar, which a system starts a step from with <c>whenever</c>.</summary>
        public IReadOnlyList<SignalRef> SignalRefs => _signalRefs;

        /// <summary>
        /// Bumped whenever a declaration is added, removed or re-pointed.
        /// </summary>
        public int Revision { get; private set; }

        /// <summary>
        /// Bumped whenever somebody has said what a trigger is.
        /// </summary>
        /// <remarks>Bumped even when the trigger is set to what it already was.</remarks>
        public int Wiring { get; private set; }

        /// <summary>Bumped whenever somebody changes what the program says.</summary>
        public int Edits { get; private set; }

        /// <summary>
        /// Raised whenever a sequence, or one of a system, comes or goes.
        /// </summary>
        public event Action Reshaped;

        /// <summary>Bumped whenever a system or one of its installations is switched on, which is when every problem is forgotten and found again.</summary>
        public int Starts { get; private set; }

        /// <summary>Bumped whenever a system or one of its installations is switched on or off, which is when what is watched for it is matched again.</summary>
        public int Switches { get; private set; }

        void Start()
        {
            Starts++;
        }

        void Switch()
        {
            Switches++;
            Change();
        }

        /// <summary>Raised whenever what the program says, or what is switched off, may have changed: every edit, wiring, declaration and switch.</summary>
        /// <remarks>Raised in the middle of the change, so whatever answers it waits until the change is done.</remarks>
        public event Action Changed;

        /// <summary>Says a declaration's keys were brought up to date with the block they find, which changes what the program says and nothing it does.</summary>
        public void Rekeyed()
        {
            Change();
        }

        void Change()
        {
            var changed = Changed;
            if (changed != null)
                changed();
        }

        void Reshape()
        {
            var reshaped = Reshaped;
            if (reshaped != null)
                reshaped();
        }

        /// <summary>Says that something moved and whatever arranges triggers should look again.</summary>
        public void Rewire()
        {
            Wiring++;
            Change();
        }

        /// <summary>Says that the program has been changed, and anything worked out from it is stale.</summary>
        public void Edited()
        {
            Edits++;
            Change();
        }

        /// <summary>
        /// Names this entity, or hands back the name it already has.
        /// </summary>
        /// <remarks>Called while editing, never while running.</remarks>
        public EntityRef Declare(GridWorld world, GridEntity entity)
        {
            foreach (var entityRef in _refs)
            {
                int matched;
                var found = world.Keys.Match(entityRef.Keys, out matched);
                if (found != null && found.Id == entity.Id && entityRef.KindId == entity.Kind.Id)
                {
                    // Chosen by somebody, so a block only its name found is the block meant.
                    if (matched != 0)
                        Identify(world, entity, entityRef);

                    return entityRef;
                }
            }

            var called = NameRules.Clean(entity.Name);
            var declared = new EntityRef(Free(called.Length > 0 ? called : entity.Kind.DisplayName), entity.Kind.Id);
            world.Keys.Identify(entity, declared.Keys);
            return Restore(declared);
        }

        /// <summary>Makes <paramref name="keys" /> what <paramref name="declared" /> finds its block by.</summary>
        public void Rekey(EntityRef declared, IList<EntityKey> keys)
        {
            var kept = new List<EntityKey>(keys);
            declared.Keys.Clear();
            declared.Keys.AddRange(kept);
            Revision++;
            Rewire();
        }

        void Identify(GridWorld world, GridEntity entity, EntityRef declared)
        {
            var keys = new List<EntityKey>();
            world.Keys.Identify(entity, keys);
            Rekey(declared, keys);
        }

        /// <summary>Puts back a declaration read from storage.</summary>
        public EntityRef Restore(EntityRef declared)
        {
            _refs.Add(declared);
            if (!_refsByName.ContainsKey(declared.Name))
                _refsByName.Add(declared.Name, declared);

            Revision++;
            Rewire();
            return declared;
        }

        /// <summary>
        /// The variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names and no other, declared
        /// where there is none, named <paramref name="called" /> as a block's variable is.
        /// </summary>
        public SignalRef DeclareSignal(string block, ToolbarSlot at, string called)
        {
            var declared = FindSignalRefOnly(block, at);
            if (declared != null)
                return declared;

            return Restore(new SignalRef(Free(called), new[] { block }, at));
        }

        /// <summary>Puts back a signal variable read from storage.</summary>
        public SignalRef Restore(SignalRef declared)
        {
            _signalRefs.Add(declared);
            if (!_signalRefsByName.ContainsKey(declared.Name))
                _signalRefsByName.Add(declared.Name, declared);

            Revision++;
            Rewire();
            return declared;
        }

        /// <summary>The signal variable called <paramref name="name" />, or null.</summary>
        public SignalRef FindSignalRef(string name)
        {
            SignalRef found;
            return _signalRefsByName.TryGetValue(name, out found) ? found : null;
        }

        /// <summary>The signal variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names, or null.</summary>
        public SignalRef FindSignalRef(string block, ToolbarSlot at)
        {
            foreach (var signalRef in _signalRefs)
            {
                if (signalRef.Holds(block, at))
                    return signalRef;
            }

            return null;
        }

        /// <summary>The signal variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names and no other, or null.</summary>
        public SignalRef FindSignalRefOnly(string block, ToolbarSlot at)
        {
            foreach (var signalRef in _signalRefs)
            {
                if (signalRef.HoldsOnly(block, at))
                    return signalRef;
            }

            return null;
        }

        /// <summary>Whether a variable, of either sort, is called <paramref name="name" />.</summary>
        public bool Names(string name)
        {
            return FindRef(name) != null || FindSignalRef(name) != null;
        }

        /// <summary><paramref name="called" />, numbered where a variable already has it: Door, Door 2.</summary>
        public string Free(string called)
        {
            var name = called;
            for (var n = 2; Names(name); n++)
                name = $"{called} {n}";

            return name;
        }

        /// <summary>
        /// The toolbar slots that send the signal <paramref name="trigger" /> of <paramref name="system" /> starts on, as
        /// <paramref name="instance" /> runs it: the blocks, in <paramref name="blocks" />, and the slot on each, in
        /// <paramref name="at" />.
        /// </summary>
        /// <remarks>Both lists are cleared first.</remarks>
        public void Answers(AutomationSystem system, SystemInstance instance, SignalTrigger trigger, List<string> blocks,
            List<ToolbarSlot> at)
        {
            if (!trigger.IsWritten)
            {
                Answers(system, instance, trigger.Part, blocks, at);
                return;
            }

            blocks.Clear();
            at.Clear();
            if (trigger.At == null)
                return;

            var sources = SystemInstance.Through(instance, trigger.Sources);
            foreach (var source in sources)
            {
                blocks.Add(source);
                at.Add(trigger.At);
            }
        }

        /// <summary>
        /// What answers trigger <paramref name="trigger" /> of <paramref name="system" /> as <paramref name="instance" />
        /// runs it: the blocks, in <paramref name="blocks" />, and the slot on each, in <paramref name="at" />.
        /// </summary>
        /// <remarks>Both lists are cleared first.</remarks>
        public void Answers(AutomationSystem system, SystemInstance instance, string trigger, List<string> blocks,
            List<ToolbarSlot> at)
        {
            blocks.Clear();
            at.Clear();

            if (system.IsBlueprint)
            {
                var filled = instance?.FilledWith(trigger);
                var positions = instance?.Positions(trigger);
                for (var i = 0; filled != null && positions != null && i < filled.Count && i < positions.Count; i++)
                {
                    blocks.Add(filled[i]);
                    at.Add(positions[i]);
                }

                return;
            }

            var declared = FindSignalRef(trigger);
            if (declared == null)
                return;

            for (var i = 0; i < declared.Blocks.Count; i++)
            {
                blocks.Add(declared.Blocks[i]);
                at.Add(declared.At);
            }
        }

        /// <summary>The name <paramref name="reference" /> was last known by on the construct, or the reference itself where none was recorded.</summary>
        public string Called(string reference, Values values)
        {
            var found = FindRef(reference);
            if (found == null)
                return reference;

            foreach (var key in found.Keys)
            {
                string text;
                if (key.Cells == null && values.TryText(key.Kind, key.Value, out text))
                    return text;
            }

            return reference;
        }

        /// <summary>The block variable called <paramref name="name" />, or null.</summary>
        public EntityRef FindRef(string name)
        {
            EntityRef found;
            return _refsByName.TryGetValue(name, out found) ? found : null;
        }

        /// <summary>
        /// A new system, named, running on this construct's own blocks.
        /// </summary>
        /// <param name="numbered">The default name, with <c>{number}</c> where the number goes.</param>
        public AutomationSystem AddSystem(string numbered = "Logic system {number}")
        {
            var system = new AutomationSystem(_nextSystemId++, Guid.NewGuid());
            system.Reshaped = Reshape;
            system.Started = Start;
            system.Switched = Switch;
            system.Name = FreeName(numbered);
            _systems.Add(system);
            return system;
        }

        /// <summary>
        /// The first <paramref name="numbered" /> nobody is using, counting on from the systems there are.
        /// </summary>
        string FreeName(string numbered)
        {
            for (var number = _systems.Count + 1; ; number++)
            {
                var wanted = numbered.Replace("{number}", number.ToString(CultureInfo.InvariantCulture));

                var taken = false;
                for (var i = 0; i < _systems.Count && !taken; i++)
                    taken = _systems[i].Name == wanted;

                if (!taken)
                    return wanted;
            }
        }

        /// <summary>
        /// Puts back a system that was saved, by what it is.
        /// </summary>
        public AutomationSystem Restore(Guid identity, string space, string name)
        {
            var system = new AutomationSystem(_nextSystemId++, identity);
            system.Reshaped = Reshape;
            system.Started = Start;
            system.Switched = Switch;
            system.Namespace = space ?? "";
            system.Name = name ?? "";
            _systems.Add(system);
            return system;
        }

        /// <summary>Puts back a sequence that was saved, keeping the id it had.</summary>
        public Sequence Restore(AutomationSystem system, int number, Trigger trigger)
        {
            var sequence = new Sequence(_nextSequenceId++);
            sequence.Number = number;
            _sequences.Add(sequence);
            system.Add(sequence);

            if (trigger != null)
            {
                trigger.Does = new Run(number);
                system.AddTrigger(trigger);
            }

            return sequence;
        }

        /// <summary>The logic system numbered <paramref name="id" />, or null where there is none.</summary>
        public AutomationSystem FindSystem(int id)
        {
            foreach (var system in _systems)
            {
                if (system.Id == id)
                    return system;
            }
            return null;
        }

        /// <summary>The system with that identity, or null.</summary>
        public AutomationSystem FindSystem(Guid identity)
        {
            foreach (var system in _systems)
            {
                if (system.Identity == identity)
                    return system;
            }
            return null;
        }

        /// <summary>The logic system made from the stored blueprint <paramref name="origin" />, or null.</summary>
        public AutomationSystem MadeFrom(Guid origin)
        {
            foreach (var system in _systems)
            {
                if (system.Origin == origin)
                    return system;
            }
            return null;
        }

        /// <summary>The system called this, by this namespace, or null.</summary>
        public AutomationSystem FindSystem(string space, string name)
        {
            foreach (var system in _systems)
            {
                if (system.Is(space, name))
                    return system;
            }
            return null;
        }

        /// <summary>
        /// Adds a sequence to a system, making one if none was given.
        /// </summary>
        public Sequence AddSequence(Trigger trigger)
        {
            return AddSequence(null, trigger);
        }

        /// <summary>A new sequence in <paramref name="system" /> that nothing starts yet.</summary>
        public Sequence AddSequence(AutomationSystem system)
        {
            return AddSequence(system, null);
        }

        /// <summary>A new sequence in <paramref name="system" />, or in the last logic system where it is null, started by <paramref name="trigger" />.</summary>
        public Sequence AddSequence(AutomationSystem system, Trigger trigger)
        {
            if (system == null)
                system = _systems.Count > 0 ? _systems[_systems.Count - 1] : AddSystem();

            var sequence = new Sequence(_nextSequenceId++);
            sequence.Number = system.NextSequenceNumber();
            _sequences.Add(sequence);
            system.Add(sequence);

            if (trigger != null)
            {
                trigger.Does = new Run(sequence.Number);
                system.AddTrigger(trigger);
            }

            Rewire();
            return sequence;
        }

        /// <summary>
        /// Another sequence in the same system with the same steps, waiting to be told what starts it.
        /// </summary>
        /// <remarks>The copy has no trigger and is switched off.</remarks>
        public Sequence Duplicate(Sequence sequence)
        {
            var copy = AddSequence(sequence.System);
            copy.Annotation = sequence.Annotation;

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = Copied(steps[i]);

                // A kind with no copy is left out, never shared; the test EveryKindOfStepIsCopied catches a new kind.
                if (step != null)
                    copy.Steps.Add(step);
            }

            return copy;
        }

        /// <summary>
        /// <paramref name="trigger" /> again, doing a copy of what it does, deep enough that editing the copy cannot reach the
        /// original; started from the slot <paramref name="from" /> in place of what the original starts from, where given.
        /// </summary>
        public static Trigger Copied(Trigger trigger, string from)
        {
            Trigger copy;
            var every = trigger as EveryTrigger;
            var when = trigger as WhenTrigger;
            if (every != null)
                copy = new EveryTrigger(every.Interval);
            else if (when != null)
            {
                var made = new WhenTrigger(when.EventId);
                made.Quantifier = when.Quantifier;
                made.Relation = when.Relation;
                made.Value = when.Value;
                made.Interval = when.Interval;
                made.Provider = when.Provider;
                for (var i = 0; i < when.Arguments.Count; i++)
                    made.Arguments.Add(when.Arguments[i]);

                copy = made;
            }
            else
            {
                var signal = (SignalTrigger)trigger;
                var made = new SignalTrigger(signal.EventId);
                made.Provider = signal.Provider;
                made.At = signal.At;
                copy = made;
            }

            var moment = trigger as MomentTrigger;
            if (moment != null)
            {
                var sources = ((MomentTrigger)copy).Sources;
                if (from != null)
                    sources.Add(from);
                else
                {
                    for (var i = 0; i < moment.Sources.Count; i++)
                        sources.Add(moment.Sources[i]);
                }
            }

            copy.Annotation = trigger.Annotation;
            copy.Does = Copied(trigger.Does);
            return copy;
        }

        /// <summary>One step again, deep enough that editing the copy cannot reach the original.</summary>
        public static Step Copied(Step step)
        {
            var act = step as Act;
            if (act != null)
            {
                var copy = new Act(act.ActionId);
                copy.Form = act.Form;
                copy.Provider = act.Provider;
                for (var i = 0; i < act.Targets.Count; i++)
                    copy.Targets.Add(act.Targets[i]);
                for (var i = 0; i < act.Arguments.Count; i++)
                    copy.Arguments.Add(act.Arguments[i].Copied);
                return Annotated(copy, step);
            }

            var wait = step as Wait;
            if (wait != null)
                return Annotated(new Wait(wait.Ticks), step);

            var run = step as Run;
            if (run != null)
                return Annotated(new Run(run.Number, run.Stops), step);

            if (step is Await)
                return Annotated(new Await(), step);

            var abort = step as Abort;
            if (abort != null)
                return Annotated(new Abort { Message = abort.Message, Level = abort.Level }, step);

            var opens = step as If;
            if (opens != null)
                return Annotated(new If(Copied(opens.Test)), step);

            var branch = step as Else;
            if (branch != null)
                return Annotated(new Else(Copied(branch.Test)), step);

            if (step is EndIf)
                return Annotated(new EndIf(), step);

            var sets = step as SetState;
            if (sets != null)
                return Annotated(new SetState { State = sets.State, Value = sets.Value, From = sets.From }, step);

            return null;
        }

        /// <summary>A test again, deep enough that editing the copy cannot reach the original, or null for none.</summary>
        static BranchTest Copied(BranchTest test)
        {
            if (test == null)
                return null;

            var compared = test as Comparison;
            if (compared != null)
                return Copied(compared);

            return ((StateTest)test).Copy();
        }

        /// <summary>A comparison again, deep enough that editing the copy cannot reach the original.</summary>
        static Comparison Copied(Comparison test)
        {
            var copy = new Comparison();
            copy.MemberId = test.MemberId;
            copy.Provider = test.Provider;
            copy.Quantifier = test.Quantifier;
            copy.Relation = test.Relation;
            copy.Value = test.Value;
            for (var i = 0; i < test.Targets.Count; i++)
                copy.Targets.Add(test.Targets[i]);

            for (var i = 0; i < test.Arguments.Count; i++)
                copy.Arguments.Add(test.Arguments[i].Copied);

            return copy;
        }

        static Step Annotated(Step made, Step from)
        {
            made.Annotation = from.Annotation;
            return made;
        }

        /// <summary>Takes <paramref name="sequence" /> out of the program, with everything that started it.</summary>
        public void RemoveSequence(Sequence sequence)
        {
            _sequences.Remove(sequence);
            foreach (var system in _systems)
            {
                if (system.Remove(sequence))
                    system.DropTriggersFor(sequence.Number);
            }

            Rewire();
        }

        /// <summary>Empties it of every system and every block it declared.</summary>
        public void Discard()
        {
            for (var i = _systems.Count - 1; i >= 0; i--)
                RemoveSystem(_systems[i]);

            _refs.Clear();
            _signalRefs.Clear();
            _refsByName.Clear();
            _signalRefsByName.Clear();
            Annotation = "";
        }

        /// <summary>Takes <paramref name="system" /> out of the program, with its sequences, its triggers and every one of it, and says whether it was here.</summary>
        public bool RemoveSystem(AutomationSystem system)
        {
            if (!_systems.Remove(system))
                return false;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _sequences.Remove(sequences[i]);

            system.Reshaped = null;
            system.Started = null;
            system.Switched = null;
            Reshape();
            Rewire();
            return true;
        }

        /// <summary>Puts a removed system back where it stood, <paramref name="at" />, as the system it was, with its sequences.</summary>
        public void PutBack(AutomationSystem system, int at)
        {
            if (system == null || _systems.Contains(system))
                return;

            _systems.Insert(at < 0 || at > _systems.Count ? _systems.Count : at, system);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (!_sequences.Contains(sequences[i]))
                    _sequences.Add(sequences[i]);
            }

            system.Reshaped = Reshape;
            system.Started = Start;
            system.Switched = Switch;
            Reshape();
            Rewire();
        }

        /// <summary>Puts a removed sequence back into <paramref name="system" />, where it stood among its sequences, <paramref name="at" />, as the sequence it was.</summary>
        public void PutBack(AutomationSystem system, Sequence sequence, int at)
        {
            if (sequence == null || _sequences.Contains(sequence))
                return;

            _sequences.Add(sequence);
            system.PutBack(sequence, at);
            Rewire();
        }

        /// <summary>The sequence numbered <paramref name="id" />, or null where there is none.</summary>
        public Sequence FindSequence(int id)
        {
            foreach (var sequence in _sequences)
            {
                if (sequence.Id == id)
                    return sequence;
            }
            return null;
        }

        /// <summary>Whether <paramref name="sequence" /> names the variable or slot <paramref name="name" />, in what starts it or in a step.</summary>
        public static bool References(Sequence sequence, string name)
        {
            var moment = sequence.Trigger as MomentTrigger;
            if (moment != null && moment.Sources.Contains(name))
                return true;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                if (sequence.Steps[i].Targets.Contains(name))
                    return true;
            }
            return false;
        }
    }
}
