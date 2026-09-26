using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How an element looks: a base, and whatever states the style's own control has.</summary>
    /// <remarks>
    ///     Applying lays down the neutral values, then <see cref="Base" />, then the states the control is in, in the
    ///     order <see cref="LayStates" /> lays them, so a later layer wins. A change to a style shows on an element once
    ///     the element is restyled.
    /// </remarks>
    public class Style<TState> : IStyle where TState : ElementState, new()
    {
        readonly Style<TState> _basedOn;
        readonly List<TState> _layers = new List<TState>();

        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public Style()
        {
            Base = Layer();
        }

        /// <summary>A style that is everything <paramref name="basedOn" /> is, with whatever this sets laid over it.</summary>
        /// <remarks>
        ///     Reads through to <paramref name="basedOn" /> each time it is applied, so a change to that style shows
        ///     here too. Throws for a null base, which is what a static style declared above the one it is based on
        ///     receives.
        /// </remarks>
        public Style(Style<TState> basedOn) : this()
        {
            if (basedOn == null)
                throw new ArgumentNullException("basedOn", "A style's base must be declared before it.");

            _basedOn = basedOn;
        }

        /// <summary>How the element looks whatever state it is in.</summary>
        public TState Base { get; }

        /// <inheritdoc />
        public void Apply(Element element)
        {
            Base.Neutral(element);
            Lay(Base, element);
            LayStates(element);
        }

        /// <summary>A state layer for a style type to declare, matched to the same layer of the style this is based on.</summary>
        /// <remarks>Layers are matched to the base's by the order they are declared in.</remarks>
        protected TState Layer()
        {
            var layer = new TState();
            _layers.Add(layer);
            return layer;
        }

        /// <summary>Lays the layers for the states <paramref name="element" /> is in, in the order they should win.</summary>
        protected virtual void LayStates(Element element)
        {
        }

        /// <summary>Writes what <paramref name="layer" /> sets onto <paramref name="element" />, over what the same layer of the style this is based on sets.</summary>
        protected void Lay(TState layer, Element element)
        {
            LayAt(_layers.IndexOf(layer), element);
        }

        void LayAt(int index, Element element)
        {
            if (_basedOn != null && index < _basedOn._layers.Count)
                _basedOn.LayAt(index, element);

            _layers[index].Lay(element);
        }
    }
}
