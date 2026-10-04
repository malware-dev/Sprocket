using System.Collections.Generic;
using Lumel;
using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>
    /// One screen's worth of editor.
    /// </summary>
    /// <remarks>
    /// A page makes its view once, is built again when the shape of the screen changes, and brings what it shows up to
    /// date in place every tick. Making controls afresh on every tick is exactly what a screen cannot afford.
    /// </remarks>
    abstract class EditorPage
    {
        Interval _asked = new Interval(Second);

        Element _view;

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
        /// <see cref="Refresh" />: this one runs when the screen is made and says what can be renamed; the other runs on
        /// a cadence and may only change words. A page that overrides one nearly always has to override the other.
        /// </para>
        /// </remarks>
        public virtual void Heading(List<HeadingPart> parts)
        {
            var part = new HeadingPart(Title);
            part.About = NameAbout;
            part.NameRequired = NameRequired;

            if (CanRename)
            {
                part.Renames = Editor.Renaming(Rename, AcceptsName);
                part.Accepts = AcceptsName;
            }

            part.NamedOnArrival = NameOnArrival;
            NameOnArrival = false;

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

        /// <summary>Whether the page opens with its name in hand to be given, once, as it is first shown.</summary>
        internal bool NameOnArrival { get; set; }

        /// <summary>Whether the page was opened on something made as it opened, which is taken back if left untouched.</summary>
        internal bool IsMade { get; private set; }

        /// <summary>Whether what the page was opened on is as it was made, so taking it back loses nothing.</summary>
        public virtual bool Untouched => false;

        public virtual bool CanRename => false;

        /// <summary>What the name in the heading is and what renaming it does, in markdown-lite, said where the pointer rests on it; null for nothing.</summary>
        public virtual string NameAbout => null;

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

        /// <summary>What opens the blueprint library from this page, or null where it is not offered.</summary>
        public virtual ICommand Library => null;

        /// <summary>Whether this page shows what the construct's toolbars say, so reading them again can change it.</summary>
        /// <remarks>The read is the construct's whole, the same one the screen makes as it opens; the page only decides whether it is offered.</remarks>
        public virtual bool CanRefresh => false;

        /// <summary>
        /// Whether this page's subject can be switched on and off.
        /// </summary>
        public virtual bool CanSwitch => false;

        public virtual bool SwitchIsOn => false;

        public virtual void Switch(bool on) { }

        /// <summary>What pressing the switch would do, said as a note on it, or null for none.</summary>
        public virtual string SwitchAbout => null;

        /// <summary>Whether the switch cannot be pressed now, which it shows dimmed with <see cref="SwitchAbout" /> saying why.</summary>
        public virtual bool SwitchLocked => false;

        /// <summary>Whether the subject is running now, which turns the run mark into a stop.</summary>
        public virtual bool IsRunning => false;

        /// <summary>Runs the subject by hand, or null where it cannot be run from here.</summary>
        /// <remarks>Asked as the screen is built, and made through the editor's commands, so a copy lent to look at is refused it.</remarks>
        public virtual ICommand Runs => null;

        /// <summary>Stops the subject, or null where it cannot be stopped from here.</summary>
        public virtual ICommand Stops => null;

        /// <summary>Deletes the subject, or null where it cannot be deleted from here.</summary>
        public virtual ICommand Deletes => null;

        /// <summary>What deleting the subject does, said as the bin's note.</summary>
        public virtual string DeleteAbout => "";

        /// <summary>Whether this page shows <paramref name="system" /> or something in it, so it goes when the system does.</summary>
        public virtual bool IsAbout(AutomationSystem system)
        {
            return false;
        }

        /// <summary>Marks the page as opened on something just made, and notes how it stands, for <see cref="Untouched" /> to compare with.</summary>
        internal void Made()
        {
            IsMade = true;
            Remember();
        }

        /// <summary>Notes how what the page was opened on stands as it was made.</summary>
        protected virtual void Remember()
        {
        }

        /// <summary>Takes what the page was opened on back out, without asking, for something made and left untouched.</summary>
        public virtual void Unmake()
        {
        }

        /// <summary>
        /// Whether this screen offers a way back from what is done on it.
        /// </summary>
        /// <remarks>A screen with a history of its own. Which things are in it is the page's business.</remarks>
        public virtual bool HasHistory => false;

        public virtual bool CanUndo => false;

        public virtual string UndoText => "";

        public virtual void Undo() { }

        public virtual bool CanRedo => false;

        public virtual string RedoText => "";

        public virtual void Redo() { }

        /// <summary>Whether the page changes something, and so is finished with Done as well as left by going back.</summary>
        public virtual bool Edits => false;

        /// <summary>The words on the button at the right end of the header, or empty where the page has none.</summary>
        public virtual string PrimaryText => Edits ? Editor.Texts.ChromeDone : "";

        /// <summary>Whether the header's button can be pressed yet.</summary>
        public virtual bool CanPrimary => true;

        /// <summary>What the header's button does, said as its note while it can be pressed.</summary>
        public virtual string PrimaryAbout => Edits ? Editor.Texts.ChromeDoneAbout : "";

        /// <summary>What has to happen before the header's button can be pressed, said as its note while it cannot.</summary>
        public virtual string PrimaryWaits => "";

        /// <summary>Does what the header's button says; on a page that edits, that is leaving it, keeping what it was opened on.</summary>
        public virtual void Primary()
        {
            if (Edits)
                Editor.Finish();
        }

        /// <summary>Brings the page to the shape the program is in now, whenever the screen is built.</summary>
        /// <remarks>
        /// Called again whenever the page changes shape. Keep what is already made wherever possible and change what it
        /// says - that is what keeps the focus and caret somebody may be typing into.
        /// </remarks>
        public abstract void Build();

        /// <summary>The page as it is drawn, made the first time it is asked for.</summary>
        /// <remarks>Made when the page is first shown, so a page nobody opens builds nothing to draw.</remarks>
        public Element View
        {
            get
            {
                if (_view == null)
                    Create();

                return _view;
            }
        }

        /// <summary>Whether the page's view is given the whole of the screen's room, scrolling whatever it has to itself.</summary>
        public virtual bool ScrollsItself => false;

        /// <summary>Makes the page as it is drawn.</summary>
        /// <remarks>
        /// The view definition and nothing else, as a designer would write it: the elements, their styles and
        /// the names the page reaches them by. A page keeps it in its own <c>.View.cs</c> file, the same class in two
        /// halves.
        /// </remarks>
        protected virtual Element CreateView()
        {
            return null;
        }

        /// <summary>Gives the elements <see cref="CreateView" /> just made what they do and what they say, and fills them for the first time.</summary>
        protected virtual void ViewCreated()
        {
        }

        void Create()
        {
            _view = CreateView();
            if (_view != null)
                ViewCreated();
        }

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
        /// A page is built again when the screen is rebuilt, so everything it only says on a cadence has to be said
        /// again at once - a row whose words arrive a second after it appears is a row that appears blank.
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

        /// <summary>Brings what the page shows up to date, every tick.</summary>
        public virtual void Refresh() { }
    }
}
