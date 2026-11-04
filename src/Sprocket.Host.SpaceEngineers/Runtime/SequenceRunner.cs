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
        // Ticks between looks, at an await, for something that has sent its blocks the other way.
        const int Conflicts = 60;

        readonly Sequence _sequence;

        // Kept in what the world saves.
        readonly RunMemory _memory;

        readonly CompiledRun _workings;

        readonly RunFaults _faults;

        /// <summary>
        /// One sequence, running for one of a system.
        /// </summary>
        /// <remarks>
        /// A null <paramref name="instance" /> means the slots are the blocks themselves. A run <paramref name="memory" />
        /// already holds takes up where it was, on the rota as well; a new one is given the next place.
        /// </remarks>
        public SequenceRunner(Sequence sequence, SystemInstance instance, AutomationRunner owner, RunMemory memory)
        {
            _sequence = sequence;
            _instance = instance;
            _owner = owner;
            _rota = owner.Rota;
            _memory = memory;
            _workings = new CompiledRun(sequence, instance, owner, memory, Heard);

            var kept = memory.Busy != null;
            if (!kept)
            {
                memory.Busy = new ShiftMemory { Place = _rota.Join() };
                memory.ConflictsAt = -Conflicts;
            }

            _busy = new Shift(memory.Busy, Continued);
            _faults = new RunFaults(sequence, instance, owner, memory, kept);

            if (!kept)
                return;

            _rota.Restore(_busy);

            // What the world remembered can be of the program before an edit; a run at a step it no longer has is ended.
            if (IsRunning && !Fits())
            {
                Finish();
                return;
            }

            // A run held for blocks that say when they get there listens to them from the first tick after the world loads,
            // whenever its own next look falls.
            if (IsRunning && _memory.Holding)
            {
                _listen = new Shift(0, Listen);
                _rota.At(_listen, int.MinValue);
            }

            if (!IsRunning || _busy.Listed)
                return;

            // Nothing in a session leaves a run going with nothing to move it; only what was written down can.
            GameLog.Say($"A run of \"{_sequence.Name}\" had nothing on the rota to move it, and was put back.");
            _rota.At(_busy, int.MinValue);
        }

        /// <summary>Whether every step the remembered run is at, or waits on, is a step of the sequence, and each it waits on an act.</summary>
        bool Fits()
        {
            var steps = _sequence.Steps;
            if (_memory.Step >= steps.Count)
                return false;

            foreach (var step in _memory.Outstanding)
            {
                if (step < 0 || step >= steps.Count || !(steps[step] is Act))
                    return false;
            }

            return true;
        }

        /// <summary>Works out the acts the run is held for, which listens to every block among them that says when it gets there.</summary>
        /// <remarks>Once, on the first tick after a run is taken up from what the world remembered; looks at nothing and moves nothing on.</remarks>
        void Listen(int tick)
        {
            _bindings = _owner.Current;
            foreach (var step in _memory.Outstanding)
            {
                if (((Act)_sequence.Steps[step]).HasAction)
                    _workings.Compile(step, _bindings);
            }
        }

        /// <summary>What it remembers, which the world's save writes.</summary>
        public RunMemory Memory => _memory;

        /// <summary>
        /// Starts another sequence of the same system, on the same one of it.
        /// </summary>
        /// <remarks>Set by whatever owns the runners; null until then.</remarks>
        public Action<int, bool> Calls { get; set; }

        readonly SystemInstance _instance;

        /// <summary>
        /// Whether this sequence may run for this one of its system.
        /// </summary>
        /// <remarks>Three switches, all of which have to be on: the sequence's, its system's, and this instance's.</remarks>
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
        public StopCause StoppedBy
        {
            get { return (StopCause)_memory.StoppedBy; }
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
        /// <remarks>Read, never worked out, and the worst news first.</remarks>
        public SequenceFault Fault => _faults.Fault;

        /// <summary>What is known to be wrong with this run, as each part of Sprocket finds it.</summary>
        public RunFaults Faults => _faults;

        readonly AutomationRunner _owner;
        readonly Rota _rota;

        readonly Shift _busy;

        // Once, after a held run is taken up from what the world remembered; null otherwise.
        readonly Shift _listen;


        /// <summary>
        /// What a block being waited on says has taken it out of this sequence's hands, at its step and naming it, or none;
        /// and keeps what one says stops it finishing.
        /// </summary>
        /// <remarks>
        /// Asked on a cadence of a run already held, and only of the blocks of the steps holding it; each answer costs the
        /// block a component lookup and a comparison.
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
                            stuck = new SequenceFault(SequenceProblem.Stalled, step, BlockTrouble.None, entity.Name);

                        continue;
                    }

                    // Done what it was told short of the state, which only the block can say; the step is settled there.
                    if (!reached && acted.Bound.Done(entity.Block, acted.Seen))
                    {
                        acted.Done = true;
                        continue;
                    }

                    BlockTrouble said = acted.Bound.Trouble(entity.Block, acted.Seen);
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

                // From the runner's clock, since nothing looks at a run between the start of a wait and its end.
                var left = _memory.WaitUntil - _owner.CurrentTick;
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
        /// <remarks>A trigger that fires while the sequence runs is skipped, never queued.</remarks>
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
        /// <remarks>Calls only: its own trigger firing while it is off is not counted.</remarks>
        public int RefusedCount
        {
            get { return _memory.Refused; }
            private set { _memory.Refused = value; }
        }

        /// <summary>Stops the run where what it runs on has just been switched off, unless it was started by hand.</summary>
        /// <remarks>
        /// Told on the tick a switch is flipped, and on no other. Nothing moves on while the machine Sprocket runs on is
        /// unpowered or switched off, and what was running is kept rather than stopped; the run's own next look stops it
        /// once the machine is back.
        /// </remarks>
        public void Switched()
        {
            if (IsRunning && _owner.Live && !CanRun && !_memory.ByHand)
                Interrupt(StopCause.Switch, 0);
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
                Interrupt(StopCause.Switch, 0);
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
                // Every block says when it gets there, so it is looked at then, and once in a hundred ticks in case it never says.
                if (Announced())
                {
                    _rota.Every(_busy, Rota.Hundred);
                    return;
                }

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

        /// <summary>Takes this sequence off the rota for good, when it or its Sprocket no longer exists, leaving what it remembers as it was.</summary>
        /// <remarks>What it remembers may be another runner's now, as where a client's edit builds the Sprocket again, so letting go of the rota must not change it.</remarks>
        public void Retire()
        {
            _busy.Forget();
            _rota.Leave(_busy);
            if (_listen != null)
                _rota.Leave(_listen);

            _workings.Deafen();
        }

        /// <summary>Whether everything the run is held for is a block listened to for getting where it was told, and no sequence it started.</summary>
        /// <remarks>Reads what is worked out already, and asks the blocks nothing.</remarks>
        bool Announced()
        {
            if (_memory.Started.Count > 0)
                return false;

            foreach (var step in _memory.Outstanding)
            {
                if (!((Act)_sequence.Steps[step]).HasAction)
                    continue;

                var compiled = _workings.Compile(step, _bindings);
                foreach (var acted in compiled.Acts)
                {
                    if (acted.Action.Until != null && !acted.Heard)
                        return false;
                }
            }

            return true;
        }

        /// <summary>A block the run listens to says it may have got where it was told: a run held for it is looked at on the next tick.</summary>
        void Heard()
        {
            if (IsRunning && _memory.Holding)
                _rota.At(_busy, _owner.CurrentTick + 1);
        }

        /// <summary>Starts the sequence by name, from another sequence, a trigger or a button.</summary>
        /// <remarks>
        /// Refused, and counted, where the machine has no power, or where the sequence is switched off and the call is not
        /// by hand, or where a slot it needs is empty.
        /// </remarks>
        public void Call(Bindings bindings, int tick, bool live = true,
            bool byHand = false)
        {
            _bindings = bindings;
            _memory.Looked = tick;

            // Power is refused whoever asks, a person at the screen included.
            if (!live)
            {
                RefusedCount++;
                _owner.Log?.Refused(this, tick, RunRefusal.Unpowered);
                return;
            }

            // The switch is not refused to a run by hand; an empty slot is, as nothing can fill it mid-run.
            if (!CanRun && !byHand)
            {
                RefusedCount++;
                _owner.Log?.Refused(this, tick, RunRefusal.Off);
                return;
            }

            if (_faults.Found.Waits)
            {
                RefusedCount++;
                _owner.Log?.Refused(this, tick, RunRefusal.EmptySlot);
                return;
            }

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                _owner.Log?.Refused(this, tick, RunRefusal.Busy);
                return;
            }

            _memory.ByHand = byHand;
            Start(tick, byHand ? RunStart.Hand : RunStart.Call);
        }

        /// <summary>Whether the run under way was started by hand, which lets it run where its sequence is switched off.</summary>
        public bool ByHand => _memory.ByHand;

        /// <summary>
        /// Starts the sequence because its trigger fired, where it may run and is not already running.
        /// </summary>
        /// <remarks>
        /// A switched-off sequence, or one waiting for a slot to be filled, lets its own trigger go by uncounted; one already
        /// running counts it as skipped.
        /// </remarks>
        public void Triggered(Bindings bindings, int tick)
        {
            _bindings = bindings;
            _memory.Looked = tick;

            if (!CanRun || _faults.Found.Waits)
            {
                _owner.Log?.Refused(this, tick, CanRun ? RunRefusal.EmptySlot : RunRefusal.Off);
                return;
            }

            if (IsRunning)
            {
                SkippedCount++;
                LastSkippedTick = tick;
                _owner.Log?.Refused(this, tick, RunRefusal.Busy);
                return;
            }

            _memory.ByHand = false;
            Start(tick, RunStart.Trigger);
        }

        /// <summary>
        /// Ends the run, and calls off what it had in flight.
        /// </summary>
        public void Stop()
        {
            Called();
            Finish();
        }

        /// <summary>
        /// Ends the run from outside, as <see cref="Stop" /> does, and keeps what stopped it and at which step, where
        /// <paramref name="sequence" /> is the number of the sequence that did, where one did.
        /// </summary>
        /// <remarks>Kept until the sequence next runs, and only where it was running.</remarks>
        public void Interrupt(StopCause by, int sequence)
        {
            var running = IsRunning;
            var at = _memory.Step;
            Stop();
            if (!running)
                return;

            _owner.Log?.Stopped(this, _owner.CurrentTick, at, by, sequence);
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
        /// An action that waits for nothing is done the instant it is invoked. A block that has said it is done with what it
        /// was told, short of what it waits for, is settled until the act is done again or the run ends.
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

        void Start(int tick, RunStart how)
        {
            _owner.Log?.Started(this, tick, how);
            LastFiredTick = tick;
            FiredCount++;
            IsRunning = true;

            // What stopped the last run is about the last run.
            _faults.Quit = SequenceFault.None;
            _faults.Met(SequenceFault.None);
            _faults.Hold(SequenceFault.None);
            StoppedBy = StopCause.None;
            _memory.Step = 0;
            EnterStep();
            Advance();
            Schedule();
        }

        void EnterStep()
        {
            _memory.WaitTicks = 0;
            _memory.WaitUntil = -1;
            if (_memory.Step < 0 || _memory.Step >= _sequence.Steps.Count)
                return;

            _owner.Log?.Entered(this, _owner.CurrentTick, _memory.Step);
            var wait = _sequence.Steps[_memory.Step] as Wait;
            if (wait != null)
                _memory.WaitTicks = wait.Ticks;
        }

        /// <summary>
        /// Walks the steps until something holds the sequence up.
        /// </summary>
        /// <remarks>An act starts and the next line follows in the same tick, so acts overlap until an await.</remarks>
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

                    // A block the step needs that is gone, or may have been swapped, is found here; the working-out it rests
                    // on is done once per change.
                    var needed = act.HasAction ? _workings.Compile(_memory.Step, _bindings) : null;
                    if (needed != null && needed.Problem != SequenceProblem.None)
                    {
                        Meet(new SequenceFault(needed.Problem, _memory.Step, BlockTrouble.None, needed.Which));
                        return;
                    }

                    // Asked of a block that is off, an action or a value is lost, so the run ends here and says so.
                    var unable = needed == null ? SequenceFault.None : Unable(needed);
                    if (unable.Exists)
                    {
                        Meet(unable);
                        return;
                    }

                    Execute(_memory.Step);
                    if (needed != null)
                        _owner.Log?.Acted(this, _memory.Looked, _memory.Step, needed);

                    // Refused on the spot: the run ends here and says which block.
                    var refused = Refused(_memory.Step);
                    if (refused.Exists)
                    {
                        Meet(refused);
                        return;
                    }

                    // Kept so an await below knows what it is waiting for; an instant action is nothing to wait on.
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
                        Interrupt(StopCause.Switch, 0);
                        return;
                    }

                    Step();
                    continue;
                }

                var sets = step as SetState;
                if (sets != null)
                {
                    IsWaiting = false;

                    // Set in this step, and what it starts is started in it too, before the run goes on.
                    _owner.Set(_sequence.System, _instance, sets);
                    if (!IsRunning)
                        return;

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
                    // A block gone while it is waited on ends the run here, as one gone when its step is reached. Looked
                    // at as the wait is reached, and from then on at the hold's pace.
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
                            _owner.Log?.Holding(this, _memory.Looked, _memory.Step, _memory);
                        }

                        // A block sent the other way by something else ends the run where it stands. Looked for once a
                        // second, since a conflict comes from a press or a trigger.
                        var quit = ConflictsDue() ? Troubled() : SequenceFault.None;
                        if (quit.Exists)
                        {
                            Stop();
                            _faults.Quit = quit;
                            _owner.Log?.Faulted(this, _memory.Looked, quit);
                            return;
                        }

                        IsWaiting = true;
                        return;
                    }

                    _owner.Log?.Held(this, _memory.Looked, _memory.Step, _memory.Holding ? _memory.HoldSince : _memory.Looked);
                    Release();
                    _memory.Started.Clear();
                    _memory.Holding = false;
                    _faults.Hold(SequenceFault.None);
                    IsWaiting = false;
                    Step();
                    continue;
                }

                if (_memory.WaitUntil < 0)
                {
                    // A tick of the game's, so a wait lasts as long as it says however often the runner is woken. Pinned
                    // when the step is first reached, since a run resumed from a save is entered before it knows the tick.
                    _memory.WaitUntil = _memory.Looked + _memory.WaitTicks;
                    _owner.Log?.Waiting(this, _memory.Looked, _memory.Step, _memory.WaitUntil);
                }

                if (_memory.Looked < _memory.WaitUntil)
                {
                    IsWaiting = true;
                    return;
                }

                IsWaiting = false;
                Step();
            }

            _owner.Log?.Finished(this, _memory.Looked);
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

        /// <summary>Whether <paramref name="test" /> holds on this installation's states.</summary>
        bool Holds(StateTest test)
        {
            return _owner.States.Holds(_sequence.System, _instance, test);
        }

        /// <summary>
        /// Goes into the first branch of the if at the current step whose test holds, or past the if where none does; false
        /// where a test cannot be read and the run has ended there.
        /// </summary>
        bool Branch(BranchTest test)
        {
            var at = _memory.Step;
            while (true)
            {
                bool holds;
                var compared = test as Comparison;
                if (compared != null)
                {
                    var tested = _workings.Test(at, compared, _bindings);
                    if (tested.Problem != SequenceProblem.None)
                    {
                        Meet(new SequenceFault(tested.Problem, at, BlockTrouble.None, tested.Which));
                        return false;
                    }

                    if (compared.Quantifier == Quantifier.Average && tested.Reads.Count == 0)
                    {
                        Meet(new SequenceFault(SequenceProblem.NothingToAverage, at));
                        return false;
                    }

                    holds = Holds(tested, compared);
                    _owner.Log?.Tested(this, _memory.Looked, at, tested, holds);
                }
                else
                {
                    holds = Holds((StateTest)test);
                }

                if (holds)
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
        /// <remarks>
        /// Read once, when the run reaches the test; with no block to read, <c>all</c> holds, <c>any</c> does not and the sum
        /// is 0. An average needs at least one block read.
        /// </remarks>
        static bool Holds(CompiledTest tested, Comparison test)
        {
            if (test.Quantifier == Quantifier.Sum || test.Quantifier == Quantifier.Average)
            {
                var total = 0d;
                foreach (var read in tested.Reads)
                    total += read.Number();

                if (test.Quantifier == Quantifier.Average)
                    total /= tested.Reads.Count;

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
        SequenceFault Unable(CompiledAct compiled)
        {
            foreach (var acted in compiled.Acts)
            {
                // A block already where the step would take it has nothing to do, switched on or not.
                var until = acted.Action.Until;
                if (until != null && until.Holds(acted.Entity.Block, acted.Seen))
                    continue;

                if (acted.Entity.Refuses(acted.Action))
                    return new SequenceFault(SequenceProblem.Unable, _memory.Step, BlockTrouble.None, acted.Entity.Name);
            }

            return SequenceFault.None;
        }

        /// <summary>The first block still being waited for that is gone, or may have been swapped, or none.</summary>
        /// <remarks>A lookup per step waited on; what the step's blocks come to is worked out once per change.</remarks>
        SequenceFault Gone()
        {
            foreach (var step in _memory.Outstanding)
            {
                if (!((Act)_sequence.Steps[step]).HasAction)
                    continue;

                var compiled = _workings.Compile(step, _bindings);
                if (compiled.Problem != SequenceProblem.None)
                    return new SequenceFault(compiled.Problem, step, BlockTrouble.None, compiled.Which);
            }

            return SequenceFault.None;
        }

        /// <summary>Ends the run at a step it cannot do, keeping why, and has what it runs on switched off.</summary>
        void Meet(SequenceFault fault)
        {
            Stop();
            _faults.Met(fault);
            _owner.Log?.Faulted(this, _memory.Looked, fault);
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

                if (act.Form != ReachKind.Native)
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
        /// <remarks>Asked once, as the act is done, and only of what is done the moment it is told.</remarks>
        SequenceFault Refused(int step)
        {
            var act = (Act)_sequence.Steps[step];
            if (!act.HasAction || act.Form != ReachKind.Native)
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
        public int Compilations => _workings.Compilations;
    }
}
