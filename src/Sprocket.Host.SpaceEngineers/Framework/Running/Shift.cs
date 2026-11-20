using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// One thing on the world's rota: where it sits in the turn, and what is done when it comes round.
    /// </summary>
    /// <remarks>Made once and moved between the rota's lists, so being looked at allocates nothing.</remarks>
    public sealed class Shift
    {
        /// <summary>A shift at <paramref name="place" /> in the turn, doing <paramref name="due" /> when it comes round, remembering nothing.</summary>
        public Shift(int place, Action<int> due)
        {
            Place = place;
            Due = due;
            Ran = int.MinValue;
        }

        /// <summary>A shift whose place and standing on the rota are stored in <paramref name="stored" />, doing <paramref name="due" /> when it comes round.</summary>
        public Shift(ShiftMemory stored, Action<int> due) : this(stored.Place, due)
        {
            Stored = stored;
        }

        /// <summary>Where this stands on the rota, stored for the world's save as the rota moves it; null for a shift nothing remembers.</summary>
        public ShiftMemory Stored { get; private set; }

        /// <summary>Stops storing where this stands, so what it stored stays as it was whatever the rota does to it after.</summary>
        public void Forget()
        {
            Stored = null;
        }

        /// <summary>Where this sits in the turn, as <see cref="Rota.Join" /> takes it.</summary>
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
