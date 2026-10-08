using System.Collections.Generic;
using Lumel.Controls;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Codex
{
    /// <summary>The codex, read one term at a time in a window that floats over the screen and stays open while the editor is used.</summary>
    /// <remarks>
    /// It has a history of its own like a browser's: following a link or choosing a term from its contents moves forward and
    /// drops whatever had been stepped back from, and its back and forward move through it. It first stands just outside the
    /// editor's right edge where the screen has room for it there, and inside that edge where it has not; where it is moved
    /// to is kept for as long as the block's editor is.
    /// </remarks>
    public sealed partial class CodexWindow : FloatingWindow
    {
        const float Wide = 700f;
        const float High = 860f;
        const float Gap = 16f;

        readonly SprocketEditor _editor;
        readonly List<Term> _history = new List<Term>();
        int _at;
        bool _listing;

        /// <summary>The codex in the editor <paramref name="editor" />, where its window was last moved to.</summary>
        public CodexWindow(SprocketEditor editor)
        {
            _editor = editor;
            CreateView();
            Position = editor.Habits.CodexAt;
            Moved += delegate { _editor.Habits.CodexAt = Position; };
        }

        /// <summary>The term being read, or null before any has been.</summary>
        public Term Term => _history.Count == 0 ? null : _history[_at];

        /// <summary>Whether there is somewhere in the codex's history to step back to.</summary>
        public bool CanStepBack => _at > 0;

        /// <summary>Whether there is somewhere in the codex's history to step forward to.</summary>
        public bool CanStepForward => _at < _history.Count - 1;

        /// <summary>Whether the window shows its contents, every term, in place of the page.</summary>
        public bool IsListing => _listing;

        /// <summary>Moves on to <paramref name="term" />, dropping whatever had been stepped back from, and shows its page.</summary>
        public void Go(Term term)
        {
            if (term != null && term != Term)
            {
                if (_history.Count > 0)
                    _history.RemoveRange(_at + 1, _history.Count - _at - 1);

                _history.Add(term);
                _at = _history.Count - 1;
            }

            _listing = false;
            Show();
        }

        /// <summary>Steps back to the term read before, where there is one.</summary>
        public void StepBack()
        {
            if (!CanStepBack)
                return;

            _at--;
            _listing = false;
            Show();
        }

        /// <summary>Steps forward to the term stepped back from, where there is one.</summary>
        public void StepForward()
        {
            if (!CanStepForward)
                return;

            _at++;
            _listing = false;
            Show();
        }

        /// <summary>Shows the contents in place of the page, or the page again.</summary>
        public void List(bool listing)
        {
            _listing = listing;
            Show();
        }

        /// <inheritdoc />
        /// <remarks>Beside the editor's right edge, outside it where the screen has the room and inside it where it has not.</remarks>
        protected override Vector2 Home(RectangleF area, Vector2 size)
        {
            var right = area.X + area.Width;
            var room = Screen.Width > 0f ? Screen.X + Screen.Width - right : 0f;
            var x = room >= size.X + 2f * Gap ? right + Gap : right - size.X - Gap;
            return new Vector2(x, area.Y + (area.Height - size.Y) * 0.5f);
        }

        /// <summary>Brings the window up to the term being read, or its contents.</summary>
        void Show()
        {
            var term = Term;
            string name = null;
            _body.Markdown = term == null ? "" : term.Read(out name);
            _title.Text = _listing || term == null ? _editor.Texts.CodexContents : name;
            _body.IsVisible = !_listing;
            _list.IsVisible = _listing;
            _stepsBack.CanExecute = CanStepBack;
            _stepsForward.CanExecute = CanStepForward;
            _contents.IsPressed = _listing;

            var terms = _editor.Codex.All;
            for (var i = 0; i < _entries.Count; i++)
                _entries[i].Style = Entry(terms[i], terms[i] == term);
        }

        /// <summary>How <paramref name="term" /> stands in the contents: as a section's heading or a term, and as the one being read where <paramref name="shown" /> is set.</summary>
        static ButtonStyle Entry(Term term, bool shown)
        {
            if (term.Opens)
                return shown ? Styles.CodexSectionShown : Styles.CodexSection;

            return shown ? Styles.CodexEntryShown : Styles.CodexEntry;
        }
    }
}
