using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Screen;

namespace Sprocket.Host.SpaceEngineers.Editing
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
        /// <summary>
        /// When <see cref="SecondPassed" /> last said yes.
        /// </summary>
        /// <remarks>
        /// A page is refreshed the instant it is built, and that is when it most needs to know, so the
        /// first ask is answered rather than held back - see <see cref="Interval" />, which keeps the
        /// clock and the reasoning about where it starts.
        /// </remarks>
        Interval _asked = new Interval(Second);

        /// <summary>Ticks in a second, at the rate the game runs at.</summary>
        const int Second = 60;

        protected EditorPage(SprocketEditor editor)
        {
            Editor = editor;
        }

        protected SprocketEditor Editor { get; private set; }

        /// <summary>
        /// What this page is called, when it is called one thing.
        /// </summary>
        /// <remarks>
        /// Still one string, and only in here. What a target sees is <see cref="EditorScreen.Heading" />,
        /// which is a list - this is the simple case, fed into it by <see cref="Heading" />, because nearly
        /// every page is one thing with one name and should not have to say so in a list to prove it.
        /// </remarks>
        public abstract string Title { get; }

        /// <summary>
        /// Fills in what this page is called, in as many parts as it takes.
        /// </summary>
        /// <remarks>
        /// The default is one part carrying <see cref="Title" /> and whatever the page says about renaming,
        /// which is right for every page that is about one thing. A page whose heading is really two things
        /// overrides it - see <c>SequencePage</c>, where what starts a sequence and what it then does are
        /// separate, and only the second is somebody's to name.
        /// <para>
        /// Paired with <see cref="RefreshHeading" /> exactly the way <see cref="Build" /> is paired with
        /// <see cref="Refresh" />, and for the same reason: this one runs when the screen is made and may
        /// claim command ids, which can only be claimed once; the other runs every tick and may only change
        /// words. A page that overrides one nearly always has to override the other.
        /// </para>
        /// </remarks>
        public virtual void Heading(List<HeadingPart> parts)
        {
            var part = new HeadingPart(Title);
            part.Prompt = RenamePrompt;
            part.NameRequired = NameRequired;

            if (CanRename)
                part.RenameCommand = Editor.RegisterText(Cmd.Rename, Rename, AcceptsName);

            parts.Add(part);
            RefreshHeading(parts);
        }

        /// <summary>
        /// Brings the words in the heading up to date, without touching its shape.
        /// </summary>
        /// <remarks>
        /// On a cadence, and again the moment a name is typed here, because what these words are made
        /// of - a grid's name, a block's name inside a sentence - is nothing the game announces. It must
        /// not add, remove or renumber anything: the target is holding these and has already decided
        /// what to draw for each one.
        /// </remarks>
        public virtual void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count == 0)
                return;

            parts[0].Text = Title;
            parts[0].Name = CustomName;
        }

        public virtual string Subtitle => "";

        /// <summary>Whether the heading's words have changed since this was last asked, which clears it.</summary>
        /// <remarks>
        /// For a page whose title follows what is being chosen on it, so the heading catches up the tick
        /// the choice lands rather than on the heading's own cadence.
        /// </remarks>
        public virtual bool TakeRetitle()
        {
            return false;
        }

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
        public virtual string CustomName => "";

        public virtual bool CanRename => false;

        /// <summary>What to ask when offering the rename.</summary>
        public virtual string RenamePrompt => "";

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
        public virtual bool NameRequired => false;

        /// <summary>
        /// Takes a new name, or empty to go back to the worked-out heading.
        /// </summary>
        /// <remarks>
        /// A page that says <see cref="NameRequired" /> has to refuse an empty one here too, rather than
        /// rely on a target having asked. Targets are the part that can be written wrong.
        /// </remarks>
        public virtual void Rename(string name) { }

        /// <summary>
        /// Whether this name can be given, before it is: false only where something else already has it.
        /// </summary>
        /// <remarks>
        /// An empty name is not refused here; <see cref="NameRequired" /> already tells a target that.
        /// </remarks>
        public virtual bool AcceptsName(string name)
        {
            return true;
        }

        /// <summary>
        /// Whether this page's subject can be switched on and off.
        /// </summary>
        public virtual bool CanSwitch => false;

        public virtual bool SwitchIsOn => false;

        public virtual void Switch(bool on) { }

        /// <summary>
        /// Whether this screen offers a way back from what is done on it.
        /// </summary>
        /// <remarks>
        /// A screen with a history of its own. Which things are in it is the page's business: a
        /// system's covers what starts its sequences, and a sequence's own is on its sentence rather
        /// than here, because there it sits among the bin and the switch that act on the same thing.
        /// </remarks>
        public virtual bool HasHistory => false;

        public virtual bool CanUndo => false;

        public virtual string UndoText => "";

        public virtual void Undo() { }

        public virtual bool CanRedo => false;

        public virtual string RedoText => "";

        public virtual void Redo() { }

        public virtual string PrimaryText => "";

        /// <summary>
        /// What pressing the primary asks for first, or empty when it asks nothing.
        /// </summary>
        /// <remarks>
        /// A page that needs a value to finish gets it on the button that finishes it. What is typed
        /// arrives at <see cref="Primary(string)" />, and <see cref="Primary()" /> is never called.
        /// </remarks>
        public virtual string PrimaryQuestion => "";

        /// <summary>
        /// What each value it asks for wants, one per value, and empty when it asks for none.
        /// </summary>
        /// <remarks>
        /// One field each, named and bounded where anybody has said what the value is - which
        /// nothing but a description can say, since an action declares how many values it takes and
        /// no more.
        /// </remarks>
        public virtual IList<Field> PrimaryFields => null;

        /// <summary>
        /// What each of those fields opens holding, or null to open them empty.
        /// </summary>
        /// <remarks>
        /// A page that is changing something already has the answers to its own question. Opening
        /// empty would make somebody retype what they already gave, and a box left alone would then
        /// mean "no value" rather than "the one that was there".
        /// </remarks>
        public virtual IList<string> PrimaryFilled => null;

        public virtual void Primary() { }

        /// <summary>The same, with what was typed into the question the button asked.</summary>
        public virtual void Primary(string said) { }

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

        /// <summary>
        /// True about once a second, for facts too dear to work out on a tick.
        /// </summary>
        /// <remarks>
        /// Whether a sequence is broken is the one that needs this. A host may have to read a
        /// toolbar to answer, and something outside Sprocket can break a sequence while the screen
        /// is open - somebody clearing a button from the game's own terminal - so an answer taken
        /// only when the screen was built would never notice. Asking sixty times a second about
        /// something that changes by hand is the other mistake.
        /// <para>
        /// Ask once per <see cref="Refresh" />. It records that it said yes, so a second ask in the
        /// same tick is told no and whatever depended on it silently does not happen.
        /// </para>
        /// </remarks>
        /// <summary>
        /// Says this page has just been built and has told nobody anything yet.
        /// </summary>
        /// <remarks>
        /// A page's components are made afresh when the screen is rebuilt, so everything it only says
        /// on a cadence has to be said again at once - a row whose words arrive a second after it
        /// appears is a row that appears blank.
        /// </remarks>
        internal virtual void Resay()
        {
            _asked.Restart();
        }

        protected bool SecondPassed()
        {
            var now = Editor.CurrentTick;
            return _asked.Due(now);
        }

        /// <summary>
        /// Somebody has pressed back. True to go, false to have asked something instead.
        /// </summary>
        /// <remarks>
        /// The one hook a page gets on the way out, and it exists for questions that only make sense
        /// at that moment - a thing built and left switched off is the example, because leaving is
        /// when it stops being a work in progress and starts being something that does not happen.
        /// <para>
        /// A page that answers false must have pushed something, or back has done nothing and the
        /// person is left pressing a control that appears broken.
        /// </para>
        /// </remarks>
        public virtual bool Leaving()
        {
            return true;
        }

        /// <summary>Updates live values on components that already exist.</summary>
        public virtual void Refresh() { }
    }
}
