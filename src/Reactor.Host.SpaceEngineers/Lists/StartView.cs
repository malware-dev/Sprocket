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

        /// <summary>
        /// Told rather than asked. A page writes these once, when it builds the block, and a screen
        /// that offers different ways to start something is a different screen with a new component.
        /// </summary>
        protected override bool Stale
        {
            get { return false; }
        }

        public override Element Root
        {
            get { return _stack; }
        }

        protected override void Refresh()
        {
            Rebuild();

            // The row is what the question hangs under, which is why nothing here has to be told
            // where to put one.
            for (int i = 0; i < _rows.Count && i < _component.Options.Count; i++)
            {
                StartOption option = _component.Options[i];
                _rows[i].Press = Deed.Asks(option.Command, option.Question);
            }
        }

        void Rebuild()
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
                _rows.Add(Widgets.Way(Plus + option.Label + (option.Asks ? "…" : "")));
            }

            // Closer to the buttons than to what is under it, because the line is what ends them.
            // Even air on both sides put the line further below the buttons than the first row was
            // below the line, which reads as the buttons sitting in a box of their own.
            _line.Padding = new Thickness(0f, Space.Line, 0f, Space.Item);

            _buttons.Children.Clear();
            for (int i = 0; i < _rows.Count; i++)
                _buttons.Children.Add(_rows[i]);

            _stack.Children.Clear();
            _stack.Children.Add(_buttons);
            _stack.Children.Add(_line);
        }
    }
}
