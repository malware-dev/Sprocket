using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Lumel.Controls;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    /// <summary>What this construct's automation is saved as, written out, and edited by hand.</summary>
    /// <remarks>
    /// Reached in developer mode, and from the notice about a program whose text has an error in it, which is mended here. The
    /// script is what a program actually is on disk, so reading it is the shortest way to tell whether the editor built what its
    /// screens claim, and editing it is the only way to mend a text that would not read.
    /// <para>
    /// <b>Written out afresh once a second, not on a tick,</b> and only while nobody has changed what the box shows. Saving walks
    /// every system, sequence and step and builds a string of the lot, which is not a thing to do sixty times a second to
    /// answer a question nobody asked. The listing it is drawn on is declared in <c>ScriptPage.View.cs</c>.
    /// </para>
    /// </remarks>
    sealed partial class ScriptPage : EditorPage
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
        public EditBox Listing => _listing;

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

            _apply.Command = Editor.Act(Cmd.Mend, Applied);
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
        /// <remarks>
        /// Pressed as mending rather than as an edit, so a program that would not read asks nothing before its text is put in
        /// its place.
        /// </remarks>
        protected override void ViewCreated()
        {
            _apply.Text = Editor.Texts.ScriptApply;
            _apply.Command = Editor.Act(Cmd.Mend, Applied);
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
            _shown = Written();
            _listing.Text = _shown;
        }

        /// <summary>The text that would not read, where there is one, and the program as it is saved otherwise.</summary>
        string Written()
        {
            var program = Editor.Program;
            return program.Unread.Length > 0 ? program.Unread : ProgramText.Save(program, Editor.World.Values);
        }

        /// <summary>Says what is wrong with the text, in the reader's own words.</summary>
        void Said()
        {
            var trouble = Trouble;
            _problem.Text = trouble;
            _problem.IsVisible = trouble.Length > 0;

            // The words take the room the button does not, where there are any.
            _spacer.IsVisible = trouble.Length == 0;
        }

        /// <summary>Selects the line what is wrong names, in view and ready to be typed over, where it names one.</summary>
        void Point()
        {
            var line = ProgramText.LineOf(Trouble);
            if (line < 1)
                return;

            _listing.SelectLine(line - 1);
            if (Editor.Input != null)
                Editor.Input.Focus(_listing);
        }
    }
}
