namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// Something a sequence can tell an entity to do.
    /// </summary>
    /// <remarks>
    /// Actions carry no parameters. That is a scope decision, not an oversight:
    /// parameters bring value pickers, types and unit formatting with them, and none
    /// of that is needed to find out whether people can build a working airlock.
    /// </remarks>
    public sealed class EntityAction
    {
        public EntityAction(string id, string text, string leadsToStateId, string gameActionId)
        {
            Id = id;
            Text = text;
            LeadsToStateId = leadsToStateId;
            GameActionId = gameActionId;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// How it reads in a sentence, e.g. "open". Phrased as an instruction so it
        /// completes "___ the hangar door".
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The <see cref="EntityState"/> this action is trying to bring about, or
        /// null if it does not settle into a state.
        /// </summary>
        /// <remarks>
        /// This is what makes interlocks possible. Knowing that "open" is an attempt
        /// to reach "is open" lets the runtime refuse the action while a promise says
        /// that state must not hold - and, far more importantly, say so afterwards.
        /// An action that declares nothing here simply cannot be interlocked.
        /// </remarks>
        public string LeadsToStateId { get; private set; }

        /// <summary>
        /// The game's own name for this act, or null where the game has no single action for it.
        /// </summary>
        /// <remarks>
        /// What lets a trigger that does one plain thing be written into a toolbar slot as the game's
        /// action rather than as Reactor's, so the button keeps working with the mod removed. The
        /// game's convention is that an on/off switch called <c>X</c> offers <c>X_On</c> and
        /// <c>X_Off</c>; a door's switch is <c>Open</c>, a vent's is <c>Depressurize</c>, and every
        /// functional block has <c>OnOff</c>.
        /// <para>
        /// Null for anything Reactor performs out of several property writes, and for any block whose
        /// own actions have not been mapped yet. A name is never trusted: the block is asked for it
        /// before a slot is written, so a wrong or missing one costs a Reactor trigger rather than a
        /// dead button.
        /// </para>
        /// </remarks>
        public string GameActionId { get; private set; }
    }
}
