using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Features.Developing
{
    /// <summary>What this construct's automation is saved as, written out and shown.</summary>
    /// <remarks>
    /// A developer tool and not part of the product, reached only where <c>developerMode</c> is on. The script is what a
    /// program actually is on disk, so reading it is the shortest way to tell whether the editor built what its screens
    /// claim, and the first thing to reach for whenever the saved program and the screen disagree.
    /// <para>
    /// <b>Written out afresh once a second, not on a tick.</b> Saving walks every system, sequence and step and builds a
    /// string of the lot, which is not a thing to do sixty times a second to answer a question nobody asked. A second is
    /// fast enough to watch an edit appear. The listing it is drawn on is declared in <c>ScriptPage.View.cs</c>.
    /// </para>
    /// </remarks>
    sealed partial class ScriptPage : EditorPage
    {
        /// <summary>What it is called, on its own heading and on the way in to it.</summary>
        public const string Called = "Script";

        /// <summary>The page of the script the editor <paramref name="editor" /> saves.</summary>
        public ScriptPage(SprocketEditor editor) : base(editor)
        {
        }

        /// <inheritdoc />
        public override string Title => Called;

        /// <summary>The listing is the whole of the page and scrolls both ways by itself.</summary>
        public override bool ScrollsItself => true;

        /// <summary>What the listing shows.</summary>
        public string Text => _listing == null ? "" : _listing.Text;

        /// <summary>Describes no components: the page is drawn by its own view, which this fills.</summary>
        public override void Build(List<EditorComponent> components)
        {
            if (_listing != null)
                _listing.Text = Written();
        }

        /// <inheritdoc />
        public override void Refresh()
        {
            if (_listing == null || !SecondPassed())
                return;

            _listing.Text = Written();
        }

        /// <summary>Fills the listing for the first time.</summary>
        protected override void ViewCreated()
        {
            _listing.Text = Written();
        }

        string Written()
        {
            return ProgramText.Save(Editor.Program, Editor.World.Values);
        }
    }
}
