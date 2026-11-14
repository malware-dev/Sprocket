using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Lumel.Controls;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    /// <summary>What this construct's automation is saved as, written out, and edited by hand.</summary>
    /// <remarks>
    /// Written out again once a second, not every tick, and only while nobody has changed what the box shows, since saving
    /// builds a string of the whole program.
    /// </remarks>
    public sealed partial class ScriptPage : EditorPage
    {
        string _shown;
        string _trouble = "";

        /// <summary>The page of the script the editor <paramref name="editor" /> saves.</summary>
        public ScriptPage(SprocketEditor editor) : base(editor)
        {
        }

        /// <inheritdoc />
        public override string Title => Editor.Texts.ScriptTitle;

        /// <summary>The listing is the whole of the page and scrolls both ways by itself.</summary>
        public override bool ScrollsItself => true;

        /// <summary>The box the script is written in, where the view has been made.</summary>
        public TextArea Listing => _listing;

        /// <summary>The button that puts what the box says in place of the program, where the view has been made.</summary>
        public Button Apply => _apply;

        /// <summary>What is wrong with the text as the page says it, or empty.</summary>
        public string Problem => _problem == null || !_problem.IsVisible ? "" : _problem.Text;

        /// <summary>What is wrong with the text: what the last Apply found, or why the program would not read.</summary>
        string Trouble => _trouble.Length > 0 ? _trouble : Editor.Program.Trouble;

        /// <summary>Fills the listing, where it has been made and nobody has changed it, and makes the button's press as things stand.</summary>
        public override void Build()
        {
            if (_listing == null)
                return;

            _apply.Command = Editor.Act(CommandKind.Mend, Applied);
            Settle();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            if (_listing == null || !SecondPassed())
                return;

            Settle();
        }

        /// <summary>Fills the listing for the first time, and gives the button its words and its press.</summary>
        protected override void ViewCreated()
        {
            _apply.Text = Editor.Texts.ScriptApply;
            _apply.Command = Editor.Act(CommandKind.Mend, Applied);
            Show();
            Said();
            Point();
        }

        /// <summary>Puts what the box says in place of the program, or says what is wrong with it.</summary>
        void Applied()
        {
            if (_listing == null || (_listing.Text == _shown && Editor.Program.Intact))
                return;

            _trouble = Editor.Apply(_listing.Text);
            if (_trouble.Length == 0)
                Show();

            Said();
            Point();
        }

        /// <summary>Writes the listing out again while nobody has changed it, which also lets go of what the last Apply found.</summary>
        void Settle()
        {
            if (_listing.Text == _shown)
            {
                _trouble = "";
                Show();
            }

            Said();
        }

        void Show()
        {
            // Setting a box's text replaces it, measuring every line again and putting the caret at the start, so the same
            // listing written out again is left where it is.
            var written = Written();
            if (written == _shown && _listing.Text == written)
                return;

            _shown = written;
            _listing.Text = _shown;
        }

        /// <summary>The text that would not read, where there is one, and the program as it is saved otherwise.</summary>
        string Written()
        {
            var program = Editor.Program;
            return program.Unread.Length > 0 ? program.Unread : ProgramWriter.Save(program, Editor.World.Values);
        }

        /// <summary>Says what is wrong with the text, in the reader's own words.</summary>
        void Said()
        {
            var trouble = Trouble;
            _problem.Text = trouble;
            _problem.IsVisible = trouble.Length > 0;

            _spacer.IsVisible = trouble.Length == 0;
        }

        /// <summary>Selects the line what is wrong names, in view and ready to be typed over, where it names one.</summary>
        void Point()
        {
            var line = ProgramReader.LineOf(Trouble);
            if (line < 1)
                return;

            _listing.SelectLine(line - 1);
            if (Editor.Input != null)
                Editor.Input.Focus(_listing);
        }
    }
}
