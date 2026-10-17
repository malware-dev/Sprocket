namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Why a member cannot be made into a step, for the person looking at it.</summary>
    public enum UnreadyReason
    {
        /// <summary>Nothing holds it back.</summary>
        None,

        /// <summary>The game keeps this block's events from mods.</summary>
        Events,

        /// <summary>It chooses a camera, which is planned.</summary>
        Camera,

        /// <summary>It chooses a rotor, which is planned.</summary>
        Rotor,

        /// <summary>It chooses ammunition, which is planned.</summary>
        Ammo,

        /// <summary>Its values cannot be read yet.</summary>
        Unread
    }
}
