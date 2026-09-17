using System.Collections.Generic;
using Reactor.Host.SpaceEngineers.Screen;
using Reactor.Host.SpaceEngineers.Layout;
using Reactor.Host.SpaceEngineers.Controls;
using Reactor.Host.SpaceEngineers.Look;

namespace Reactor.Host.SpaceEngineers.Lists
{
    /// <summary>
    /// The ways to start something new.
    /// </summary>
    /// <remarks>
    /// Everything in here adds something, so everything in here gets a plus. Core does not spell that
    /// into the labels - it says these are the ways to start something, which is already the whole of the
    /// claim - and drawing it as a plus is this target agreeing with the other one.
    /// <para>
    /// A line closes the block off, because what follows is a different kind of thing: above is what can
    /// be added, below is what is already there.
    /// </para>
    /// </remarks>
    public sealed class StartView : ComponentView
    {
        public const string Plus = "+ ";

        readonly StartComponent _component;
        readonly Stack _stack = Widgets.Column(Space.Row);

        /// <summary>
        /// The buttons themselves, in a row of their own.
        /// </summary>
        /// <remarks>
        /// A row rather than a column, so each one is as wide as its words: the same button that adds a
        /// step to a sentence, laid out the same way. Full-width rows read as a list of places to go,
        /// which is what the things underneath the line are.
        /// </remarks>
        readonly Stack _buttons = Widgets.RowOf(Space.Item);
        readonly List<Label> _rows = new List<Label>();
        readonly Rule _line = Widgets.Line();

        int _count = -1;

        public StartView(StartComponent component)
        {
            _component = component;
        }

        public override Element Root
        {
            get { return _stack; }
        }

        public override void Update(int tick)
        {
            if (_count == _component.Options.Count)
                return;

            _count = _component.Options.Count;
            _rows.Clear();

            for (int i = 0; i < _component.Options.Count; i++)
            {
                StartOption option = _component.Options[i];

                // An ellipsis on the ones that will ask something, which is the convention every desktop
                // already uses for "this opens a question rather than doing it".
                //
                // The same button that adds a step to a sentence, at this screen's size: adding a system
                // and adding a step are one act, and a filled panel here said they were two.
                Label row = Widgets.Ghost(Plus + option.Label + (option.Asks ? "…" : ""),
                    Lettering.Body);
                row.Highlight = Palette.Panel;
                _rows.Add(row);
            }

            _line.Padding = new Thickness(0f, Space.Item);

            _buttons.Children.Clear();
            for (int i = 0; i < _rows.Count; i++)
                _buttons.Children.Add(_rows[i]);

            _stack.Children.Clear();
            _stack.Children.Add(_buttons);
            _stack.Children.Add(_line);
        }

        public override void Collect(List<Hit> hits)
        {
            for (int i = 0; i < _rows.Count && i < _component.Options.Count; i++)
            {
                StartOption option = _component.Options[i];

                // The row is what the question hangs under, so it is handed over with the hit. Nothing
                // here opens anything: the row does not know a popup exists, only that pressing it asks.
                var ask = new Hit(_rows[i].Bounds, option.Command, option.Question, _rows[i]);
                ask.Hot = _rows[i];
                hits.Add(ask);
            }
        }
    }
}
