using System.Collections.Generic;
using Reactor.World;

namespace Reactor.Sequences
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
        /// <see cref="Storage.ProgramText.Load" />, and it stays out of the model: core has no words.
        /// </para>
        /// </remarks>
        public bool Intact { get; private set; } = true;

        /// <summary>Says this program is not all of what was saved, and cannot be saved over it.</summary>
        public void Damaged()
        {
            Intact = false;
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
        /// The other half of <see cref="World.IWorld.Revision" />. Between them they say whether
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
        public EntityRef Declare(IWorld world, IEntity entity)
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
        /// A new system, with one of it already on the ship.
        /// </summary>
        /// <remarks>
        /// Making a system and then being asked to place one would be two steps for one
        /// thought. Somebody who builds a thing has one of that thing.
        /// </remarks>
        public AutomationSystem AddSystem()
        {
            var system = new AutomationSystem(_nextSystemId++);
            system.AddInstance();
            _systems.Add(system);
            return system;
        }

        /// <summary>
        /// Puts back a system that was saved, by what it is.
        /// </summary>
        /// <remarks>
        /// The system is its namespace and name, so that is what comes back; its session handle is new, since
        /// nothing outside this session ever held one.
        ///
        /// No instance is added, unlike <see cref="AddSystem"/>: whatever was saved is what
        /// comes back, and inventing an extra one would multiply systems on every reload.
        /// </remarks>
        public AutomationSystem Restore(string space, string name)
        {
            var system = new AutomationSystem(_nextSystemId++);
            system.Namespace = space ?? "";
            system.Name = name ?? "";
            _systems.Add(system);
            return system;
        }

        /// <summary>Puts back a sequence that was saved, keeping the id it had.</summary>
        public Sequence Restore(AutomationSystem system, int id, Trigger trigger)
        {
            var sequence = new Sequence(id, trigger);
            _sequences.Add(sequence);
            system.Add(sequence);

            if (id >= _nextSequenceId)
                _nextSequenceId = id + 1;

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

        /// <summary>The system that is this namespace and name, or null.</summary>
        public AutomationSystem FindSystem(string space, string name)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                if (_systems[i].Is(space, name))
                    return _systems[i];
            }
            return null;
        }

        /// <summary>The system a sequence belongs to, or null if it somehow belongs to none.</summary>
        public AutomationSystem SystemOf(Sequence sequence)
        {
            for (var i = 0; i < _systems.Count; i++)
            {
                var sequences = _systems[i].Sequences;
                for (var s = 0; s < sequences.Count; s++)
                {
                    if (ReferenceEquals(sequences[s], sequence))
                        return _systems[i];
                }
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

        public Sequence AddSequence(AutomationSystem system, Trigger trigger)
        {
            if (system == null)
                system = _systems.Count > 0 ? _systems[_systems.Count - 1] : AddSystem();

            var sequence = new Sequence(_nextSequenceId++, trigger);
            _sequences.Add(sequence);
            system.Add(sequence);
            Wiring++;
            return sequence;
        }

        public void RemoveSequence(Sequence sequence)
        {
            _sequences.Remove(sequence);
            for (var i = 0; i < _systems.Count; i++)
                _systems[i].Remove(sequence);

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

        /// <summary>Named sequences, which are the ones another trigger can start.</summary>
        public void NamedSequences(List<Sequence> result)
        {
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (_sequences[i].HasName)
                    result.Add(_sequences[i]);
            }
        }

        /// <summary>
        /// How many sequences name this declaration, without allocating.
        /// </summary>
        /// <remarks>
        /// Asked once per listed block on every tick, so it must not produce garbage
        /// - core runs inside the game's update loop.
        /// </remarks>
        public int CountReferencing(string name)
        {
            var count = 0;
            for (var i = 0; i < _sequences.Count; i++)
            {
                if (References(_sequences[i], name))
                    count++;
            }
            return count;
        }

        public static bool References(Sequence sequence, string name)
        {
            var when = sequence.Trigger as WhenTrigger;
            if (when != null && when.Sources.Contains(name))
                return true;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var step = sequence.Steps[i];
                if (step.Targets.Contains(name))
                    return true;

                var gate = step.Gate as OnceGate;
                if (gate != null && gate.Condition.Entities.Contains(name))
                    return true;
            }
            return false;
        }
    }
}
