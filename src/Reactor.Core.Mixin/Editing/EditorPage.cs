using System.Collections.Generic;
using Reactor.Ui;

namespace Reactor.Editing
{
    /// <summary>
    /// One screen's worth of editor.
    /// </summary>
    /// <remarks>
    /// A page builds its components once, when the shape of the screen changes, and
    /// then updates their values in place every tick. Rebuilding on every tick would
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

        /// <summary>
        /// The obvious next move on this page, or empty for none.
        /// </summary>
        /// <remarks>
        /// Never the same thing as going back. A page in the middle of a run of
        /// questions moves the user on; going back retreats one question. Both have
        /// to exist, or one of them ends up lying.
        /// </remarks>
        /// <summary>
        /// The name somebody gave this page's subject, or empty when the heading is worked
        /// out from what the thing is.
        /// </summary>
        /// <remarks>
        /// Pages that can be renamed override this and <see cref="Rename"/> together. Neither
        /// is any use alone: a target has to know what the current name is before it can offer
        /// to change it, or it opens a box holding a sentence nobody typed.
        /// </remarks>
        public virtual string CustomName { get { return ""; } }

        public virtual bool CanRename { get { return false; } }

        /// <summary>What to ask when offering the rename.</summary>
        public virtual string RenamePrompt { get { return ""; } }

        /// <summary>
        /// Whether the name is the only thing this page has to be called by.
        /// </summary>
        /// <remarks>
        /// False nearly everywhere, and it should stay that way: a sequence with no name reads as the
        /// sentence it makes and is no worse off, which is what keeps naming genuinely optional rather
        /// than a step somebody is quietly expected to complete.
        /// <para>
        /// A system is the exception, because there is nothing underneath it to fall back to. It is not
        /// made of anything that describes itself - it is a name and the ones of it - so a system with its
        /// name taken away is a row nobody can tell from the next one.
        /// </para>
        /// </remarks>
        public virtual bool NameRequired { get { return false; } }

        /// <summary>
        /// Takes a new name, or empty to go back to the worked-out heading.
        /// </summary>
        /// <remarks>
        /// A page that says <see cref="NameRequired" /> has to refuse an empty one here too, rather than
        /// rely on a target having asked. Targets are the part that can be written wrong.
        /// </remarks>
        public virtual void Rename(string name) { }

        /// <summary>
        /// Whether this page's subject can be switched on and off.
        /// </summary>
        public virtual bool CanSwitch { get { return false; } }

        public virtual bool SwitchIsOn { get { return false; } }

        public virtual void Switch(bool on) { }

        public virtual string PrimaryText { get { return ""; } }

        public virtual void Primary() { }

        /// <summary>
        /// Adds this page's components, in order, and registers their commands.
        /// </summary>
        /// <remarks>
        /// Called again whenever the page changes shape. Hand back the same component
        /// instances wherever possible and change their contents instead of making
        /// new ones - that is what lets a target keep its controls, and with them the
        /// focus and caret somebody may be typing into.
        /// </remarks>
        public abstract void Build(List<EditorComponent> components);

        /// <summary>Updates live values on components that already exist.</summary>
        public virtual void Refresh() { }
    }
}
