using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>
    /// One screen's worth of editor.
    /// </summary>
    /// <remarks>
    /// The view is made once and built again only when the screen changes shape; <see cref="Refresh" /> runs every tick
    /// and must not make controls.
    /// </remarks>
    public abstract class EditorPage
    {
        Interval _asked = new Interval(Second);

        int _revision = -1;

        Element _view;

        const int Second = 60;

        /// <summary>A page of <paramref name="editor" />.</summary>
        protected EditorPage(SprocketEditor editor)
        {
            Editor = editor;
        }

        /// <summary>The editor this page is part of.</summary>
        protected SprocketEditor Editor { get; private set; }

        /// <summary>How many times the page has recounted what the runs show.</summary>
        public int Recounts { get; private set; }

        /// <summary>
        /// What this page is called, when it is called one thing.
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Fills in what this page is called, in as many parts as it takes.
        /// </summary>
        /// <remarks>Called when the screen is made; a page that overrides it overrides <see cref="RefreshHeading" /> too.</remarks>
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
        /// Called on a cadence and when a name is typed here; it changes only words and never adds, removes or reorders
        /// parts.
        /// </remarks>
        public virtual void RefreshHeading(List<HeadingPart> parts)
        {
            if (parts.Count == 0)
                return;

            parts[0].Text = Title;
            parts[0].Name = CustomName;
        }

        /// <summary>The words under the heading, or empty for none.</summary>
        public virtual string Subtitle => "";

        /// <summary>Whether the heading's words have changed since this was last asked, which clears it.</summary>
        public virtual bool TakeRetitle()
        {
            return false;
        }

        /// <summary>
        /// The name somebody gave this page's subject, or empty when the heading is worked
        /// out from what the thing is.
        /// </summary>
        /// <remarks>A page that can be renamed overrides this and <see cref="Rename"/> together.</remarks>
        public virtual string CustomName => "";

        /// <summary>Whether the page opens with its name in hand to be given, once, as it is first shown.</summary>
        public bool NameOnArrival { get; set; }

        /// <summary>The page's number, counted on as pages are opened, which the changes made on it carry.</summary>
        public int Opened { get; set; }

        /// <summary>Whether the page was opened on something made as it opened, which is taken back if left untouched.</summary>
        public bool IsMade { get; private set; }

        /// <summary>Whether what the page was opened on is as it was made, so taking it back loses nothing.</summary>
        public virtual bool Untouched => false;

        /// <summary>Whether the name in the heading can be changed from here.</summary>
        public virtual bool CanRename => false;

        /// <summary>What the name in the heading is and what renaming it does, in markdown-lite, said where the pointer rests on it; null for nothing.</summary>
        public virtual string NameAbout => null;

        /// <summary>
        /// Whether the name is the only thing this page has to be called by.
        /// </summary>
        public virtual bool NameRequired => false;

        /// <summary>
        /// Takes a new name, or empty to go back to the worked-out heading.
        /// </summary>
        /// <remarks>A page that says <see cref="NameRequired" /> refuses an empty name here too.</remarks>
        public virtual void Rename(string name) { }

        /// <summary>
        /// Whether this name can be given, before it is: false only where something else already has it.
        /// </summary>
        public virtual bool AcceptsName(string name)
        {
            return true;
        }

        /// <summary>What opens the blueprint library from this page, or null where it is not offered.</summary>
        public virtual ICommand Library => null;

        /// <summary>What the Share menu in the heading offers of this page's subject, or null where the page has nothing to share.</summary>
        public virtual ShareOffer Shares => null;

        /// <summary>Whether this page is a question asked for the screen under it, rather than a screen of its own.</summary>
        public virtual bool Asks => false;

        /// <summary>What was last typed into the block search of a picker opened from this screen, kept while the screen is open.</summary>
        public string Search { get; set; } = "";

        /// <summary>How far down the screen's scroller stood when the page was last left, given back when it is shown again.</summary>
        public float Scrolled { get; set; }

        /// <summary>
        /// Whether this page's subject can be switched on and off.
        /// </summary>
        public virtual bool CanSwitch => false;

        /// <summary>Whether this page's subject is switched on.</summary>
        public virtual bool SwitchIsOn => false;

        /// <summary>Switches this page's subject on, or off.</summary>
        public virtual void Switch(bool on) { }

        /// <summary>What pressing the switch would do, said as a note on it, or null for none.</summary>
        public virtual string SwitchAbout => null;

        /// <summary>Whether the switch cannot be pressed now, which it shows dimmed with <see cref="SwitchAbout" /> saying why.</summary>
        public virtual bool SwitchLocked => false;

        /// <summary>Whether the subject is running now, which turns the run mark into a stop.</summary>
        public virtual bool IsRunning => false;

        /// <summary>How the subject stands, as its row on the home screen shows it: something wrong, a warning, or neither.</summary>
        /// <remarks>Asked every tick, so it reads what the page last worked out and works nothing out itself.</remarks>
        public virtual NoticeLevel Standing => NoticeLevel.Info;

        /// <summary>Runs the subject by hand, or null where it cannot be run from here.</summary>
        /// <remarks>Asked as the screen is built; made through the editor's commands, which a lent copy is refused.</remarks>
        public virtual ICommand Runs => null;

        /// <summary>Why the subject cannot be run now, which the play button says dimmed in its place; null where nothing holds it.</summary>
        public virtual string RunLocked => null;

        /// <summary>Stops the subject, or null where it cannot be stopped from here.</summary>
        public virtual ICommand Stops => null;

        /// <summary>Deletes the subject, or null where it cannot be deleted from here.</summary>
        public virtual ICommand Deletes => null;

        /// <summary>What deleting the subject does, said as the bin's note.</summary>
        public virtual string DeleteAbout => "";

        /// <summary>Why the subject cannot be deleted now, said on a bin that keeps its place but cannot be pressed; null where nothing holds it.</summary>
        public virtual string DeleteLocked => null;

        /// <summary>Whether this page shows <paramref name="system" /> or something in it, so it goes when the system does.</summary>
        public virtual bool IsAbout(AutomationSystem system)
        {
            return false;
        }

        /// <summary>Marks the page as opened on something just made, and notes how it stands, for <see cref="Untouched" /> to compare with.</summary>
        public void Made()
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
        public virtual bool HasHistory => false;

        /// <summary>Whether there is a change on this screen to take back.</summary>
        public virtual bool CanUndo => false;

        /// <summary>What taking the last change back would undo, said on the undo button.</summary>
        public virtual string UndoText => "";

        /// <summary>Takes the last change on this screen back.</summary>
        public virtual void Undo() { }

        /// <summary>Whether there is a change taken back on this screen to make again.</summary>
        public virtual bool CanRedo => false;

        /// <summary>What making the last change again would redo, said on the redo button.</summary>
        public virtual string RedoText => "";

        /// <summary>Makes the last change taken back on this screen again.</summary>
        public virtual void Redo() { }

        /// <summary>Whether the page changes something, and so is finished with Done as well as left by going back.</summary>
        public virtual bool Edits => false;

        /// <summary>The words on the button at the right end of the header, or empty where the page has none.</summary>
        public virtual string PrimaryText => Edits ? Editor.Texts.ChromeDone : "";

        /// <summary>Whether the header's button can be pressed yet.</summary>
        public virtual bool CanPrimary => true;

        /// <summary>What the header's button does, said as its note while it can be pressed.</summary>
        public virtual string PrimaryAbout => Edits ? Editor.Texts.Codex.ChromeDoneAbout : "";

        /// <summary>What has to happen before the header's button can be pressed, said as its note while it cannot.</summary>
        public virtual string PrimaryWaits => "";

        /// <summary>Does what the header's button says; on a page that edits, that is leaving it, keeping what it was opened on.</summary>
        public virtual void Primary()
        {
            if (Edits)
                Editor.Finish();
        }

        /// <summary>Brings the page to the shape the program is in now, whenever the screen is built.</summary>
        /// <remarks>Called again whenever the page changes shape; it keeps what is already made, so focus and caret survive.</remarks>
        public abstract void Build();

        /// <summary>The page as it is drawn, made the first time it is asked for.</summary>
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
        /// Says this page has just been built, so what it says on a cadence is said again at once.
        /// </summary>
        public virtual void Resay()
        {
            _asked.Restart();
            _revision = -1;
        }

        /// <summary>
        /// True about once a second, for words made of what nothing announces changing.
        /// </summary>
        /// <remarks>Ask once per <see cref="Refresh" />: a second ask in the same tick is told no.</remarks>
        protected bool SecondPassed()
        {
            var now = Editor.CurrentTick;
            return _asked.Due(now);
        }

        /// <summary>
        /// True on the tick what the runs show has changed since this page last asked: a fault, a hold, a stop or a halt.
        /// </summary>
        /// <remarks>
        /// One comparison against <see cref="AutomationRunner.Revision" /> per call. Ask once per <see cref="Refresh" />: a
        /// second ask in the same tick is told no.
        /// </remarks>
        protected bool Revised()
        {
            var now = Editor.Runner.Revision;
            if (now == _revision)
                return false;

            _revision = now;
            Recounts++;
            return true;
        }

        /// <summary>
        /// Whether back may leave the page, false where the page has asked something instead.
        /// </summary>
        /// <remarks>
        /// A page that answers false has asked or pushed something of its own. Where a change made here still stands, it asks
        /// whether to keep it, and goes back once answered.
        /// </remarks>
        public virtual bool Leaving()
        {
            return Answered(() => Editor.Left(this));
        }

        /// <summary>
        /// Whether nothing changed here is left to answer for; where something is, asks whether to keep it, as a picker does,
        /// and goes on with <paramref name="then" /> either way: keeping leaves the changes, discarding takes them back first.
        /// </summary>
        /// <remarks>The changes made on the pages opened from this one were answered for there, and stay.</remarks>
        protected bool Answered(Action then)
        {
            if (!Editor.Changed(this))
                return true;

            Editor.Ask(Editor.Texts.PickKeepQuestion);
            Editor.Answer(Editor.Texts.PickKeepThem, delegate
            {
                Editor.Keep(this);
                then();
            }, true);
            Editor.Answer(Editor.Texts.PickDiscardThem, delegate
            {
                Editor.Discard(this);
                then();
            });
            return false;
        }

        /// <summary>Brings what the page shows up to date, every tick.</summary>
        public virtual void Refresh() { }
    }
}
