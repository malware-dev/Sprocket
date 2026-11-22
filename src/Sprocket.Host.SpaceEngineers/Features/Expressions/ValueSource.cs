namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>A kind of value dragged from the expression builder's tray, chosen once it is dropped.</summary>
    public enum ValueSource
    {
        /// <summary>No kind of value: something else is dragged.</summary>
        None,

        /// <summary>A value read from blocks, chosen in the selector.</summary>
        Reading,

        /// <summary>One of the logic system's settings.</summary>
        Setting,

        /// <summary>One of the logic system's states.</summary>
        State,

        /// <summary>A fixed value typed in.</summary>
        Typed,
    }
}
