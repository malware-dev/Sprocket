using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Windows;
using Sprocket.Host.SpaceEngineers.Features.Look;

namespace Sprocket.Host.SpaceEngineers.Features.Shell
{
    /// <summary>The plus a place adds things with, given every choice it has, which shows as many as may be added now.</summary>
    /// <remarks>
    /// Where several may be added it shows its own words and hangs a menu of them beside itself; where one may, it shows that
    /// one and adds it when pressed; where none may, it is hidden. So a place hands over every choice it ever has and never
    /// counts them itself. The menu's entries are made when it opens, which is when somebody presses it, so a screen shown
    /// every tick makes none.
    /// </remarks>
    public class PlusMenu : Button
    {
        static readonly AddOption[] _none = Array.Empty<AddOption>();

        readonly Popup _menu;
        readonly VStack _entries;
        readonly ICommand _toggles;

        IList<AddOption> _choices = _none;

        /// <summary>A plus offering nothing yet, hidden until it is shown something to add.</summary>
        public PlusMenu()
        {
            Style = Styles.Start;
            IsVisible = false;
            _entries = new VStack();
            _menu = new Popup { Style = Styles.Menu, Anchor = this, Side = PopupSide.Beside, Gap = 4f, Content = _entries };
            _toggles = new Relay(Toggle);
        }

        /// <summary>Everything it was given to offer, in the order the menu lists them, those that may not be added now among them.</summary>
        public IList<AddOption> Choices => _choices;

        /// <summary>Whether anything it offers may be added now.</summary>
        public bool Offers => Offered() > 0;

        /// <summary>
        /// Offers the choices <paramref name="head" /> holds: as a menu under its words where several may be added now, as the
        /// one itself where one may, and not at all where none may.
        /// </summary>
        public void Show(AddOption head)
        {
            _choices = head.Choices ?? _none;

            var offered = Offered();
            IsVisible = offered > 0;
            if (offered != 1)
            {
                Text = head.Shown;
                Tip = Tip.For(Tip, head.About);
                Command = _toggles;
                return;
            }

            _menu.Close();
            for (var i = 0; i < _choices.Count; i++)
            {
                var only = _choices[i];
                if (only.Adds == null)
                    continue;

                Text = only.Shown;
                Tip = Tip.For(Tip, only.About);
                Command = only.Adds;
                return;
            }
        }

        /// <summary>How many of its choices may be added now.</summary>
        int Offered()
        {
            var offered = 0;
            for (var i = 0; i < _choices.Count; i++)
            {
                if (_choices[i].Adds != null)
                    offered++;
            }

            return offered;
        }

        /// <summary>Hangs the menu open with what may be added now, or puts it away where it is already open.</summary>
        void Toggle()
        {
            if (_menu.IsOpen)
            {
                _menu.Close();
                return;
            }

            _entries.Children.Clear();
            for (var i = 0; i < _choices.Count; i++)
            {
                var choice = _choices[i];
                if (choice.Adds == null)
                    continue;

                _entries.Children.Add(new HStack
                {
                    Style = Styles.MenuRow,
                    Command = new Relay(() => Choose(choice)),
                    Tip = Tip.For(null, choice.About),
                    Children = { new Label { Style = Styles.ListWord, Text = choice.Text } }
                });
            }

            _menu.Open();
        }

        /// <summary>Puts the menu away, then adds what <paramref name="choice" /> adds.</summary>
        void Choose(AddOption choice)
        {
            _menu.Close();
            choice.Adds.Execute();
        }
    }
}
