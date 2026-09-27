using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// Runs one sequence, and carries everything the editor needs to show what it is
    /// doing right now.
    /// </summary>
    public sealed class SequenceRunner
    {
        readonly Sequence _sequence;

        int _stepIndex = -1;

        int _waitTicks;

        int _waitUntil = -1;

        /// <summary>
        /// One sequence, running for one of a system.
        /// </summary>
        /// <remarks>
        /// The same sequence runs once for each of a system on the ship, each resolving the
        /// block slots the author built with into its own doors. Null means the slots are the
        /// blocks themselves, which is what the first one of a system does and what every
        /// system built before block slots existed keeps doing.
        /// <para>
        /// The owner is the runner for the whole program, which knows the world, its bindings and the rota.
        /// </para>
        /// </remarks>
        public SequenceRunner(Sequence sequence, SystemInstance instance, AutomationRunner owner)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _rota = owner.Rota;

            var place = _rota.Join();
            _sampling = new Shift(place, Sample);
            _busy = new Shift(place, Continued);
            _rota.Every(_sampling, Rota.Ten);
            LastFiredTick = -1;
            LastSkippedTick = -1;
        }

        /// <summary>
        /// Starts another sequence of the same system, on the same one of it.
        /// </summary>
        /// <remarks>
        /// Set by whatever owns the runners, because a sequence knows nothing about anything running
        /// beside it. Null until then, which is what a sequence run on its own in a test has.
        /// </remarks>
        public Action<int> Calls { get; set; }

        readonly SystemInstance _instance;

        /// <summary>
        /// Whether this sequence may run for this one of its system.
        /// </summary>
        /// <remarks>
        /// Three switches, all of which have to be on: the sequence's, its system's, and this
        /// one of the system's. They are asked separately everywhere else because they are
        /// three different intentions - "I parked this rule", "I am rewiring the whole thing",
        /// "leave the port airlock alone while I weld" - and none may overwrite another.
        /// </remarks>
        bool CanRun => _sequence.CanRun(_instance);

        Bindings _bindings;

        readonly List<string> _actedOn = new List<string>();
        readonly List<string> _watched = new List<string>();

        List<string> ActedOn(IList<string> slots)
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
            return _actedOn;
        }

        List<string> Watched(IList<string> slots)
        {
            _watched.Clear();
            if (_instance == null)
            {
                for (var i = 0; i < slots.Count; i++)
                    _watched.Add(slots[i]);
            }
            else
            {
                _instance.Resolve(slots, _watched);
            }
            return _watched;
        }

        public Sequence Sequence => _sequence;

        public bool IsRunning { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep => _stepIndex;

        /// <summary>True while the sequence is held at an await or a wait.</summary>
        public bool IsWaiting { get; private set; }

        /// <summary>
        /// Why the last run ended before its last step, or null where it did not.
        /// </summary>
        /// <remarks>
        /// Kept after the run has stopped, because that is the only moment somebody can be told: the
        /// sequence is no longer running and nothing is outstanding to be asked. Cleared when the
        /// sequence next runs, so it says what happened last time and never what happened once.
        /// </remarks>
        public string Quit { get; private set; }

        Interval _conflicts = new Interval(60);

        readonly AutomationRunner _owner;
        readonly Rota _rota;

        readonly Shift _sampling;

        readonly Shift _busy;

        Hold _hold;
        bool _holding;

        int _tick;

        /// <summary>
        /// What a block being waited on says has taken it out of this sequence's hands, or null.
        /// </summary>
        /// <remarks>
        /// Asked of a run that is already held, and only of the blocks of the steps holding it -
        /// which for an airlock is one vent. Each answer costs the block a component lookup and a
        /// comparison, which is why it is asked on a cadence rather than on the tick.
        /// </remarks>
        string Conflicted()
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                var compiled = Compile(_outstanding[i]);
                for (var t = 0; t < compiled.Entities.Count; t++)
                {
                    EntityAction doing = compiled.Actions[t];
                    if (string.IsNullOrEmpty(doing.LeadsToStateId))
                        continue;

                    Trouble said = compiled.Entities[t].Trouble(doing.LeadsToStateId);
                    if (said.Ends)
                        return said.Said;
                }
            }

            return null;
        }

        /// <summary>
        /// The acts this is held waiting on, empty unless it is waiting.
        /// </summary>
        /// <remarks>
        /// Read by <c>SequenceSummary.Fault</c>, which asks the blocks of these steps whether they
        /// can finish at all. The runner does not ask: what is wrong with a sequence is one
        /// question with one answer, and it already has somewhere to live.
        /// </remarks>
        public IList<Act> Outstanding
        {
            get { return _outstanding; }
        }


        /// <summary>How much of a wait is left to spend, in ticks.</summary>
        /// <remarks>Written down when a world is saved, so a pause survives being reloaded.</remarks>
        public int WaitTicks
        {
            get
            {
                if (_waitUntil < 0)
                    return _waitTicks;

                var left = _waitUntil - _tick;
                return left > 0 ? left : 0;
            }
        }

        public int LastFiredTick { get; private set; }

        public int FiredCount { get; private set; }

        /// <summary>
        /// How often the trigger fired while the sequence was already running.
        /// </summary>
        /// <remarks>
        /// Re-triggering skips rather than queueing, restarting, or running a second
        /// copy. Skipping is the only option that cannot silently accumulate work,
        /// and it matches how people think about a system that is busy. With real
        /// gates the wait is legible - "waiting for the inner door to shut" - so busy
        /// explains itself instead of being a mystery.
        /// </remarks>
        public int SkippedCount { get; private set; }

        public int LastSkippedTick { get; private set; }

        /// <summary>
        /// How often something called this sequence while it was switched off.
        /// </summary>
        /// <remarks>
        /// Counted separately from being busy because it is a different piece of news, and counted at
        /// all because the alternative is silence: a step saying <c>run</c> that meets a parked sequence
        /// would otherwise evaporate, which is the one thing Sprocket does nowhere else.
        /// <para>
        /// Calls only. Its own trigger firing while it is off is not news - that is what off means, and
        /// counting it would report a door somebody opened ten times as ten refusals.
        /// </para>
        /// </remarks>
        public int RefusedCount { get; private set; }

        /// <summary>Whether the watched signal is true right now.</summary>
        public bool TriggerSignalIsTrue { get; private set; }

        /// <summary>One or more listened-to entities are no longer on the construct.</summary>
        public bool HasMissingSources { get; private set; }

        /// <summary>
        /// Samples this sequence's triggers, and keeps an eye on a run: done every ten ticks while
        /// there is anything to sample or a run under way, and every hundred otherwise.
        /// </summary>
        /// <remarks>
        /// Whether the machine Sprocket runs on can do anything at all - its own block powered and
        /// switched on - is asked here. Nothing starts and nothing moves on while it cannot, and what
        /// was already running is <b>kept</b> rather than stopped: a power cut is not somebody
        /// pressing a switch, and coming back up part way through a cycle is the whole point of
        /// remembering the run at all.
        /// <para>
        /// The trigger is still sampled. Edge detection compares a sample with the one before it, so
        /// a door opened during a blackout must not read as having just opened when the lights come
        /// back. What matters is that no turn is skipped, not that every tick is looked at: every edge
        /// sampled is a change that stays changed - a door that is open afterwards - and is still
        /// there when the turn comes round. Anything that exists for a moment only is answered rather
        /// than sampled.
        /// </para>
        /// <para>
        /// The same rule the programmable block follows: it will not instantiate a script until its
        /// block is powered and enabled, and it asks for updates again when power arrives. On a world
        /// still loading, that deferral is also what keeps a resumed run from judging a half-built
        /// construct.
        /// </para>
        /// </remarks>
        void Sample(int tick)
        {
            Enter(tick);
            var world = _owner.World;
            var triggered = SampleTrigger(world);

            if (_owner.Live)
            {
                // A run somebody started by hand is theirs, and the switch is not about them: they
                // are standing at the screen watching it. Everything else a switched-off sequence
                // refuses.
                if (!CanRun && !_byHand)
                {
                    if (IsRunning)
                        Stop();
                }
                else if (IsRunning)
                {
                    if (triggered)
                    {
                        SkippedCount++;
                        LastSkippedTick = tick;
                    }

                    Rescue(tick);
                }
                else if (triggered)
                {
                    Start(world, tick);
                }
            }

            Pace();
        }

        /// <summary>Takes the run a step further: done when what it is waiting on is due.</summary>
        void Continued(int tick)
        {
            Enter(tick);
            if (!IsRunning)
                return;

            // Held, not stopped, and looked at again once the machine could have come back.
            if (!_owner.Live)
            {
                _rota.Every(_busy, Rota.Ten);
                return;
            }

            if (!CanRun && !_byHand)
            {
                Stop();
                return;
            }

            Advance(_owner.World);
            Schedule();
        }

        void Enter(int tick)
        {
            _tick = tick;
            // Held for the tick rather than passed down, because every step of a tick wants it. Both entry points
            // set it, so nothing reads one left from a previous tick.
            _bindings = _owner.Current;
        }

        /// <summary>Puts the run's next look on the rota, for whatever it has stopped at.</summary>
        void Schedule()
        {
            if (!IsRunning)
            {
                _rota.Leave(_busy);
                return;
            }

            if (_holding)
            {
                _rota.Every(_busy, _hold.Period(_tick));
                return;
            }

            if (_waitUntil >= 0)
            {
                _rota.At(_busy, _waitUntil);
                return;
            }

            _rota.At(_busy, _tick + 1);
        }

        /// <summary>
        /// A run that is going on with nothing on the rota to move it, put back on.
        /// </summary>
        /// <remarks>
        /// Every change of state schedules the next look, and a run left without one would never move
        /// again with nothing to say so. Said in the log when it happens, because it means something
        /// forgot to.
        /// </remarks>
        void Rescue(int tick)
        {
            if (_busy.Listed)
                return;

            Told.Say("A run of \"" + _sequence.Name + "\" had nothing on the rota to move it, and was put back.");
            _rota.At(_busy, tick);
        }

        /// <summary>How often the triggers are sampled: every ten while there is anything to watch.</summary>
        /// <remarks>
        /// A sequence with only buttons to start it, and nothing running, is still looked at every
        /// hundred ticks, which is how a trigger added to it later comes to be sampled.
        /// </remarks>
        void Pace()
        {
            _rota.Every(_sampling, IsRunning || Reads() ? Rota.Ten : Rota.Hundred);
        }

        /// <summary>Whether any trigger of this sequence is read off the blocks rather than answered.</summary>
        bool Reads()
        {
            var system = _sequence.System;
            var triggers = system == null ? null : system.Triggers;
            for (var i = 0; triggers != null && i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger.Runs == _sequence.Number && (trigger is WhenTrigger || trigger is EveryTrigger))
                    return true;
            }

            return false;
        }

        /// <summary>Takes this sequence off the rota for good, when it no longer exists.</summary>
        public void Retire()
        {
            _rota.Leave(_sampling);
            _rota.Leave(_busy);
        }

        /// <summary>Starts the sequence by name, from another sequence, a trigger or a button.</summary>
        /// <remarks>
        /// The switch holds here. A parked sequence is parked for everything, including being called by
        /// another one - what a person switched off must not run because something else asked.
        /// <para>
        /// So is the machine's own power. A press arrives as a call rather than as a tick, so a slot
        /// on somebody's panel would otherwise start a cycle on a Sprocket that has no power at all.
        /// </para>
        /// <para>
        /// Either way the call is refused rather than dropped. Off is a legitimate answer and silence
        /// is not: the refusal is counted and shown, the way a switched-off block refuses an action
        /// whoever asks.
        /// </para>
        /// </remarks>
        public void Call(GridWorld world, Bindings bindings, int tick, bool live = true,
            bool byHand = false)
        {
            _bindings = bindings;
            _tick = tick;

            // Power is refused whoever asks: a machine with none does nothing, and a person at the
            // screen of a dead block is not an exception to that.
            if (!live)
            {
                RefusedCount++;
                return;
            }

            // The switch is not. Somebody who parks a sequence so its triggers leave it alone, and
            // then presses run, has said exactly what they want - and that is the state anybody
            // debugging one is in.
            if (!CanRun && !byHand)
            {
                RefusedCount++;
                return;
            }

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                return;
            }

            _byHand = byHand;
            Start(world, tick);
        }

        bool _byHand;

        /// <summary>
        /// Puts a run back where a reload interrupted it.
        /// </summary>
        /// <remarks>
        /// A world saved mid-cycle has to carry on when it is loaded: the doors are where they were
        /// and the room is as empty as it was, so forgetting the run would leave an airlock with its
        /// inner door shut, its outer door closed, and nobody left to open either.
        /// <para>
        /// The step and the clock are restored; what was started is not. Whatever was invoked before
        /// the save has either arrived by now or has not, and an await asks the world rather than
        /// anything written down - so the acts between the previous await and this step are put back
        /// as what it is waiting on, and the answer comes from the doors themselves.
        /// </para>
        /// <para>
        /// Nothing is restored where the sequence cannot run or the step is no longer there. Both
        /// happen when a program is edited between saving and loading, and a run resumed into a
        /// sequence that has since changed is a machine doing something nobody asked for.
        /// </para>
        /// </remarks>
        public void Resume(int step, bool waiting, int waitTicks)
        {
            if (!CanRun || step < 0 || step >= _sequence.Steps.Count)
                return;

            IsRunning = true;
            IsWaiting = waiting;
            _stepIndex = step;
            // What was left when the world was written down, and the first tick after loading is the
            // one after that: pinning the whole of it there would end the wait a tick late.
            _waitTicks = waitTicks > 1 ? waitTicks - 1 : 0;
            _waitUntil = -1;
            _holding = false;

            _outstanding.Clear();

            // Backwards to the await before this step, because that is the span an await covers. An
            // act that has already arrived is no burden: Settled says so at once.
            for (var i = step - 1; i >= 0; i--)
            {
                var earlier = _sequence.Steps[i];
                if (earlier is Await)
                    break;

                var act = earlier as Act;
                if (act != null)
                    _outstanding.Add(act);
            }

            // Looked at on the first tick the rota runs: the world has only just loaded, and what the
            // run is waiting for is answered by the blocks as they are then.
            _rota.At(_busy, int.MinValue);
            Pace();
        }

        /// <summary>
        /// Ends the run, and calls off what it had in flight.
        /// </summary>
        /// <remarks>
        /// Every way a run ends comes through here - switched off, stopped by hand, taken over,
        /// at fault - so this is where a machine left moving is told to stop. What that means is the
        /// block's own to say, and for most of them it means nothing at all.
        /// </remarks>
        public void Stop()
        {
            Called();

            IsRunning = false;
            IsWaiting = false;
            _byHand = false;
            _stepIndex = -1;
            _waitTicks = 0;
            _waitUntil = -1;
            _holding = false;
            _outstanding.Clear();
            _rota.Leave(_busy);
        }

        /// <summary>Tells every block still being waited on to call off what was started on it.</summary>
        void Called()
        {
            if (_bindings == null)
                return;

            for (var i = 0; i < _outstanding.Count; i++)
            {
                Act act = _outstanding[i];
                if (!act.HasAction || act.Sets)
                    continue;

                var compiled = Compile(act);
                for (var t = 0; t < compiled.Entities.Count; t++)
                    compiled.Entities[t].Stop(act.ActionId);
            }
        }

        /// <summary>
        /// Whether every entity this step acted on has reached the state the action
        /// was reaching for.
        /// </summary>
        /// <remarks>
        /// An action that declares no destination - switching a light on - is done
        /// the instant it is invoked, so instant things stay instant and only things
        /// that genuinely take time are waited on.
        ///
        /// An entity that never gets there holds the sequence indefinitely. That is
        /// the right failure: carrying on regardless is how a jammed door vents a
        /// ship. It does mean being visibly stuck has to be impossible to miss, which
        /// is why the step being waited on is reported rather than inferred.
        /// </remarks>
        public bool Settled(GridWorld world, Act step)
        {
            if (!step.HasAction)
                return true;

            var compiled = Compile(step);
            for (var i = 0; i < compiled.Entities.Count; i++)
            {
                var entity = compiled.Entities[i];
                var action = compiled.Actions[i];
                if (string.IsNullOrEmpty(action.LeadsToStateId))
                    continue;

                var state = entity.Kind.FindState(action.LeadsToStateId);
                if (state == null)
                    continue;

                if (!state.Holds(world.Values, entity.GetValue(state.PropertyId)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Whether a signal reads as true right now.
        /// </summary>
        /// <remarks>
        /// A value does not carry its own kind, so reading one needs the world that made it. An event
        /// is declared against a property whose reading is a flag in every case that matters, and
        /// anything else answers the same question - is there anything there.
        /// </remarks>
        static bool Reads(GridWorld world, GridEntity entity, string propertyId)
        {
            var property = entity.Kind.FindProperty(propertyId);
            var kind = property == null ? ValueKind.Flag : property.Kind;

            return world.Values.Flag(kind, entity.GetValue(propertyId));
        }

        void Start(GridWorld world, int tick)
        {
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;

            // What stopped the last run is about the last run. Somebody starting this one has
            // either mended it or is about to find out again.
            Quit = null;
            _stepIndex = 0;
            EnterStep();
            Advance(world);
            Schedule();
            Pace();
        }

        void EnterStep()
        {
            _waitTicks = 0;
            _waitUntil = -1;
            if (_stepIndex < 0 || _stepIndex >= _sequence.Steps.Count)
                return;

            var wait = _sequence.Steps[_stepIndex] as Wait;
            if (wait != null)
                _waitTicks = wait.Ticks;
        }

        /// <summary>
        /// Walks the steps until something holds the sequence up.
        /// </summary>
        /// <remarks>
        /// An act starts and the next line follows in the same tick, so several acts overlap by
        /// default. <see cref="Await" /> is what stops that, and it is what makes a numbered list
        /// mean what it looks like it means.
        /// </remarks>
        void Advance(GridWorld world)
        {
            while (_stepIndex >= 0 && _stepIndex < _sequence.Steps.Count)
            {
                var step = _sequence.Steps[_stepIndex];

                var act = step as Act;
                if (act != null)
                {
                    IsWaiting = false;
                    Execute(world, act);

                    // Kept so an await below knows what it is waiting for. Nothing with a
                    // destination is nothing to wait on, which is how an instant action stays
                    // instant.
                    if (!Settled(world, act))
                        _outstanding.Add(act);

                    Step();
                    continue;
                }

                var run = step as Run;
                if (run != null)
                {
                    IsWaiting = false;

                    // Started and left to it. An await waits for actions this sequence began, not for
                    // a sequence somebody called - waiting for one of those is the concurrency work.
                    if (Calls != null)
                        Calls(run.Number);

                    Step();
                    continue;
                }

                if (step is Await)
                {
                    // Nothing to ask is not nothing to wait for. The half of the job that was
                    // asked for has not happened, and walking on would act on the rest of the ship
                    // as though it had - which for an airlock means opening the outer door because
                    // the inner one is no longer there to be shut.
                    //
                    // Holding is all this does. A missing block is a fault, and a sequence at fault
                    // is switched off within the second by SprocketEditor.ParkBroken, which is what
                    // stops the run and marks it. The same road a step that cannot act at all
                    // already travels.
                    // Looked at the moment it is reached, so what has already arrived passes in the same
                    // tick, and from then on when the hold's pace brings it round.
                    if (Missing() || !Finished(world))
                    {
                        if (!_holding)
                        {
                            _hold = new Hold(_tick, Expected());
                            _holding = true;
                        }

                        // A block that has been sent the other way by something else is not worth
                        // waiting for: nothing is heading towards the state this step wants, and
                        // no amount of waiting changes that. The run ends where it stands.
                        //
                        // Once a second, not every tick. A conflict is made by a person pressing
                        // something or a trigger firing, so it cannot arrive faster than that, and
                        // giving up a second later than it could have is a second nobody can
                        // measure against a machine that has already stopped moving.
                        string quit = _conflicts.Due(_tick) ? Conflicted() : null;
                        if (quit != null)
                        {
                            Stop();
                            Quit = quit;
                            return;
                        }

                        IsWaiting = true;
                        return;
                    }

                    _outstanding.Clear();
                    _holding = false;
                    IsWaiting = false;
                    Step();
                    continue;
                }

                if (_waitUntil < 0)
                    // A tick of the game's rather than a count of calls, so a wait lasts as long as it says however often
                    // the runner is woken. Pinned when the step is first reached rather than entered, because a run resumed
                    // from a save is entered before it knows what tick it is.
                    _waitUntil = _tick + _waitTicks;

                if (_tick < _waitUntil)
                {
                    IsWaiting = true;
                    return;
                }

                IsWaiting = false;
                Step();
            }

            Stop();
        }

        void Step()
        {
            _stepIndex++;
            EnterStep();
        }

        /// <summary>
        /// Whether anything still being waited for names a block that is not there.
        /// </summary>
        /// <remarks>
        /// Unbound rather than unsettled: the question is not whether the door has shut, it is
        /// whether there is still a door. A block that has not loaded yet counts too, and holding is
        /// the right answer for both - one of them arrives, and the other gets the sequence switched
        /// off for being at fault.
        /// </remarks>
        bool Missing()
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!_outstanding[i].HasAction)
                    continue;

                var waiting = ActedOn(_outstanding[i].Targets);
                for (var q = 0; q < waiting.Count; q++)
                {
                    if (_bindings.Entity(waiting[q]) == null)
                        return true;
                }
            }

            return false;
        }

        /// <summary>How many ticks the slowest of what is being waited on should take, or unknown if any cannot say.</summary>
        int Expected()
        {
            var longest = 0;
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!_outstanding[i].HasAction)
                    continue;

                var compiled = Compile(_outstanding[i]);
                for (var q = 0; q < compiled.Entities.Count; q++)
                {
                    var expected = compiled.Entities[q].Expected(_outstanding[i].ActionId, compiled.Values[q]);
                    if (expected < 0)
                        return GridEntity.Unestimated;

                    if (expected > longest)
                        longest = expected;
                }
            }

            return longest;
        }

        /// <summary>Whether everything started since the last await has got where it was going.</summary>
        bool Finished(GridWorld world)
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!Settled(world, _outstanding[i]))
                    return false;
            }

            return true;
        }

        readonly List<Act> _outstanding = new List<Act>();

        void Execute(GridWorld world, Act step)
        {
            if (!step.HasAction)
                return;

            var compiled = Compile(step);
            for (var i = 0; i < compiled.Entities.Count; i++)
            {
                if (step.Form == Form.Native)
                    world.Invoke(compiled.Entities[i], step.ActionId, compiled.Values[i], step.Form);
                else if (compiled.Orders[i] != null)
                    compiled.Orders[i].Give();
            }
        }

        /// <summary>
        /// What one act comes to on this one's blocks: each block it acts on, what it does there, its
        /// values as that takes them, and where it is one of the game's own, the order made ready.
        /// </summary>
        sealed class Compiled
        {
            public readonly List<GridEntity> Entities = new List<GridEntity>();
            public readonly List<EntityAction> Actions = new List<EntityAction>();
            public readonly List<IList<Literal>> Values = new List<IList<Literal>>();
            public readonly List<Order> Orders = new List<Order>();
        }

        readonly Dictionary<Act, Compiled> _compiled = new Dictionary<Act, Compiled>();

        int _boundAt = -1;
        int _editedAt = -1;

        /// <summary>How many times an act has been worked out.</summary>
        /// <remarks>Counted so it can be shown that a run repeating what it asks works nothing out again.</remarks>
        internal int Compilations { get; private set; }

        /// <summary>
        /// What an act comes to, worked out the first time it is asked after anything it rests on has
        /// changed.
        /// </summary>
        /// <remarks>
        /// <b>Every act a run does and waits on is asked for here, on the tick it happens.</b> So the
        /// blocks, what each is told and the values it is handed are found once per change to the
        /// program or to what its names point at, and a tick that repeats a step does a lookup and
        /// the act. A block whose action takes a value as some other kind is left out: the step is
        /// at fault, and the fault says so.
        /// </remarks>
        Compiled Compile(Act step)
        {
            var bound = _bindings.Revision;
            var edited = _owner.Program.Edits;
            if (bound != _boundAt || edited != _editedAt)
            {
                _compiled.Clear();
                _boundAt = bound;
                _editedAt = edited;
            }

            Compiled compiled;
            if (_compiled.TryGetValue(step, out compiled))
                return compiled;

            Compilations++;
            compiled = new Compiled();
            var acting = ActedOn(step.Targets);
            for (var i = 0; i < acting.Count; i++)
            {
                var entity = _bindings.Entity(acting[i]);
                if (entity == null)
                    continue;

                var action = step.Names(entity.Kind.Actions);
                if (action == null)
                    continue;

                var values = Converted(compiled, action, step.Values);
                if (values == null)
                    continue;

                compiled.Entities.Add(entity);
                compiled.Actions.Add(action);
                compiled.Values.Add(values);
                compiled.Orders.Add(entity.Prepare(action, values));
            }

            _compiled[step] = compiled;
            return compiled;
        }

        /// <summary>
        /// The values as <paramref name="action" /> takes them, or null where it cannot take them.
        /// </summary>
        /// <remarks>
        /// Shared between the blocks of one act that do the same thing, which is every block of the
        /// same kind: an airlock's two doors are handed one list.
        /// </remarks>
        static IList<Literal> Converted(Compiled compiled, EntityAction action, IList<Literal> given)
        {
            for (var i = 0; i < compiled.Actions.Count; i++)
            {
                if (compiled.Actions[i] == action)
                    return compiled.Values[i];
            }

            var converted = new List<Literal>(given.Count);
            return action.Converts(given, converted) ? converted : null;
        }

        /// <summary>
        /// Samples everything that starts this sequence, and answers whether any of it fired.
        /// </summary>
        /// <remarks>
        /// All of them, every tick, because edge detection is stateful: a trigger that is not sampled
        /// has no previous reading, and the next sample it gets is wrong. Two triggers on one sequence
        /// are the ordinary case - the inside button and the outside one - and each keeps its own
        /// reading, so one firing never robs the other of its edge.
        /// </remarks>
        bool SampleTrigger(GridWorld world)
        {
            var anySignal = false;
            var anyMissing = false;
            var fired = false;
            var watched = 0;

            var system = _sequence.System;
            var triggers = system == null ? null : system.Triggers;
            for (var i = 0; triggers != null && i < triggers.Count; i++)
            {
                var trigger = triggers[i];
                if (trigger.Runs != _sequence.Number)
                    continue;

                Watch watch = Watching(trigger);
                if (Sampled(world, trigger, watch))
                    fired = true;

                // What the sample just read is this trigger's, until the next one overwrites it.
                watch.Live = TriggerSignalIsTrue;

                anySignal = anySignal || TriggerSignalIsTrue;
                anyMissing = anyMissing || HasMissingSources;
                watched++;
            }

            // A trigger somebody has changed or deleted is still in here holding an edge nothing
            // will ever read again. Swept only when there is something to sweep, which is after an
            // edit and never on an ordinary tick.
            if (_watching.Count > watched)
                Forget(triggers);

            TriggerSignalIsTrue = anySignal;
            HasMissingSources = anyMissing;
            return fired;
        }

        bool Sampled(GridWorld world, Trigger trigger, Watch watch)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
                return when.RequiresAll ? SampleWhenAll(world, when, watch) : SampleWhen(world, when, watch);

            var every = trigger as EveryTrigger;
            if (every != null)
                return SampleEvery(every, watch);

            // A trigger the construct answers fires by being fired: a slot invokes Sprocket's action
            // and the host calls in. There is nothing here to sample.
            return false;
        }

        /// <summary>
        /// What one trigger read last time, kept per trigger.
        /// </summary>
        /// <remarks>
        /// Per trigger rather than per sequence, because an edge is a change since <em>this</em>
        /// trigger last looked. Shared state made the second trigger on a sequence read the first
        /// one's history, which fires nothing or fires twice.
        /// </remarks>
        sealed class Watch
        {
            public readonly Dictionary<string, bool> Signals = new Dictionary<string, bool>();

            public int All = -1;

            /// <summary>The tick a timer last fell due, or negative before it has been sampled.</summary>
            public int Due = -1;

            /// <summary>
            /// What this one trigger's signal read at the last sample.
            /// </summary>
            /// <remarks>
            /// Kept per trigger as well as gathered into one, because a screen listing triggers
            /// shows each of them saying what it is reading - and two triggers on one sequence
            /// would otherwise both light because one of them was true.
            /// </remarks>
            public bool Live;
        }

        readonly Dictionary<Trigger, Watch> _watching = new Dictionary<Trigger, Watch>();

        /// <summary>Forgets what a trigger read, for every trigger this sequence no longer has.</summary>
        void Forget(IReadOnlyList<Trigger> triggers)
        {
            _forgotten.Clear();

            foreach (Trigger held in _watching.Keys)
            {
                var kept = false;
                for (var i = 0; triggers != null && i < triggers.Count; i++)
                    kept = kept || (triggers[i] == held && triggers[i].Runs == _sequence.Number);

                if (!kept)
                    _forgotten.Add(held);
            }

            for (var i = 0; i < _forgotten.Count; i++)
                _watching.Remove(_forgotten[i]);
        }

        readonly List<Trigger> _forgotten = new List<Trigger>();

        /// <summary>Whether that trigger's own signal reads true at this instant.</summary>
        /// <remarks>
        /// Nothing where it has not been sampled yet, which is the honest answer: a trigger that
        /// has never been read has no reading.
        /// </remarks>
        public bool LiveNow(Trigger trigger)
        {
            Watch watch;
            return _watching.TryGetValue(trigger, out watch) && watch.Live;
        }

        Watch Watching(Trigger trigger)
        {
            Watch watch;
            if (_watching.TryGetValue(trigger, out watch))
                return watch;

            watch = new Watch();
            _watching.Add(trigger, watch);
            return watch;
        }

        bool SampleWhen(GridWorld world, WhenTrigger when, Watch watch)
        {
            var sources = Watched(when.Sources);
            var fired = false;
            var anySignalTrue = false;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var sourceId = sources[i];
                var entity = _bindings.Entity(sourceId);
                if (entity == null)
                {
                    anyMissing = true;
                    continue;
                }

                var definition = entity.Kind.FindEvent(when.EventId);
                if (definition == null)
                    continue;

                var now = Reads(world, entity, definition.PropertyId);
                if (now)
                    anySignalTrue = true;

                bool previous;
                if (watch.Signals.TryGetValue(sourceId, out previous))
                {
                    // No previous sample means the sequence has only just come into
                    // existence, and an edge needs two. Firing on the first tick
                    // would set every sequence off the moment it was created.
                    if (definition.Edge == Edge.Rising && !previous && now)
                        fired = true;
                    else if (definition.Edge == Edge.Falling && previous && !now)
                        fired = true;
                }

                watch.Signals[sourceId] = now;
            }

            TriggerSignalIsTrue = anySignalTrue;
            HasMissingSources = anyMissing;
            return fired;
        }

        /// <summary>
        /// Fires when the last of the sources reaches the state, and not again until
        /// one of them has left it.
        /// </summary>
        /// <remarks>
        /// Watching for several separate edges on the same tick would almost never
        /// fire, because the sources reach their state at different moments. What the
        /// sentence means is "the moment they are all there", so that is what is
        /// watched: one edge, on the whole condition.
        /// </remarks>
        bool SampleWhenAll(GridWorld world, WhenTrigger when, Watch watch)
        {
            var sources = Watched(when.Sources);
            var all = sources.Count > 0;
            var anyMissing = false;

            for (var i = 0; i < sources.Count; i++)
            {
                var entity = _bindings.Entity(sources[i]);
                if (entity == null)
                {
                    anyMissing = true;
                    all = false;
                    continue;
                }

                var definition = entity.Kind.FindEvent(when.EventId);
                if (definition == null)
                {
                    all = false;
                    continue;
                }

                var wanted = definition.Edge == Edge.Rising;
                if (Reads(world, entity, definition.PropertyId) != wanted)
                    all = false;
            }

            TriggerSignalIsTrue = all;
            HasMissingSources = anyMissing;

            // Unknown until the second sample, so a condition that is already true
            // when the sequence appears does not set it off.
            var now = all ? 1 : 0;
            var fired = watch.All == 0 && now == 1;
            watch.All = now;
            return fired;
        }

        /// <remarks>
        /// Measured in the game's ticks rather than in samples, because samples are only taken when
        /// this sequence's place comes round. The next time falls due from when the last one did,
        /// not from when it was noticed, so a timer noticed a few ticks late runs late by the same
        /// few ticks every time rather than drifting further each time.
        /// </remarks>
        bool SampleEvery(EveryTrigger every, Watch watch)
        {
            var interval = every.IntervalTicks < 1 ? 1 : every.IntervalTicks;
            if (watch.Due < 0 || _tick < watch.Due)
            {
                watch.Due = _tick;
                return false;
            }

            if (_tick - watch.Due < interval)
                return false;

            // A world that was paused, or a sequence that was not ticked, starts counting again
            // rather than firing once for every period it missed.
            watch.Due = _tick - watch.Due < 2 * interval ? watch.Due + interval : _tick;
            return true;
        }
    }
}
