namespace Sprocket.Host.SpaceEngineers.Shares
{
    /// <summary>What a client asks of the server about shared blueprints.</summary>
    public enum ShareAct
    {
        /// <summary>Tells what reaches this player.</summary>
        Ask,

        /// <summary>Tells what reaches this player, who has opened the library, and counts it as seen.</summary>
        Look,

        /// <summary>Shares a blueprint, or changes a share.</summary>
        Share,

        /// <summary>Takes back a share: this player's own, or anyone's for an admin.</summary>
        Withdraw,

        /// <summary>Hides what a sharer shares from this player.</summary>
        Block,

        /// <summary>Shows this player what a sharer shares again.</summary>
        Unblock,

        /// <summary>Stops a player sharing, for an admin.</summary>
        Bar,

        /// <summary>Lets a player share again, for an admin.</summary>
        Unbar
    }
}
