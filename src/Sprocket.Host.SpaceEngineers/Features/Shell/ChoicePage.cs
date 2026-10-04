using System;
using System.Collections.Generic;
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

        string _hint;
        string _takes;
        Func<string, bool> _accepts;
        Action<string> _take;
        Relay _taking;

        /// <summary>A question called <paramref name="title" />, in the editor <paramref name="editor" />, with no answers yet.</summary>
        public ChoicePage(SprocketEditor editor, string title) : base(editor)
        {
            _title = title;
        }

        /// <inheritdoc />
        public override string Title => _title;

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

        /// <summary>What the box an answer is typed into is for, said where the pointer rests on it; null for nothing.</summary>
        public string TypedAbout { get; set; }

        /// <summary>Why the button that takes a typed answer cannot be pressed while nothing is typed; null for nothing.</summary>
        public string TypeFirst { get; set; }

        /// <summary>The answers, in the order they are offered, for whoever reads them back.</summary>
        public IReadOnlyList<ChoiceRow> Answers => _rows;

        /// <summary>The box an answer is typed into, or null where the question offers none.</summary>
        public TextBox Box => _take == null ? null : _box;

        /// <summary>Offers <paramref name="text" /> as an answer, <paramref name="choose" /> carried out when it is given.</summary>
        public void Add(string text, bool isCurrent, Action choose)
        {
            _options.Add(new Option(text, isCurrent, choose));
        }

        /// <summary>Offers a box an answer is typed into, <paramref name="take" /> given what is typed on Enter or the button.</summary>
        /// <remarks>
        /// For an answer that is one of too many to list, such as a length of time. <paramref name="accepts" /> refuses
        /// what could never be taken as it is typed, rather than taking it and doing nothing.
        /// </remarks>
        public void AddTyped(string hint, string takes, Func<string, bool> accepts, Action<string> take)
        {
            _hint = hint;
            _takes = takes;
            _accepts = accepts;
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
            _box.Hint = _hint;
            _box.Accepts = _accepts;
            _give.Text = _takes;
            _box.Tip = Tip.For(_box.Tip, TypedAbout);
            _give.Tip = Tip.For(_give.Tip, TypeFirst);

            var takes = Editor.Act(Cmd.ChoiceTaken, delegate { _take(_box.Text); });
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
        }
    }
}
