
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A moment Sprocket watches for itself: "when any of these doors finishes opening".
    /// </summary>
    /// <remarks>
    /// For the moments the game announces to nobody. Sprocket reads the blocks every tick and fires
    /// on the edge, which is the expensive way and the only way there is - so it is what a moment
    /// gets when the construct cannot raise it. See <see cref="RequiredTrigger" /> for the rest.
    /// </remarks>
    public sealed class WhenTrigger : MomentTrigger
    {
        public WhenTrigger(string eventId) : base(eventId)
        {
        }

        /// <summary>
        /// Whether every source has to be in the event's state, rather than any one
        /// of them reaching it.
        /// </summary>
        /// <remarks>
        /// "Any" is the default because it is what people mean most of the time - any
        /// of these doors opening should turn the lights on. "All" is a genuinely
        /// different sentence: "when tank A <em>and</em> tank B are empty" fires at
        /// the moment the last of them empties, not when the first one does.
        ///
        /// So this is an edge on the whole condition rather than on any one signal,
        /// which is why the runtime tracks it separately instead of just requiring
        /// several edges at once - they would almost never land on the same tick.
        /// </remarks>
        public bool RequiresAll { get; set; }

        /// <summary>How often the moment is looked for, in ticks, or 0 where the program leaves it to the default.</summary>
        public int IntervalTicks { get; set; }
    }
}
