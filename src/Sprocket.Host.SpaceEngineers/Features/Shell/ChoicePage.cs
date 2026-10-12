using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>A question answered by choosing one of a few answers, or by typing one where none of them will do.</summary>
    /// <remarks>
    /// Choosing returns at once. These lists are short by construction: they only ever hold what can actually be done,
    /// so there is never an answer to offer, refuse or explain. The elements it is drawn with are declared in
    /// <c>ChoicePage.View.cs</c>, and nothing else is; this half is what it asks and what each answer does.
    /// </remarks>
    public sealed partial class ChoicePage : EditorPage
    {
        readonly string _title;
        readonly List<Option> _options = new List<Option>();
        readonly List<ChoiceRow> _rows = new List<ChoiceRow>();

        /// <summary>A question called <paramref name="title" />, in the editor <paramref name="editor" />, with no answers yet.</summary>
        public ChoicePage(SprocketEditor editor, string title) : base(editor)
        {
            _title = title;
        }

        /// <inheritdoc />
        public override string Title => _title;

        /// <inheritdoc />
        public override bool Asks => true;

        /// <summary>What the question is about, which a choice page says for as long as it is up.</summary>
        /// <remarks>
        /// Set when the page is made and never after, because the header holds its size for the life of a screen: a
        /// line that arrived later would take the whole screen down with it.
        /// </remarks>
        public override string Subtitle => Caption ?? "";

        /// <summary>Shown where the answers would be when there is nothing to choose from yet.</summary>
        public string EmptyText { get; set; }

        /// <summary>What the question is about, when the title cannot carry it.</summary>
        /// <remarks>
        /// "Delete this sequence?" is the question; which sequence is a whole sentence and belongs underneath it.
        /// Naming the thing is the difference between a question somebody can answer and one they have to guess at.
        /// </remarks>
        public string Caption { get; set; }

        /// <summary>The answers, in the order they are offered, for whoever reads them back.</summary>
        public IReadOnlyList<ChoiceRow> Answers => _rows;

        /// <summary>Offers <paramref name="text" /> as an answer, <paramref name="choose" /> carried out when it is given.</summary>
        public void Add(string text, bool isCurrent, Action choose)
        {
            _options.Add(new Option(text, isCurrent, choose));
        }

        /// <summary>Sets the mark of what the answer added last names.</summary>
        public void MarkLast(string mark)
        {
            if (_options.Count > 0)
                _options[_options.Count - 1].Mark = mark;
        }

        /// <summary>Sets the caution shown under the answer added last.</summary>
        public void WarnOnLast(string warning)
        {
            if (_options.Count > 0)
                _options[_options.Count - 1].Warning = warning;
        }

        /// <summary>Builds nothing: the rows are made with the view.</summary>
        public override void Build()
        {
        }

        /// <summary>Makes a row for each answer, each given by its own press.</summary>
        protected override void ViewCreated()
        {
            _empty.Text = EmptyText;
            _empty.IsVisible = _options.Count == 0 && !string.IsNullOrEmpty(EmptyText);

            for (var i = 0; i < _options.Count; i++)
            {
                var option = _options[i];
                var row = new ChoiceRow
                {
                    Mark = option.Mark,
                    Text = option.Text,
                    IsGiven = option.IsCurrent,
                    Caution = option.Warning,
                    Command = Editor.Act(Cmd.Choice, option.Choose)
                };

                _rows.Add(row);
                _answers.Children.Add(row);
            }
        }

        sealed class Option
        {
            public Option(string text, bool isCurrent, Action choose)
            {
                Text = text;
                IsCurrent = isCurrent;
                Choose = choose;
                Warning = "";
            }

            public string Text { get; private set; }
            public bool IsCurrent { get; private set; }
            public Action Choose { get; private set; }
            public string Warning { get; set; }
            public string Mark { get; set; }
        }
    }
}
