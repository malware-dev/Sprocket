using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Input;
using Lumel.Text;

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

        string _named;
        string _takes;
        string _widest;
        int _most;
        Action<int> _take;
        Relay _taking;

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

        /// <summary>What the box a number is typed into is for, said where the pointer rests on it; null for nothing.</summary>
        public string TypedAbout { get; set; }

        /// <summary>Why the button that takes a typed number cannot be pressed while nothing is typed; null for nothing.</summary>
        public string TypeFirst { get; set; }

        /// <summary>The answers, in the order they are offered, for whoever reads them back.</summary>
        public IReadOnlyList<ChoiceRow> Answers => _rows;

        /// <summary>The box a number is typed into, or null where the question offers none.</summary>
        public NumberBox Box => _take == null ? null : _box;

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

        /// <summary>
        /// Offers a whole number from one to <paramref name="most" /> to be typed beside what it is <paramref name="named" />,
        /// in a box as wide as <paramref name="widest" />, and <paramref name="take" /> given it on Enter or the button.
        /// </summary>
        /// <remarks>For an answer that is one of too many to list, such as a length of time.</remarks>
        public void AddNumber(string named, string takes, string widest, int most, Action<int> take)
        {
            _named = named;
            _takes = takes;
            _widest = widest;
            _most = most;
            _take = take;
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

        /// <summary>Makes a row for each answer, each given by its own press, and the box to type one in where there is one.</summary>
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

            if (_take == null)
                return;

            _typed.IsVisible = true;
            _called.Text = _named;
            _box.Widest = _widest;
            _box.Most = _most;
            _box.Accepts = Fits;
            _give.Text = _takes;
            _box.Tip = Tip.For(_box.Tip, TypedAbout);
            _give.Tip = Tip.For(_give.Tip, TypeFirst);

            var takes = Editor.Act(Cmd.ChoiceTaken, delegate
            {
                int number;
                if (int.TryParse(_box.Text, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number > 0)
                    _take(number);
            });
            _taking = new Relay(delegate
            {
                if (takes != null && takes.CanExecute)
                    takes.Execute();
            }) { CanExecute = false };

            // Offered only while there is something to take, and told so by the box as it is typed into.
            _box.Edited += delegate
            {
                _taking.CanExecute = _box.Text.Length > 0;
                _give.Tip = Tip.For(_give.Tip, _taking.CanExecute ? null : TypeFirst);
            };

            _give.Command = _taking;
            _box.Submit = _taking;
        }

        /// <summary>Whether <paramref name="said" /> is nothing yet, or a number no larger than the most the box takes.</summary>
        bool Fits(string said)
        {
            int number;
            return said.Length == 0 || int.TryParse(said, NumberStyles.None, CultureInfo.InvariantCulture, out number) && number <= _most;
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
