namespace Sprocket.Host.SpaceEngineers.Layout
{
    /// <summary>What pressing something means.</summary>
    /// <remarks>
    /// One field saying which of the few things it is, rather than a command, two flags and three
    /// references to be read in the right order. A press does exactly one of these, and the screen's
    /// press path is a choice between them.
    /// </remarks>
    public enum Means
    {
        /// <summary>Nothing at all. Not a control, whatever it looks like.</summary>
        Nothing,

        /// <summary>Sends its command, which is what most controls do.</summary>
        Send,

        /// <summary>Puts a switch into the state it carries, rather than inverting whatever is there.</summary>
        Switch,

        /// <summary>Raises a question, and sends the answer to its command.</summary>
        Ask,

        /// <summary>Puts the keyboard in its box, and sends what is typed to its command.</summary>
        Type,

        /// <summary>Hangs something over the screen, under itself.</summary>
        Open,

        /// <summary>Sends the answer standing in the question already open.</summary>
        Commit,

        /// <summary>
        /// Abandons the question standing over the screen, answering nothing.
        /// </summary>
        /// <remarks>
        /// The other half of <see cref="Commit" />. A question owns the screen while it is up, so
        /// the only controls that can reach anything are the two that end it, and both have to say
        /// which they are - a control inside a question meaning anything else is one whose press
        /// goes nowhere at all.
        /// </remarks>
        Cancel,

        /// <summary>
        /// Asks to leave the editor rather than to do anything inside it.
        /// </summary>
        /// <remarks>
        /// Its own act and not a command, because there is no command: core has no idea it is being
        /// looked at, and closing a window is not a thing it could be asked to do.
        /// </remarks>
        Leave
    }
}
