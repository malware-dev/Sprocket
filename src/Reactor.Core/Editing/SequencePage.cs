using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;
using Reactor.World;

namespace Reactor.Editing
{
    /// <summary>
    /// One sequence, with room for the whole sentence.
    /// </summary>
    sealed class SequencePage : EditorPage
    {
        static readonly int[] WaitPresets = { 60, 120, 300, 600, 1800, 3600 };

        readonly Sequence _sequence;

        SequenceComponent _sentence;

        public SequencePage(ReactorEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;
        }

        public int SequenceId { get { return _sequence.Id; } }

        /// <summary>
        /// A named sequence is called by its name; an unnamed one reads as its own
        /// sentence. Either way "Sequence" as a heading would say nothing the user
        /// cannot already see.
        /// </summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing right now used to be repeated up here, and
        /// it reads as two different things saying the same words. Its state belongs on the
        /// sentence itself, beside the switch and the run button that change it, where it is
        /// next to the thing it is about.
        /// </remarks>
        public override string Title
        {
            get { return SequenceSummary.Describe(Editor.World, _sequence); }
        }

        /// <summary>
        /// Naming happens on the heading, because the heading is what the name is for.
        /// </summary>
        /// <remarks>
        /// Optional, and it has to stay optional. A name buys two things - a list of twenty
        /// sequences becomes legible, and more than one trigger can start the same one - but
        /// requiring it puts a naming task in front of somebody who only wants a door to open.
        /// An unnamed sequence reads as its own sentence and is no worse off, which is why
        /// clearing the name is as ordinary an act as giving one.
        /// </remarks>
        public override string CustomName { get { return _sequence.Name ?? ""; } }

        public override bool CanRename { get { return true; } }

        public override string RenamePrompt { get { return "What do you call this?"; } }

        public override void Rename(string name)
        {
            SetName(name);
        }

        public override void Build(List<EditorComponent> components)
        {
            _sentence = new SequenceComponent(_sequence.Id, BuildTrigger());
            _sentence.Enabled = _sequence.Enabled;
            _sentence.ToggleCommand = Editor.RegisterToggle(SetEnabled);

            // Everything that changes what the machine does waits until it is switched off.
            // The commands are simply not issued, so there is nothing for a target to invoke
            // by accident - and the screen keeps the same shape either way, so nothing appears
            // or disappears as somebody flips the switch.
            _sentence.Editable = !_sequence.CanRun;
            _sentence.LockedText = "Running. Stop it to make changes - whatever it has already "
                + "done stays done.";
            _sentence.DeleteCommand = Change(Delete);

            // Undo is an edit like any other, so it waits for the switch too.
            _sentence.UndoCommand = Editor.CanUndo(_sequence)
                ? Change(delegate { Editor.Undo(_sequence); })
                : 0;
            _sentence.UndoText = Editor.UndoLabel(_sequence);

            for (var i = 0; i < _sequence.Steps.Count; i++)
                _sentence.Steps.Add(BuildStep(i, _sequence.Steps[i]));

            // Two kinds of step, so two ways to add one. Waiting a set time is not an
            // action on a block and pretending otherwise is what makes the flow
            // awkward - it gets its own door instead.
            _sentence.AddOptions.Add(new ChoiceEntry("do something", false,
                Change(AddAction)));
            _sentence.AddOptions.Add(new ChoiceEntry("wait", false,
                Change(AddWait)));

            components.Add(_sentence);
        }

        public override void Refresh()
        {
            if (_sentence == null)
                return;

            var state = Editor.Runner.Get(_sequence);

            _sentence.Enabled = _sequence.Enabled;
            _sentence.Status = SequenceSummary.Status(_sequence, state);
            _sentence.Problem = SequenceSummary.Problem(Editor.World, _sequence, state);
            _sentence.JustRan = state != null && state.LastFiredTick >= 0
                && Editor.CurrentTick - state.LastFiredTick < 10;
            _sentence.SkippedCount = state == null ? 0 : state.SkippedCount;

            _sentence.LastRun = Elapsed.Ago(SequenceSummary.TicksSinceRun(state, Editor.CurrentTick));
            _sentence.Trigger.IsLiveNow = state != null && state.TriggerSignalIsTrue;

            for (var i = 0; i < _sentence.Steps.Count; i++)
                _sentence.Steps[i].IsCurrent = state != null && state.IsRunning && state.CurrentStep == i;
        }

        SentenceTrigger BuildTrigger()
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when != null)
                return BuildWhen(when);

            var every = _sequence.Trigger as EveryTrigger;
            if (every != null)
                return BuildEvery(every);

