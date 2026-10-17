using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>What each act and test of a run comes to on its blocks, worked out once per change to the program or to what its names point at.</summary>
    public sealed class CompiledRun
    {
        readonly Sequence _sequence;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly RunMemory _memory;
        readonly List<string> _actedOn = new List<string>();
        CompiledAct[] _steps = new CompiledAct[0];
        CompiledTest[] _tests = new CompiledTest[0];
        int _boundAt = -1;
        int _editedAt = -1;

        // How many times what every act comes to has been worked out afresh; the first takes up what the world remembered of each
        // block waited on, and every later one lets it go.
        int _generation;

        /// <summary>The workings of <paramref name="sequence" /> running for <paramref name="instance" />, owned by <paramref name="owner" />, remembering what is waited on in <paramref name="memory" />.</summary>
        public CompiledRun(Sequence sequence, SystemInstance instance, AutomationRunner owner, RunMemory memory)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _memory = memory;
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
            var acting = ActedOn(act.Targets, bindings);
            foreach (var name in acting)
            {
                var entity = bindings.Entity(name);
                if (entity == null)
                    continue;

                var action = act.Names(entity.Kind.Actions);
                if (action == null)
                    continue;

                // A block lacking the facet its slot holds is left out, and the fault names it.
                var type = action.Type;
                var facet = type == null ? 0 : FacetOf(act.Targets, name);
                if (type != null && facet > 0 && type.Find(entity.Kind, facet) == null)
                    continue;

                var shared = Shared(compiled, action, act.Arguments, facet);
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
                    Kept = Kept(step, compiled.Acts.Count, action.Doing != null)
                });
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

            var reading = ActedOn(test.Targets, bindings);
            foreach (var name in reading)
            {
                // A block lacking the facet its slot holds is left out, and the fault names it.
                var entity = bindings.Entity(name);
                var member = entity == null ? null : test.Names(entity.Kind.Actions);
                var facet = FacetOf(test.Targets, name);
                if (facet > 0 && member?.Type != null && member.Type.Find(entity.Kind, facet) == null)
                    continue;

                var probe = Probe.Of(entity, test.Provider, test.MemberId, test.Arguments,
                    test.Quantifier, test.Relation, test.Value, null, _owner.Clock, facet);

                if (probe != null)
                    tested.Reads.Add(probe);
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
                Bound = action.Doing?.Bind(action.Type == null ? converted : HostType.Arguments(facet, converted))
            };
        }

        /// <summary>The number of the facet of <paramref name="block" /> the slots <paramref name="targets" /> hold, from 1, or 0 where they hold none.</summary>
        int FacetOf(IList<string> targets, string block)
        {
            return _instance == null ? 0 : _instance.FacetOf(targets, block);
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
                if (!_memory.Acts.Contains(acted.Kept))
                    _memory.Acts.Add(acted.Kept);
            }
        }

        /// <summary>
        /// What is remembered of the block at <paramref name="index" /> of the act at <paramref name="step" />: what the world kept
        /// where this is the first working out since a load, and a fresh one otherwise, remembering what the block does where
        /// <paramref name="remembers" /> says.
        /// </summary>
        ActMemory Kept(int step, int index, bool remembers)
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
