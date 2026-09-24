using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Ui.Styling
{
    /// <summary>How an element looks: a base, and whatever states the style's own control has.</summary>
    /// <remarks>
    ///     Applying lays down the neutral values, then <see cref="Base" />, then the states the control is in, in the
    ///     order <see cref="LayStates" /> lays them, so a later layer wins.
    /// </remarks>
    public class Style<TState> : IStyle where TState : ElementState, new()
    {
        readonly Style<TState> _basedOn;
        readonly List<TState> _layers = new List<TState>();

        /// <summary>A style with nothing set, which applies as the neutral values.</summary>
        public Style()
        {
            Base = Held();
        }

        /// <summary>A style starting from a copy of everything <paramref name="basedOn" /> sets.</summary>
        /// <remarks>Throws for a null base, which is what a static style declared above the one it is based on receives.</remarks>
        public Style(Style<TState> basedOn) : this()
        {
            if (basedOn == null)
                throw new ArgumentNullException("basedOn", "A style's base must be declared before it.");

            _basedOn = basedOn;
            Base.Copy(basedOn.Base);
        }

        /// <summary>How the element looks whatever state it is in.</summary>
        public TState Base { get; }

        /// <inheritdoc />
        public bool IsFrozen { get; private set; }

        /// <inheritdoc />
        public void Freeze()
        {
            IsFrozen = true;
        }

        /// <inheritdoc />
        public void Apply(Element element)
        {
            Base.Neutral(element);
            Base.Lay(element);
            LayStates(element);
        }

        /// <summary>A state layer for a style type to declare, copied from the same layer of the style this is based on.</summary>
        /// <remarks>Layers are matched to the base's by the order they are declared in.</remarks>
        protected TState Layer()
        {
            var layer = Held();

            if (_basedOn != null && _layers.Count < _basedOn._layers.Count)
                layer.Copy(_basedOn._layers[_layers.Count]);

            _layers.Add(layer);
            return layer;
        }

        /// <summary>Lays the layers for the states <paramref name="element" /> is in, in the order they should win.</summary>
        protected virtual void LayStates(Element element)
        {
        }

        /// <summary>Writes what <paramref name="layer" /> sets onto <paramref name="element" />, and nothing else.</summary>
        protected static void Lay(TState layer, Element element)
        {
            layer.Lay(element);
        }

        TState Held()
        {
            var state = new TState();
            state.Owner = this;
            return state;
        }
    }
}
