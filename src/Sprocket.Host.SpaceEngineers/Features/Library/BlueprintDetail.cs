using Sprocket.Host.SpaceEngineers.Features.Sharing;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Shell;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Tips;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Lumel;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>What the library's chosen row holds, opened under it: who made it and when, its description, and the slots and installations it has.</summary>
    public partial class BlueprintDetail : VStack
    {
        readonly SprocketEditor _editor;
        readonly List<string> _names = new List<string>();

        /// <summary>The details of no blueprint, in the editor <paramref name="editor" />.</summary>
        public BlueprintDetail(SprocketEditor editor)
        {
            _editor = editor;
            CreateView();
            editor.Codex.Explain(_firstHeading);
            editor.Codex.Explain(_secondHeading);
        }

        /// <summary>Shows what <paramref name="chosen" /> holds, with <paramref name="refusal" /> where a move on it was just refused.</summary>
        public void Show(LibraryListing chosen, string refusal)
        {
            var texts = _editor.Texts;
            var live = chosen.LiveIn(_editor.Program);
            var system = chosen.Section == LibrarySection.OnConstruct ? live : chosen.Read;

            ShowSharing(chosen, system);
            var refused = refusal.Length > 0 ? refusal : Refused(texts);
            var trouble = refused.Length > 0 ? refused : system == null ? chosen.Trouble : "";
            _trouble.Text = trouble;
            _trouble.IsVisible = trouble.Length > 0;

            _line.IsVisible = false;
            _about.IsVisible = false;
            Say(_firstSection, _firstHeading, _first, null, null, null);
            Say(_secondSection, _secondHeading, _second, null, null, null);
            if (system == null)
                return;

            _line.Text = Made(texts, system);
            _line.IsVisible = _line.Text.Length > 0;
            _about.Markdown = system.Annotation;
            _about.IsVisible = system.Annotation.Length > 0;

            _names.Clear();
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (!parts[i].Optional)
                    _names.Add(parts[i].Id);
            }

            Say(_firstSection, _firstHeading, _first, texts.BlueprintParts, CodexPages.Slot, _names);

            _names.Clear();
            for (var i = 0; chosen.Section == LibrarySection.OnConstruct && live != null && i < live.Instances.Count; i++)
                _names.Add(SystemSummary.DescribeInstance(texts, _editor.Runner.Bindings, live, live.Instances[i]));

            Say(_secondSection, _secondHeading, _second, texts.LibraryInstallationsHeading, CodexPages.Installation, _names);
        }

        /// <summary>Offers what can be done about sharing <paramref name="chosen" />, holding <paramref name="system" />.</summary>
        void ShowSharing(LibraryListing chosen, AutomationSystem system)
        {
            var texts = _editor.Texts;
            var board = _editor.Sharing;
            var volume = chosen.Section == LibrarySection.Kept ? _editor.Shelf?.Find(chosen.Identity) : null;
            var mine = volume == null || board == null ? null : board.MineOf(volume.Identity);
            var line = chosen.Share;

            var shares = volume != null && system != null;
            var inWorld = board != null && !board.IsBarred && board.Limit > 0;
            _share.IsVisible = shares;
            if (shares)
            {
                _share.Text = mine == null ? texts.LibraryShare : texts.LibraryShareChange;
                _share.Command = _editor.Act(CommandKind.Expand, () => _editor.Windows?.Open(new ShareDialog(_editor, volume.Name, volume.Text, inWorld, mine)));
            }

            _withdraw.IsVisible = mine != null;
            if (mine != null)
            {
                _withdraw.Text = texts.LibraryShareWithdraw;
                _withdraw.Command = _editor.Act(CommandKind.Expand, () => board.Withdraw(mine.Sharer, volume.Identity));
            }

            _reach.IsVisible = mine != null;
            if (mine != null)
                _reach.Text = Reach(texts, mine);

            _keep.IsVisible = line != null && _editor.Shelf != null && chosen.Read != null;
            _block.IsVisible = line != null;
            if (line != null)
            {
                _keep.Text = texts.LibraryKeepCopy;
                _keep.Tip = CodexTip.For(null, Template.Fill(texts.LibraryKeepCopyAbout, "sharer", line.SharerName));
                _keep.Command = _editor.Act(CommandKind.Shelve, () => _editor.Holdings.KeepCopy(line));
                _block.Text = Template.Fill(texts.LibraryBlock, "name", line.SharerName);
                _block.Tip = CodexTip.For(null, Template.Fill(texts.LibraryBlockAbout, "name", line.SharerName));
                _block.Command = _editor.Act(CommandKind.Expand, () => board?.Block(line.Sharer));
            }

            _shares.IsVisible = _share.IsVisible || _withdraw.IsVisible || _keep.IsVisible;
        }

        /// <summary>Why the server last turned down a blueprint offered for sharing, or empty.</summary>
        string Refused(Texts texts)
        {
            switch (_editor.Sharing?.Answer)
            {
                case ShareAnswer.Closed:
                    return texts.LibraryShareClosed;
                case ShareAnswer.Barred:
                    return texts.LibraryShareBarred;
                case ShareAnswer.AtLimit:
                    return Template.Fill(texts.LibraryShareAtLimit, "count", _editor.Sharing.Limit.ToString(CultureInfo.InvariantCulture));
                case ShareAnswer.Unreadable:
                    return texts.LibraryShareUnreadable;
                default:
                    return "";
            }
        }

        /// <summary>Who <paramref name="share" /> is for, as a line says it.</summary>
        static string Reach(Texts texts, ShareLine share)
        {
            switch (share.Reach)
            {
                case ShareReach.Faction:
                    return texts.LibrarySharedFaction;
                case ShareReach.Players:
                    var count = share.Players.Split(' ').Length;
                    return count == 1 ? texts.LibrarySharedPlayer : Template.Fill(texts.LibrarySharedPlayers, "count", count.ToString(CultureInfo.InvariantCulture));
                default:
                    return texts.LibrarySharedEveryone;
            }
        }

        /// <summary>Shows <paramref name="names" /> under <paramref name="title" />, the term whose codex page is <paramref name="term" />, or neither where there are none.</summary>
        static void Say(Section section, RichText heading, Label list, string title, string term, List<string> names)
        {
            var shown = names != null && names.Count > 0;
            section.IsVisible = shown;
            if (!shown)
                return;

            heading.Markdown = CodexPages.Term(title.ToUpperInvariant(), term);
            var joined = new StringBuilder();
            for (var i = 0; i < names.Count; i++)
            {
                if (i > 0)
                    joined.Append(", ");

                joined.Append(names[i]);
            }

            list.Text = joined.ToString();
        }

        /// <summary>Who made <paramref name="system" /> and when it last changed, as far as either is known.</summary>
        static string Made(Texts texts, AutomationSystem system)
        {
            var by = system.Namespace;
            if (!system.Changed.HasValue)
                return by.Length == 0 ? "" : Template.Fill(texts.LibraryBy, "name", by);

            var time = LibraryPage.Time(system.Changed.Value);
            return by.Length == 0
                ? Template.Fill(texts.LibraryChanged, "time", time)
                : Template.Fill(Template.Fill(texts.LibraryByChanged, "name", by), "time", time);
        }
    }
}
