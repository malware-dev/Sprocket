using System;
using Sprocket.Host.SpaceEngineers.Memory;
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
        /// <summary>How often a run held at an await looks for something that has sent its blocks the other way.</summary>
        const int Conflicts = 60;

        readonly Sequence _sequence;

        /// <summary>Everything the run remembers, kept in what the world saves, so a reload carries it on as if nothing happened.</summary>
        readonly RunMemory _memory;

        readonly Workings _workings;

        readonly Faults _faults;

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
        /// <para>
        /// What it remembers is <paramref name="memory" />: a run the world kept takes up where it was, on the rota as well, and
        /// a new one is given the next place.
        /// </para>
        /// </remarks>
        public SequenceRunner(Sequence sequence, SystemInstance instance, AutomationRunner owner, RunMemory memory)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _rota = owner.Rota;
            _memory = memory;
            _workings = new Workings(sequence, instance, owner, memory);

            var kept = memory.Busy != null && memory.Sampling != null;
            if (!kept)
            {
                var place = _rota.Join();
                memory.Sampling = new ShiftMemory { Place = place };
                memory.Busy = new ShiftMemory { Place = place };
                memory.ConflictsAt = -Conflicts;
            }

            _sampling = new Shift(memory.Sampling, Sample);
            _busy = new Shift(memory.Busy, Continued);
            _faults = new Faults(sequence, instance, owner, memory, kept);

            if (!kept)
                return;

            _rota.Restore(_sampling);
            _rota.Restore(_busy);
        }

        /// <summary>What it remembers, which the world's save writes.</summary>
        public RunMemory Memory => _memory;

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

        /// <summary>The sequence this runs.</summary>
        public Sequence Sequence => _sequence;

        /// <summary>Whether a run of it is under way.</summary>
        public bool IsRunning
        {
            get { return _memory.Running; }
            private set { _memory.Running = value; }
        }

        /// <summary>What stopped the last run from outside before it finished, or none.</summary>
        public Stopper StoppedBy
        {
            get { return (Stopper)_memory.StoppedBy; }
            private set
            {
                if (_memory.StoppedBy != (int)value)
                    _owner.Revise();

                _memory.StoppedBy = (int)value;
            }
        }

        /// <summary>The step the last run was at when something stopped it.</summary>
        public int StoppedAt
        {
            get { return _memory.StoppedAt; }
            private set { _memory.StoppedAt = value; }
        }

        /// <summary>The number of the sequence that stopped the last run, where <see cref="StoppedBy" /> is a sequence.</summary>
        public int StoppedFrom
        {
            get { return _memory.StoppedFrom; }
            private set { _memory.StoppedFrom = value; }
        }

        /// <summary>Which step is executing or waiting, or -1 when idle.</summary>
        public int CurrentStep => _memory.Step;

        /// <summary>True while the sequence is held at an await or a wait.</summary>
        public bool IsWaiting
        {
            get { return _memory.Waiting; }
            private set
            {
                if (_memory.Waiting != value)
                    _owner.Revise();

                _memory.Waiting = value;
            }
        }

        /// <summary>The one of its system this runs on, or null where the system has none.</summary>
        public SystemInstance Instance => _instance;

        /// <summary>What is known to be wrong with this sequence on the one of its system it runs on, or none.</summary>
        /// <remarks>
        /// Read, never worked out: what a run met, what an open editor saw, what the program said when it was built, and
        /// what the blocks of a step being waited on said. The worst news first, because a line can only say one thing.
        /// </remarks>
        public SequenceFault Fault => _faults.Fault;

        /// <summary>What is known to be wrong with this run, as each part of Sprocket finds it.</summary>
        public Faults Faults => _faults;

        readonly AutomationRunner _owner;
        readonly Rota _rota;

        readonly Shift _sampling;

        readonly Shift _busy;


        /// <summary>
        /// What a block being waited on says has taken it out of this sequence's hands, at its step and naming it, or none;
        /// and keeps what one says stops it finishing.
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
        SequenceFault Troubled()
        {
            var stuck = SequenceFault.None;
            foreach (var step in _memory.Outstanding)
            {
                var compiled = _workings.Compile(step, _bindings);
                foreach (var acted in compiled.Acts)
                {
                    var until = acted.Action.Until;
                    if (until == null)
                        continue;

                    // Switched off, unpowered or broken on the way: it gets no further until somebody sees to it.
                    var entity = acted.Entity;
                    var reached = until.Holds(entity.Block, acted.Seen);
                    if (!reached && entity.Stalls(acted.Action))
                    {
                        if (!stuck.Exists)
                            stuck = new SequenceFault(SequenceProblem.Stalled, step, Trouble.None, entity.Name);

                        continue;
                    }

                    // Done what it was told short of the state, which only the block can say; the step is settled there.
                    if (!reached && acted.Bound.Done(entity.Block, acted.Seen))
                    {
                        acted.Done = true;
                        continue;
                    }

                    Trouble said = acted.Bound.Trouble(entity.Block, acted.Seen);
                    if (said.Ends)
                    {
                        _faults.Hold(stuck);
                        return new SequenceFault(SequenceProblem.Conflicted, step, said, entity.Name);
                    }

                    if (said.Exists && !stuck.Exists)
                        stuck = new SequenceFault(SequenceProblem.Stuck, step, said, entity.Name);
                }
            }

            _faults.Hold(stuck);
            return SequenceFault.None;
        }

        /// <summary>How much of a wait is left to spend, in ticks.</summary>
        /// <remarks>Written down when a world is saved, so a pause survives being reloaded.</remarks>
        public int WaitTicks
        {
            get
            {
                if (_memory.WaitUntil < 0)
                    return _memory.WaitTicks;

                var left = _memory.WaitUntil - _memory.Looked;
                return left > 0 ? left : 0;
            }
        }

        /// <summary>The tick the last run started on.</summary>
        public int LastFiredTick
        {
            get { return _memory.LastFired; }
            private set { _memory.LastFired = value; }
        }

        /// <summary>How many runs have started.</summary>
        public int FiredCount
        {
            get { return _memory.Fired; }
            private set { _memory.Fired = value; }
        }

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
        public int SkippedCount
        {
            get { return _memory.Skipped; }
            private set { _memory.Skipped = value; }
        }

        /// <summary>The tick the trigger last fired while the sequence was already running.</summary>
        public int LastSkippedTick
        {
            get { return _memory.LastSkipped; }
            private set { _memory.LastSkipped = value; }
        }

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
        public int RefusedCount
        {
            get { return _memory.Refused; }
            private set { _memory.Refused = value; }
        }

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
                if (!CanRun && !_memory.ByHand)
                {
                    if (IsRunning)
                        Interrupt(Stopper.Switch, 0);
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

            if (!CanRun && !_memory.ByHand)
            {
                Interrupt(Stopper.Switch, 0);
                return;
            }

            Advance();
            Schedule();
        }

        void Enter(int tick)
        {
            _memory.Looked = tick;
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

            if (_memory.Holding)
            {
                // Looking at the blocks again and again until they have finished is a repeat, so the world's fastest update holds it.
                var period = new Hold(_memory.HoldSince, _memory.HoldExpected).Period(_memory.Looked);
                _rota.Every(_busy, Math.Max(period, _owner.FastestUpdate));
                return;
            }

            if (_memory.WaitUntil >= 0)
            {
                _rota.At(_busy, _memory.WaitUntil);
                return;
            }

            _rota.At(_busy, _memory.Looked + 1);
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

            Told.Say($"A run of \"{_sequence.Name}\" had nothing on the rota to move it, and was put back.");
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

        /// <summary>Takes this sequence off the rota for good, when it or its Sprocket no longer exists, leaving what it remembers as it was.</summary>
        /// <remarks>What it remembers may be another runner's now, as where a client's edit builds the Sprocket again, so letting go of the rota must not change it.</remarks>
        public void Retire()
        {
            _sampling.Forget();
            _busy.Forget();
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
        public void Call(Bindings bindings, int tick, bool live = true,
            bool byHand = false)
        {
            _bindings = bindings;
            _memory.Looked = tick;

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

            _memory.ByHand = byHand;
            Start(tick);
        }

        /// <summary>Whether the run under way was started by hand, which lets it run where its sequence is switched off.</summary>
        public bool ByHand => _memory.ByHand;

        /// <summary>
        /// Starts the sequence because its trigger fired, where it may run and is not already running.
        /// </summary>
        /// <remarks>
        /// A switched-off sequence lets its own trigger go by without counting it: that is what off
        /// means, and counting it would report a door somebody opened ten times as ten refusals. One
        /// already running skips, which is counted, because the press or the moment was real.
        /// </remarks>
        public void Triggered(Bindings bindings, int tick)
        {
            _bindings = bindings;
            _memory.Looked = tick;

            if (!CanRun)
                return;

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                return;
            }

            _memory.ByHand = false;
            Start(tick);
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

            _memory.ByHand = byHand;
            IsRunning = true;
            IsWaiting = waiting;
            _memory.Step = step;
            // What was left when the world was written down, and the first tick after loading is the
            // one after that: pinning the whole of it there would end the wait a tick late.
            _memory.WaitTicks = waitTicks > 1 ? waitTicks - 1 : 0;
            _memory.WaitUntil = -1;
            _memory.Holding = false;

            Release();
            _memory.Started.Clear();

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

                if (earlier is Act)
                    _memory.Outstanding.Add(i);

                var run = earlier as Run;
                if (run != null && !run.Stops)
                    _memory.Started.Add(run.Number);
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
            var at = _memory.Step;
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
            _memory.ByHand = false;
            _memory.Step = -1;
            _memory.WaitTicks = 0;
            _memory.WaitUntil = -1;
            _memory.Holding = false;
            Release();
            _memory.Started.Clear();
            _rota.Leave(_busy);
            _rota.Leave(_sampling);
        }

        /// <summary>Tells every block still being waited on to call off what was started on it.</summary>
        void Called()
        {
            if (_bindings == null)
                return;

            foreach (var step in _memory.Outstanding)
            {
                var act = (Act)_sequence.Steps[step];
                if (!act.HasAction || act.Sets)
                    continue;

                var compiled = _workings.Compile(step, _bindings);
                foreach (var acted in compiled.Acts)
                {
                    if (acted.Bound != null)
                        acted.Bound.Stop(acted.Entity.Block);
                }
            }
        }

        /// <summary>
        /// Whether every block the act at <paramref name="step" /> acted on has got to what the action
        /// was reaching for.
        /// </summary>
        /// <remarks>
        /// An action that waits for nothing - switching a light on - is done
        /// the instant it is invoked, so instant things stay instant and only things
        /// that genuinely take time are waited on.
        ///
        /// A block that never gets there holds the sequence indefinitely. That is
        /// the right failure: carrying on regardless is how a jammed door vents a
        /// ship. It does mean being visibly stuck has to be impossible to miss, which
        /// is why the step being waited on is reported rather than inferred.
        /// <para>
        /// A block that has said it is done with what it was told, short of what it waits for, is settled: found on the
        /// cadence troubles are asked on, and kept until the act is done again or the run ends.
        /// </para>
        /// </remarks>
        bool Settled(int step)
        {
            var act = (Act)_sequence.Steps[step];
            if (!act.HasAction)
                return true;

            var compiled = _workings.Compile(step, _bindings);
            foreach (var acted in compiled.Acts)
            {
                var until = acted.Action.Until;
                if (until != null && !acted.Done && !until.Holds(acted.Entity.Block, acted.Seen))
                    return false;
            }

            return true;
        }

        /// <summary>Lets go of everything being waited on, forgetting what any block said it was done with.</summary>
        void Release()
        {
            _workings.Forget();

            _memory.Outstanding.Clear();
            _memory.Acts.Clear();
        }

        /// <summary>Waits on the act at <paramref name="step" />, keeping what is remembered of each of its blocks in what the world saves.</summary>
        void WaitOn(int step)
        {
            _memory.Outstanding.Add(step);
            _workings.Listed(_workings.Compile(step, _bindings));
        }

        void Start(int tick)
        {
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;

            // What stopped the last run is about the last run. Somebody starting this one has
            // either mended it or is about to find out again.
            _faults.Quit = SequenceFault.None;
            _faults.Met(SequenceFault.None);
            _faults.Hold(SequenceFault.None);
            StoppedBy = Stopper.None;
            _memory.Step = 0;
            EnterStep();
            Advance();
            Schedule();
            Pace();
        }

        void EnterStep()
        {
            _memory.WaitTicks = 0;
            _memory.WaitUntil = -1;
            if (_memory.Step < 0 || _memory.Step >= _sequence.Steps.Count)
                return;

            var wait = _sequence.Steps[_memory.Step] as Wait;
            if (wait != null)
                _memory.WaitTicks = wait.Ticks;
        }

        /// <summary>
        /// Walks the steps until something holds the sequence up.
        /// </summary>
        /// <remarks>
        /// An act starts and the next line follows in the same tick, so several acts overlap by
        /// default. <see cref="Await" /> is what stops that, and it is what makes a numbered list
        /// mean what it looks like it means.
        /// </remarks>
        void Advance()
        {
            while (_memory.Step >= 0 && _memory.Step < _sequence.Steps.Count)
            {
                var step = _sequence.Steps[_memory.Step];

                // Switched off: kept, and never done, and an if switched off takes its branches with it.
                if (step.IsOff)
                {
                    if (step is If)
                        Go(_sequence.EndOf(_memory.Step) + 1);
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
                    var needed = act.HasAction ? _workings.Compile(_memory.Step, _bindings) : null;
                    if (needed != null && needed.Problem != SequenceProblem.None)
                    {
                        Meet(new SequenceFault(needed.Problem, _memory.Step, Trouble.None, needed.Which));
                        return;
                    }

                    // Asked of a block that is off, an action or a value is lost; so the run ends where it was asked, and says so.
                    var unable = needed == null ? SequenceFault.None : Unable(needed);
                    if (unable.Exists)
                    {
                        Meet(unable);
                        return;
                    }

                    Execute(_memory.Step);

                    // Refused on the spot, as a script that does not run is: the run ends there and says which block.
                    var refused = Refused(_memory.Step);
                    if (refused.Exists)
                    {
                        Meet(refused);
                        return;
                    }

                    // Kept so an await below knows what it is waiting for. Nothing with a
                    // destination is nothing to wait on, which is how an instant action stays
                    // instant.
                    if (!Settled(_memory.Step))
                        WaitOn(_memory.Step);

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
                        _memory.Started.Add(run.Number);

                    // Stopped by that: by stopping itself, or by a sequence it ran; or the one it ran met trouble and
                    // switched the system off. The run ends here.
                    if (!IsRunning)
                        return;

                    if (!CanRun && !_memory.ByHand)
                    {
                        Interrupt(Stopper.Switch, 0);
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
                    Go(_sequence.EndOf(_memory.Step) + 1);
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

                    if (!Finished())
                    {
                        if (!_memory.Holding)
                        {
                            _memory.HoldSince = _memory.Looked;
                            _memory.HoldExpected = Expected();
                            _memory.Holding = true;
                        }

                        // A block that has been sent the other way by something else is not worth
                        // waiting for: nothing is heading towards the state this step wants, and
                        // no amount of waiting changes that. The run ends where it stands.
                        //
                        // Once a second, not every tick. A conflict is made by a person pressing
                        // something or a trigger firing, so it cannot arrive faster than that, and
                        // giving up a second later than it could have is a second nobody can
                        // measure against a machine that has already stopped moving.
                        var quit = ConflictsDue() ? Troubled() : SequenceFault.None;
                        if (quit.Exists)
                        {
                            Stop();
                            _faults.Quit = quit;
                            return;
                        }

                        IsWaiting = true;
                        return;
                    }

                    Release();
                    _memory.Started.Clear();
                    _memory.Holding = false;
                    _faults.Hold(SequenceFault.None);
                    IsWaiting = false;
                    Step();
                    continue;
                }

                if (_memory.WaitUntil < 0)
                    // A tick of the game's rather than a count of calls, so a wait lasts as long as it says however often
                    // the runner is woken. Pinned when the step is first reached rather than entered, because a run resumed
                    // from a save is entered before it knows what tick it is.
                    _memory.WaitUntil = _memory.Looked + _memory.WaitTicks;

                if (_memory.Looked < _memory.WaitUntil)
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
            Go(_memory.Step + 1);
        }

        void Go(int step)
        {
            _memory.Step = step;
            EnterStep();
        }

        /// <summary>
        /// Goes into the first branch of the if at the current step whose test holds, or past the if where none does; false
        /// where a test cannot be read and the run has ended there.
        /// </summary>
        bool Branch(Comparison test)
        {
            var at = _memory.Step;
            while (true)
            {
                var tested = _workings.Test(at, test, _bindings);
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

        /// <summary>Whether <paramref name="test" /> holds on its blocks as they are now: on one of them, on all, or on their sum or average.</summary>
        /// <remarks>Read once, when the run reaches the test; with no block to read, <c>all</c> holds and <c>any</c> does not.</remarks>
        static bool Holds(Tested tested, Comparison test)
        {
            if (test.Quantifier == Quantifier.Sum || test.Quantifier == Quantifier.Average)
            {
                var total = 0d;
                foreach (var read in tested.Reads)
                    total += read.Number();

                // An average of nothing is no number; with no block read, nothing holds.
                if (test.Quantifier == Quantifier.Average)
                {
                    if (tested.Reads.Count == 0)
                        return false;

                    total /= tested.Reads.Count;
                }

                return Relations.Holds(Literal.Of(total), test.Relation, test.Value);
            }

            var all = test.Quantifier == Quantifier.All;
            foreach (var read in tested.Reads)
            {
                var holds = read.Holds();
                if (holds != all)
                    return holds;
            }

            return all;
        }

        /// <summary>The first block of an act that is switched off, and so cannot do what it is asked; none where all can.</summary>
        SequenceFault Unable(Compiled compiled)
        {
            foreach (var acted in compiled.Acts)
            {
                // A block already where the step would take it has nothing to do, switched on or not.
                var until = acted.Action.Until;
                if (until != null && until.Holds(acted.Entity.Block, acted.Seen))
                    continue;

                if (acted.Entity.Refuses(acted.Action))
                    return new SequenceFault(SequenceProblem.Unable, _memory.Step, Trouble.None, acted.Entity.Name);
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
            foreach (var step in _memory.Outstanding)
            {
                if (!((Act)_sequence.Steps[step]).HasAction)
                    continue;

                var compiled = _workings.Compile(step, _bindings);
                if (compiled.Problem != SequenceProblem.None)
                    return new SequenceFault(compiled.Problem, step, Trouble.None, compiled.Which);
            }

            return SequenceFault.None;
        }

        /// <summary>Ends the run at a step it cannot do, keeping why, and has what it runs on switched off.</summary>
        void Meet(SequenceFault fault)
        {
            Stop();
            _faults.Met(fault);
            _owner.Faulted(_sequence, _instance, fault);
        }

        /// <summary>Whether conflicts are due to be looked for, once every <see cref="Conflicts" /> ticks, and notes that they are.</summary>
        bool ConflictsDue()
        {
            var last = _memory.ConflictsAt;
            if (_memory.Looked - last < Conflicts && _memory.Looked >= last)
                return false;

            _memory.ConflictsAt = _memory.Looked;
            return true;
        }

        /// <summary>How many ticks the slowest of what is being waited on should take, or unknown if any cannot say.</summary>
        int Expected()
        {
            if (_memory.Started.Count > 0)
                return GridEntity.Unestimated;

            var longest = 0;
            foreach (var step in _memory.Outstanding)
            {
                if (!((Act)_sequence.Steps[step]).HasAction)
                    continue;

                var compiled = _workings.Compile(step, _bindings);
                foreach (var acted in compiled.Acts)
                {
                    var expected = acted.Bound?.Expected(acted.Entity.Block) ?? GridEntity.Unestimated;
                    if (expected < 0)
                        return GridEntity.Unestimated;

                    if (expected > longest)
                        longest = expected;
                }
            }

            return longest;
        }

        /// <summary>Whether everything started since the last await has got where it was going, and every sequence started since has finished.</summary>
        bool Finished()
        {
            foreach (var step in _memory.Outstanding)
            {
                if (!Settled(step))
                    return false;
            }

            var system = _sequence.System;
            for (var i = 0; i < _memory.Started.Count && system != null; i++)
            {
                var started = system.FindSequence(_memory.Started[i]);
                var running = started == null ? null : _owner.Get(started, _instance);
                if (running != null && running.IsRunning)
                    return false;
            }

            return true;
        }


        void Execute(int step)
        {
            var act = (Act)_sequence.Steps[step];
            if (!act.HasAction)
                return;

            var compiled = _workings.Compile(step, _bindings);
            foreach (var acted in compiled.Acts)
            {
                // Done again, so what its blocks said of the last time is no answer for this one.
                acted.Done = false;

                if (act.Form != Nature.Native)
                {
                    if (acted.Order != null)
                        acted.Order.Give();

                    continue;
                }

                if (acted.Bound != null && acted.Bound.Acts)
                    acted.Bound.Do(acted.Entity.Block, acted.Seen);
                else if (acted.Order != null)
                    acted.Order.Give();

                acted.Entity.Asked();
            }
        }

        /// <summary>The first block an act at <paramref name="step" /> was refused by as it was done, with the block's reason; none where every block did it.</summary>
        /// <remarks>Asked once, as the act is done, of what is done the moment it is told: what takes time says its troubles while it is waited for.</remarks>
        SequenceFault Refused(int step)
        {
            var act = (Act)_sequence.Steps[step];
            if (!act.HasAction || act.Form != Nature.Native)
                return SequenceFault.None;

            var compiled = _workings.Compile(step, _bindings);
            foreach (var acted in compiled.Acts)
            {
                if (acted.Bound == null || acted.Action.Until != null)
                    continue;

                var said = acted.Bound.Trouble(acted.Entity.Block, acted.Seen);
                if (said.Ends)
                    return new SequenceFault(SequenceProblem.Stuck, step, said, acted.Entity.Name);
            }

            return SequenceFault.None;
        }

        /// <summary>How many times an act has been worked out.</summary>
        /// <remarks>Counted so it can be shown that a run repeating what it asks works nothing out again.</remarks>
        public int Compilations => _workings.Compilations;
    }
}
