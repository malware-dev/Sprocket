using Sprocket.Host.SpaceEngineers.Features.Sharing.Text;
using Sprocket.Host.SpaceEngineers.Framework.Ui.Look;
using System;
using Lumel;
using Lumel.Input;
using Lumel.Menus;
using Lumel.Windows;

namespace Sprocket.Host.SpaceEngineers.Features.Sharing
{
    /// <summary>The menu of ways to share a blueprint or a logic system: the dialog for sharing it, and its text copied as text or packed small.</summary>
    /// <remarks>The items are made when somebody opens the menu, so a screen shown every tick makes none.</remarks>
    public sealed class ShareMenu
    {
        readonly SprocketEditor _editor;
        readonly Menu _menu;
        ShareOffer _offer;

        /// <summary>A menu hanging under <paramref name="anchor" />, offering nothing until it is given an offer.</summary>
        public ShareMenu(SprocketEditor editor, Element anchor)
        {
            _editor = editor;
            _menu = new Menu { Style = Styles.Menu, Anchor = anchor, Side = PopupSide.Bottom, Base = { Gap = 4f } };
            Toggles = new RelayCommand(Toggle);
        }

        /// <summary>What pressing the anchor does: opens the menu, or puts it away where it is open.</summary>
        public ICommand Toggles { get; }

        /// <summary>What the menu offers, or null for nothing, which it closes at once.</summary>
        public ShareOffer Offer
        {
            get { return _offer; }
            set
            {
                _offer = value;
                if (value == null)
                    _menu.Close();
            }
        }

        /// <summary>Hangs the menu open with what the offer holds, or puts it away where it is already open.</summary>
        void Toggle()
        {
            if (_menu.IsOpen)
            {
                _menu.Close();
                return;
            }

            if (_offer == null)
                return;

            var texts = _editor.Texts;
            _menu.Items.Clear();
            if (_offer.Shares != null)
            {
                _menu.Items.Add(new MenuItem { Style = Styles.MenuItem, Text = _offer.SharesText, Command = _offer.Shares, Tip = CodexTip.For(null, _offer.SharesAbout) });
                _menu.Items.Add(new MenuSeparator());
            }

            var offer = _offer;
            _menu.Items.Add(Copy(texts.ShareCopyText, texts.ShareCopyTextAbout, texts.ShareCopiedPlain, () => offer.FnText()));
            _menu.Items.Add(Copy(texts.ShareCopyCompact, texts.ShareCopyCompactAbout, texts.ShareCopied, () => BlueprintText.Write(offer.FnName(), offer.FnText())));
            _menu.Open();
        }

        /// <summary>A menu item, <paramref name="label" /> with the tip <paramref name="about" />, that puts what <paramref name="written" /> gives on the clipboard and says <paramref name="said" />.</summary>
        MenuItem Copy(string label, string about, string said, Func<string> written)
        {
            return new MenuItem
            {
                Style = Styles.MenuItem,
                Text = label,
                Tip = CodexTip.For(null, about),
                Command = new RelayCommand(() =>
                {
                    _editor.Input?.Clipboard?.Set(written());
                    _editor.Ask(said);
                    _editor.Answer(_editor.Texts.ShareCopiedOk, () => { }, true);
                })
            };
        }
    }
}
