using Sprocket.Host.SpaceEngineers.Features.LogicSystems;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System;
using Lumel.Layout;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>
    /// The strip between the library's panes: the moves between this construct and the library for the one row chosen in
    /// either, and a copy of it, each saying in its tip what it does to that row.
    /// </summary>
    public partial class LibraryMoves : VStack
    {
        readonly SprocketEditor _editor;
        readonly RelayCommand _outs;
        readonly RelayCommand _ins;
        readonly RelayCommand _copies;

        LibraryListing _chosen;

        /// <summary>The moves for no row, in the editor <paramref name="editor" />.</summary>
        public LibraryMoves(SprocketEditor editor)
        {
            _editor = editor;
            _outs = new RelayCommand(Out);
            _ins = new RelayCommand(In);
            _copies = new RelayCommand(Copy);

            CreateView();
            _out.Command = _outs;
            _in.Command = _ins;
            _copy.Command = _copies;
        }

        /// <summary>The button that puts the chosen row in the library.</summary>
        public MoveButton ToLibrary => _out;

        /// <summary>The button that puts the chosen row on this construct.</summary>
        public MoveButton ToConstruct => _in;

        /// <summary>The button that makes a copy of the chosen logic system.</summary>
        public MoveButton Copies => _copy;

        /// <summary>Why the last move on the chosen row was refused, or empty.</summary>
        public string Refusal { get; private set; } = "";

        /// <summary>Offers the moves that apply to <paramref name="chosen" />, or none where it is null; why a move on another row was refused is forgotten.</summary>
        public void Show(LibraryListing chosen)
        {
            if (!ReferenceEquals(_chosen, chosen))
                Refusal = "";

            _chosen = chosen;
            Offer();
        }

        /// <summary>Lights the moves that apply to the chosen row and says on each what it would do.</summary>
        void Offer()
        {
            var texts = _editor.Texts;
            var codex = texts.Codex;
            var entry = _chosen;
            var shelf = _editor.Shelf;
            var free = _editor.Holder == null;

            var live = entry?.LiveIn(_editor.Program);
            var volume = entry == null ? null : Shelved(entry, live);
            var system = entry == null ? null : entry.Section == LibrarySection.OnConstruct ? live : entry.Read;
            var backed = entry != null && entry.Section == LibrarySection.BackedUp;
            var kept = backed ? entry.Read : live;
            var same = shelf != null && kept != null && volume != null && shelf.Holds(kept);
            var changed = live != null && _editor.Holdings.ChangedSince(live);

            var makes = !backed && live != null && volume == null;
            var updates = !backed && live != null && volume != null && !same && changed;
            var shelves = backed && kept != null && !same;
            _outs.CanExecute = shelf != null && (makes || updates || shelves);

            // A shared blueprint is saved by the Save a copy button in its detail, so the arrow has nothing to say of it.
            _out.Tip = KeptTip.For(_out.Tip, same ? codex.LibrarySame
                : backed ? codex.LibraryPromoteAbout
                : entry != null && entry.Section == LibrarySection.Kept && live == null ? codex.LibraryInLibrary
                : entry != null && entry.Section == LibrarySection.Shared && live == null ? ""
                : updates ? codex.SystemsUpdateBlueprintAbout : codex.SystemsMakeBlueprintAbout);

            var putsOn = entry != null && entry.Section != LibrarySection.OnConstruct && live == null && system != null;
            var replaces = !backed && live != null && volume != null && !same;
            _ins.CanExecute = free && (putsOn || replaces);
            var here = live != null;
            _in.Tip = KeptTip.For(_in.Tip, replaces ? (changed ? codex.LibraryResetAbout : codex.SystemsUpdateFromLibraryAbout)
                : here ? codex.LibraryAlreadyHere : codex.LibraryPutOn);

            _copies.CanExecute = free && entry != null && entry.Section == LibrarySection.OnConstruct && system != null;
            _copy.Tip = KeptTip.For(_copy.Tip, codex.LibraryCopyAbout);

            _out.Live = _outs.CanExecute;
            _in.Live = _ins.CanExecute;
            _copy.Live = _copies.CanExecute;
        }

        /// <summary>Puts the chosen logic system in the library, as a new blueprint or over the one it was made from; or promotes a chosen backup to a blueprint; each after asking.</summary>
        void Out()
        {
            var entry = _chosen;
            if (entry == null)
                return;

            if (entry.Section == LibrarySection.BackedUp)
            {
                var backed = entry.Read;
                if (backed != null)
                    Run(CommandKind.Shelve, () => _editor.Holdings.PromoteBackup(backed));

                return;
            }

            var live = entry.LiveIn(_editor.Program);
            if (live == null)
                return;

            if (!_editor.Holdings.Stored(live))
                Run(CommandKind.Shelve, () => _editor.Holdings.AskShelve(live));
            else if (_editor.Holdings.ChangedSince(live))
                Run(CommandKind.Shelve, () => _editor.Holdings.AskUpdate(live));
        }

        /// <summary>Puts the chosen blueprint on this construct as a logic system, or the library's blueprint in the place of the logic system made from it, each after asking: an update where the blueprint is newer, a reset where the logic system is.</summary>
        void In()
        {
            var entry = _chosen;
            if (entry == null)
                return;

            var live = entry.LiveIn(_editor.Program);
            if (live != null)
            {
                if (_editor.Holdings.ChangedSince(live))
                    Run(CommandKind.Renew, () => _editor.Holdings.AskReset(live));
                else
                    Run(CommandKind.Renew, () => _editor.Holdings.AskRenew(live));

                return;
            }

            if (entry.Backup != null)
            {
                var backed = entry.Read;
                if (backed != null)
                    Run(CommandKind.PutOn, () => Refusal = _editor.Holdings.PutOn(backed));

                return;
            }

            if (entry.Share != null)
            {
                var shared = entry.Share;
                Run(CommandKind.PutOn, () => Refusal = _editor.Holdings.PutOnShared(shared));
                return;
            }

            var volume = _editor.Shelf?.Find(entry.Identity);
            if (volume != null)
                Run(CommandKind.PutOn, () => Refusal = _editor.Holdings.PutOn(volume));
        }

        /// <summary>Puts a copy of the chosen logic system on the construct, as a logic system of its own, and opens it to be changed, after asking.</summary>
        void Copy()
        {
            var live = _chosen?.LiveIn(_editor.Program);
            if (live == null || _chosen.Section != LibrarySection.OnConstruct)
                return;

            var text = ProgramWriter.Blueprint(live);
            Run(CommandKind.PutOn, () => _editor.Holdings.AskCopy(live.Name, delegate
            {
                string refused;
                var made = _editor.Holdings.Copy(text, out refused);
                Refusal = refused ?? "";
                if (made != null)
                    _editor.Push(new LogicSystemPage(_editor, made));
            }));
        }

        void Run(CommandKind kind, Action action)
        {
            var command = _editor.Act(kind, action);
            if (command != null)
                command.Execute();

            Offer();
        }

        /// <summary>
        /// The blueprint in the library <paramref name="entry" /> is, or the one its logic system, <paramref name="live" /> or the
        /// backup's, is linked to; null where the library has none.
        /// </summary>
        LibraryEntry Shelved(LibraryListing entry, AutomationSystem live)
        {
            var shelf = _editor.Shelf;
            if (shelf == null)
                return null;

            if (entry.Section == LibrarySection.Kept)
                return shelf.Find(entry.Identity);

            var linked = entry.Section == LibrarySection.BackedUp ? entry.Read : live;
            return linked != null && linked.HasOrigin ? shelf.Find(linked.Origin) : null;
        }
    }
}
