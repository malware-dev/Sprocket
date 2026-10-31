using System;
using Lumel;
using Lumel.Drawing;
using Lumel.Images;
using Lumel.Screens;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;
using VRage.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>Everything Sprocket shows on the game's screen, the editor and the notices, whose pictures include Sprocket's own marks.</summary>
    public class SprocketWorkspace : Workspace
    {
        /// <summary>The name the game knows Sprocket's screen by.</summary>
        public const string ScreenName = "Sprocket";

        EditorWindow _editor;

        /// <summary>A workspace that stands apart from the game, drawn by whoever holds it, for tests and the simulator.</summary>
        public SprocketWorkspace()
        {
            Theme = Styles.Defaults;
            Windows.Closed += Forget;
        }

        /// <summary>The workspace on the game's screen, timed by <paramref name="clock" />, or the game's frame count where it is null.</summary>
        public SprocketWorkspace(Func<int> clock) : this(new GameScreenSpace(), clock)
        {
        }

        SprocketWorkspace(IScreenSpace screen, Func<int> clock)
            : base(ScreenName, screen, new MarkImages(new MaterialImages(screen.TextureSize)), clock)
        {
            Theme = Styles.Defaults;
            Windows.Closed += Forget;
        }

        /// <summary>The editor open here, or null where none is.</summary>
        public EditorWindow Editor => _editor;

        /// <summary>Opens <paramref name="editor" /> over the game's screen, with the world faded behind it, its codex the one every tip here finds.</summary>
        /// <remarks>The codex is set before the editor opens, so no tip drawn over it opens without one.</remarks>
        public void Open(EditorWindow editor)
        {
            _editor = editor;
            Resources.Add(editor.Codex);
            Open(editor, true);
        }

        /// <summary>Lets go of <paramref name="window" />, just closed, and its codex where it is the editor, so an editor put away is held by nothing here.</summary>
        void Forget(Window window)
        {
            if (window != _editor)
                return;

            _editor = null;
            Resources.Remove(ResourceDictionary.KeyOf<Lumel.Codex>());
        }

        /// <inheritdoc />
        /// <remarks>Brings the open editor's chrome into line with its screen.</remarks>
        public override void Update()
        {
            Editor?.Update();
        }

        /// <inheritdoc />
        /// <remarks>The open editor takes Escape, and Enter while it asks something, before anything else does.</remarks>
        public override bool Key(MyKeys key, bool shift, bool ctrl)
        {
            var editor = Editor;
            bool taken;
            if (editor != null && editor.Decides(key, out taken))
                return taken;

            return base.Key(key, shift, ctrl);
        }

        /// <inheritdoc />
        /// <remarks>The open editor decides which tips open, and how.</remarks>
        protected override Tip TipFor(Element owner, Tip tip)
        {
            var editor = Editor;
            return editor != null ? editor.TipFor(tip) : tip;
        }

        /// <summary>Closes the window on top where it is not the editor, and says whether one closed.</summary>
        public bool CloseOverEditor()
        {
            var top = Windows.Top;
            if (top == null || top == _editor)
                return false;

            top.Close();
            return true;
        }
    }
}
