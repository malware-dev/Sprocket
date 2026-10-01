using System;
using Sprocket.Host.SpaceEngineers.Localization;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>
    /// A named repeat rate, so the user dials a rhythm instead of typing a number.
    /// </summary>
    public sealed class Cadence
    {
        readonly Func<Texts, string> _name;
        readonly Func<Texts, string> _warning;

        /// <summary>A rate of <paramref name="ticks" />, called what <paramref name="name" /> reads, with what it costs where <paramref name="warning" /> reads it.</summary>
        public Cadence(Func<Texts, string> name, int ticks, Func<Texts, string> warning = null)
        {
            _name = name;
            _warning = warning;
            Ticks = ticks;
        }

        public int Ticks { get; private set; }

        /// <summary>What it is called, in <paramref name="texts" />.</summary>
        public string Name(Texts texts)
        {
            return _name(texts);
        }

        /// <summary>
        /// What it costs to pick this, in plain words, or empty.
        /// </summary>
        /// <remarks>
        /// The fast rates are genuinely expensive, and the user has no way to know
        /// that. Finding out by watching the server stutter is not a fair way to
        /// learn it.
        /// </remarks>
        public string Warning(Texts texts)
        {
            return _warning == null ? "" : _warning(texts);
        }
    }
}
