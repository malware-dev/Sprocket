using Lumel.Input;
using Lumel.Text;
using Sprocket.Host.SpaceEngineers.Features.Ownership;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>What the editor says above whatever page is showing: a program that would not read, somebody else holding the Sprocket, or the automation paused.</summary>
    public sealed class EditorNotices
    {
        readonly SprocketEditor _editor;
        readonly PressFilter _gate;
        Notice _notice;
        ICommand _reviews;
        ICommand _expands;
        ICommand _resolves;
        ICommand _mends;
        bool _open;

        /// <summary>The banner of <paramref name="editor" />, whose replacing goes through <paramref name="gate" />.</summary>
        public EditorNotices(SprocketEditor editor, PressFilter gate)
        {
            _editor = editor;
            _gate = gate;
        }

        /// <summary>What the person at the editor needs told above whatever page is showing, or null for nothing.</summary>
        public Notice Notice => _notice;

        ICommand Reviews => _reviews ?? (_reviews = _editor.Act(CommandKind.ReviewPaused, _editor.Review));

        ICommand Expands => _expands ?? (_expands = _editor.Act(CommandKind.Expand, delegate
        {
            _open = !_open;
            if (_notice != null)
                _notice.Expanded = _open;
        }));

        ICommand Resolves => _resolves ?? (_resolves = _editor.Act(CommandKind.Resolve, () => _gate.AskBeforeReplacing(() => { })));

        ICommand Mends => _mends ?? (_mends = _editor.Act(CommandKind.ShowScript, _editor.ShowScript));

        /// <summary>Brings the banner up to date: a program that would not read, somebody else holding the Sprocket, the automation paused, or nothing.</summary>
        /// <remarks>
        /// <b>Every screen, not the front one.</b> What it says is that this is not what the block
        /// holds, and that is as true three pages deep as it is on arrival - somebody who walked
        /// straight into a system and started building is exactly the person it is for.
        /// <para>
        /// Two things to say, and which one depends on whether they have decided yet. Until the edit
        /// that replaces it, the block still holds the old program and this is a warning about what is
        /// in front of them. Afterwards it is the answer to "what happened to the old one".
        /// </para>
        /// <para>
        /// Above the page, because a notice below the thing it is about is a footnote.
        /// </para>
        /// </remarks>
        public void Say()
        {
            if (_editor.Holder != null && _editor.Program.Trouble.Length == 0)
            {
                Held();
                return;
            }

            if (_editor.Program.Trouble.Length == 0 && _editor.Program.Paused && !(_editor.Current is PausedPage))
            {
                Paused();
                return;
            }

            if (_editor.Program.Trouble.Length == 0)
            {
                if (_notice == null)
                    return;

                // Told to go rather than dropped, and kept until it says it has gone: a banner taken away
                // between two frames is the jump this whole arrangement avoids.
                _notice.Leaving = true;

                if (_notice.Closed)
                    _notice = null;

                return;
            }

            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Problem;

            // One line, and the rest when they ask for it. The line says what is wrong; opening it
            // says what happened and what it will cost, which is what somebody needs to decide. Two
            // faults, because what somebody can do differs: a program from another version, and one
            // with an error in its text, which can be mended there.
            if (_editor.Program.Incompatible)
            {
                _notice.Text = _editor.Texts.SalvageDifferent;
                _notice.Detail = _editor.Texts.SalvageDifferentDetail;
            }
            else
            {
                _notice.Text = _editor.Texts.SalvageBroken;
                _notice.Detail = _editor.Texts.SalvageBrokenDetail;
            }

            // The reader's own words, kept because somebody has to be able to say what happened to
            // whoever mends it, and because a person who can read it is better off than one who cannot.
            _notice.Diagnostic = Template.Fill(_editor.Texts.SalvageTrouble, "trouble", _editor.Program.Trouble);

            _notice.Expanded = _open;
            _notice.Expands = Expands;

            // The mending, on the thing that reports the fault, so somebody who has read what it costs
            // does not have to go and find an edit to make. A text with an error in it is mended in the
            // text; one from another version can only be replaced, which asks the same question the
            // first edit does, because it throws away the same program.
            if (_editor.Program.Incompatible)
            {
                _notice.OfferText = _editor.Texts.SalvageReplace;
                _notice.Offers = Resolves;
            }
            else
            {
                _notice.OfferText = _editor.Texts.SalvageMend;
                _notice.Offers = Mends;
            }
        }

        /// <summary>
        /// Says, at the head of every page, who is editing the Sprocket this editor only shows.
        /// </summary>
        /// <remarks>
        /// For as long as the editor is open, so it never comes or goes while somebody is looking. Not
        /// a problem: nothing is wrong, the Sprocket is simply somebody else's for now.
        /// </remarks>
        void Held()
        {
            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Info;
            _notice.Text = Template.Fill(_editor.Texts.LeaseHolder, "holder", _editor.Holder);
            _notice.Detail = _editor.Texts.LeaseDetail;
            _notice.Diagnostic = "";
            _notice.Expanded = _open;
            _notice.Expands = Expands;
            _notice.OfferText = "";
            _notice.Offers = null;
        }

        /// <summary>Says, on every screen but the resume screen, that the automation is paused, with the way back to that screen.</summary>
        void Paused()
        {
            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Warning;
            _notice.Text = Template.Fill(_editor.Texts.PausedBanner, "construct", _editor.World.Name);
            _notice.Detail = "";
            _notice.Diagnostic = "";
            _notice.Expanded = false;
            _notice.Expands = null;
            _notice.OfferText = _editor.Texts.PausedReview;
            _notice.Offers = Reviews;
        }
    }
}
