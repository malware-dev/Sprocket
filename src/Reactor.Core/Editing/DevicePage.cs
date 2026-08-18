using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// One block: what it is doing, what you can make it do, and what already touches it.
    /// </summary>
    /// <remarks>
    /// Authoring runs backward from here. The user points at a thing and answers a
    /// question about it - "what should make this happen?" - and the sequence appears as
    /// a side effect of the answer. There is no blank canvas anywhere in the flow,
    /// and no moment where somebody has to know what a sequence is before making one.
    /// </remarks>
    sealed class DevicePage : EditorPage
    {
        readonly long _deviceId;
        readonly List<Sequence> _referencing = new List<Sequence>();

        SequenceListComponent _sequences;

        public DevicePage(ReactorEditor editor, long deviceId) : base(editor)
        {
            _deviceId = deviceId;
        }

        public override string Title
        {
            get
            {
                var device = Editor.World.FindDevice(_deviceId);
                return device == null ? SequenceText.MissingDeviceText : device.Name;
            }
        }

        public override string Subtitle
        {
            get
            {
                var device = Editor.World.FindDevice(_deviceId);
                if (device == null)
                    return "No longer on the ship";
                return device.Kind.DisplayName + " — " + device.Status;
            }
        }

        public override void Build(List<EditorComponent> components)
        {
            var device = Editor.World.FindDevice(_deviceId);
            if (device == null)
            {
                components.Add(new NoticeComponent(
                    "This isn't part of your ship any more.", NoticeLevel.Problem));
                return;
            }

            var ids = new List<long>();
            ids.Add(_deviceId);

            var actions = Vocabulary.ActionsFor(Editor.World, ids);
            if (actions.Count > 0)
            {
                components.Add(new HeadingComponent("What should make this happen?"));
                var list = new ChoiceListComponent();
                for (var i = 0; i < actions.Count; i++)
                {
                    var actionId = actions[i].Id;
                    list.Entries.Add(new ChoiceEntry(actions[i].Text, false,
                        Editor.Register(delegate { CreateAffecting(actionId); })));
                }
                components.Add(list);
            }

            var events = Vocabulary.EventsFor(Editor.World, ids);
            if (events.Count > 0)
            {
                components.Add(new HeadingComponent("When this happens…"));
                var list = new ChoiceListComponent();
                for (var i = 0; i < events.Count; i++)
                {
                    var eventId = events[i].Id;
                    list.Entries.Add(new ChoiceEntry(events[i].Text, false,
                        Editor.Register(delegate { CreateTriggeredBy(eventId); })));
                }
                components.Add(list);
            }

            CollectSequences();
            if (_referencing.Count == 0)
                return;

            components.Add(new HeadingComponent("Already using this"));
            _sequences = new SequenceListComponent();
            for (var i = 0; i < _referencing.Count; i++)
            {
                var sequence = _referencing[i];
                _sequences.Entries.Add(new SequenceEntry(sequence.Id, "",
                    Editor.Register(delegate { Open(sequence); })));
            }
            components.Add(_sequences);
        }

        public override void Refresh()
        {
            // A sequence appearing or disappearing changes the shape of the screen, so it
            // has to be a rebuild rather than a value update.
            var before = _referencing.Count;
            CollectSequences();
            if (_referencing.Count != before)
            {
                Editor.Invalidate();
                return;
            }

            if (_sequences == null)
                return;

            SequenceSummary.Fill(_sequences.Entries, _referencing, Editor.World, Editor.Runner, Editor.CurrentTick);
        }

        void CollectSequences()
        {
            _referencing.Clear();
            var sequences = Editor.Program.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (SequenceSummary.References(sequences[i], _deviceId))
                    _referencing.Add(sequences[i]);
            }
        }

        void CreateAffecting(string actionId)
        {
            var sequence = Editor.Program.AddSequence(new WhenTrigger(""));
            var step = new Step(actionId);
            step.Targets.Add(_deviceId);
            sequence.Steps.Add(step);
            Editor.Push(new SequencePage(Editor, sequence));
        }

        void CreateTriggeredBy(string eventId)
        {
            var trigger = new WhenTrigger(eventId);
            trigger.Sources.Add(_deviceId);
            Editor.Push(new SequencePage(Editor, Editor.Program.AddSequence(trigger)));
        }

        void Open(Sequence sequence)
        {
            Editor.Push(new SequencePage(Editor, sequence));
        }
    }
}
