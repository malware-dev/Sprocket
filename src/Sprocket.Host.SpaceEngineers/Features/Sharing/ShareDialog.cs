using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Menus;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing
{
    /// <summary>
    /// Shares a blueprint from the library: as text copied for another world, and, where the world allows it, with everyone,
    /// the player's faction, or players they tick.
    /// </summary>
    public sealed class ShareDialog : Dialog
    {
        const float Wide = 440f;
        const float PlayersHigh = 160f;
        static readonly char[] Spaces = { ' ' };

        readonly ShareBoard _board;
        readonly string _blueprint;
        readonly string[] _words;
        readonly ListField _who;
        readonly TextBox _search;
        readonly List<KeyValuePair<long, CheckBox>> _ticks = new List<KeyValuePair<long, CheckBox>>();

        /// <summary>
        /// The dialog for the blueprint <paramref name="blueprint" /> called <paramref name="name" />, offering to share it in the
        /// world where <paramref name="inWorld" />, starting from <paramref name="had" />, its share where it has one.
        /// </summary>
        public ShareDialog(SprocketEditor editor, string name, string blueprint, bool inWorld, ShareLine had)
        {
            _board = inWorld ? editor.Sharing : null;
            _blueprint = blueprint;
            var texts = editor.Texts;
            Style = Styles.Dialog;
            Placement = FixedSize.Width(Wide);
            var close = new RelayCommand(Close);
            CancelCommand = close;

            var copies = new Button { Style = Styles.Button, Text = texts.ShareCopy, Align = Align.Left, Tip = CodexTip.For(null, texts.ShareCopyAbout) };
            var ways = new Menu { Style = Styles.Menu, Anchor = copies, Side = PopupSide.Bottom, Base = { Gap = 4f } };
            ways.Items.Add(Copy(editor, texts.ShareCopyText, texts.ShareCopyTextAbout, blueprint, texts.ShareCopiedPlain));
            ways.Items.Add(Copy(editor, texts.ShareCopyCompact, texts.ShareCopyCompactAbout, BlueprintText.Write(name, blueprint), texts.ShareCopied));
            copies.Command = new RelayCommand(() =>
            {
                if (ways.IsOpen)
                    ways.Close();
                else
                    ways.Open();
            });

            _words = new[] { texts.ShareEveryone, texts.ShareFaction, texts.SharePlayers };
            _who = new ListField(_words);
            // A new share is for the player's faction, or for players they choose where they are in none.
            var first = editor.IsInFaction == null || editor.IsInFaction() ? ShareReach.Faction : ShareReach.Players;
            _who.Text = _words[had == null ? (int)first : (int)had.Reach];
            _who.Edited += Follow;

            var players = new VStack();
            var chosen = new HashSet<string>((had?.Players ?? "").Split(Spaces, System.StringSplitOptions.RemoveEmptyEntries));
            if (_board != null)
            {
                foreach (var player in _board.Known)
                {
                    var tick = new CheckBox
                    {
                        Style = Styles.NamedPickBox,
                        Text = player.Name,
                        Focusable = true,
                        IsChecked = chosen.Contains(player.Identity.ToString(System.Globalization.CultureInfo.InvariantCulture))
                    };
                    tick.Command = new RelayCommand(() => tick.IsChecked = !tick.IsChecked);
                    _ticks.Add(new KeyValuePair<long, CheckBox>(player.Identity, tick));
                    players.Children.Add(tick);
                }
            }

            _search = new TextBox { Style = Styles.Search, Focusable = true, Hint = texts.PickSearch };
            _search.Edited += Narrow;
            var picking = new VStack
            {
                Base = { Spacing = Gaps.Parts },
                Children =
                {
                    _search,
                    new ScrollView { Style = Styles.FramedList, Placement = FixedSize.Height(PlayersHigh), Content = players }
                }
            };

            var content = new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = Template.Fill(texts.ShareTitle, "name", name) },
                    copies
                }
            };
            var buttons = new HStack { Base = { Spacing = Gaps.Parts }, Align = Align.Right };
            if (_board != null)
            {
                content.Children.Add(new VStack
                {
                    Base = { Spacing = Gaps.Parts },
                    Children =
                    {
                        new Label { Style = Styles.Detail, Text = texts.ShareInWorld },
                        _who,
                        picking
                    }
                });
                buttons.Children.Add(new Button { Style = Styles.Button, Text = texts.ShareCancel, Command = close });
                buttons.Children.Add(new Button { Style = Styles.Primary, Text = texts.ShareConfirm, Command = new RelayCommand(Confirm) });
            }
            else
            {
                buttons.Children.Add(new Button { Style = Styles.Primary, Text = texts.ShareClose, Command = close });
            }

            content.Children.Add(buttons);
            Content = content;
            Follow();
        }

        /// <summary>A menu entry, <paramref name="label" /> with the tip <paramref name="about" />, that puts <paramref name="copied" /> on the clipboard, closes the dialog and says <paramref name="said" />.</summary>
        MenuItem Copy(SprocketEditor editor, string label, string about, string copied, string said)
        {
            return new MenuItem
            {
                Style = Styles.MenuItem,
                Text = label,
                Tip = CodexTip.For(null, about),
                Command = new RelayCommand(() =>
                {
                    editor.Input?.Clipboard?.Set(copied);
                    Close();
                    editor.Ask(said);
                    editor.Answer(editor.Texts.ShareCopiedOk, () => { }, true);
                })
            };
        }

        // The players stay in place while another reach is chosen, so the dialog keeps one size from the moment it opens.
        void Follow()
        {
            var picking = Reach == ShareReach.Players;
            _search.IsEnabled = picking;
            foreach (var tick in _ticks)
                tick.Value.IsEnabled = picking;
        }

        void Narrow()
        {
            foreach (var tick in _ticks)
                tick.Value.IsVisible = EntityQuery.MatchesText(tick.Value.Text, _search.Text);
        }

        ShareReach Reach => (ShareReach)System.Array.IndexOf(_words, _who.Text);

        void Confirm()
        {
            var players = new List<long>();
            foreach (var tick in _ticks)
            {
                if (tick.Value.IsChecked)
                    players.Add(tick.Key);
            }

            _board.Share(_blueprint, Reach, players);
            Close();
        }
    }
}
