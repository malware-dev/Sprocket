using System;
using System.Collections.Generic;
using System.Globalization;
using Lumel;
using Lumel.Controls;
using Lumel.Images;
using Lumel.Input;
using Lumel.Layout;
using Lumel.Text;
using Lumel.Viewing;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Features.Shell;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// The slots of a toolbar a person presses, laid out as the game lays out a hotbar: its pages, then one page's slots, each
    /// showing whether it holds anything, and a line saying what the chosen one holds.
    /// </summary>
    /// <remarks>
    /// Made again only when the toolbar's shape changes; what the slots hold and which is chosen are painted onto the same
    /// elements.
    /// </remarks>
    public class SlotStrip : Reveal
    {
        const float Square = 36f;
        // Where the words of the row above start: the row's room before its mark, the mark, and the gap after it.
        const float Indent = 40f;

        readonly Texts _texts;
        readonly HStack _pages;
        readonly HStack _row;
        readonly Label _says;
        readonly List<Button> _tabs = new List<Button>();
        readonly List<ZStack> _squares = new List<ZStack>();
        readonly List<Image> _pictures = new List<Image>();
        readonly List<Label> _numbers = new List<Label>();
        readonly List<ZStack> _rings = new List<ZStack>();
        readonly List<string> _drawn = new List<string>();
        readonly SlotHeld _empty = new SlotHeld();

        IList<SlotHeld> _held = new List<SlotHeld>();
        int _pageCount;
        int _slotCount;
        int _page = 1;
        int _chosenPage;
        int _chosenSlot;

        /// <summary>No toolbar yet, saying its fixed words in <paramref name="texts" />.</summary>
        public SlotStrip(Texts texts)
        {
            _texts = texts;
            // The gutter's spine runs down under the row's mark and ties the strip to the row it belongs to.
            var line = new Gutter(Indent, Gaps.Parts);
            var body = line.Body;
            Content = line;
            body.Children.Add(new HStack { Base = { Spacing = Gaps.Within } }.As(out _pages));
            body.Children.Add(new HStack { Base = { Spacing = Gaps.Within } }.As(out _row));
            body.Children.Add(new Label { Style = Styles.ValueLabel, TextWrapping = TextWrapping.Wrap }.As(out _says));
            IsOpen = true;
        }

        /// <summary>Raised with the page and slot somebody pressed, each counted from 1.</summary>
        public event Action<int, int> Picked;

        /// <summary>The page whose slots are on show, counted from 1.</summary>
        public int Page => _page;

        /// <summary>The line under the slots, saying which is chosen and what it holds.</summary>
        public string Line => _says.Text;

        /// <summary>What the slot at <paramref name="page" /> and <paramref name="slot" /> holds, each counted from 1.</summary>
        public SlotHeld HeldAt(int page, int slot) => Held(page, slot);

        /// <summary>
        /// Shows a toolbar of <paramref name="pages" /> pages of <paramref name="slots" /> slots holding <paramref name="held" />,
        /// page by page, with the slot at <paramref name="page" /> and <paramref name="slot" /> chosen; a slot of 0 chooses none.
        /// </summary>
        /// <remarks>Opens on the page of the slot chosen, and stays on whichever page somebody turns to after that.</remarks>
        public void Show(int pages, int slots, IList<SlotHeld> held, int page, int slot)
        {
            if (pages != _pageCount || slots != _slotCount)
                Shape(pages, slots);

            if (slot > 0 && (page != _chosenPage || slot != _chosenSlot))
                _page = page;

            _held = held;
            _chosenPage = page;
            _chosenSlot = slot;
            Paint();
        }

        void Shape(int pages, int slots)
        {
            _pageCount = pages;
            _slotCount = slots;
            _page = 1;

            _pages.Children.Clear();
            _tabs.Clear();
            _pages.IsVisible = pages > 1;
            if (pages > 1)
            {
                _pages.Children.Add(new Label { Style = Styles.ValueLabel, Text = _texts.SlotPage });
                for (var page = 1; page <= pages; page++)
                {
                    var turned = page;
                    var tab = new Button { Style = Styles.SlotPage, Text = Number(page), Command = new RelayCommand(() => Turn(turned)) };
                    _tabs.Add(tab);
                    _pages.Children.Add(tab);
                }
            }

            _row.Children.Clear();
            _squares.Clear();
            _pictures.Clear();
            _numbers.Clear();
            _rings.Clear();
            _drawn.Clear();
            for (var slot = 1; slot <= slots; slot++)
            {
                var pressed = slot;
                var square = new ZStack { Style = Styles.SlotFree, Placement = FixedSize.Size(Square, Square), Command = new RelayCommand(() => Press(pressed)) };
                var picture = new Image { Base = { Padding = new Thickness(4f) }, IsVisible = false };
                var number = new Label { Style = Styles.SlotNumber, Text = Number(slot), Align = Align.Center };
                var ring = new ZStack { Style = Styles.SlotChosen, IsVisible = false };
                square.Children.Add(picture);
                square.Children.Add(number);
                square.Children.Add(ring);

                _squares.Add(square);
                _pictures.Add(picture);
                _numbers.Add(number);
                _rings.Add(ring);
                _drawn.Add("");
                _row.Children.Add(square);
            }
        }

        /// <summary>Shows the slots of <paramref name="page" />, as pressing its number does.</summary>
        public void Turn(int page)
        {
            _page = page;
            Paint();
        }

        /// <summary>Chooses <paramref name="slot" /> of the page on show, as pressing it does.</summary>
        public void Press(int slot)
        {
            var picked = Picked;
            if (picked != null)
                picked(_page, slot);
        }

        void Paint()
        {
            for (var page = 1; page <= _tabs.Count; page++)
            {
                var holds = false;
                for (var slot = 1; slot <= _slotCount && !holds; slot++)
                    holds = Held(page, slot).Holds;

                _tabs[page - 1].Set(LookStates.Chosen, page == _page);
                _tabs[page - 1].Set(LookStates.Taken, holds);
            }

            for (var slot = 1; slot <= _squares.Count; slot++)
            {
                var held = Held(_page, slot);
                var at = slot - 1;
                _squares[at].Set(LookStates.Answering, held.Answering);
                _squares[at].Set(LookStates.Taken, held.Taken);
                _squares[at].Tip = held.Holds ? new Tip(Tipped(held)) : null;
                _rings[at].IsVisible = _page == _chosenPage && slot == _chosenSlot;

                if (_drawn[at] != held.Picture)
                {
                    _drawn[at] = held.Picture;
                    _pictures[at].Source = held.Picture.Length == 0 ? null : Icons.PictureFor(held.Picture);
                    _pictures[at].IsVisible = held.Picture.Length > 0;
                    _numbers[at].Align = held.Picture.Length > 0 ? Align.Left | Align.Top : Align.Center;
                }
            }

            if (_chosenSlot < 1)
            {
                _says.Text = "";
                return;
            }

            var chosen = Held(_chosenPage, _chosenSlot);
            var says = !chosen.Holds ? _texts.SlotFree
                : chosen.Lines.Count > 1 ? $"{chosen.Says} {Template.Fill(_texts.SlotMore, "count", Number(chosen.Lines.Count - 1))}"
                : chosen.Says;
            var line = _pageCount > 1
                ? Template.Fill(Template.Fill(_texts.SlotOnPage, "page", Number(_chosenPage)), "slot", Number(_chosenSlot))
                : Template.Fill(_texts.SlotAlone, "slot", Number(_chosenSlot));
            _says.Text = Template.Fill(line, "held", says);
        }

        /// <summary>A slot's tip: its label where somebody gave it one, over what it holds on each block, a line each.</summary>
        string Tipped(SlotHeld held)
        {
            var tip = string.Join("\n", held.Lines);
            return held.Label.Length == 0 ? tip : $"{Template.Fill(_texts.SlotLabel, "label", held.Label)}\n{tip}";
        }

        SlotHeld Held(int page, int slot)
        {
            var at = (page - 1) * _slotCount + slot - 1;
            return at >= 0 && at < _held.Count ? _held[at] : _empty;
        }

        static string Number(int counted)
        {
            return counted.ToString(CultureInfo.InvariantCulture);
        }
    }
}
