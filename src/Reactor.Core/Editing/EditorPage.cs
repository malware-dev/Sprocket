using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// One screen's worth of editor.
    /// </summary>
    /// <remarks>
    /// A page builds its components once, when the shape of the screen changes, and
    /// then updates their values in place every beat. Rebuilding on every beat would
    /// force targets to recreate native controls sixty times a second, which is
    /// exactly what they cannot afford.
    /// </remarks>
    abstract class EditorPage
    {
        protected EditorPage(ReactorEditor editor)
        {
            Editor = editor;
        }

        protected ReactorEditor Editor { get; private set; }

        public abstract string Title { get; }

        public virtual string Subtitle { get { return ""; } }

        /// <summary>Creates the components and registers their commands.</summary>
        public abstract void Build(EditorScreen screen);

        /// <summary>Updates live values on components that already exist.</summary>
        public virtual void Refresh() { }
    }
}