            return new SentenceTrigger("When");
        }

        SentenceTrigger BuildWhen(WhenTrigger when)
        {
            var events = Vocabulary.EventsFor(Editor.World, when.Sources);

            // Changing the blocks can invalidate the chosen event. Moving to a valid
            // one keeps the sentence honest; leaving it would show something that
            // reads fine but could never fire.
            if (events.Count > 0 && !HasEvent(events, when.EventId))
                when.EventId = events[0].Id;

            var trigger = new SentenceTrigger("When");
            trigger.Slots.Add(new SentenceSlot(
                SequenceText.DescribeSources(Editor.World, when),
                when.Sources.Count == 0,
                Change(EditTrigger)));

            trigger.Slots.Add(new SentenceSlot(
                EventText(events, when.EventId),
                events.Count == 0 || !HasEvent(events, when.EventId),
                Change(EditTrigger)));

            return trigger;
        }

        SentenceTrigger BuildEvery(EveryTrigger every)
        {
            var trigger = new SentenceTrigger("Every");
            trigger.Slots.Add(new SentenceSlot(
                every.IntervalTicks == 1 ? "tick" : every.IntervalTicks + " ticks",
                false,
                Change(EditCadence)));

            return trigger;
        }

        /// <summary>
        /// Issues a command only while the sequence is switched off.
        /// </summary>
        /// <remarks>
        /// Zero is the way a page says "not now". A target cannot invoke it, so there is
        /// nothing to guard against on the far side and no way for a stray press to reach a
        /// running machine. The screen keeps the same shape either way - the rows are all
        /// still there, they simply do nothing until the switch is off, which is why
        /// <see cref="SequenceComponent.LockedText"/> exists to say so.
        /// </remarks>
        int Change(System.Action action)
        {
            return _sequence.CanRun ? 0 : Editor.Register(action);
        }

        SentenceStep BuildStep(int index, Step step)
        {
            var position = index;
            var built = new SentenceStep(index + 1);
            built.MoveUpCommand = Change(delegate { Move(position, position - 1); });
            built.MoveDownCommand = Change(delegate { Move(position, position + 1); });
            built.DeleteCommand = Change(delegate { RemoveStep(step); });

            // A pure wait has no blocks and no verb, just a duration.
            var pause = step.Gate as AfterGate;
            if (step.Targets.Count == 0 && pause != null)
            {
                built.Lead = "wait";
                built.Slots.Add(new SentenceSlot(
                    SequenceText.DescribeDuration(pause.Ticks), false,
                    Change(delegate { EditWait(step); })));
                return built;
            }

            // Ordinarily a step is just "close the inner door" - the ordering is
            // carried by the list, so nothing here says when it happens. A gate reads
            // as a lead-in, and only appears when one was deliberately added.
            built.Lead = SequenceText.DescribeGate(Editor.World, step.Gate);

            var actions = Vocabulary.ActionsFor(Editor.World, step.Targets);
            if (actions.Count > 0 && !HasAction(actions, step.ActionId))
                step.ActionId = actions[0].Id;

            built.Slots.Add(new SentenceSlot(
                ActionText(actions, step.ActionId),
                actions.Count == 0 || !HasAction(actions, step.ActionId),
                Change(delegate { EditStep(step); })));

            built.Slots.Add(new SentenceSlot(
                SequenceText.DescribeEntities(Editor.World, step.Targets),
                step.Targets.Count == 0,
                Change(delegate { EditStep(step); })));

            return built;
        }

        /// <summary>
        /// Opens the whole trigger - blocks and what happens - as one path.
        /// </summary>
        /// <remarks>
        /// Both slots lead here rather than each opening half of it. Which half you
        /// pressed is not really a different intent: you are looking at the trigger.
        /// </remarks>
        void EditTrigger()
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            Editor.Push(new PickPage(Editor, "When something happens", true,
                when.Sources, when.EventId,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeTrigger(when, answer.Targets, answer.Chosen, answer.All));
                    Editor.Pop();
                },
                true, when.RequiresAll));
        }

        void EditCadence()
        {
            var every = _sequence.Trigger as EveryTrigger;
            if (every == null)
                return;

            // Same page the wizard uses, so the rates read identically wherever they
            // are met. Choosing one here simply returns.
            var was = every.IntervalTicks;
            Editor.Push(Editor.RatePage(every, delegate
            {
                var now = every.IntervalTicks;
                every.IntervalTicks = was;
                Editor.Do(_sequence, new SetCadence(was, now));
                Editor.Pop();
            }));
        }

        void EditStep(Step step)
        {
            var index = _sequence.Steps.IndexOf(step);

            Editor.Push(new PickPage(Editor, "Do something", false,
                step.Targets, step.ActionId,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeStep(index, step, answer.Targets, answer.Chosen));
                    Editor.Pop();
                }));
        }

        /// <summary>
        /// Asks what to affect, then what to do to it.
        /// </summary>
        /// <remarks>
        /// Blocks first, because the user knows which block they mean but may not know
        /// what verbs exist - pointing at a door and being told what a door can do is
        /// discovery, whereas every verb on the ship in one list is a wall.
        ///
        /// The step is not added until both are answered, so backing out leaves no
        /// half-written line behind.
        /// </remarks>
        void AddAction()
        {
            var origin = Editor.Depth;

            Editor.Push(new PickPage(Editor, "Do something", false,
                new List<long>(), "",
                delegate(PickAnswer answer)
                {
                    // Only once both answers are in. Backing out of the questions adds no
                    // step, so there is nothing to take back.
                    var step = new Step(answer.Chosen);
                    for (var i = 0; i < answer.Targets.Count; i++)
                        step.Targets.Add(answer.Targets[i]);

                    Editor.Do(_sequence,
                        new AddStep("adding a step", _sequence.Steps.Count, step));
                    Editor.UnwindTo(origin);
                }));
        }

        /// <summary>
        /// A timed wait, for the things that have no completion signal to wait on -
        /// flashing a beacon, mostly. Rare, and deliberately the second option.
        /// </summary>
        void AddWait()
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                var step = new Step("");
                step.Gate = new AfterGate(ticks);
                Editor.Do(_sequence,
                    new AddStep("adding a wait", _sequence.Steps.Count, step));
                Editor.UnwindTo(origin);
            }));
        }

        ChoicePage WaitPage(System.Action<int> chosen)
        {
            var page = new ChoicePage(Editor, "How long should it wait?");
            for (var i = 0; i < WaitPresets.Length; i++)
            {
                var ticks = WaitPresets[i];
                var label = SequenceText.DescribeDuration(ticks) + " - " + ticks + " ticks";
                page.Add(label, false, delegate { chosen(ticks); });
            }
            return page;
        }

        void EditWait(Step step)
        {
            var origin = Editor.Depth;
            Editor.Push(WaitPage(delegate(int ticks)
            {
                var after = step.Gate as AfterGate;
                Editor.Do(_sequence,
                    new SetWait(_sequence.Steps.IndexOf(step), after == null ? 0 : after.Ticks, ticks));
                Editor.UnwindTo(origin);
            }));
        }

        void RemoveStep(Step step)
        {
            Editor.Do(_sequence, new RemoveStep(_sequence.Steps.IndexOf(step), step));
        }

        void Move(int from, int to)
        {
            if (to < 0 || to >= _sequence.Steps.Count)
                return;

            Editor.Do(_sequence, new MoveStep(from, to));
        }

        /// <summary>
        /// Renaming deliberately does not rebuild the screen. The shape has not
        /// changed, and rebuilding would recreate the field mid-word and throw away
        /// the caret.
        /// </summary>
        void SetName(string value)
        {
            // Folded into one, because a name arrives a letter at a time.
            Editor.Do(_sequence, new Rename(_sequence.Name, value), true);
        }

        /// <summary>
        /// Running or parked, which is not one of the things undo covers.
        /// </summary>
        /// <remarks>
        /// Undo walks back what a sequence *is*, not whether it is running. Recording the
        /// switch would mean a press of undo could arm a machine - somebody stepping back
        /// through their edits and having the airlock quietly go live behind them, which is
        /// the exact accident the switch exists to prevent.
        ///
        /// Nothing is lost by leaving it out: the switch is one press either way and it is the
        /// most visible control on the screen.
        /// </remarks>
        void SetEnabled(bool on)
        {
            _sequence.Enabled = on;
            Editor.Invalidate();
        }

        /// <summary>
        /// Asks first. There is no undo, and this throws away everything somebody built.
        /// </summary>
        /// <remarks>
        /// It used to delete on the press, which is the one place in the editor where a
        /// mistake could not be walked back: every other action is live and reversible by
        /// doing the opposite, and this one is not reversible at all.
        ///
        /// The safe answer is listed first, so the reflex press lands on it, and it is spelt
        /// out rather than left to Back. Back does mean keep - it always means return to where
        /// you came from - but a screen whose only row is the destructive one is a screen where
        /// a stray press destroys something.
        ///
        /// How the question gets asked is the target's business as usual. Core says there is a
        /// question, what it is about, and what the answers do.
        /// </remarks>
        void Delete()
        {
            var page = new ChoicePage(Editor, "Delete this sequence?");
            page.Caption = SequenceSummary.Describe(Editor.World, _sequence);

            page.Add("Keep it", false, delegate { Editor.Pop(); });

            page.Add("Delete it", false, delegate
            {
                // The question goes first. Forgetting the sequence drops the page showing it,
                // but not this one on top of it, which would leave somebody standing on a
                // question about something that no longer exists.
                Editor.Pop();
                Editor.Program.RemoveSequence(_sequence);
                Editor.ForgetSequence(_sequence.Id);
            });
            page.WarnOnLast("This cannot be undone.");

            Editor.Push(page);
        }

        static bool HasEvent(List<EntityEvent> events, string id)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == id)
                    return true;
            }
            return false;
        }

        static bool HasAction(List<EntityAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id)
                    return true;
            }
            return false;
        }

        static string EventText(List<EntityEvent> events, string id)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == id)
                    return events[i].Text;
            }
            return SequenceText.EmptySlotText;
        }

        static string ActionText(List<EntityAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id)
                    return actions[i].Text;
            }
            return SequenceText.EmptySlotText;
        }
    }
}
