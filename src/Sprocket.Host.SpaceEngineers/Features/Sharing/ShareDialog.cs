using Sprocket.Host.SpaceEngineers.Features.Selector.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Sharing.Messages;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Fields;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing
{
    /// <summary>
    /// Shares a blueprint with everyone, the player's faction, or players they tick.
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
        /// The dialog for sharing the blueprint <paramref name="blueprint" /> called <paramref name="name" />, starting from
        /// <paramref name="had" />, its share where it has one.
        /// </summary>
        public ShareDialog(SprocketEditor editor, string name, string blueprint, ShareLine had)
        {
            _board = editor.Sharing;
            _blueprint = blueprint;
            var texts = editor.Texts;
            Style = Styles.Dialog;
            Placement = FixedSize.Width(Wide);
            var close = new RelayCommand(Close);
            CancelCommand = close;

            _words = new[] { texts.ShareEveryone, texts.ShareFaction, texts.SharePlayers };
            _who = new ListField(_words);
            // A new share is for the player's faction, or for players they choose where they are in none.
            var first = editor.IsInFaction == null || editor.IsInFaction() ? ShareReach.Faction : ShareReach.Players;
            _who.Text = _words[had == null ? (int)first : (int)had.Reach];
            _who.Edited += Follow;

            var players = new VStack();
            var chosen = new HashSet<string>((had?.Players ?? "").Split(Spaces, System.StringSplitOptions.RemoveEmptyEntries));
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
                    new Label { Style = Styles.Phrase, Text = Template.Fill(texts.ShareTitle, "name", name) }
                }
            };
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
            content.Children.Add(new HStack
            {
                Base = { Spacing = Gaps.Parts },
                Align = Align.Right,
                Children =
                {
                    new Button { Style = Styles.Button, Text = texts.ShareCancel, Command = close },
                    new Button { Style = Styles.Primary, Text = texts.ShareConfirm, Command = new RelayCommand(Confirm) }
                }
            });
            Content = content;
            Follow();
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
