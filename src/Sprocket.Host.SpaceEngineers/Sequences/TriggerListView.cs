using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Layout;
using Sprocket.Host.SpaceEngineers.Ui.Look;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Words;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// The triggers of a system, each as the sentence it reads as.
    /// </summary>
    /// <remarks>
    /// The same line a step of a sequence is drawn as, at the same size and with the same chips,
    /// because it is the same act: words with the parts somebody chose sitting in them, each one
    /// opening the question it was answered with. A row drawn as a card could only be pressed as a
    /// whole, which is a row that has to ask which half of itself was meant.
    /// </remarks>
    public sealed class TriggerListView : ComponentView
    {
        sealed class Row
        {
            public Bar Line;
            public Wrap Sentence;
            public Label Lead;
            public Label Between;
            public Label Live;
            public Label Problem;
            public Glyph Delete;
            public Stack Text;

            public readonly List<Element> Starts = new List<Element>();
            public readonly List<Element> Does = new List<Element>();

            /// <summary>The word before each part of what it does, hidden where there is none.</summary>
            public readonly List<Label> Joins = new List<Label>();

            /// <summary>The crosses, and which part of the sentence each one belongs to.</summary>
            public readonly List<Glyph> Clears = new List<Glyph>();
            public readonly List<int> ClearsAt = new List<int>();

            /// <summary>What each part was last given, so an unchanged one is not worded again.</summary>
            public readonly List<SentenceSlot> Shown = new List<SentenceSlot>();

            public string Led;
            public string Joined;
        }

        readonly TriggerListComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);
        readonly List<Row> _rows = new List<Row>();

        int _revision = -1;

        public TriggerListView(TriggerListComponent component)
        {
            _component = component;
        }

        /// <summary>
        /// Told rather than asked. Every row announces its own values and the list announces its
        /// shape, so there is nothing to read on a frame where nothing has happened.
        /// </summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _stack; }
        }

        protected override void Refresh()
        {
            if (_revision != _component.Triggers.Revision || !SameShape())
                Rebuild();

            for (int i = 0; i < _rows.Count && i < _component.Triggers.Count; i++)
                Update(_rows[i], _component.Triggers[i]);
        }

        /// <summary>Whether every row still has a part for every part of its sentence.</summary>
        bool SameShape()
        {
            if (_rows.Count != _component.Triggers.Count)
                return false;

            for (int i = 0; i < _rows.Count; i++)
            {
                SentenceTrigger trigger = _component.Triggers[i];
                if (_rows[i].Starts.Count != trigger.Slots.Count
                    || _rows[i].Does.Count != trigger.Does.Count)
                    return false;
            }

            return true;
        }

        void Update(Row row, SentenceTrigger trigger)
        {
            // The words of the lead only when it changes, like every other wording on a row that is
            // read sixty times a second.
            string lead = SentenceText.Lead(trigger);
            if (!ReferenceEquals(row.Led, lead))
            {
                row.Led = lead;
                row.Lead.Text = lead;
            }

            if (!ReferenceEquals(row.Joined, trigger.DoesLead))
            {
                row.Joined = trigger.DoesLead;
                row.Between.Text = trigger.DoesLead;
                row.Between.Collapsed = trigger.DoesLead.Length == 0;
            }

            // Before the parts are filled, which is what records them as shown: a join is worked out
            // only for a part that has changed.
            for (int i = 0; i < row.Joins.Count && i < trigger.Does.Count; i++)
            {
                if (ReferenceEquals(row.Shown[trigger.Slots.Count + i], trigger.Does[i]))
                    continue;

                string join = SentenceText.Joining(trigger.Does, i);
                row.Joins[i].Text = join;
                row.Joins[i].Collapsed = join.Length == 0;
            }

            Fill(row, row.Starts, trigger.Slots, 0);
            Fill(row, row.Does, trigger.Does, trigger.Slots.Count);

            for (int i = 0; i < row.Clears.Count && i < row.ClearsAt.Count; i++)
            {
                SentenceSlot slot = Part(trigger, row.ClearsAt[i], trigger.Slots.Count);
                Widgets.Waiting(row.Clears[i], slot == null ? 0 : slot.ClearCommand,
                    "Ask again what this should do");
            }

            // Idle while the signal reads true is not a contradiction. It is the difference between a
            // moment and a situation, shown rather than explained.
            row.Live.Collapsed = !trigger.IsLiveNow;

            row.Problem.Text = trigger.Problem;
            row.Problem.Collapsed = trigger.Problem.Length == 0;

            // The same wash a broken row in a list gets, so arriving here from one arrives at
            // something already recognised.
            row.Line.Background = trigger.Problem.Length > 0 ? Palette.ProblemWash : Palette.None;

            Widgets.Waiting(row.Delete, trigger.DeleteCommand, "Delete this trigger");
        }

        /// <summary>The slot a part of the row stands for, whichever half it is in.</summary>
        static SentenceSlot Part(SentenceTrigger trigger, int at, int starts)
        {
            if (at < starts)
                return at < trigger.Slots.Count ? trigger.Slots[at] : null;

            var into = at - starts;
            return into < trigger.Does.Count ? trigger.Does[into] : null;
        }

        void Fill(Row row, List<Element> parts, List<SentenceSlot> slots, int from)
        {
            for (int i = 0; i < parts.Count && i < slots.Count; i++)
            {
                // Every tick, unlike the words: a slot handed back unchanged can still have stopped
                // being something to press.
                parts[i].Press = Deed.Sends(slots[i].Command);

                int at = from + i;
                if (ReferenceEquals(row.Shown[at], slots[i]))
                    continue;

                row.Shown[at] = slots[i];
                SentenceView.Apply(parts[i], slots[i], SentenceText.InTrigger(Host.World, slots[i]));
            }
        }

        void Rebuild()
        {
            _revision = _component.Triggers.Revision;

            _rows.Clear();
            _stack.Children.Clear();

            for (int i = 0; i < _component.Triggers.Count; i++)
                _stack.Children.Add(Build(_component.Triggers[i]).Line);
        }

        Row Build(SentenceTrigger trigger)
        {
            var row = new Row();

            row.Lead = Widgets.Label("", Palette.Muted, Lettering.Body);
            row.Lead.Wraps = false;

            row.Between = Widgets.Label("", Palette.Muted, Lettering.Body);
            row.Between.Wraps = false;

            row.Live = Widgets.Wash("true right now", Palette.Live, Palette.LiveWash);
            row.Live.Wraps = false;

            row.Sentence = new Wrap();
            row.Sentence.ItemSpacing = Space.Item;
            row.Sentence.LineSpacing = Space.Row;
            row.Sentence.Children.Add(row.Lead);

            for (int i = 0; i < trigger.Slots.Count; i++)
                Chip(row, row.Starts, row.Sentence, trigger.Slots[i]);

            row.Sentence.Children.Add(row.Between);

            for (int i = 0; i < trigger.Does.Count; i++)
            {
                Label join = Widgets.Label("", Palette.Muted, Lettering.Body);
                join.Wraps = false;
                join.Collapsed = true;
                row.Joins.Add(join);
                row.Sentence.Children.Add(join);

                Chip(row, row.Does, row.Sentence, trigger.Does[i]);
            }

            row.Sentence.Children.Add(row.Live);

            // Its own line under the sentence, because what is wrong and what the trigger says are
            // two readings, and losing one to show the other is how a diagnosis screen lies.
            row.Problem = Widgets.Label("", Palette.Problem, Lettering.Tiny);

            row.Text = Widgets.Column(Space.Line);
            row.Text.Children.Add(row.Sentence);
            row.Text.Children.Add(row.Problem);

            // The same bin the steps of a sequence carry, washed red on the hover, because it is the
            // same act: what this is on is gone when it is pressed.
            row.Delete = Widgets.Symbol(Symbols.Delete, Palette.Faint);
            row.Delete.Highlight = Palette.ProblemWash;

            row.Line = new Bar();
            row.Line.Spacing = Space.Item;
            row.Line.Padding = new Thickness(Space.Row, Space.ChipY);
            row.Line.Fill = row.Text;
            row.Line.Right = row.Delete;

            _rows.Add(row);
            return row;
        }

        /// <summary>
        /// One part of the sentence, with the cross inside it where it has one.
        /// </summary>
        /// <remarks>
        /// Inside the chip, because what it empties is that chip. A mark in the gap beside it reads
        /// as belonging to the whole sentence, and the sentence has its own mark at the end of the
        /// row - which destroys the trigger rather than emptying a part of it.
        /// </remarks>
        static void Chip(Row row, List<Element> parts, Wrap into, SentenceSlot slot)
        {
            var part = new Slot();
            parts.Add(part);
            row.Shown.Add(null);
            into.Children.Add(part);

            if (slot.ClearCommand == 0)
                return;

            // Putting away rather than destroying, which is what the two marks are for: this takes
            // back the answer, where the bin at the end of the row takes the trigger itself.
            Glyph clear = Widgets.Symbol(Symbols.Close, Palette.Faint);
            clear.Highlight = Palette.Panel;

            part.Clear = clear;

            row.Clears.Add(clear);
            row.ClearsAt.Add(row.Shown.Count - 1);
        }
    }
}
