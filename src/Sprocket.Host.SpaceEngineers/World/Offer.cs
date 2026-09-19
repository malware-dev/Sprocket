namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// One thing the game says a block can be told to do, in the game's own terms.
    /// </summary>
    /// <remarks>
    /// What a block declares for itself, before Sprocket has any opinion about it. An
    /// <see cref="EntityAction" /> is what Sprocket makes of one: the same id, and words chosen for a
    /// sentence where there are better ones to be had.
    /// </remarks>
    public struct Offer
    {
        public Offer(string id, string text) : this(id, text, 0)
        {
        }

        public Offer(string id, string text, int wants)
        {
            Id = id;
            Text = text;
            Wants = wants;
        }

        /// <summary>The game's own name for it, which is what a slot and a saved program hold.</summary>
        public string Id { get; private set; }

        /// <summary>What the terminal calls it, which is what the player has already read.</summary>
        public string Text { get; private set; }

        /// <summary>
        /// How many values it has to be given, which is nearly always none.
        /// </summary>
        /// <remarks>
        /// A step cannot carry a value yet, so this is how an action that needs one is told apart
        /// from one that does not - and left out rather than offered as something that would do
        /// nothing when it ran. See issue 7.
        /// <para>
        /// The game declares one default per parameter, so the count is the arity and the type is
        /// the runtime type of each default. Nothing declares what a value may be <em>one of</em>;
        /// a set of permitted values exists on a terminal control rather than on an action.
        /// </para>
        /// </remarks>
        public int Wants { get; private set; }
    }
}
