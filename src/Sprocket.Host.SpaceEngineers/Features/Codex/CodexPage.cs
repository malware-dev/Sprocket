using System.Collections.Generic;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>The codex, read one term at a time beside the list of every term: its name, the sentence that says what it is, and the page that explains it.</summary>
    /// <remarks>
    /// One page on the editor's stack however far it is read, with a history of its own like a browser's: following a link or
    /// pressing a term in the margin moves forward and drops whatever had been stepped back from, Back and Forward move through
    /// it, and closing it returns to the screen it was opened from in one press.
    /// </remarks>
    public sealed partial class CodexPage : EditorPage
    {
        readonly List<Term> _history = new List<Term>();
        int _at;

        /// <summary>The codex open at <paramref name="term" />, in the editor <paramref name="editor" />.</summary>
        public CodexPage(SprocketEditor editor, Term term) : base(editor)
        {
            _history.Add(term);
        }

        /// <inheritdoc />
        public override string Title => Term.Name;

        /// <summary>The term being read.</summary>
        public Term Term => _history[_at];

        /// <inheritdoc />
        public override bool Browses => true;

        /// <inheritdoc />
        public override bool CanStepBack => _at > 0;

        /// <inheritdoc />
        public override bool CanStepForward => _at < _history.Count - 1;

        /// <summary>Shows the term being read, where the view has been made.</summary>
        public override void Build()
        {
            Show();
        }

        /// <summary>Moves on to <paramref name="term" />, dropping whatever had been stepped back from.</summary>
        public void Go(Term term)
        {
            if (term == null || term == Term)
                return;

            _history.RemoveRange(_at + 1, _history.Count - _at - 1);
            _history.Add(term);
            _at++;
            Editor.Invalidate();
        }

        /// <inheritdoc />
        public override void StepBack()
        {
            if (!CanStepBack)
                return;

            _at--;
            Editor.Invalidate();
        }

        /// <inheritdoc />
        public override void StepForward()
        {
            if (!CanStepForward)
                return;

            _at++;
            Editor.Invalidate();
        }

        /// <summary>Makes the page's links lead on within the codex, and writes the lead and the page of the term being read.</summary>
        protected override void ViewCreated()
        {
            Editor.Codex.Wire(_body, Go);
            Show();
        }

        void Show()
        {
            if (_lead == null)
                return;

            _lead.Text = Term.Summary;
            _body.Document = Markdown.Parse(Term.Page);

            var terms = Editor.Codex.All;
            for (var i = 0; i < _listed.Count; i++)
                _listed[i].Style = terms[i] == Term ? Styles.CodexEntryShown : Styles.CodexEntry;
        }
    }
}
