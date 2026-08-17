namespace Reactor.World
{
    /// <summary>
    /// Something a sequence can tell a device to do.
    /// </summary>
    /// <remarks>
    /// Actions carry no parameters. That is a scope decision, not an oversight:
    /// parameters bring value pickers, types and unit formatting with them, and none
    /// of that is needed to find out whether people can build a working airlock.
    /// </remarks>
    public sealed class DeviceAction
    {
        public DeviceAction(string id, string text, string leadsToStateId)
        {
            Id = id;
            Text = text;
            LeadsToStateId = leadsToStateId;
        }

        /// <summary>Stable identifier, referenced by saved sequences.</summary>
        public string Id { get; private set; }

        /// <summary>
        /// How it reads in a sentence, e.g. "open". Phrased as an instruction so it
        /// completes "___ the hangar door".
        /// </summary>
        public string Text { get; private set; }

        /// <summary>
        /// The <see cref="DeviceState"/> this action is trying to bring about, or
        /// null if it does not settle into a state.
        /// </summary>
        /// <remarks>
        /// This is what makes interlocks possible. Knowing that "open" is an attempt
        /// to reach "is open" lets the runtime refuse the action while a promise says
        /// that state must not hold - and, far more importantly, say so afterwards.
        /// An action that declares nothing here simply cannot be interlocked.
        /// </remarks>
        public string LeadsToStateId { get; private set; }
    }
}
