using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>What each act and test of a run comes to on its blocks, worked out once per change to the program or to what its names point at.</summary>
    public sealed class CompiledRun
    {
        readonly Sequence _sequence;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly RunMemory _memory;
        readonly List<string> _actedOn = new List<string>();
        readonly Action _heard;
        readonly List<IDisposable> _listening = new List<IDisposable>();
        CompiledAct[] _steps = new CompiledAct[0];
        CompiledTest[] _tests = new CompiledTest[0];
        int _boundAt = -1;
        int _editedAt = -1;

        // How many times what every act comes to has been worked out afresh; the first takes up what the world remembered of each
        // block waited on, and every later one lets it go.
        int _generation;

        /// <summary>
        /// The workings of <paramref name="sequence" /> running for <paramref name="instance" />, owned by <paramref name="owner" />,
        /// remembering what is waited on in <paramref name="memory" />, and telling <paramref name="heard" /> when a block that
        /// announces getting where it was told may have.
        /// </summary>
        public CompiledRun(Sequence sequence, SystemInstance instance, AutomationRunner owner, RunMemory memory, Action heard)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _memory = memory;
            _heard = heard;
        }

        /// <summary>How many times an act has been worked out.</summary>
        public int Compilations { get; private set; }

        /// <summary>
        /// What the act at <paramref name="step" /> comes to, worked out the first time it is asked after anything it rests
        /// on has changed.
        /// </summary>
        /// <remarks>
        /// Asked on the tick every act happens; a tick that repeats a step reads an array. A block whose action takes a value
        /// as some other kind is left out, and the fault says so.
        /// </remarks>
        public CompiledAct Compile(int step, Bindings bindings)
        {
            Fresh(bindings);

            var compiled = _steps[step];
            if (compiled != null)
                return compiled;

            Compilations++;
            var act = (Act)_sequence.Steps[step];
            compiled = new CompiledAct();
            compiled.Problem = Needed(act.Targets, SequenceProblem.MissingTarget, out compiled.Which, bindings);
            var given = Reached(act.Arguments, compiled, bindings);
            var acting = ActedOn(act.Targets, bindings);
            foreach (var name in acting)
            {
                var entity = bindings.Entity(name);
                if (entity == null)
                    continue;

                var action = act.Names(entity.Kind.Actions);
                if (action == null)
                    continue;

                // Each thing a block is held as is acted on once: two screens of one block are two things.
                FacetsHeld(action.Type, act.Targets, name);
                for (var f = 0; f < _facetsHeld.Count; f++)
                {
                    // A block lacking the facet its slot holds is left out, and the fault names it.
                    var facet = _facetsHeld[f];
                    if (action.Type != null && facet > 0 && action.Type.Find(entity.Kind, facet) == null)
                        continue;

                    var shared = Shared(compiled, action, given, facet);
                    if (shared == null)
                        continue;

                    compiled.Acts.Add(new ActTarget
                    {
                        Entity = entity,
                        Action = action,
                        Facet = facet,
                        Arguments = shared.Arguments,
                        Bound = shared.Bound,
                        Order = entity.Prepare(action, shared.Bound, shared.Arguments),
                        Stored = StoredAct(step, compiled.Acts.Count, action.OwnAction != null),
                        Heard = action.ArrivalAnnounced && Listen(entity)
                    });
                }
            }

            _steps[step] = compiled;
            if (_memory.Outstanding.Contains(step))
                Listed(compiled);

            return compiled;
        }

        /// <summary>What the test at <paramref name="step" /> comes to, worked out the first time it is asked after anything it rests on has changed.</summary>
        /// <remarks>A block that has no such member, or cannot compare it as the test does, is left out: the step is at fault, and the fault says so.</remarks>
        public CompiledTest Test(int step, Comparison test, Bindings bindings)
        {
            Fresh(bindings);

            var tested = _tests[step];
            if (tested != null)
                return tested;

            tested = new CompiledTest();
            tested.Problem = Needed(test.Targets, SequenceProblem.MissingGate, out tested.Which, bindings);
            tested.Value = Settled(test.Value, ref tested.Problem, ref tested.Which);
            var given = test.Arguments;
            for (var i = 0; i < given.Count; i++)
                Settled(given[i], ref tested.Problem, ref tested.Which);

            var arguments = SystemInstance.Settled(_instance, given);
            var reading = ActedOn(test.Targets, bindings);
            foreach (var name in reading)
            {
                // A block lacking the facet its slot holds is left out, and the fault names it.
                var entity = bindings.Entity(name);
                var member = entity == null ? null : test.Names(entity.Kind.Actions);
                FacetsHeld(member?.Type, test.Targets, name);
                for (var f = 0; f < _facetsHeld.Count; f++)
                {
                    var facet = _facetsHeld[f];
                    if (facet > 0 && member?.Type != null && member.Type.Find(entity.Kind, facet) == null)
                        continue;

                    var probe = Probe.Of(entity, test.Key, arguments,
                        test.Quantifier, test.Relation, tested.Value, null, _owner.Clock, facet);

                    if (probe != null && !probe.PassedOver)
                        tested.Reads.Add(probe);
                }
            }

            _tests[step] = tested;
            return tested;
        }

        /// <summary>Forgets what every act and test came to, where the program or what its names point at has changed since.</summary>
        void Fresh(Bindings bindings)
        {
            var bound = bindings.Revision;
            var edited = _owner.Program.Edits;
            if (bound == _boundAt && edited == _editedAt)
                return;

            Deafen();
            var count = _sequence.Steps.Count;
            _steps = new CompiledAct[count];
            _tests = new CompiledTest[count];
            _boundAt = bound;
            _editedAt = edited;

            // Worked out again after something changed: the blocks may be other blocks, so nothing remembered of them stands.
            _generation++;
            if (_generation > 1)
                _memory.Acts.Clear();
        }

        /// <summary>
        /// The first block a step needs and cannot have, by its reference: a slot left empty, a block gone, or one possibly
        /// swapped for another; with why, where a block gone is <paramref name="lost" />.
        /// </summary>
        /// <remarks>A slot that may be left empty is skipped, and so is a block gone from one.</remarks>
        SequenceProblem Needed(IList<string> targets, SequenceProblem lost, out string which, Bindings bindings)
        {
            which = null;
            var system = _sequence.System;
            for (var s = 0; s < targets.Count; s++)
            {
                var slot = targets[s];
                var part = system?.FindPart(slot);
                var filled = _instance?.FilledWith(slot);
                if (part != null && _instance != null && (filled == null || filled.Count == 0))
                {
                    if (part.Optional)
                        continue;

                    which = slot;
                    return SequenceProblem.EmptySlot;
                }

                var count = filled?.Count ?? 1;
                for (var n = 0; n < count; n++)
                {
                    var name = filled == null ? slot : filled[n];
                    var points = bindings.State(name);
                    if (points == RefState.Bound || (points == RefState.Lost && part != null && part.Optional))
                        continue;

                    which = name;
                    return points == RefState.Lost ? lost : SequenceProblem.Adrift;
                }
            }

            return SequenceProblem.None;
        }

        /// <summary>
        /// The values as <paramref name="action" /> takes them, and what the action makes of them, or null where it cannot
        /// take them.
        /// </summary>
        /// <remarks>
        /// Shared between the blocks of one act that do the same thing, and for a member of a type the host provides, only
        /// as far as they share a facet.
        /// </remarks>
        static ActTarget Shared(CompiledAct compiled, EntityAction action, IList<Literal> given, int facet)
        {
            foreach (var acted in compiled.Acts)
            {
                if (acted.Action == action && acted.Facet == facet)
                    return acted;
            }

            var converted = new List<Literal>(given.Count);
            if (!action.Converts(given, converted))
                return null;

            return new ActTarget
            {
                Arguments = converted,
                Bound = action.OwnAction?.Bind(action.Type == null ? converted : HostType.Arguments(facet, converted))
            };
        }

        /// <summary>Fills what the facets held are for the member of <paramref name="type" /> on <paramref name="block" /> in <paramref name="targets" />: one 0 where it is the block itself that is held.</summary>
        void FacetsHeld(HostType type, IList<string> targets, string block)
        {
            _facetsHeld.Clear();
            if (type != null)
                _instance?.FacetsOf(targets, block, _facetsHeld);

            if (_facetsHeld.Count == 0)
                _facetsHeld.Add(0);
        }

        /// <summary>
        /// <paramref name="given" /> with each list of slots holding what those slots hold now, and any slot a step needs and cannot have said on
        /// <paramref name="compiled" />, where it has no problem already.
        /// </summary>
        IList<Literal> Reached(IList<Literal> given, CompiledAct compiled, Bindings bindings)
        {
            List<Literal> reached = null;
            for (var i = 0; i < given.Count; i++)
            {
                if (given[i].IsSetting)
                {
                    if (reached == null)
                        reached = new List<Literal>(given);

                    reached[i] = Settled(given[i], ref compiled.Problem, ref compiled.Which);
                    continue;
                }

                if (given[i].Kind != ValueKind.Slots)
                    continue;

                if (reached == null)
                    reached = new List<Literal>(given);

                if (compiled.Problem == SequenceProblem.None)
                    compiled.Problem = Needed(given[i].Names, SequenceProblem.MissingTarget, out compiled.Which, bindings);

                reached[i] = Literal.Reaching(given[i].Names, Things(given[i].Names, bindings));
            }

            return reached ?? given;
        }

        /// <summary>
        /// <paramref name="value" /> as this installation has it, a setting given its value; where it gives that setting none,
        /// the setting as it is, and why said in <paramref name="problem" /> and <paramref name="which" /> where they say nothing yet.
        /// </summary>
        Literal Settled(Literal value, ref SequenceProblem problem, ref string which)
        {
            Literal settled;
            if (SystemInstance.Settled(_instance, value, out settled) || problem != SequenceProblem.None)
                return settled;

            problem = SequenceSummary.Unanswered(_instance, value.Text);
            which = value.Text;
            return settled;
        }

        /// <summary>Everything the slots <paramref name="slots" /> hold, each thing once, in the order they hold them.</summary>
        List<HeldThing> Things(IList<string> slots, Bindings bindings)
        {
            var things = new List<HeldThing>();
            var names = new List<string>();
            if (_instance == null)
                names.AddRange(slots);
            else
                _instance.Resolve(slots, names);

            bindings.Distinct(names);
            var facets = new List<int>();
            foreach (var name in names)
            {
                var entity = bindings.Entity(name);
                if (entity == null)
                    continue;

                _instance?.FacetsOf(slots, name, facets);
                if (facets.Count == 0)
                    things.Add(new HeldThing { Block = entity.Block });

                foreach (var facet in facets)
                    things.Add(new HeldThing { Block = entity.Block, Facet = facet });

                facets.Clear();
            }

            return things;
        }

        /// <summary>Listens to <paramref name="entity" />, which announces getting where it was told, and answers whether it can.</summary>
        bool Listen(GridEntity entity)
        {
            var listening = entity.Listen(_heard);
            if (listening == null)
                return false;

            _listening.Add(listening);
            return true;
        }

        /// <summary>Stops listening to every block what is worked out listens to.</summary>
        public void Deafen()
        {
            foreach (var listening in _listening)
                listening.Dispose();

            _listening.Clear();
        }

        /// <summary>Has every block of the acts waited on forget what it said it was done with, reading only what is worked out already.</summary>
        public void Forget()
        {
            foreach (var step in _memory.Outstanding)
            {
                var compiled = step < _steps.Length ? _steps[step] : null;
                for (var t = 0; compiled != null && t < compiled.Acts.Count; t++)
                    compiled.Acts[t].Done = false;
            }
        }

        readonly List<int> _facetsHeld = new List<int>();

        List<string> ActedOn(IList<string> slots, Bindings bindings)
        {
            _actedOn.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < slots.Count; i++)
                    _actedOn.Add(slots[i]);
            }
            else
            {
                _instance.Resolve(slots, _actedOn);
            }
            bindings.Distinct(_actedOn);
            return _actedOn;
        }

        /// <summary>Keeps what is remembered of each block of <paramref name="compiled" /> in what the world saves, where it is not kept already.</summary>
        public void Listed(CompiledAct compiled)
        {
            foreach (var acted in compiled.Acts)
            {
                if (!_memory.Acts.Contains(acted.Stored))
                    _memory.Acts.Add(acted.Stored);
            }
        }

        /// <summary>
        /// What is remembered of the block at <paramref name="index" /> of the act at <paramref name="step" />: what the world stored
        /// where this is the first working out since a load, and a fresh one otherwise, remembering what the block does where
        /// <paramref name="remembers" /> says.
        /// </summary>
        ActMemory StoredAct(int step, int index, bool remembers)
        {
            for (var i = 0; _generation == 1 && i < _memory.Acts.Count; i++)
            {
                var kept = _memory.Acts[i];
                if (kept.Step != step || kept.Index != index)
                    continue;

                if (kept.Seen != null)
                    kept.Seen.Clock = _owner.Clock;

                return kept;
            }

            return new ActMemory { Step = step, Index = index, Seen = remembers ? new ReaderMemory { Clock = _owner.Clock } : null };
        }
    }
}
