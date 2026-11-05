using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Admin
{
    /// <summary>The world's settings, for an admin to change: a tab for this world and one for every world, each setting a list of its choices.</summary>
    /// <remarks>A trial has no default under this world.</remarks>
    public sealed class AdminDialog : Dialog
    {
        const float Wide = 600f;

        readonly SprocketEditor _editor;
        readonly List<Choice> _choices = new List<Choice>();
        readonly VStack _shares = new VStack { Base = { Spacing = Gaps.Parts } };

        Settings _shown;

        /// <summary>The dialog for <paramref name="editor" />, showing the settings the world runs by now, on this world's tab.</summary>
        public AdminDialog(SprocketEditor editor)
        {
            _editor = editor;
            var texts = editor.Texts;
            Style = Styles.Dialog;
            Placement = FixedSize.Width(Wide);
            var close = new RelayCommand(Close);
            CancelCommand = close;
            AcceptCommand = close;

            Content = new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = texts.AdminTitle },
                    new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Wrap, Text = texts.AdminLine },
                    new TabView
                    {
                        Tabs =
                        {
                            new Tab { Style = Styles.Tab, Header = texts.AdminThisWorld, Content = Page(texts, true) },
                            new Tab { Style = Styles.Tab, Header = texts.AdminSharing, IsVisible = editor.Sharing != null, Content = _shares },
                            new Tab { Style = Styles.Tab, Header = texts.AdminEveryWorld, Tip = CodexTip.For(null, texts.Codex.AdminEveryWorldAbout), Content = Page(texts, false) }
                        }
                    },
                    new Button { Style = Styles.Primary, Text = texts.AdminClose, Align = Align.Right, Command = close }
                }
            };

            Show(editor.WorldSettings());

            if (editor.Sharing != null)
            {
                editor.Sharing.Changed += ListShares;
                editor.Sharing.Ask();
                ListShares();
            }
        }

        /// <inheritdoc />
        protected override void OnClosed(CloseReason reason)
        {
            if (_editor.Sharing != null)
                _editor.Sharing.Changed -= ListShares;

            base.OnClosed(reason);
        }

        /// <summary>Lists every share in the world and every player stopped from sharing, each with what an admin can do about it.</summary>
        void ListShares()
        {
            var texts = _editor.Texts;
            var board = _editor.Sharing;
            _shares.Children.Clear();
            if (board.All.Count == 0)
                _shares.Children.Add(new Label { Style = Styles.Detail, Text = texts.AdminNoShares });

            foreach (var share in board.All)
            {
                var sharer = share.Sharer;
                var identity = Guid.Parse(share.Identity);
                var said = Template.Fill(Template.Fill(texts.AdminShareLine, "name", share.Name), "sharer", share.SharerName);
                _shares.Children.Add(new HStack
                {
                    Base = { Spacing = Gaps.Parts },
                    Children =
                    {
                        new Label { Style = Styles.Detail, Text = said, Placement = Flex.Grow },
                        new Button { Style = Styles.Button, Text = texts.AdminRemove, Command = new RelayCommand(() => board.Withdraw(sharer, identity)) },
                        new Button { Style = Styles.Button, Text = Template.Fill(texts.AdminBar, "name", share.SharerName), Command = new RelayCommand(() => board.Bar(sharer)) }
                    }
                });
            }

            foreach (var player in board.Known)
            {
                if (!player.IsBarred)
                    continue;

                var barred = player.Identity;
                _shares.Children.Add(new Button
                {
                    Style = Styles.Button,
                    Text = Template.Fill(texts.AdminUnbar, "name", player.Name),
                    Align = Align.Left,
                    Command = new RelayCommand(() => board.Unbar(barred))
                });
            }
        }

        /// <summary>The settings it shows, which are what it changes from.</summary>
        public Settings Shown => _shown;

        /// <summary>The page of one level: this world's where <paramref name="world" /> says, every world's otherwise.</summary>
        Element Page(Texts texts, bool world)
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Items },
                Children =
                {
                    Setting(texts.AdminFastest, texts.AdminFastestAbout, Choose(texts, world,
                        new[] { texts.AdminEveryTick, texts.AdminEveryTen, texts.AdminEveryHundred },
                        new[] { texts.AdminDefaultEveryTick, texts.AdminDefaultEveryTen, texts.AdminDefaultEveryHundred },
                        layer => layer.FastestUpdate.HasValue ? Array.IndexOf(SettingsLayer.Updates, layer.FastestUpdate.Value) : -1,
                        settings => Array.IndexOf(SettingsLayer.Updates, settings.DefaultFastestUpdate),
                        (layer, at) => layer.WithFastestUpdate(at < 0 ? (int?)null : SettingsLayer.Updates[at]))),
                    Setting(texts.AdminShareLimit, texts.AdminShareLimitAbout, Choose(texts, world,
                        LimitWords(texts, null),
                        LimitWords(texts, texts.AdminDefaultLimit),
                        layer => layer.ShareLimit.HasValue ? Array.IndexOf(SettingsLayer.ShareLimits, layer.ShareLimit.Value) : -1,
                        settings => Array.IndexOf(SettingsLayer.ShareLimits, SettingsLayer.NearestLimit(settings.DefaultShareLimit)),
                        (layer, at) => layer.WithShareLimit(at < 0 ? (int?)null : SettingsLayer.ShareLimits[at]))),
                    Setting(texts.AdminDeveloper, texts.AdminDeveloperAbout, Choose(texts, world,
                        new[] { texts.AdminOn, texts.AdminOff },
                        new[] { texts.AdminDefaultOn, texts.AdminDefaultOff },
                        layer => layer.DeveloperMode.HasValue ? Switch(layer.DeveloperMode.Value) : -1,
                        settings => Switch(settings.DefaultDeveloperMode),
                        (layer, at) => layer.WithDeveloperMode(at < 0 ? (bool?)null : at == 0))),
                    Setting(texts.AdminDebugLog, texts.AdminDebugLogAbout, Choose(texts, world,
                        new[] { texts.AdminOn, texts.AdminOff },
                        new[] { texts.AdminDefaultOn, texts.AdminDefaultOff },
                        layer => layer.DebugLog.HasValue ? Switch(layer.DebugLog.Value) : -1,
                        settings => Switch(settings.DefaultDebugLog),
                        (layer, at) => layer.WithDebugLog(at < 0 ? (bool?)null : at == 0)))
                }
            };
        }

        /// <summary>
        /// The list of one setting at one level: <paramref name="words" /> for its values, and under this world, first, the default,
        /// spelled as <paramref name="defaults" /> has the value it stands for, where the setting has one.
        /// </summary>
        Element Choose(Texts texts, bool world, string[] words, string[] defaults, Func<SettingsLayer, int> given, Func<Settings, int> standing,
            Func<SettingsLayer, int, SettingsLayer> giving)
        {
            var defaulted = world && defaults != null;
            var listed = new List<string>();
            if (defaulted)
                listed.Add(defaults[0]);
            listed.AddRange(words);

            var field = new ListField(listed) { Align = Align.Left };
            if (defaulted)
                field.Explain(0, CodexTip.For(null, texts.Codex.AdminDefaultAbout));

            var choice = new Choice(world, field, words, defaulted ? defaults : null, given, standing, giving);
            field.Edited += () => Change(choice);
            _choices.Add(choice);
            return field;
        }

        /// <summary>Makes the change the word <paramref name="choice" /> now gives asks for, at its level.</summary>
        void Change(Choice choice)
        {
            var at = Array.IndexOf(choice.Words, choice.Field.Text);
            var next = choice.World
                ? _shown.WithWorld(choice.Giving(_shown.World, at))
                : _shown.WithGlobal(choice.Giving(_shown.Global, at));

            _editor.ChangeSettings(next);
            Show(next);
        }

        /// <summary>Gives each list the word <paramref name="settings" /> has for it, each default spelled with the value it stands for.</summary>
        void Show(Settings settings)
        {
            _shown = settings;

            foreach (var choice in _choices)
            {
                var standing = choice.Standing(settings);
                var given = choice.World ? choice.GivenAt(settings.World) : -1;
                if (choice.Defaults == null || !choice.World)
                {
                    choice.Field.Text = choice.Words[given < 0 ? standing : given];
                    continue;
                }

                choice.Field.Respell(0, choice.Defaults[standing]);
                choice.Field.Text = given < 0 ? choice.Defaults[standing] : choice.Words[given];
            }
        }

        /// <summary>Each share limit as a word: none as off, the rest as numbers, filled into <paramref name="template" /> where it is given.</summary>
        static string[] LimitWords(Texts texts, string template)
        {
            var words = new string[SettingsLayer.ShareLimits.Length];
            for (var i = 0; i < words.Length; i++)
            {
                var word = i == 0 ? texts.AdminOff : SettingsLayer.ShareLimits[i].ToString(CultureInfo.InvariantCulture);
                words[i] = template == null ? word : Template.Fill(template, "value", word);
            }

            return words;
        }

        /// <summary>Where a switch being <paramref name="on" /> stands among its words: on, then off.</summary>
        static int Switch(bool on)
        {
            return on ? 0 : 1;
        }
        /// <summary>A setting as it reads: its name, what it does, then <paramref name="control" />.</summary>
        static Element Setting(string name, string about, Element control)
        {
            return new VStack
            {
                Base = { Spacing = Gaps.Within },
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = name },
                    new Label { Style = Styles.Detail, TextWrapping = TextWrapping.Wrap, Text = about },
                    control
                }
            };
        }

        /// <summary>One setting's list at one level, and how its words stand for the setting's values.</summary>
        sealed class Choice
        {
            public Choice(bool world, ListField field, string[] words, string[] defaults, Func<SettingsLayer, int> given,
                Func<Settings, int> standing, Func<SettingsLayer, int, SettingsLayer> giving)
            {
                World = world;
                Field = field;
                Words = words;
                Defaults = defaults;
                GivenAt = given;
                Standing = standing;
                Giving = giving;
            }

            /// <summary>Whether it is this world's list, every world's otherwise.</summary>
            public bool World { get; }

            /// <summary>The list.</summary>
            public ListField Field { get; }

            /// <summary>The setting's values, as words.</summary>
            public string[] Words { get; }

            /// <summary>The default, spelled with each value it may stand for, in the order of <see cref="Words" />; null where the list has none.</summary>
            public string[] Defaults { get; }

            /// <summary>Where the value a level gives stands among <see cref="Words" />, or -1 where the level leaves it.</summary>
            public Func<SettingsLayer, int> GivenAt { get; }

            /// <summary>Where the value a world that leaves the setting takes stands among <see cref="Words" />.</summary>
            public Func<Settings, int> Standing { get; }

            /// <summary>A level giving the value at a place among <see cref="Words" />, or leaving it at -1.</summary>
            public Func<SettingsLayer, int, SettingsLayer> Giving { get; }
        }
    }
}
