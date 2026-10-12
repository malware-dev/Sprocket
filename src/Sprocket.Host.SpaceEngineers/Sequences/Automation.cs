using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Everything one Sprocket unit is running.
    /// </summary>
    /// <remarks>
    /// Sequences are the only thing in here, and that is a deliberate ceiling.
    /// Sprocket is an automation system, not a collection of finished automations, so
    /// anything expressible as a trigger plus steps does not get to become a
    /// primitive of its own.
    ///
    /// A worked example: "the doors must never both be open" looks like it wants a
    /// dedicated concept, and it does not. Two sequences say it - lock the outer door
    /// when the inner one opens, unlock it when the inner one shuts - and they say it
    /// better, because locking is a real action on the real world and therefore holds
    /// against a player operating the door by hand. A built-in rule could only ever
    /// have refused Sprocket's own actions.
    /// </remarks>
    public sealed class Automation
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<EntityRef> _refs = new List<EntityRef>();
        readonly List<SignalRef> _signalRefs = new List<SignalRef>();

        // Every name in a program is looked up here while it is read, so a scan of the lists would make reading
        // quadratic in the size of the program.
        readonly Dictionary<string, EntityRef> _refsByName = new Dictionary<string, EntityRef>();
        readonly Dictionary<string, SignalRef> _signalRefsByName = new Dictionary<string, SignalRef>();

        int _nextSequenceId = 1;
        int _nextSystemId = 1;

        /// <summary>
        /// Every sequence, flat, in the order they were made.
        /// </summary>
        /// <remarks>
        /// What the runtime iterates. Sequences also belong to a system, and the two views
        /// are kept in step here rather than by flattening on demand - the runtime asks for
        /// this on every tick and must not be given a freshly built list each time.
        /// </remarks>
        public IReadOnlyList<Sequence> Sequences => _sequences;

        /// <summary>The systems, which is what the editor opens onto.</summary>
        public IReadOnlyList<AutomationSystem> Systems => _systems;

        /// <summary>
        /// Whether this is all of the program that was saved.
        /// </summary>
        /// <remarks>
        /// False means something in the script could not be read, and the one thing that must follow
        /// is that <b>nothing writes this program back over the save it came from</b>. Saving runs
        /// whenever the program might have changed, so a half-read load plus any edit at all would
        /// write the damage over the only copy - and it is the only copy, because the script is the
        /// program rather than a view of it.
        /// <para>
        /// It is here rather than in whatever did the reading because it is a fact about this program,
        /// and every host that saves has to honour it. What was wrong is a separate answer, from
        /// <see cref="Storage.ProgramText.Load(string, Automation, Values, out string)" />, and it stays out of the model: core has no words.
        /// </para>
        /// </remarks>
        public bool Intact { get; private set; } = true;

        /// <summary>
        /// What would not read, in the words of whatever read it, or empty.
        /// </summary>
        /// <remarks>
        /// Kept so a screen can say it. A program that was read from something unreadable has to be
        /// able to tell the person in front of it, because the alternative is what happened before this
        /// existed: they build for an hour on a block that was never going to keep any of it.
        /// <para>
        /// The first one only. A save with ten bad lines is one thing that went wrong to the person
        /// reading it, and a screen that listed all ten would bury the sentence that matters.
        /// </para>
        /// </remarks>
        public string Trouble { get; private set; } = "";

        /// <summary>
        /// Whether what was saved came from a version of Sprocket this one does not read at all,
        /// rather than being damaged in places.
        /// </summary>
        /// <remarks>
        /// Either way nothing of it is here: a damaged text is trusted in no part, and an incompatible
        /// one is not read at all. They differ in what somebody is told.
        /// </remarks>
        public bool Incompatible { get; private set; }

        /// <summary>What somebody wrote above the program's first line, or empty.</summary>
        public string Annotation { get; set; } = "";

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
        /// <remarks>
        /// The protection exists so that a half-read load cannot be written over the only copy. Once
        /// the person has replaced it, refusing to write costs them everything they do from then on,
        /// silently, which is far the worse of the two. Whoever replaced it says so here.
        /// <para>
        /// <see cref="Trouble" /> goes with it. What was wrong was wrong with a program this one has
        /// taken the place of, so a screen still saying it would be describing something that is no
        /// longer there.
        /// </para>
        /// </remarks>
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
        /// <remarks>
        /// Declared once rather than repeated, because a claim about which door was meant is worth
        /// mending in one place. Twenty steps that each carried their own copy would be twenty
        /// things to re-point when somebody rebuilds the door.
        /// </remarks>
        public IReadOnlyList<EntityRef> Refs => _refs;

        /// <summary>Every trigger this program names as a variable: a slot on a block's toolbar, which a system starts a step from with <c>whenever</c>.</summary>
        public IReadOnlyList<SignalRef> SignalRefs => _signalRefs;

        /// <summary>
        /// Bumped whenever a declaration is added, removed or re-pointed.
        /// </summary>
        /// <remarks>
        /// The other half of <see cref="World.GridWorld.Revision" />. Between them they say whether
        /// what a name points at could possibly have moved, which is what lets the runtime look at
        /// nothing at all on an ordinary tick.
        /// </remarks>
        public int Revision { get; private set; }

        /// <summary>
        /// Bumped whenever somebody has said what a trigger is.
        /// </summary>
        /// <remarks>
        /// Not the same question as <see cref="Revision" />, which is about declarations moving, and
        /// not the same as "the program changed" either. A host may have to arrange something outside
        /// the program for a trigger to work - a toolbar slot, in Space Engineers - and it needs to
        /// know when to look again.
        /// <para>
        /// <b>Bumped even when nothing changed.</b> Somebody who opens a trigger and picks the same
        /// button has asserted what they want, and that assertion is the whole of how a trigger gets
        /// repaired: the model is quite right that nothing changed, and the arrangement outside it is
        /// exactly what did.
        /// </para>
        /// </remarks>
        public int Wiring { get; private set; }

        /// <summary>Bumped whenever somebody changes what the program says.</summary>
        /// <remarks>
        /// What a runner holds what it worked out from the program against, alongside what the
        /// program's names point at, so a step is worked out once rather than every time it runs.
        /// </remarks>
        public int Edits { get; private set; }

        /// <summary>
        /// Raised whenever a sequence, or one of a system, comes or goes.
        /// </summary>
        /// <remarks>
        /// What the runtime keeps one runner for, so it is told rather than looking: whatever changed
        /// the program - an edit, a step back, a load, and in time a message from somebody editing on
        /// another machine - changed it here.
        /// </remarks>
        public event Action Reshaped;

        /// <summary>Bumped whenever a system is switched on, which is when what it does is checked again for the start it is given.</summary>
        public int Starts { get; private set; }

        void Start()
        {
            Starts++;
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
        }

        /// <summary>Says that the program has been changed, and anything worked out from it is stale.</summary>
        public void Edited()
        {
            Edits++;
        }

        /// <summary>
        /// Names this entity, or hands back the name it already has.
        /// </summary>
        /// <remarks>
        /// Asked while editing, never while running. The same block picked twice is one declaration,
        /// which is what makes re-pointing it later a single act - and the way to tell is to resolve
        /// what is already declared rather than to remember an id, because an id is exactly the thing
        /// that stops being true.
        /// <para>
        /// The variable is named for the block as it is called when it is declared, numbered where another variable has
        /// that name: the name a person recognizes, given once and kept.
        /// </para>
        /// </remarks>
        public EntityRef Declare(GridWorld world, GridEntity entity)
        {
            for (var i = 0; i < _refs.Count; i++)
            {
                int matched;
                var found = world.Match(_refs[i].Keys, out matched);
                if (found != null && found.Id == entity.Id && _refs[i].KindId == entity.Kind.Id)
                    return _refs[i];
            }

            var called = Naming.Clean(entity.Name);
            var declared = new EntityRef(Free(called.Length > 0 ? called : entity.Kind.DisplayName), entity.Kind.Id);
            world.Identify(entity, declared.Keys);
            return Restore(declared);
        }

        /// <summary>Puts back a declaration read from storage.</summary>
        public EntityRef Restore(EntityRef declared)
        {
            _refs.Add(declared);
            if (!_refsByName.ContainsKey(declared.Name))
                _refsByName.Add(declared.Name, declared);

            Revision++;
            Wiring++;
            return declared;
        }

        /// <summary>
        /// The variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names and no other, declared
        /// where there is none, named <paramref name="called" /> as a block's variable is.
        /// </summary>
        /// <remarks>One on several blocks is left alone: a trigger on it would be sent from all of them.</remarks>
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
            Wiring++;
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
            for (var i = 0; i < _signalRefs.Count; i++)
            {
                if (_signalRefs[i].Holds(block, at))
                    return _signalRefs[i];
            }

            return null;
        }

        /// <summary>The signal variable holding slot <paramref name="at" /> on the block <paramref name="block" /> names and no other, or null.</summary>
        public SignalRef FindSignalRefOnly(string block, ToolbarSlot at)
        {
            for (var i = 0; i < _signalRefs.Count; i++)
            {
                if (_signalRefs[i].HoldsOnly(block, at))
                    return _signalRefs[i];
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
                name = called + " " + n;

            return name;
        }

        /// <summary>
        /// The toolbar slots that send the signal <paramref name="trigger" /> of <paramref name="system" /> starts on, as
        /// <paramref name="instance" /> runs it: the blocks, in <paramref name="blocks" />, and the slot on each, in
        /// <paramref name="at" />.
        /// </summary>
        /// <remarks>
        /// A signal written in place on a toolbar slot is sent from that slot on each of its blocks; a named one is answered
        /// as its part or variable is; a block's own change is sent from no toolbar, and leaves both lists empty.
        /// </remarks>
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
            for (var i = 0; i < sources.Count; i++)
            {
                blocks.Add(sources[i]);
                at.Add(trigger.At);
            }
        }

        /// <summary>
        /// What answers trigger <paramref name="trigger" /> of <paramref name="system" /> as <paramref name="instance" />
        /// runs it: the blocks, in <paramref name="blocks" />, and the slot on each, in <paramref name="at" />.
        /// </summary>
        /// <remarks>
        /// An instance answers a blueprint's signal slot; a system's signal is a variable of the program's own. Both
        /// lists are cleared first, and stay empty where nothing answers it.
        /// </remarks>
        public void Answers(AutomationSystem system, SystemInstance instance, string trigger, List<string> blocks,
            List<ToolbarSlot> at)
        {
            blocks.Clear();
            at.Clear();

            if (system.IsBlueprint)
            {
                var filled = instance == null ? null : instance.FilledWith(trigger);
                var positions = instance == null ? null : instance.Positions(trigger);
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

            for (var i = 0; i < found.Keys.Count; i++)
            {
                string text;
                if (found.Keys[i].Cells == null && values.TryText(found.Keys[i].Kind, found.Keys[i].Value, out text))
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
        /// <remarks>
        /// <b>Named from birth, because a name is not optional.</b> The format has no spelling for a
        /// system without one, so an unnamed system is a program that cannot be written down: it
        /// saves as an empty name and refuses to read back. Whoever made it says what it is called
        /// straight afterwards - the editor asks before it gets here - and this is what the ones
        /// nobody names are left with.
        /// </remarks>
        /// <param name="numbered">What the ones nobody names are called, with <c>{number}</c> where the number goes: in the player's language where a person made it.</param>
        public AutomationSystem AddSystem(string numbered = "Logic system {number}")
        {
            var system = new AutomationSystem(_nextSystemId++, Guid.NewGuid());
            system.Reshaped = Reshape;
            system.Started = Start;
            system.Name = FreeName(numbered);
            _systems.Add(system);
            return system;
        }

        /// <summary>
        /// The first <paramref name="numbered" /> nobody is using, counting on from the systems there are.
        /// </summary>
        /// <remarks>
        /// So the number says which one it is: beside "Airlock" a new one is "Logic system 2", never "Logic system 1". The
        /// same rule a new sequence is named by.
        /// </remarks>
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
        /// <remarks>
        /// The system is its identity, so that comes back with its name; its session handle is new, since
        /// nothing outside this session ever held one.
        /// </remarks>
        public AutomationSystem Restore(Guid identity, string space, string name)
        {
            var system = new AutomationSystem(_nextSystemId++, identity);
            system.Reshaped = Reshape;
            system.Started = Start;
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
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Id == id)
                    return _systems[i];
            }
            return null;
        }

        /// <summary>The system with that identity, or null.</summary>
        public AutomationSystem FindSystem(Guid identity)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Identity == identity)
                    return _systems[i];
            }
            return null;
        }

        /// <summary>The logic system made from the stored blueprint <paramref name="origin" />, or null.</summary>
        public AutomationSystem MadeFrom(Guid origin)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Origin == origin)
                    return _systems[i];
            }
            return null;
        }

        /// <summary>The system called this, by this namespace, or null.</summary>
        public AutomationSystem FindSystem(string space, string name)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Is(space, name))
                    return _systems[i];
            }
            return null;
        }

        /// <summary>
        /// Adds a sequence to a system, making one if none was given.
        /// </summary>
        /// <remarks>
        /// Nothing is declared before it is used, so somebody who starts building gets a
        /// system without having asked for one and without being made to name it first.
        /// </remarks>
        public Sequence AddSequence(Trigger trigger)
        {
            return AddSequence(null, trigger);
        }

        /// <summary>A sequence nothing starts yet, which is what somebody halfway through building has.</summary>
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

            Wiring++;
            return sequence;
        }

        /// <summary>
        /// Another sequence in the same system with the same steps, waiting to be told what starts it.
        /// </summary>
        /// <remarks>
        /// A direct copy and nothing cleverer. The steps point at the same blocks, and whoever asked
        /// for it changes whatever they wanted different from there.
        /// <para>
        /// No trigger, and switched off. A toolbar slot holds one claim on Sprocket's action, so a copy
        /// arriving with the trigger it was copied from would be a second claim on a slot that is
        /// already taken - and two live sequences on one button cycle an airlock twice with nothing on
        /// screen saying why.
        /// </para>
        /// </remarks>
        public Sequence Duplicate(Sequence sequence)
        {
            var copy = AddSequence(sequence.System);
            copy.Annotation = sequence.Annotation;

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var step = Copied(steps[i]);

                // A kind nothing here knows how to copy is left out rather than shared, because two
                // sequences holding one step object would edit each other. EveryKindOfStepIsCopied
                // fails the moment a fifth kind exists, which is where that is caught.
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
                    copy.Arguments.Add(act.Arguments[i]);
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

            var opens = step as If;
            if (opens != null)
                return Annotated(new If(Copied(opens.Test)), step);

            var branch = step as Else;
            if (branch != null)
                return Annotated(new Else(branch.Test == null ? null : Copied(branch.Test)), step);

            if (step is EndIf)
                return Annotated(new EndIf(), step);

            return null;
        }

        /// <summary>A test again, deep enough that editing the copy cannot reach the original.</summary>
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
                copy.Arguments.Add(test.Arguments[i]);

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
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Remove(sequence))
                    _systems[i].DropTriggersFor(sequence.Number);
            }

            Wiring++;
        }

        /// <summary>Empties it of every system and every block it declared.</summary>
        /// <remarks>For a text with an error in it, which is trusted in no part: nothing of it runs.</remarks>
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
        /// <remarks>What it was running stops, and the toolbar slots its triggers claimed are let go of, once the program is looked at again.</remarks>
        public bool RemoveSystem(AutomationSystem system)
        {
            if (!_systems.Remove(system))
                return false;

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                _sequences.Remove(sequences[i]);

            system.Reshaped = null;
            system.Started = null;
            Reshape();
            Wiring++;
            return true;
        }

        /// <summary>Puts a removed system back where it stood, <paramref name="at" />, as the system it was, with its sequences.</summary>
        /// <remarks>For undo: its runners, its pages and the toolbar slots it claims all hold or name the system itself.</remarks>
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
            Reshape();
            Wiring++;
        }

        /// <summary>Puts a removed sequence back into <paramref name="system" />, where it stood among its sequences, <paramref name="at" />, as the sequence it was.</summary>
        /// <remarks>
        /// For undo. Everything that pointed at it named it by number - a toolbar slot, a step that
        /// runs it - so one that came back as a new sequence would come back as a different thing.
        /// </remarks>
        public void PutBack(AutomationSystem system, Sequence sequence, int at)
        {
            if (sequence == null || _sequences.Contains(sequence))
                return;

            _sequences.Add(sequence);
            system.PutBack(sequence, at);
            Wiring++;
        }

        /// <summary>The sequence numbered <paramref name="id" />, or null where there is none.</summary>
        public Sequence FindSequence(int id)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (_sequences[i].Id == id)
                    return _sequences[i];
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
