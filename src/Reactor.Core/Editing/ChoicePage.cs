using System;
using System.Collections.Generic;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// Picks a single value for a slot - an event, an action, a repeat rate.
    /// </summary>
    /// <remarks>
    /// Choosing returns immediately. These lists are short by construction: they
    /// only ever contain what the blocks in the sequence can actually do, so there is
    /// never an invalid option to offer, refuse, or explain.
    /// </remarks>
    sealed class ChoicePage : EditorPage
    {
        readonly string _title;
        readonly List<Option> _options = new List<Option>();

        public ChoicePage(ReactorEditor editor, string title) : base(editor)
        {
            _title = title;
        }

        public override string Title { get { return _title; } }

        public override string Subtitle
        {
            get
            {
                if (!string.IsNullOrEmpty(Caption))
                    return Caption;
                return _options.Count == 0 ? EmptyText : "";
            }
        }

        /// <summary>Shown when there is nothing to choose from yet.</summary>
        public string EmptyText { get; set; }

        /// <summary>
        /// What the question is about, when the title cannot carry it.
        /// </summary>
        /// <remarks>
        /// "Delete this sequence?" is the question; which sequence is a whole sentence and
        /// belongs underneath it. Naming the thing is the difference between a question
        /// somebody can answer and one they have to guess at.
        /// </remarks>
        public string Caption { get; set; }

        public void Add(string text, bool isCurrent, Action choose)
        {
            _options.Add(new Option(text, isCurrent, 0, choose));
        }

        /// <summary>
        /// Adds an option whose indicator blinks at the given rate, for choices about
        /// speed that a label alone cannot convey.
        /// </summary>
        public void Add(string text, bool isCurrent, int intervalTicks, Action choose)
        {
            _options.Add(new Option(text, isCurrent, intervalTicks, choose));
        }

        /// <summary>Sets the caution shown beside the option added last.</summary>
        public void WarnOnLast(string warning)
        {
            if (_options.Count > 0)
                _options[_options.Count - 1].Warning = warning;
        }

        public override void Build(List<EditorComponent> components)
        {
            var list = new ChoiceListComponent();
            list.Fills = true;
            list.EmptyText = EmptyText;

            for (var i = 0; i < _options.Count; i++)
            {
                var option = _options[i];
                var entry = new ChoiceEntry(option.Text, option.IsCurrent,
                    Editor.Register(option.Choose));
                entry.IntervalTicks = option.IntervalTicks;
                entry.Warning = option.Warning;
                list.Entries.Add(entry);
            }

            components.Add(list);
        }

        sealed class Option
        {
            public Option(string text, bool isCurrent, int intervalTicks, Action choose)
            {
                Text = text;
                IsCurrent = isCurrent;
                IntervalTicks = intervalTicks;
                Choose = choose;
                Warning = "";
            }

            public string Text { get; private set; }
            public bool IsCurrent { get; private set; }
            public int IntervalTicks { get; private set; }
            public Action Choose { get; private set; }
            public string Warning { get; set; }
        }
    }
}
