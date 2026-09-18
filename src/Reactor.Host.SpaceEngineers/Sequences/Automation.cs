using System;
using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// Everything one Reactor unit is running.
    /// </summary>
    /// <remarks>
    /// Sequences are the only thing in here, and that is a deliberate ceiling.
    /// Reactor is an automation system, not a collection of finished automations, so
    /// anything expressible as a trigger plus steps does not get to become a
    /// primitive of its own.
    ///
    /// A worked example: "the doors must never both be open" looks like it wants a
    /// dedicated concept, and it does not. Two sequences say it - lock the outer door
    /// when the inner one opens, unlock it when the inner one shuts - and they say it
    /// better, because locking is a real action on the real world and therefore holds
    /// against a player operating the door by hand. A built-in rule could only ever
    /// have refused Reactor's own actions.
    /// </remarks>
    public sealed class Automation
    {
        readonly List<Sequence> _sequences = new List<Sequence>();
        readonly List<AutomationSystem> _systems = new List<AutomationSystem>();
        readonly List<EntityRef> _refs = new List<EntityRef>();

        int _nextSequenceId = 1;
        int _nextSystemId = 1;
        int _nextRefNumber = 1;

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

        /// <summary>Says this program is not all of what was saved, and cannot be saved over it.</summary>
        public void Damaged(string what)
        {
            Intact = false;

            if (Trouble.Length == 0)
                Trouble = what ?? "";
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
            Trouble = "";
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

        /// <summary>Says that something moved and whatever arranges triggers should look again.</summary>
        public void Rewire()
        {
            Wiring++;
        }

        /// <summary>
        /// Names this entity, or hands back the name it already has.
        /// </summary>
        /// <remarks>
        /// Asked while editing, never while running. The same block picked twice is one declaration,
        /// which is what makes re-pointing it later a single act - and the way to tell is to resolve
        /// what is already declared rather than to remember an id, because an id is exactly the thing
        /// that stops being true.
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

            var declared = new EntityRef("entity" + _nextRefNumber++, entity.Kind.Id);
            world.Identify(entity, declared.Keys);
            _refs.Add(declared);
            Revision++;
            Wiring++;
            return declared;
        }

        /// <summary>
        /// Puts back a declaration read from storage, keeping generated names from colliding with it.
        /// </summary>
        public EntityRef Restore(EntityRef declared)
        {
            _refs.Add(declared);
            Revision++;
            Wiring++;

            var number = Number(declared.Name);
            if (number >= _nextRefNumber)
                _nextRefNumber = number + 1;

            return declared;
        }

        public EntityRef FindRef(string name)
        {
            for (var i = 0; i < _refs.Count; i++)
            {
                if (_refs[i].Name == name)
                    return _refs[i];
            }

            return null;
        }

        /// <summary>The number a generated name ends in, or zero for one somebody wrote.</summary>
        static int Number(string name)
        {
            var digits = 0;
            while (digits < name.Length && name[name.Length - 1 - digits] >= '0'
                   && name[name.Length - 1 - digits] <= '9')
                digits++;

            if (digits == 0 || digits == name.Length)
                return 0;

            var number = 0;
            for (var i = name.Length - digits; i < name.Length; i++)
                number = number * 10 + (name[i] - '0');

            return number;
        }

        /// <summary>
        /// A new system, named, with one of it already on the ship.
        /// </summary>
        /// <remarks>
        /// Making a system and then being asked to place one would be two steps for one
        /// thought. Somebody who builds a thing has one of that thing.
        /// <para>
        /// <b>Named from birth, because a name is not optional.</b> The format has no spelling for a
        /// system without one, so an unnamed system is a program that cannot be written down: it
        /// saves as an empty name and refuses to read back. Whoever made it says what it is called
        /// straight afterwards - the editor asks before it gets here - and this is what the ones
        /// nobody names are left with.
        /// </para>
        /// </remarks>
        public AutomationSystem AddSystem()
        {
            var system = new AutomationSystem(_nextSystemId++, Guid.NewGuid());
            system.Name = FreeName();
            system.AddInstance();
            _systems.Add(system);
            return system;
        }

        /// <summary>
        /// The first <c>System n</c> nobody is using.
        /// </summary>
        /// <remarks>
        /// The first free number rather than a running count, so deleting the second of three and
        /// adding another gives "System 2" back - the same rule a new sequence is named by, and the
        /// same rule the game names a block by.
        /// </remarks>
        string FreeName()
        {
            for (var number = 1; ; number++)
            {
                var wanted = "System " + number;

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
        ///
        /// No instance is added, unlike <see cref="AddSystem"/>: whatever was saved is what
        /// comes back, and inventing an extra one would multiply systems on every reload.
        /// </remarks>
        public AutomationSystem Restore(Guid identity, string space, string name)
        {
            var system = new AutomationSystem(_nextSystemId++, identity);
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
                trigger.Runs = number;
                system.AddTrigger(trigger);
            }

            return sequence;
        }

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
                trigger.Runs = sequence.Number;
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
        /// No trigger, and switched off. A toolbar slot holds one claim on Reactor's action, so a copy
        /// arriving with the trigger it was copied from would be a second claim on a slot that is
        /// already taken - and two live sequences on one button cycle an airlock twice with nothing on
        /// screen saying why.
        /// </para>
        /// </remarks>
        public Sequence Duplicate(Sequence sequence)
        {
            var copy = AddSequence(sequence.System);
            copy.Annotation = sequence.Annotation;
            copy.Enabled = false;

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

        /// <summary>One step again, deep enough that editing the copy cannot reach the original.</summary>
        static Step Copied(Step step)
        {
            var act = step as Act;
            if (act != null)
            {
                var copy = new Act(act.ActionId);
                for (var i = 0; i < act.Targets.Count; i++)
                    copy.Targets.Add(act.Targets[i]);
                return Annotated(copy, step);
            }

            var wait = step as Wait;
            if (wait != null)
                return Annotated(new Wait(wait.Ticks), step);

            var run = step as Run;
            if (run != null)
                return Annotated(new Run(run.Number), step);

            if (step is Await)
                return Annotated(new Await(), step);

            return null;
        }

        static Step Annotated(Step made, Step from)
        {
            made.Annotation = from.Annotation;
            return made;
        }

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

        /// <summary>
        /// Puts a removed sequence back, as the sequence it was.
        /// </summary>
        /// <remarks>
        /// For undo. Everything that pointed at it named it by number - a toolbar slot, a step that
        /// runs it - so one that came back as a new sequence would come back as a different thing.
        /// </remarks>
        public void PutBack(Sequence sequence)
        {
            if (sequence == null || _sequences.Contains(sequence))
                return;

            _sequences.Add(sequence);

            if (sequence.System != null)
                sequence.System.Add(sequence);

            Wiring++;
        }

        public Sequence FindSequence(int id)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (_sequences[i].Id == id)
                    return _sequences[i];
            }
            return null;
        }

        public static bool References(Sequence sequence, string name)
        {
            var moment = sequence.Trigger as MomentTrigger;
            if (moment != null && moment.Sources.Contains(name))
                return true;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act != null && act.Targets.Contains(name))
                    return true;
            }
            return false;
        }
    }
}
