using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using System.Collections.Generic;
using Lumel;
using Lumel.Controls;
using Lumel.Input;
using Lumel.Menus;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>The plus a place adds things with, offering those of its choices that may be added now.</summary>
    /// <remarks>The menu's entries are made when somebody opens it, so a screen shown every tick makes none.</remarks>
    public class PlusMenu : Button
    {
        static readonly AddOption[] None = Array.Empty<AddOption>();

        readonly Menu _menu;
        readonly ICommand _toggles;

        IList<AddOption> _choices = None;

        // What the menu lists as it opens: its choices, or the menu of the one choice that may be added now where that offers one.
        IList<AddOption> _listed = None;

        /// <summary>A plus offering nothing yet, hidden until it is shown something to add.</summary>
        public PlusMenu()
        {
            Style = Styles.Start;
            IsVisible = false;
            _menu = new Menu { Style = Styles.Menu, Anchor = this, Side = PopupSide.Right, Base = { Gap = 4f } };
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
            _listed = _choices;

            var offered = Offered();
            IsVisible = offered > 0;
            if (offered != 1)
            {
                Text = head.Shown;
                Tip = CodexTip.For(Tip, head.About);
                Command = _toggles;
                return;
            }

            _menu.Close();
            for (var i = 0; i < _choices.Count; i++)
            {
                var only = _choices[i];
                if (!IsOffered(only))
                    continue;

                // The one choice offering a menu of its own is offered as that choice, opening its menu.
                if (only.Adds == null)
                {
                    _listed = only.Choices;
                    Text = only.Shown;
                    Tip = CodexTip.For(Tip, only.About);
                    Command = _toggles;
                    return;
                }

                Text = only.Shown;
                Tip = CodexTip.For(Tip, only.About);
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
                if (IsOffered(_choices[i]))
                    offered++;
            }

            return offered;
        }

        /// <summary>Whether <paramref name="choice" /> may be added now, or offers a menu of its own holding something that may.</summary>
        static bool IsOffered(AddOption choice)
        {
            if (choice.Adds != null)
                return true;

            for (var i = 0; choice.Choices != null && i < choice.Choices.Count; i++)
            {
                if (IsOffered(choice.Choices[i]))
                    return true;
            }

            return false;
        }

        /// <summary>Fills <paramref name="menu" /> with those of <paramref name="choices" /> that may be added now, a choice offering a menu of its own opening it beside itself.</summary>
        static void Fill(Menu menu, IList<AddOption> choices)
        {
            menu.Items.Clear();
            for (var i = 0; i < choices.Count; i++)
            {
                var choice = choices[i];
                if (!IsOffered(choice))
                    continue;

                if (choice.Adds != null)
                {
                    menu.Items.Add(new MenuItem { Style = Styles.MenuItem, Text = choice.Text, Command = choice.Adds, Tip = CodexTip.For(null, choice.About) });
                    continue;
                }

                var within = new Menu { Style = Styles.Menu };
                Fill(within, choice.Choices);
                menu.Items.Add(new MenuItem { Style = Styles.MenuItem, Text = choice.Text, Submenu = within, Tip = CodexTip.For(null, choice.About) });
            }
        }

        /// <summary>Hangs the menu open with what may be added now, or puts it away where it is already open.</summary>
        void Toggle()
        {
            if (_menu.IsOpen)
            {
                _menu.Close();
                return;
            }

            Fill(_menu, _listed);
            _menu.Open();
        }
    }
}
