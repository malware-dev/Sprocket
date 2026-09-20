using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Editing;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.Developing
{
    /// <summary>What this construct's automation is saved as, written out and shown.</summary>
    /// <remarks>
    /// A developer tool and not part of the product, reached only where <c>developerMode</c> is on. The
    /// script is what a program actually is on disk, so reading it is the shortest way to tell whether
    /// the editor built what its screens claim - and the first thing to reach for whenever the saved
    /// program and the screen disagree.
    /// <para>
    /// <b>Written out afresh once a second, not on a tick.</b> Saving walks every system, sequence and
    /// step and builds a string of the lot, which is not a thing to do sixty times a second to answer a
    /// question nobody asked. A second is fast enough to watch an edit appear.
    /// </para>
    /// </remarks>
    sealed class ScriptPage : EditorPage
    {
        /// <summary>What it is called, on its own heading and on the way in to it.</summary>
        public const string Called = "Script";

        ScriptComponent _script;

        public ScriptPage(SprocketEditor editor) : base(editor)
        {
        }

        public override string Title
        {
            get { return Called; }
        }

        public override void Build(List<EditorComponent> components)
        {
            _script = new ScriptComponent();

            // The whole body. It is the only thing on the screen, and it does its own scrolling - said
            // here because a screen that does not claim its height is wrapped in the screen's own
            // scroller instead, and then two scrollers fight over one gesture.
            _script.Fills = true;
            _script.Text = Written();

            components.Add(_script);
        }

        public override void Refresh()
        {
            if (_script == null || !SecondPassed())
                return;

            _script.Text = Written();
        }

        string Written()
        {
            return ProgramText.Save(Editor.Program, Editor.World.Values);
        }
    }
}
