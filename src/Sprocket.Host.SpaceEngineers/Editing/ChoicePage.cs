using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Lists;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Editing
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

        string _hint;
        string _takes;
        Func<string, bool> _accepts;
        Action<string> _take;
        string _typed = "";

        public ChoicePage(SprocketEditor editor, string title) : base(editor)
        {
            _title = title;
        }

        public override string Title => _title;

        /// <summary>
        /// What the question is about, which a choice page says for as long as it is up.
        /// </summary>
        /// <remarks>
        /// Set when the page is made and never after, because the header holds its size for the life
        /// of a screen: a line that arrived later would take the whole screen down with it.
        /// </remarks>
        public override string Subtitle => Caption ?? "";

        /// <summary>Shown where the rows would be when there is nothing to choose from yet.</summary>
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

        /// <summary>A row that is typed into rather than pressed, taking what is typed on Enter.</summary>
        /// <remarks>
        /// For an answer that is one of too many to list, such as a length of time. Typed in the list
        /// itself, beside the answers that are listed, rather than on a screen of its own.
        /// </remarks>
        public void AddTyped(string hint, string takes, Func<string, bool> accepts, Action<string> take)
        {
            _hint = hint;
            _takes = takes;
            _accepts = accepts;
            _take = take;
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
                    Editor.Register(Cmd.Choice, i, option.Choose));
                entry.IntervalTicks = option.IntervalTicks;
                entry.Warning = option.Warning;
                list.Entries.Add(entry);
            }

            if (_take != null)
            {
                var typed = new ChoiceEntry(_takes, false,
                    Editor.Register(Cmd.ChoiceTaken, delegate { _take(_typed); }));
                typed.TextCommand = Editor.RegisterText(Cmd.ChoiceTyped,
                    delegate(string said) { _typed = said; }, _accepts);
                typed.Hint = _hint;
                typed.Typed = _typed;
                list.Entries.Add(typed);
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
