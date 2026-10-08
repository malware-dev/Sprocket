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
        public Action<int, bool> Calls { get; set; }

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

            _bindings.Distinct(_actedOn);
            return _actedOn;
        }

        /// <summary>The sequence this runs.</summary>
        public Sequence Sequence => _sequence;

        /// <summary>Whether a run of it is under way.</summary>
        public bool IsRunning { get; private set; }

        /// <summary>What stopped the last run from outside before it finished, or none.</summary>
        public Stopper StoppedBy { get; private set; }

        /// <summary>The step the last run was at when something stopped it.</summary>
        public int StoppedAt { get; private set; }

        /// <summary>The number of the sequence that stopped the last run, where <see cref="StoppedBy" /> is a sequence.</summary>
        public int StoppedFrom { get; private set; }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep => _stepIndex;

        /// <summary>True while the sequence is held at an await or a wait.</summary>
        public bool IsWaiting { get; private set; }

        /// <summary>
        /// Why the last run ended before its last step, or none where it did not.
        /// </summary>
        /// <remarks>
        /// Kept after the run has stopped, because that is the only moment somebody can be told: the
        /// sequence is no longer running and nothing is outstanding to be asked. Cleared when the
        /// sequence next runs, so it says what happened last time and never what happened once.
        /// </remarks>
        public Trouble Quit { get; private set; }

        /// <summary>What was wrong with the program for this sequence when it was last built, or none.</summary>
        /// <remarks>Set by the runner of the whole program when the program or its wiring changes, and on no tick.</remarks>
        public SequenceFault Found { get; private set; }

        /// <summary>What an open editor last saw gone, or possibly swapped, among the blocks this names, or none.</summary>
        public SequenceFault Looked { get; set; }

        /// <summary>The one of its system this runs on, or null where the system has none.</summary>
        public SystemInstance Instance => _instance;

        /// <summary>What is known to be wrong with this sequence on the one of its system it runs on, or none.</summary>
        /// <remarks>
        /// Read, never worked out: what a run met, what an open editor saw, what the program said when it was built, and
        /// what the blocks of a step being waited on said. The worst news first, because a line can only say one thing.
        /// </remarks>
        public SequenceFault Fault
        {
            get
            {
                if (_missingSeen != _owner.MissingStamp)
                {
                    _missing = _owner.Missing(_sequence, _instance);
                    _missingSeen = _owner.MissingStamp;
                }

                if (_missing)
                    return new SequenceFault(SequenceProblem.MissingSource, SequenceFault.Trigger);
                if (_met.Exists)
                    return _met;
                if (Looked.Exists)
                    return Looked;
                if (Found.Exists && !Found.IsWarning)
                    return Found;
                if (_stuck.Exists && IsWaiting)
                    return _stuck;
                if (Quit.Exists)
                    return new SequenceFault(SequenceProblem.Conflicted, SequenceFault.Trigger, Quit);
                return Found;
            }
        }

        Interval _conflicts = new Interval(60);

        readonly AutomationRunner _owner;
        readonly Rota _rota;

        readonly Shift _sampling;

        readonly Shift _busy;

        Hold _hold;
        bool _holding;

        SequenceFault _met;
        SequenceFault _stuck;

        int _missingSeen = -1;
        bool _missing;

        int _tick;

        /// <summary>
        /// What a block being waited on says has taken it out of this sequence's hands, or none; and keeps what one says
        /// stops it finishing.
        /// </summary>
        /// <remarks>
        /// Asked of a run that is already held, and only of the blocks of the steps holding it -
        /// which for an airlock is one vent. Each answer costs the block a component lookup and a
        /// comparison, which is why it is asked on a cadence rather than on the tick.
        /// <para>
        /// About the state the step is waiting for, not about the block. One vent serves the cycle out and the cycle in,
        /// and they wait for opposite things of it - so what is wrong with the vent has two answers, and only the step
        /// knows which it is owed.
        /// </para>
        /// </remarks>
        Trouble Troubled()
        {
            _stuck = SequenceFault.None;
            for (var i = 0; i < _outstanding.Count; i++)
            {
                var compiled = Compile(_outstanding[i]);
                for (var t = 0; t < compiled.Entities.Count; t++)
                {
                    EntityAction doing = compiled.Actions[t];
                    if (string.IsNullOrEmpty(doing.LeadsToStateId))
                        continue;

                    // Switched off, unpowered or broken on the way: it gets no further until somebody sees to it.
                    var entity = compiled.Entities[t];
                    var reached = Reached(_owner.World, entity, doing.LeadsToStateId);
                    if (!reached && entity.Stalls(doing))
                    {
                        if (!_stuck.Exists)
                            _stuck = new SequenceFault(SequenceProblem.Stalled, _sequence.Steps.IndexOf(_outstanding[i]), Trouble.None, entity.Name);

                        continue;
                    }

                    // Done what it was told short of the state, which only the block can say; the step is settled there.
                    if (!reached && entity.Done(doing.Id, compiled.Arguments[t]))
                    {
                        Finish(_outstanding[i], entity);
                        continue;
                    }

                    Trouble said = entity.Trouble(doing.Id, compiled.Arguments[t], doing.LeadsToStateId);
                    if (said.Ends)
                        return said;

                    if (said.Exists && !_stuck.Exists)
                        _stuck = new SequenceFault(SequenceProblem.Stuck, _sequence.Steps.IndexOf(_outstanding[i]), said);
                }
            }

            return Trouble.None;
        }

        /// <summary>
        /// The acts this is held waiting on, empty unless it is waiting.
        /// </summary>
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

        /// <summary>The tick the last run started on.</summary>
        public int LastFiredTick { get; private set; }

        /// <summary>How many runs have started.</summary>
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

        /// <summary>The tick the trigger last fired while the sequence was already running.</summary>
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

        /// <summary>
        /// Keeps an eye on a run: done every ten ticks while one is under way.
        /// </summary>
        /// <remarks>
        /// Whether the machine Sprocket runs on can do anything at all - its own block powered and
        /// switched on - is asked here. Nothing moves on while it cannot, and what was already running
        /// is <b>kept</b> rather than stopped: a power cut is not somebody pressing a switch, and coming
        /// back up part way through a cycle is the whole point of remembering the run at all.
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
                    Rescue(tick);
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

        /// <summary>Watches a run every ten ticks while there is one, and leaves the rota while there is not.</summary>
        void Pace()
        {
            if (IsRunning)
                _rota.Every(_sampling, Rota.Ten);
            else
                _rota.Leave(_sampling);
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

        /// <summary>Whether the run under way was started by hand, which lets it run where its sequence is switched off.</summary>
        public bool ByHand => _byHand;

        /// <summary>
        /// Starts the sequence because its trigger fired, where it may run and is not already running.
        /// </summary>
        /// <remarks>
        /// A switched-off sequence lets its own trigger go by without counting it: that is what off
        /// means, and counting it would report a door somebody opened ten times as ten refusals. One
        /// already running skips, which is counted, because the press or the moment was real.
        /// </remarks>
        public void Triggered(GridWorld world, Bindings bindings, int tick)
        {
            _bindings = bindings;
            _tick = tick;

            if (!CanRun)
                return;

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                return;
            }

            _byHand = false;
            Start(world, tick);
        }

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
        public void Resume(int step, bool waiting, int waitTicks, bool byHand)
        {
            if ((!CanRun && !byHand) || step < 0 || step >= _sequence.Steps.Count)
                return;

            _byHand = byHand;
            IsRunning = true;
            IsWaiting = waiting;
            _stepIndex = step;
            // What was left when the world was written down, and the first tick after loading is the
            // one after that: pinning the whole of it there would end the wait a tick late.
            _waitTicks = waitTicks > 1 ? waitTicks - 1 : 0;
            _waitUntil = -1;
            _holding = false;

            _outstanding.Clear();
            _finished.Clear();
            _started.Clear();

            // Backwards to the await before this step, because that is the span an await covers. An
            // act that has already arrived is no burden: Settled says so at once, and a sequence no
            // longer running is none either.
            //
            // Which branch of an if ran is not written down, so a whole if before this step is passed over, and from
            // inside a branch the branches before it are: what they started is left to the blocks.
            for (var i = step - 1; i >= 0; i--)
            {
                var earlier = _sequence.Steps[i];
                if (earlier is EndIf || earlier is Else)
                {
                    i = _sequence.OpenOf(i);
                    continue;
                }

                if (earlier.IsOff)
                    continue;

                if (earlier is Await)
                    break;

                var act = earlier as Act;
                if (act != null)
                    _outstanding.Add(act);

                var run = earlier as Run;
                if (run != null && !run.Stops)
                    _started.Add(run.Number);
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
        /// Every way a run is cut short comes through here - switched off, stopped by hand, taken over,
        /// at fault - so this is where a machine left moving is told to stop. What that means is the
        /// block's own to say, and for most of them it means nothing at all.
        /// </remarks>
        public void Stop()
        {
            Called();
            Finish();
        }

        /// <summary>
        /// Ends the run from outside, as <see cref="Stop" /> does, and keeps what stopped it and at which step, where
        /// <paramref name="sequence" /> is the number of the sequence that did, where one did.
        /// </summary>
        /// <remarks>
        /// Information rather than anything wrong: somebody meant it, and the construct is wherever the run got to. Kept until
        /// the sequence next runs, and only where it was running, since stopping a sequence at rest is nothing happening.
        /// </remarks>
        public void Interrupt(Stopper by, int sequence)
        {
            var running = IsRunning;
            var at = _stepIndex;
            Stop();
            if (!running)
                return;

            StoppedBy = by;
            StoppedAt = at;
            StoppedFrom = sequence;
        }

        /// <summary>Ends a run that has done its last step, leaving what it started to carry on.</summary>
        /// <remarks>A trigger's own act has no wait after it, so what it started is still in flight when the run ends.</remarks>
        void Finish()
        {
            IsRunning = false;
            IsWaiting = false;
            _byHand = false;
            _stepIndex = -1;
            _waitTicks = 0;
            _waitUntil = -1;
            _holding = false;
            _outstanding.Clear();
            _finished.Clear();
            _started.Clear();
            _rota.Leave(_busy);
            _rota.Leave(_sampling);
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

        /// <summary>Whether <paramref name="entity" /> is in the state <paramref name="stateId" /> names, or has no such state.</summary>
        static bool Reached(GridWorld world, GridEntity entity, string stateId)
        {
            var state = entity.Kind.FindState(stateId);
            return state == null || state.Holds(world.Values, entity.GetValue(state.PropertyId));
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

                if (!state.Holds(world.Values, entity.GetValue(state.PropertyId)) && !Finished(step, entity))
                    return false;
            }

            return true;
        }

        /// <summary>The blocks each act waited on has said it is done with, short of the state the act leads to.</summary>
        /// <remarks>
        /// Found on the cadence troubles are asked on, and kept so the tick can read it: a block is asked whether it is done
        /// once a second, and the answer is good until the act is done again or the run ends.
        /// </remarks>
        readonly Dictionary<Act, List<GridEntity>> _finished = new Dictionary<Act, List<GridEntity>>();

        /// <summary>Keeps that <paramref name="entity" /> has done what <paramref name="act" /> told it.</summary>
        void Finish(Act act, GridEntity entity)
        {
            List<GridEntity> by;
            if (!_finished.TryGetValue(act, out by))
                _finished[act] = by = new List<GridEntity>();

            if (!by.Contains(entity))
                by.Add(entity);
        }

        /// <summary>Whether <paramref name="entity" /> has said it is done with what <paramref name="act" /> told it.</summary>
        bool Finished(Act act, GridEntity entity)
        {
            List<GridEntity> by;
            return _finished.Count > 0 && _finished.TryGetValue(act, out by) && by.Contains(entity);
        }

        void Start(GridWorld world, int tick)
        {
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;

            // What stopped the last run is about the last run. Somebody starting this one has
            // either mended it or is about to find out again.
            Quit = Trouble.None;
            _met = SequenceFault.None;
            _stuck = SequenceFault.None;
            StoppedBy = Stopper.None;
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

                // Switched off: kept, and never done, and an if switched off takes its branches with it.
                if (step.IsOff)
                {
                    if (step is If)
                        Go(_sequence.EndOf(_stepIndex) + 1);
                    else
                        Step();

                    continue;
                }

                var act = step as Act;
                if (act != null)
                {
                    IsWaiting = false;

                    // A block the step needs that is gone, or may have been swapped, is found here, where the run needs
                    // it; the working-out it rests on is already done once per change.
                    var needed = act.HasAction ? Compile(act) : null;
                    if (needed != null && needed.Problem != SequenceProblem.None)
                    {
                        Meet(new SequenceFault(needed.Problem, _stepIndex, Trouble.None, needed.Which));
                        return;
                    }

                    // Asked of a block that is off, an action or a value is lost; so the run ends where it was asked, and says so.
                    var unable = needed == null ? SequenceFault.None : Unable(needed);
                    if (unable.Exists)
                    {
                        Meet(unable);
                        return;
                    }

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

                    // Started and left to it, until an await below waits for it.
                    if (Calls != null)
                        Calls(run.Number, run.Stops);

                    if (!run.Stops)
                        _started.Add(run.Number);

                    // Stopped by that: by stopping itself, or by a sequence it ran; or the one it ran met trouble and
                    // switched the system off. The run ends here.
                    if (!IsRunning)
                        return;

                    if (!CanRun && !_byHand)
                    {
                        Stop();
                        return;
                    }

                    Step();
                    continue;
                }

                var opens = step as If;
                if (opens != null)
                {
                    IsWaiting = false;
                    if (!Branch(opens.Test))
                        return;

                    continue;
                }

                // Reached from the end of the branch before it, so that branch is done, and the run goes on after the
                // whole if.
                if (step is Else)
                {
                    Go(_sequence.EndOf(_stepIndex) + 1);
                    continue;
                }

                if (step is EndIf)
                {
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
                    // A block gone while it is waited on ends the run there, the same as one gone when its step
                    // is reached. Looked at the moment the wait is reached, so what has already arrived passes in
                    // the same tick, and from then on when the hold's pace brings it round.
                    var gone = Gone();
                    if (gone.Exists)
                    {
                        Meet(gone);
                        return;
                    }

                    if (!Finished(world))
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
                        var quit = _conflicts.Due(_tick) ? Troubled() : Trouble.None;
                        if (quit.Exists)
                        {
                            Stop();
                            Quit = quit;
                            return;
                        }

                        IsWaiting = true;
                        return;
                    }

                    _outstanding.Clear();
                    _finished.Clear();
                    _started.Clear();
                    _holding = false;
                    _stuck = SequenceFault.None;
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

            Finish();
        }

        void Step()
        {
            Go(_stepIndex + 1);
        }

        void Go(int step)
        {
            _stepIndex = step;
            EnterStep();
        }

        /// <summary>
        /// Goes into the first branch of the if at the current step whose test holds, or past the if where none does; false
        /// where a test cannot be read and the run has ended there.
        /// </summary>
        bool Branch(Comparison test)
        {
            var at = _stepIndex;
            while (true)
            {
                var tested = Test(test);
                if (tested.Problem != SequenceProblem.None)
                {
                    Meet(new SequenceFault(tested.Problem, at, Trouble.None, tested.Which));
                    return false;
                }

                if (Holds(tested, test))
                {
                    Go(at + 1);
                    return true;
                }

                at = _sequence.BranchAfter(at);
                var next = at < _sequence.Steps.Count ? _sequence.Steps[at] as Else : null;
                if (next == null || next.Test == null)
                {
                    Go(at + 1);
                    return true;
                }

                test = next.Test;
            }
        }

        /// <summary>Whether <paramref name="test" /> holds on its blocks as they are now: on one of them, or on all.</summary>
        /// <remarks>Read once, when the run reaches the test; with no block to read, <c>all</c> holds and <c>any</c> does not.</remarks>
        bool Holds(Tested tested, Comparison test)
        {
            var values = _owner.World.Values;
            for (var i = 0; i < tested.Entities.Count; i++)
            {
                var entity = tested.Entities[i];
                var moment = tested.Moments[i];
                var equal = moment != null
                    ? entity.In(values, moment) == test.Value.Flag
                    : entity.Holds(tested.Values[i], tested.Literals[i]);

                if (equal != test.RequiresAll)
                    return equal;
            }

            return test.RequiresAll;
        }

        /// <summary>The first block of an act that is switched off, and so cannot do what it is asked; none where all can.</summary>
        SequenceFault Unable(Compiled compiled)
        {
            for (var i = 0; i < compiled.Entities.Count; i++)
            {
                // A block already where the step would take it has nothing to do, switched on or not.
                var entity = compiled.Entities[i];
                var leads = compiled.Actions[i].LeadsToStateId;
                if (!string.IsNullOrEmpty(leads) && Reached(_owner.World, entity, leads))
                    continue;

                if (entity.Refuses(compiled.Actions[i]))
                    return new SequenceFault(SequenceProblem.Unable, _stepIndex, Trouble.None, entity.Name);
            }

            return SequenceFault.None;
        }

        /// <summary>The first block still being waited for that is gone, or may have been swapped, or none.</summary>
        /// <remarks>
        /// Unbound rather than unsettled: the question is not whether the door has shut, it is whether there is still a
        /// door. What the step's blocks come to is worked out once per change, so this is a lookup per step waited on.
        /// </remarks>
        SequenceFault Gone()
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!_outstanding[i].HasAction)
                    continue;

                var compiled = Compile(_outstanding[i]);
                if (compiled.Problem != SequenceProblem.None)
                    return new SequenceFault(compiled.Problem, _sequence.Steps.IndexOf(_outstanding[i]), Trouble.None, compiled.Which);
            }

            return SequenceFault.None;
        }

        /// <summary>Asks the blocks being waited on whether they can finish, for an open editor to show, changing nothing about the run.</summary>
        public void Glance()
        {
            if (IsWaiting && _outstanding.Count > 0 && _bindings != null)
                Troubled();
        }

        /// <summary>Ends the run at a step it cannot do, keeping why, and has what it runs on switched off.</summary>
        void Meet(SequenceFault fault)
        {
            Stop();
            _met = fault;
            _owner.Faulted(_sequence, _instance, fault);
        }

        /// <summary>Takes what the program says is wrong, now it has been built again, forgetting what the last run met.</summary>
        public void Checked(SequenceFault found)
        {
            Found = found;
            _met = SequenceFault.None;
        }

        /// <summary>How many ticks the slowest of what is being waited on should take, or unknown if any cannot say.</summary>
        int Expected()
        {
            if (_started.Count > 0)
                return GridEntity.Unestimated;

            var longest = 0;
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!_outstanding[i].HasAction)
                    continue;

                var compiled = Compile(_outstanding[i]);
                for (var q = 0; q < compiled.Entities.Count; q++)
                {
                    var expected = compiled.Entities[q].Expected(_outstanding[i].ActionId, compiled.Arguments[q]);
                    if (expected < 0)
                        return GridEntity.Unestimated;

                    if (expected > longest)
                        longest = expected;
                }
            }

            return longest;
        }

        /// <summary>Whether everything started since the last await has got where it was going, and every sequence started since has finished.</summary>
        bool Finished(GridWorld world)
        {
            for (var i = 0; i < _outstanding.Count; i++)
            {
                if (!Settled(world, _outstanding[i]))
                    return false;
            }

            var system = _sequence.System;
            for (var i = 0; i < _started.Count && system != null; i++)
            {
                var started = system.FindSequence(_started[i]);
                var running = started == null ? null : _owner.Get(started, _instance);
                if (running != null && running.IsRunning)
                    return false;
            }

            return true;
        }

        readonly List<Act> _outstanding = new List<Act>();

        readonly List<int> _started = new List<int>();

        void Execute(GridWorld world, Act step)
        {
            if (!step.HasAction)
                return;

            // Done again, so what its blocks said of the last time is no answer for this one.
            _finished.Remove(step);

            var compiled = Compile(step);
            for (var i = 0; i < compiled.Entities.Count; i++)
            {
                if (step.Form == Form.Native)
                    world.Invoke(compiled.Entities[i], step.ActionId, compiled.Arguments[i], step.Form);
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
            public readonly List<IList<Literal>> Arguments = new List<IList<Literal>>();
            public readonly List<Order> Orders = new List<Order>();

            /// <summary>Why the act cannot be done, gone or possibly swapped, or none.</summary>
            public SequenceProblem Problem;

            /// <summary>The block that is so, by its reference.</summary>
            public string Which;
        }

        /// <summary>
        /// What one test comes to on this one's blocks: each block it reads, and on each the moment, or the value with what
        /// it is compared with as the block holds it.
        /// </summary>
        sealed class Tested
        {
            public readonly List<GridEntity> Entities = new List<GridEntity>();
            public readonly List<EntityEvent> Moments = new List<EntityEvent>();
            public readonly List<EntityAction> Values = new List<EntityAction>();
            public readonly List<Literal> Literals = new List<Literal>();

            /// <summary>Why the test cannot be read, a slot empty or a block gone or possibly swapped, or none.</summary>
            public SequenceProblem Problem;

            /// <summary>The slot or block that is so, by its reference.</summary>
            public string Which;
        }

        readonly Dictionary<Act, Compiled> _compiled = new Dictionary<Act, Compiled>();

        readonly Dictionary<Comparison, Tested> _tested = new Dictionary<Comparison, Tested>();

        int _boundAt = -1;
        int _editedAt = -1;

        /// <summary>How many times an act has been worked out.</summary>
        /// <remarks>Counted so it can be shown that a run repeating what it asks works nothing out again.</remarks>
        public int Compilations { get; private set; }

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
            Fresh();

            Compiled compiled;
            if (_compiled.TryGetValue(step, out compiled))
                return compiled;

            Compilations++;
            compiled = new Compiled();
            compiled.Problem = Needed(step.Targets, SequenceProblem.MissingTarget, out compiled.Which);
            var acting = ActedOn(step.Targets);
            for (var i = 0; i < acting.Count; i++)
            {
                var entity = _bindings.Entity(acting[i]);
                if (entity == null)
                    continue;

                var action = step.Names(entity.Kind.Actions);
                if (action == null)
                    continue;

                var values = Converted(compiled, action, step.Arguments);
                if (values == null)
                    continue;

                compiled.Entities.Add(entity);
                compiled.Actions.Add(action);
                compiled.Arguments.Add(values);
                compiled.Orders.Add(entity.Prepare(action, values));
            }

            _compiled[step] = compiled;
            return compiled;
        }

        /// <summary>What a test comes to, worked out the first time it is asked after anything it rests on has changed, as an act is.</summary>
        /// <remarks>A block that has neither the moment nor the value, or holds the value as another kind, is left out: the step is at fault, and the fault says so.</remarks>
        Tested Test(Comparison test)
        {
            Fresh();

            Tested tested;
            if (_tested.TryGetValue(test, out tested))
                return tested;

            tested = new Tested();
            tested.Problem = Needed(test.Targets, SequenceProblem.MissingGate, out tested.Which);

            var given = new List<Literal>(1);
            given.Add(test.Value);
            var became = new List<Literal>(1);

            var reading = ActedOn(test.Targets);
            for (var i = 0; i < reading.Count; i++)
            {
                var entity = _bindings.Entity(reading[i]);
                if (entity == null)
                    continue;

                var moment = test.Moment(entity.Kind);
                var value = moment == null ? test.Names(entity.Kind.Actions) : null;
                became.Clear();
                if (moment == null && (value == null || !value.Converts(given, became)))
                    continue;

                tested.Entities.Add(entity);
                tested.Moments.Add(moment);
                tested.Values.Add(value);
                tested.Literals.Add(moment == null ? became[0] : test.Value);
            }

            _tested[test] = tested;
            return tested;
        }

        /// <summary>Forgets what every act and test came to, where the program or what its names point at has changed since.</summary>
        void Fresh()
        {
            var bound = _bindings.Revision;
            var edited = _owner.Program.Edits;
            if (bound == _boundAt && edited == _editedAt)
                return;

            _compiled.Clear();
            _tested.Clear();
            _boundAt = bound;
            _editedAt = edited;
        }

        /// <summary>
        /// The first block a step needs and cannot have, by its reference: a slot left empty, a block gone, or one possibly
        /// swapped for another; with why, where a block gone is <paramref name="lost" />.
        /// </summary>
        /// <remarks>A slot that may be left empty is skipped, and so is a block gone from one.</remarks>
        SequenceProblem Needed(IList<string> targets, SequenceProblem lost, out string which)
        {
            which = null;
            var system = _sequence.System;
            for (var s = 0; s < targets.Count; s++)
            {
                var slot = targets[s];
                var part = system == null ? null : system.FindPart(slot);
                var filled = _instance == null ? null : _instance.FilledWith(slot);
                if (part != null && _instance != null && (filled == null || filled.Count == 0))
                {
                    if (part.Optional)
                        continue;

                    which = slot;
                    return SequenceProblem.EmptySlot;
                }

                var count = filled == null ? 1 : filled.Count;
                for (var n = 0; n < count; n++)
                {
                    var name = filled == null ? slot : filled[n];
                    var points = _bindings.State(name);
                    if (points == RefState.Bound || (points == RefState.Lost && part != null && part.Optional))
                        continue;

                    which = name;
                    return points == RefState.Lost ? lost : SequenceProblem.Adrift;
                }
            }

            return SequenceProblem.None;
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
                    return compiled.Arguments[i];
            }

            var converted = new List<Literal>(given.Count);
            return action.Converts(given, converted) ? converted : null;
        }
    }
}
