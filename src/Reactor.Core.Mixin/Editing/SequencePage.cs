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

        int _undoCommand;
        int _redoCommand;

        public SequencePage(ReactorEditor editor, Sequence sequence) : base(editor)
        {
            _sequence = sequence;
        }

        public int SequenceId => _sequence.Id;

        /// <summary>
        /// A named sequence is called by its name; an unnamed one reads as its own
        /// sentence. Either way "Sequence" as a heading would say nothing the user
        /// cannot already see.
        /// </summary>
        /// <remarks>
        /// No subtitle. What the sequence is doing right now belongs on the sentence itself, beside
        /// the switch that changes it - repeating it up here reads as two different things saying the
        /// same words.
        /// </remarks>
        public override string Title => Editor.SequenceText.Sequence(Editor.Runner.Bindings, _sequence);

        /// <summary>
        /// Naming happens on the heading, because the heading is what the name is for.
        /// </summary>
        /// <remarks>
        /// A sequence arrives already named - "Sequence 3" - the way a block does, so this is always
        /// overtyping rather than filling in a blank. That is a reversal of what stood here before,
        /// which argued naming had to stay optional because an unnamed sequence read as its own
        /// sentence. It did read, but it meant a row's identity changed as somebody edited it, and
        /// naming was still a chore they were expected to get round to eventually. A default asks
        /// nothing and a person renames it when they care.
        /// </remarks>
        public override string CustomName => _sequence.Name ?? "";

        public override bool CanRename => true;

        public override string RenamePrompt => "What do you call this?";

        /// <summary>
        /// A sequence has to be called something, now that the name is what identifies it.
        /// </summary>
        /// <remarks>
        /// The same reasoning a system already carried: with nothing underneath to fall back on, an
        /// empty name leaves a row in a list that cannot be told from the one below it.
        /// </remarks>
        public override bool NameRequired => true;

        public override void Rename(string name)
        {
            SetName(name);
        }

        /// <summary>
        /// Two parts: what starts it, and what it then does. Only the second takes a name.
        /// </summary>
        /// <remarks>
        /// This is the whole reason a heading is a list. One line with a name replacing all of it would
        /// mean that wanting a better word for what a sequence <b>does</b> cost you the trigger that
        /// starts it, with no way to keep both but to type the trigger into the name and hold the two
        /// in step by hand forever after.
        /// <para>
        /// The trigger half is not nameable, and that is a decision rather than an omission. "When the
        /// airlock button is pressed" is already the best possible name for itself; replacing it with
        /// "Entry request" trades legibility for reuse, and reuse only pays once a trigger is a thing you
        /// can point at from somewhere else. The shape here carries a name for it the day that arrives.
        /// </para>
        /// </remarks>
        public override void Heading(List<HeadingPart> parts)
        {
            parts.Add(new HeadingPart(""));

            var does = new HeadingPart("");
            does.Prompt = RenamePrompt;
            does.NameRequired = NameRequired;
            does.RenameCommand = Editor.RegisterText(Cmd.Rename, Rename);

            parts.Add(does);
            RefreshHeading(parts);
        }

        public override void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count < 2)
                return;

            parts[0].Text = Editor.SequenceText.Trigger(Editor.Runner.Bindings, _sequence.Trigger);

            // The name where there is one, and what it actually does where there is not - so an unnamed
            // sequence still reads as a whole sentence and naming one is a choice rather than a chore.
            // The same call a list row makes, so the two cannot come to disagree.
            parts[1].Name = CustomName;
            parts[1].Text = Editor.SequenceText.Doing(Editor.Runner.Bindings, _sequence);
        }

        public override void Build(List<EditorComponent> components)
        {
            _sentence = new SequenceComponent(_sequence.Id, BuildTrigger());
            _sentence.Enabled = _sequence.Enabled;
            _sentence.ToggleCommand = Editor.RegisterToggle(Cmd.Enable, SetEnabled);

            // Everything that changes what the system does waits until it is switched off.
            // The commands are simply not issued, so there is nothing for a target to invoke
            // by accident - and the screen keeps the same shape either way, so nothing appears
            // or disappears as somebody flips the switch.
            _sentence.Editable = !_sequence.CanRun;
            _sentence.LockedText = "Running. Stop it to make changes - whatever it has already "
                + "done stays done.";
            _sentence.DeleteCommand = Change(Cmd.DeleteSequence, Delete);

            // Both are edits, so both wait for the switch. Registered here whether or not
            // there is anything to walk back, because issuing a command needs the numbering
            // only a rebuild does - and whether there is anything to walk back changes without
            // one, since renaming deliberately does not rebuild. So Build decides whether the
            // command may exist at all, and Refresh decides whether it is offered.
            _undoCommand = Change(Cmd.Undo, delegate { Editor.Undo(_sequence); });
            _redoCommand = Change(Cmd.Redo, delegate { Editor.Redo(_sequence); });

            for (var i = 0; i < _sequence.Steps.Count; i++)
                _sentence.Steps.Add(BuildStep(i, _sequence.Steps[i]));

            // Two kinds of step, so two ways to add one. Waiting a set time is not an
            // action on a block and pretending otherwise is what makes the flow
            // awkward - it gets its own door instead.
            _sentence.AddOptions.Add(new ChoiceEntry("do something", false,
                Change(Cmd.AddAction, AddAction)));
            _sentence.AddOptions.Add(new ChoiceEntry("wait", false,
                Change(Cmd.AddWait, AddWait)));

            components.Add(_sentence);
        }

        public override void Refresh()
        {
            if (_sentence == null)
                return;

            var state = Editor.Runner.Get(_sequence);

            _sentence.Enabled = _sequence.Enabled;
            _sentence.Status = Editor.SequenceText.Status(SequenceSummary.Status(_sequence, state));
            _sentence.JustRan = state != null && state.LastFiredTick >= 0
                && Editor.CurrentTick - state.LastFiredTick < 10;
            _sentence.SkippedCount = state == null ? 0 : state.SkippedCount;

            _sentence.LastRun = Editor.TimeText.Ago(SequenceSummary.TicksSinceRun(state, Editor.CurrentTick));
            _sentence.Trigger.IsLiveNow = state != null && state.TriggerSignalIsTrue;

            for (var i = 0; i < _sentence.Steps.Count; i++)
                _sentence.Steps[i].IsCurrent = state != null && state.IsRunning && state.CurrentStep == i;

            // Read every tick rather than only when the screen is rebuilt, because renaming
            // changes what there is to walk back without changing the screen's shape.
            var canUndo = Editor.CanUndo(_sequence);
            _sentence.UndoCommand = canUndo ? _undoCommand : 0;
            _sentence.UndoText = canUndo ? Editor.UndoLabel(_sequence) : "";

            var canRedo = Editor.CanRedo(_sequence);
            _sentence.RedoCommand = canRedo ? _redoCommand : 0;
            _sentence.RedoText = canRedo ? Editor.RedoLabel(_sequence) : "";

            if (SecondPassed())
                Mark();
        }

        /// <summary>
        /// Says what is wrong once, and marks the line it is wrong on.
        /// </summary>
        /// <remarks>
        /// Two statements that do not repeat each other: the words say what, the mark says where.
        /// Words alone in front of a nine-step sequence leave somebody reading all of it to work out
        /// which part is being complained about, and they were led here from two screens away
        /// precisely so they would not have to look.
        /// <para>
        /// Every line is cleared and one is set, rather than the marks being left where they were.
        /// A fault that has been mended has to stop showing, and the line that was wrong is
        /// frequently the line that was just changed.
        /// </para>
        /// </remarks>
        void Mark()
        {
            var fault = SequenceSummary.Fault(Editor.World, Editor.Runner.Bindings, _sequence,
                Editor.Runner.Get(_sequence));

            _sentence.Problem = Editor.SequenceText.Problem(fault.Problem);
            _sentence.Trigger.HasProblem = fault.Exists && fault.Step == SequenceFault.Trigger;

            for (var i = 0; i < _sentence.Steps.Count; i++)
                _sentence.Steps[i].HasProblem = fault.Exists && fault.Step == i;
        }

        SentenceTrigger BuildTrigger()
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when != null)
                return BuildWhen(when);

            var every = _sequence.Trigger as EveryTrigger;
            if (every != null)
                return BuildEvery(every);

            return new SentenceTrigger(TriggerKind.When);
        }

        SentenceTrigger BuildWhen(WhenTrigger when)
        {
            var events = Vocabulary.EventsFor(Editor.Runner.Bindings, when.Sources);
            var chosen = FindEvent(events, when.EventId);

            // Changing the blocks can invalidate the chosen event. Moving to a valid
            // one keeps the sentence honest; leaving it would show something that
            // reads fine but could never fire.
            if (events.Count > 0 && chosen == null)
            {
                chosen = events[0];
                when.EventId = chosen.Id;
            }

            // Both slots open the same trip - which blocks and what happens to them are one
            // question - but they are two controls and must be two identities.
            var trigger = new SentenceTrigger(TriggerKind.When);
            trigger.Slots.Add(new EntitiesSlot(Resolved(when.Sources), when.RequiresAll,
                Change(Cmd.TriggerSlot, 0, EditTrigger)));

            trigger.Slots.Add(new EventSlot(chosen, Change(Cmd.TriggerSlot, 1, EditTrigger)));

            return trigger;
        }

        SentenceTrigger BuildEvery(EveryTrigger every)
        {
            var trigger = new SentenceTrigger(TriggerKind.Every);
            trigger.Slots.Add(new DurationSlot(every.IntervalTicks,
                Change(Cmd.EditCadence, EditCadence)));

            return trigger;
        }

        /// <summary>
        /// Issues a command only while the sequence is switched off.
        /// </summary>
        /// <remarks>
        /// Zero is the way a page says "not now". A target cannot invoke it, so there is
        /// nothing to guard against on the far side and no way for a stray press to reach a
        /// running system. The screen keeps the same shape either way - the rows are all
        /// still there, they simply do nothing until the switch is off, which is why
        /// <see cref="SequenceComponent.LockedText"/> exists to say so.
        /// </remarks>
        int Change(Cmd kind, System.Action action)
        {
            return Change(kind, 0, 0, action);
        }

        int Change(Cmd kind, int which, System.Action action)
        {
            return Change(kind, which, 0, action);
        }

        int Change(Cmd kind, int which, int part, System.Action action)
        {
            return _sequence.CanRun ? 0 : Editor.Register(kind, which, part, action);
        }

        SentenceStep BuildStep(int index, Step step)
        {
            var position = index;

            // A pure wait has no blocks and no verb, just a duration.
            var pause = step.Gate as AfterGate;
            var waits = step.Targets.Count == 0 && pause != null;

            var built = new SentenceStep(index + 1, waits ? StepKind.Wait : StepKind.Do);
            built.Position = position;
            built.MoveToCommand = _sequence.CanRun
                ? 0
                : Editor.RegisterNumber(Cmd.MoveStep, position, delegate(int to) { Move(position, to); });
            built.DeleteCommand = Change(Cmd.DeleteStep, position, delegate { RemoveStep(step); });

            if (waits)
            {
                built.Slots.Add(new DurationSlot(pause.Ticks,
                    Change(Cmd.StepSlot, position, 0, delegate { EditWait(step); })));
                return built;
            }

            // Ordinarily a step is just "close the inner door" - the ordering is
            // carried by the list, so nothing here says when it happens. A gate reads
            // as a lead-in, and only appears when one was deliberately added.
            built.Gate = BuildGate(step.Gate);

            var actions = Vocabulary.ActionsFor(Editor.Runner.Bindings, step.Targets);
            var chosen = FindAction(actions, step.ActionId);
            if (actions.Count > 0 && chosen == null)
            {
                chosen = actions[0];
                step.ActionId = chosen.Id;
            }

            built.Slots.Add(new ActionSlot(chosen,
                Change(Cmd.StepSlot, position, 0, delegate { EditStep(step); })));

            built.Slots.Add(new EntitiesSlot(Resolved(step.Targets), true,
                Change(Cmd.StepSlot, position, 1, delegate { EditStep(step); })));

            return built;
        }

        /// <summary>
        /// What holds a step back, or null where nothing does.
        /// </summary>
        /// <remarks>
        /// The slots carry no command, because a gate cannot yet be changed from the sentence. They
        /// are slots all the same, so that the day it can be, a command number appears on one rather
        /// than a second way of saying the same thing arriving beside them.
        /// </remarks>
        SentenceGate BuildGate(StepGate gate)
        {
            var once = gate as OnceGate;
            if (once != null)
                return Guard(GateKind.Once, once.Condition);

            var only = gate as OnlyIfGate;
            if (only != null)
                return Guard(GateKind.OnlyIf, only.Condition);

            var after = gate as AfterGate;
            if (after != null)
            {
                var timed = new SentenceGate(GateKind.After);
                timed.Slots.Add(new DurationSlot(after.Ticks, 0));
                return timed;
            }

            return null;
        }

        SentenceGate Guard(GateKind kind, Condition condition)
        {
            var built = new SentenceGate(kind);
            var entities = condition == null ? new List<string>() : condition.Entities;

            built.Slots.Add(new EntitiesSlot(Resolved(entities), true, 0));
            built.Slots.Add(new StateSlot(FindState(condition), 0));

            return built;
        }

        /// <summary>
        /// The state a condition reads, taken from the first entity that still declares it.
        /// </summary>
        /// <remarks>
        /// A condition names one state id across several entities, and they need not all still be
        /// here. The first that answers is enough to say what the condition is about.
        /// </remarks>
        EntityState FindState(Condition condition)
        {
            if (condition == null)
                return null;

            for (var i = 0; i < condition.Entities.Count; i++)
            {
                var entity = Editor.Runner.Bindings.Entity(condition.Entities[i]);
                if (entity == null)
                    continue;

                var state = entity.Kind.FindState(condition.StateId);
                if (state != null)
                    return state;
            }

            return null;
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
                Resolved(when.Sources), when.EventId,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeTrigger(when, Declared(answer.Targets), answer.Chosen, answer.All));

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
                Resolved(step.Targets), step.ActionId,
                delegate(PickAnswer answer)
                {
                    Editor.Do(_sequence,
                        new ChangeStep(index, step, Declared(answer.Targets), answer.Chosen));
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
                    var named = Declared(answer.Targets);
                    for (var i = 0; i < named.Count; i++)
                        step.Targets.Add(named[i]);

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
                page.Add(Editor.TimeText.WaitChoice(ticks), false, delegate { chosen(ticks); });
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
            // Refused here as well as wherever it was typed, because a target is the part that can be
            // written wrong - and cut to length in the one place that owns what a name may be.
            var wanted = Naming.Clean(value);
            if (wanted.Length == 0)
                return;

            // Folded into one, because a name arrives a letter at a time.
            Editor.Do(_sequence, new Rename(_sequence.Name, wanted), true);
        }

        /// <summary>
        /// Running or parked, which is not one of the things undo covers.
        /// </summary>
        /// <remarks>
        /// Undo walks back what a sequence *is*, not whether it is running. Recording the
        /// switch would mean a press of undo could arm a system - somebody stepping back
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
        /// The one place in the editor where a mistake cannot be walked back: every other action is
        /// live and reversible by doing the opposite, and this one is not reversible at all.
        /// <para>
        /// The safe answer is listed first, so the reflex press lands on it, and it is spelt out rather
        /// than left to back. Back does mean keep - it always means return to where you came from - but
        /// a screen whose only row is the destructive one is a screen where a stray press destroys
        /// something.
        /// </para>
        /// <para>
        /// How the question gets asked is the target's business as usual. Core says there is a question,
        /// what it is about, and what the answers do.
        /// </para>
        /// </remarks>
        void Delete()
        {
            var page = new ChoicePage(Editor, "Delete this sequence?");
            page.Caption = Editor.SequenceText.Sequence(Editor.Runner.Bindings, _sequence);

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

        static EntityEvent FindEvent(List<EntityEvent> events, string id)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == id)
                    return events[i];
            }
            return null;
        }

        static EntityAction FindAction(List<EntityAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id)
                    return actions[i];
            }
            return null;
        }
    }
}
