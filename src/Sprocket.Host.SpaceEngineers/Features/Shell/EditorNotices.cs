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

                // Kept until the banner has slid it out and set Closed.
                _notice.Leaving = true;

                if (_notice.Closed)
                    _notice = null;

                return;
            }

            if (_notice == null)
                _notice = new Notice();

            _notice.Leaving = false;
            _notice.Level = NoticeLevel.Problem;

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

            _notice.Diagnostic = Template.Fill(_editor.Texts.SalvageTrouble, "trouble", _editor.Program.Trouble);

            _notice.Expanded = _open;
            _notice.Expands = Expands;

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

        /// <summary>Says, at the head of every page, who is editing the Sprocket this editor only shows.</summary>
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
