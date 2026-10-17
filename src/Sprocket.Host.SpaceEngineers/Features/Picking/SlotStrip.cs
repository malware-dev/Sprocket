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
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// The slots of a toolbar a person presses, laid out as the game lays out a hotbar: its pages, then one page's slots, each
    /// showing whether it holds anything, and a line saying what the chosen one holds.
    /// </summary>
    /// <remarks>
    /// Shown under the toolbar's line in the picker, set in so it reads as part of that line, and sliding open as that line
    /// is chosen. Made again only when the toolbar's shape changes; what the slots hold and which is chosen are painted
    /// onto the same elements.
    /// </remarks>
    public class SlotStrip : Reveal
    {
        const float Square = 36f;
        const float Indent = 28f;
        const float Frames = 12f;

        readonly Texts _texts;
        readonly HStack _pages;
        readonly HStack _row;
        readonly Label _says;
        readonly List<Button> _tabs = new List<Button>();
        readonly List<Layers> _squares = new List<Layers>();
        readonly List<Image> _pictures = new List<Image>();
        readonly List<Label> _numbers = new List<Label>();
        readonly List<Layers> _rings = new List<Layers>();
        readonly List<string> _drawn = new List<string>();
        readonly SlotHeld _empty = new SlotHeld();

        IList<SlotHeld> _held = new List<SlotHeld>();
        int _pageCount;
        int _slotCount;
        int _page = 1;
        int _chosenPage;
        int _chosenSlot;
        float _progress;

        /// <summary>No toolbar yet, saying its fixed words in <paramref name="texts" />.</summary>
        public SlotStrip(Texts texts)
        {
            _texts = texts;
            var line = new HStack();
            line.Children.Add(new Layers { Placement = FixedSize.Size(Indent, 1f) });
            VStack body;
            line.Children.Add(new VStack { Spacing = 8f }.As(out body));
            Content = line;
            body.Children.Add(new HStack { Spacing = 4f }.As(out _pages));
            body.Children.Add(new HStack { Spacing = 4f }.As(out _row));
            body.Children.Add(new Label { Style = Styles.ValueLabel }.As(out _says));
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

        /// <summary>Opens a step further, until it is all in view.</summary>
        /// <remarks>Called every tick the picker is shown; one comparison once open, as a reveal changes nothing it already holds.</remarks>
        public void Slide()
        {
            if (_progress >= 1f)
                return;

            _progress = Math.Min(1f, _progress + 1f / Frames);
            var left = 1f - _progress;
            Openness = 1f - left * left * left;
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
                    var tab = new Button { Style = Styles.SlotPage, Text = Number(page), Command = new Relay(() => Turn(turned)) };
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
                var square = new Layers { Style = Styles.SlotFree, Placement = FixedSize.Size(Square, Square), Command = new Relay(() => Press(pressed)) };
                var picture = new Image { Padding = new Thickness(4f), IsVisible = false };
                var number = new Label { Style = Styles.SlotNumber, Text = Number(slot), Align = Align.Center };
                var ring = new Layers { Style = Styles.SlotChosen, IsVisible = false };
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

                _tabs[page - 1].Style = page == _page ? Styles.SlotPageChosen : holds ? Styles.SlotPageTaken : Styles.SlotPage;
            }

            for (var slot = 1; slot <= _squares.Count; slot++)
            {
                var held = Held(_page, slot);
                var at = slot - 1;
                _squares[at].Style = held.Answering ? Styles.SlotAnswering : held.Taken ? Styles.SlotTaken : Styles.SlotFree;
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
