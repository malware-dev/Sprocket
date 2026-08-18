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
        public override string Title
        {
            get { return SequenceSummary.Describe(Editor.World, _sequence); }
        }

        public override string Subtitle
        {
            get { return SequenceSummary.Status(_sequence, Editor.Runner.Get(_sequence), Editor.Beat); }
        }

        public override void Build(EditorScreen screen)
        {
            _sentence = new SequenceComponent(_sequence.Id, BuildTrigger());
            _sentence.Enabled = _sequence.Enabled;
            _sentence.ToggleCommand = Editor.RegisterToggle(SetEnabled);
            _sentence.DeleteCommand = Editor.Register(Delete);
            _sentence.Name = _sequence.Name;
            _sentence.NameCommand = Editor.RegisterText(SetName);
            _sentence.RunCommand = Editor.Register(Run);

            for (var i = 0; i < _sequence.Steps.Count; i++)
                _sentence.Steps.Add(BuildStep(i, _sequence.Steps[i]));

            // Only one way to extend a sequence. There is no "wait" to add, because
            // waiting is what a step already does.
            _sentence.AddOptions.Add(new ChoiceEntry("do something", false,
                Editor.Register(AddAction)));

            screen.Components.Add(_sentence);
        }

        public override void Refresh()
        {
            if (_sentence == null)
                return;

            var state = Editor.Runner.Get(_sequence);

            _sentence.Enabled = _sequence.Enabled;
            _sentence.Name = _sequence.Name;
            _sentence.Status = SequenceSummary.Status(_sequence, state, Editor.Beat);
            _sentence.Problem = SequenceSummary.Problem(Editor.World, _sequence, state);
            _sentence.Pulse = state != null && state.LastFiredBeat >= 0
                && Editor.Beat - state.LastFiredBeat < 10;
            _sentence.SkippedCount = state == null ? 0 : state.SkippedCount;
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
                SequenceText.DescribeDevices(Editor.World, when.Sources),
                when.Sources.Count == 0,
                Editor.Register(EditSources)));

            trigger.Slots.Add(new SentenceSlot(
                EventText(events, when.EventId),
                events.Count == 0 || !HasEvent(events, when.EventId),
                Editor.Register(EditEvent)));

            return trigger;
        }

        SentenceTrigger BuildEvery(EveryTrigger every)
        {
            var trigger = new SentenceTrigger("Every");
            trigger.Slots.Add(new SentenceSlot(
                every.IntervalBeats == 1 ? "beat" : every.IntervalBeats + " beats",
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
                Editor.Register(delegate { EditAction(step); })));

            built.Slots.Add(new SentenceSlot(
                SequenceText.DescribeDevices(Editor.World, step.Targets),
                step.Targets.Count == 0,
                Editor.Register(delegate { EditTargets(step); })));

            return built;
        }

        void EditSources()
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            Editor.Push(new BlockPickerPage(Editor, "What should this listen to?",
                when.Sources, true));
        }

        void EditEvent()
        {
            var when = _sequence.Trigger as WhenTrigger;
            if (when == null)
                return;

            var page = new ChoicePage(Editor, "What should it do?");
            var events = Vocabulary.EventsFor(Editor.World, when.Sources);
            for (var i = 0; i < events.Count; i++)
            {
                var id = events[i].Id;
                page.Add(events[i].Text, id == when.EventId, delegate
                {
                    when.EventId = id;
                    Editor.Pop();
                });
            }
            page.EmptyText = "Pick what this listens to first — then there will be something to choose here.";
            Editor.Push(page);
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

        void EditAction(Step step)
        {
            var page = new ChoicePage(Editor, "What should happen to them?");
            var actions = Vocabulary.ActionsFor(Editor.World, step.Targets);
            for (var i = 0; i < actions.Count; i++)
            {
                var id = actions[i].Id;
                page.Add(actions[i].Text, id == step.ActionId, delegate
                {
                    step.ActionId = id;
                    Editor.Pop();
                });
            }
            page.EmptyText = "Pick what this affects first — then there will be something to choose here.";
            Editor.Push(page);
        }

        void EditTargets(Step step)
        {
            Editor.Push(new BlockPickerPage(Editor, "What should this affect?", step.Targets, false));
        }

        void AddAction()
        {
            _sequence.Steps.Add(new Step(""));
            Editor.Invalidate();
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
