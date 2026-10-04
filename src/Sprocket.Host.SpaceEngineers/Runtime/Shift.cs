using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// One thing on the world's rota: where it sits in the turn, and what is done when it comes round.
    /// </summary>
    /// <remarks>
    /// Made once by whatever owns it and moved between the rota's lists as often as its pace changes,
    /// so being looked at costs no allocation however long a world runs.
    /// </remarks>
    public sealed class Shift
    {
        public Shift(int place, Action<int> due)
        {
            Place = place;
            Due = due;
            Ran = int.MinValue;
        }

        /// <summary>Where this sits in the turn. See <see cref="Rota.Join" />.</summary>
        public int Place { get; private set; }

        /// <summary>What is done when this comes round, told the tick it is being done on.</summary>
        public Action<int> Due { get; private set; }

        /// <summary>Whether this is on the rota at all.</summary>
        public bool Listed => List != null;

        /// <summary>The list this sits in, or null while it is off the rota.</summary>
        public List<Shift> List;

        /// <summary>Where in <see cref="List" /> it sits, so it can be taken out without a search.</summary>
        public int Index;

        /// <summary>Whether this is done once and then taken off, rather than every time it comes round.</summary>
        public bool Once;

        /// <summary>The tick a shift done once falls due on.</summary>
        public int At;

        /// <summary>The tick this was last done on, so nothing moved during a tick is done twice in it.</summary>
        public int Ran;
    }
}
