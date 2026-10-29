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
    /// <summary>The plus a place adds things with, offering those of its choices that may be added now.</summary>
    /// <remarks>The menu's entries are made when somebody opens it, so a screen shown every tick makes none.</remarks>
    public class PlusMenu : Button
    {
        static readonly AddOption[] None = Array.Empty<AddOption>();

        readonly Popup _menu;
        readonly VStack _entries;
        readonly ICommand _toggles;

        IList<AddOption> _choices = None;

        /// <summary>A plus offering nothing yet, hidden until it is shown something to add.</summary>
        public PlusMenu()
        {
            Style = Styles.Start;
            IsVisible = false;
            _entries = new VStack();
            _menu = new Popup { Style = Styles.Menu, Anchor = this, Side = PopupSide.Right, Base = { Gap = 4f }, Content = _entries };
            _toggles = new RelayCommand(Toggle);
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
            _choices = head.Choices ?? None;

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
                    Command = new RelayCommand(() => Choose(choice)),
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
