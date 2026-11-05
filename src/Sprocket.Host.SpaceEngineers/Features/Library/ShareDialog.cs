using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Shares;

namespace Sprocket.Host.SpaceEngineers.Features.Library
{
    /// <summary>Asks who a blueprint from the library is shared with, and shares it: everyone, the player's faction, or players they tick.</summary>
    public sealed class ShareDialog : Dialog
    {
        const float Wide = 600f;
        const float PlayersHigh = 240f;
        static readonly char[] Spaces = { ' ' };

        readonly ShareBoard _board;
        readonly string _blueprint;
        readonly string[] _words;
        readonly ListField _who;
        readonly VStack _players = new VStack { Base = { Spacing = Gaps.Parts } };
        readonly ScrollView _scroll;
        readonly List<KeyValuePair<long, CheckBox>> _ticks = new List<KeyValuePair<long, CheckBox>>();

        /// <summary>The dialog for the blueprint <paramref name="blueprint" /> called <paramref name="name" />, starting from <paramref name="had" />, its share where it has one.</summary>
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
            _who = new ListField(_words) { Align = Align.Left };
            _who.Text = _words[had == null ? 0 : (int)had.Reach];
            _who.Edited += Follow;
            _scroll = new ScrollView
            {
                Style = Styles.ScrollView,
                Placement = FixedSize.Height(PlayersHigh),
                Content = _players
            };

            var chosen = new HashSet<string>((had?.Players ?? "").Split(Spaces, System.StringSplitOptions.RemoveEmptyEntries));
            foreach (var player in _board.Known)
            {
                var tick = new CheckBox
                {
                    Style = Styles.PickBox,
                    Text = player.Name,
                    Focusable = true,
                    IsChecked = chosen.Contains(player.Identity.ToString(System.Globalization.CultureInfo.InvariantCulture))
                };
                _ticks.Add(new KeyValuePair<long, CheckBox>(player.Identity, tick));
                _players.Children.Add(tick);
            }

            Content = new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = Template.Fill(texts.ShareTitle, "name", name) },
                    new Label { Style = Styles.Detail, Text = texts.ShareWho },
                    _who,
                    _scroll,
                    new HStack
                    {
                        Base = { Spacing = Gaps.Parts },
                        Align = Align.Right,
                        Children =
                        {
                            new Button { Style = Styles.Button, Text = texts.ShareCancel, Command = close },
                            new Button { Style = Styles.Primary, Text = texts.ShareConfirm, Command = new RelayCommand(Confirm) }
                        }
                    }
                }
            };

            Follow();
        }

        void Follow()
        {
            _scroll.IsVisible = Reach == ShareReach.Players;
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
