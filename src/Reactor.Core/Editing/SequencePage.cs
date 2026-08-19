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
            _sentence.DeleteCommand = Editor.Register(Delete);
            _sentence.RunCommand = Editor.Register(Run);

            for (var i = 0; i < _sequence.Steps.Count; i++)
                _sentence.Steps.Add(BuildStep(i, _sequence.Steps[i]));

            // Two kinds of step, so two ways to add one. Waiting a set time is not an
            // action on a block and pretending otherwise is what makes the flow
            // awkward - it gets its own door instead.
            _sentence.AddOptions.Add(new ChoiceEntry("do something", false,
                Editor.Register(AddAction)));
            _sentence.AddOptions.Add(new ChoiceEntry("wait", false,
                Editor.Register(AddWait)));

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
                Editor.Register(EditTrigger)));

            trigger.Slots.Add(new SentenceSlot(
                EventText(events, when.EventId),
                events.Count == 0 || !HasEvent(events, when.EventId),
                Editor.Register(EditTrigger)));

            return trigger;
        }

        SentenceTrigger BuildEvery(EveryTrigger every)
        {
            var trigger = new SentenceTrigger("Every");
            trigger.Slots.Add(new SentenceSlot(
                every.IntervalTicks == 1 ? "tick" : every.IntervalTicks + " ticks",
                false,
                Editor.Register(EditCadence)));

            return trigger;
        }

        SentenceStep BuildStep(int index, Step step)
        {
            var position = index;
            var built = new SentenceStep(index + 1);
            built.MoveUpCommand = Editor.Register(delegate { Move(position, position - 1); });
            built.MoveDownCommand = Editor.Register(delegate { Move(position, position + 1); });
            built.DeleteCommand = Editor.Register(delegate { RemoveStep(step); });

            // A pure wait has no blocks and no verb, just a duration.
            var pause = step.Gate as AfterGate;
            if (step.Targets.Count == 0 && pause != null)
            {
                built.Lead = "wait";
                built.Slots.Add(new SentenceSlot(
                    SequenceText.DescribeDuration(pause.Ticks), false,
                    Editor.Register(delegate { EditWait(step); })));
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
                Editor.Register(delegate { EditStep(step); })));

            built.Slots.Add(new SentenceSlot(
                SequenceText.DescribeDevices(Editor.World, step.Targets),
                step.Targets.Count == 0,
                Editor.Register(delegate { EditStep(step); })));

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

            Editor.Push(new PickPage(Editor, "When something happens", when.Sources, true,
                delegate { return when.EventId; },
                delegate(string id) { when.EventId = id; },
                Editor.Pop,
                delegate { return when.RequiresAll; },
                delegate(bool all) { when.RequiresAll = all; }));
        }

        void EditCadence()
        {
            var every = _sequence.Trigger as EveryTrigger;
            if (every == null)
                return;

            // Same page the wizard uses, so the rates read identically wherever they
            // are met. Choosing one here simply returns.
            Editor.Push(Editor.RatePage(every, Editor.Pop));
        }

        void EditStep(Step step)
        {
            Editor.Push(new PickPage(Editor, "Do something", step.Targets, false,
                delegate { return step.ActionId; },
                delegate(string id) { step.ActionId = id; },
                Editor.Pop));
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
            var step = new Step("");

            Editor.Push(new PickPage(Editor, "Do something", step.Targets, false,
                delegate { return step.ActionId; },
                delegate(string id) { step.ActionId = id; },
                delegate
                {
                    _sequence.Steps.Add(step);
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
                _sequence.Steps.Add(step);
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
                step.Gate = new AfterGate(ticks);
                Editor.UnwindTo(origin);
            }));
        }

        void RemoveStep(Step step)
        {
            _sequence.Steps.Remove(step);
            Editor.Invalidate();
        }

        void Move(int from, int to)
        {
            if (to < 0 || to >= _sequence.Steps.Count)
                return;

            var step = _sequence.Steps[from];
            _sequence.Steps.RemoveAt(from);
            _sequence.Steps.Insert(to, step);
            Editor.Invalidate();
        }

        /// <summary>
        /// Renaming deliberately does not rebuild the screen. The shape has not
        /// changed, and rebuilding would recreate the field mid-word and throw away
        /// the caret.
        /// </summary>
        void SetName(string value)
        {
            _sequence.Name = value;
        }

        /// <summary>Starts it by hand, standing in for whatever would normally set it off.</summary>
        void Run()
        {
            Editor.Runner.Call(_sequence);
        }

        void SetEnabled(bool on)
        {
            _sequence.Enabled = on;
            Editor.Invalidate();
        }

        void Delete()
        {
            Editor.Program.RemoveSequence(_sequence);
            Editor.ForgetSequence(_sequence.Id);
        }

        static bool HasEvent(List<DeviceEvent> events, string id)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == id)
                    return true;
            }
            return false;
        }

        static bool HasAction(List<DeviceAction> actions, string id)
        {
            for (var i = 0; i < actions.Count; i++)
            {
                if (actions[i].Id == id)
                    return true;
            }
            return false;
        }

        static string EventText(List<DeviceEvent> events, string id)
        {
            for (var i = 0; i < events.Count; i++)
            {
                if (events[i].Id == id)
                    return events[i].Text;
            }
            return SequenceText.EmptySlotText;
        }

        static string ActionText(List<DeviceAction> actions, string id)
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
