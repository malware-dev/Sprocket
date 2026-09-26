using Sprocket.Host.SpaceEngineers.Controls;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Host.SpaceEngineers.Words;
using Sprocket.Host.SpaceEngineers.World;
using Sprocket.Host.SpaceEngineers.Ui;
using Sprocket.Host.SpaceEngineers.Ui.Legacy;
using Sprocket.Host.SpaceEngineers.Ui.Text;
using Glyph = Sprocket.Host.SpaceEngineers.Ui.Legacy.Glyph;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Screen;
using Sprocket.Host.SpaceEngineers.Ui.Input;

namespace Sprocket.Host.SpaceEngineers.Blueprints
{
    /// <summary>One part of a blueprint on screen: its name, what it says about itself, and its blocks.</summary>
    /// <remarks>
    /// The blocks are a slot, the same chip a sentence fills: a part is what a sentence names, and
    /// pressing it chooses what fills it, which is what pressing a sentence's slot does too.
    /// </remarks>
    public sealed class PartRow
    {
        const float NameWidth = 360f;

        readonly Bar _line = new Bar();
        readonly Stack _words = Widgets.Column(Space.Line);
        readonly Stack _named = Widgets.RowOf(Space.Item);
        readonly Label _name = Widgets.Label("", Palette.Ink, Lettering.Body);
        readonly Glyph _pencil = Widgets.Symbol(Symbols.Pencil, Palette.Faint);
        readonly Label _about = Widgets.Label("", Palette.Faint, Lettering.Tiny);
        readonly Slot _blocks = new Slot();
        readonly List<Mention> _pieces = new List<Mention>();

        readonly Stack _slot = Widgets.RowOf(0f);
        readonly ComponentView _view;
        readonly Relay _fills;

        PartEntry _entry;
        PartEntry _worded;

        /// <summary>An empty row belonging to <paramref name="view" />, filled in by <see cref="Update" />.</summary>
        public PartRow(ComponentView view)
        {
            _view = view;
            _fills = new Relay(Fill);
            _blocks.Command = _fills;

            _name.Wraps = false;
            _named.Children.Add(_name);
            _named.Children.Add(_pencil);

            _words.Children.Add(_named);
            _words.Children.Add(_about);

            var names = new Sized(NameWidth);
            names.Content = _words;

            _slot.Children.Add(_blocks);

            _line.Left = names;
            _line.Fill = _slot;
            _line.Spacing = Space.PadX;
            _line.Padding = new Thickness(Space.PadX, Space.Row);
        }

        public Element Root
        {
            get { return _line; }
        }

        /// <summary>Shows <paramref name="entry" />, wording its blocks only when it is a different part.</summary>
        /// <param name="world">Where the blocks are named from.</param>
        /// <param name="entry">The part.</param>
        /// <param name="renaming">What renaming the part asks.</param>
        public void Update(GridWorld world, PartEntry entry, string renaming)
        {
            _name.Text = entry.Name;
            _about.Text = entry.About;
            _about.IsVisible = entry.About.Length != 0;

            _pencil.IsVisible = entry.RenameCommand != 0;
            _pencil.Press = Deed.Asks(entry.RenameCommand, renaming);

            _entry = entry;
            _fills.CanExecute = entry.Command != 0;
            if (ReferenceEquals(_worded, entry))
                return;

            // An empty part says so in the color an unfilled slot of a sentence does, because it is
            // the same news: this logic system has nothing there yet, and will act on nothing.
            _worded = entry;
            _pieces.Clear();
            SentenceText.Entities(world, entry.Blocks, true, _pieces);
            _blocks.Say(_pieces);
            _blocks.IsEmpty = entry.Blocks.Count == 0;
        }

        void Fill()
        {
            var host = _view.Host;
            if (_entry != null && host != null)
                host.Send(_blocks, _entry.Command, null);
        }
    }
}
