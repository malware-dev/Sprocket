using System;
using System.Collections.Generic;
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

        Settings _shown;

        /// <summary>The dialog for <paramref name="editor" />, showing the settings the world runs by now, on this world's tab.</summary>
        public AdminDialog(SprocketEditor editor)
        {
            _editor = editor;
            var texts = editor.Texts;
            Style = Styles.Dialog;
            Placement = FixedSize.Wide(Wide);
            var close = new Relay(Close);
            Cancel = close;
            Accept = close;

            Content = new VStack
            {
                Spacing = 16f,
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = texts.AdminTitle },
                    new Label { Style = Styles.Detail, Wraps = true, Text = texts.AdminLine },
                    new TabView
                    {
                        HeaderStyle = Styles.Tab,
                        Tabs =
                        {
                            new Tab { Header = texts.AdminThisWorld, Content = Page(texts, true) },
                            new Tab { Header = texts.AdminEveryWorld, Tip = Tip.For(null, texts.Codex.AdminEveryWorldAbout), Content = Page(texts, false) }
                        }
                    },
                    new Button { Style = Styles.Primary, Text = texts.AdminClose, Align = Align.Right, Command = close }
                }
            };

            Show(editor.WorldSettings());
        }

        /// <summary>The settings it shows, which are what it changes from.</summary>
        public Settings Shown => _shown;

        /// <summary>The page of one level: this world's where <paramref name="world" /> says, every world's otherwise.</summary>
        Element Page(Texts texts, bool world)
        {
            return new VStack
            {
                Spacing = 16f,
                Children =
                {
                    Setting(texts.AdminFastest, texts.AdminFastestAbout, Choose(texts, world,
                        new[] { texts.AdminEveryTick, texts.AdminEveryTen, texts.AdminEveryHundred },
                        new[] { texts.AdminDefaultEveryTick, texts.AdminDefaultEveryTen, texts.AdminDefaultEveryHundred },
                        layer => layer.FastestUpdate.HasValue ? Array.IndexOf(SettingsLayer.Updates, layer.FastestUpdate.Value) : -1,
                        settings => Array.IndexOf(SettingsLayer.Updates, settings.DefaultFastestUpdate),
                        (layer, at) => layer.WithFastestUpdate(at < 0 ? (int?)null : SettingsLayer.Updates[at]))),
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
                        (layer, at) => layer.WithDebugLog(at < 0 ? (bool?)null : at == 0))),
                    new Label { Style = Styles.Heading, Text = texts.AdminTrials },
                    new Label { Style = Styles.Detail, Wraps = true, Text = texts.AdminTrialsAbout },
                    Setting(texts.AdminTips, texts.AdminTipsAbout, Choose(texts, world,
                        new[] { texts.AdminTipsStay, texts.AdminTipsAlt },
                        null,
                        layer => layer.TipKeeping.HasValue ? Keeping(layer.TipKeeping.Value) : -1,
                        settings => Keeping(settings.DefaultTipKeeping),
                        (layer, at) => layer.WithTipKeeping(at < 0 ? (TipKeeping?)null : at == 0 ? TipKeeping.Stay : TipKeeping.Alt))),
                    Setting(texts.AdminLinks, texts.AdminLinksAbout, Choose(texts, world,
                        new[] { texts.AdminLinksReplace, texts.AdminLinksBeside },
                        null,
                        layer => layer.TipLinks.HasValue ? Linking(layer.TipLinks.Value) : -1,
                        settings => Linking(settings.DefaultTipLinks),
                        (layer, at) => layer.WithTipLinks(at < 0 ? (TipLinks?)null : at == 0 ? TipLinks.Replace : TipLinks.Beside)))
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
                field.Explain(0, Tip.For(null, texts.Codex.AdminDefaultAbout));

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
                var given = choice.World ? choice.Given(settings.World) : -1;
                if (choice.Defaults == null || !choice.World)
                {
                    choice.Field.Text = choice.Words[given < 0 ? standing : given];
                    continue;
                }

                choice.Field.Respell(0, choice.Defaults[standing]);
                choice.Field.Text = given < 0 ? choice.Defaults[standing] : choice.Words[given];
            }
        }

        /// <summary>Where <paramref name="keeping" /> stands among its words: staying, then kept by Alt.</summary>
        static int Keeping(TipKeeping keeping)
        {
            return keeping == TipKeeping.Stay ? 0 : 1;
        }

        /// <summary>Where <paramref name="links" /> stands among its words: in the same tip, then beside it.</summary>
        static int Linking(TipLinks links)
        {
            return links == TipLinks.Replace ? 0 : 1;
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
                Spacing = 6f,
                Children =
                {
                    new Label { Style = Styles.Phrase, Text = name },
                    new Label { Style = Styles.Detail, Wraps = true, Text = about },
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
                Given = given;
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
            public Func<SettingsLayer, int> Given { get; }

            /// <summary>Where the value a world that leaves the setting takes stands among <see cref="Words" />.</summary>
            public Func<Settings, int> Standing { get; }

            /// <summary>A level giving the value at a place among <see cref="Words" />, or leaving it at -1.</summary>
            public Func<SettingsLayer, int, SettingsLayer> Giving { get; }
        }
    }
}
