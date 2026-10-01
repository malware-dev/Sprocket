namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>What a block is doing, as shown beside it in a list of blocks.</summary>
    public enum Condition
    {
        /// <summary>Nothing to say.</summary>
        None,

        /// <summary>Damaged past working.</summary>
        Damaged,

        /// <summary>Switched off.</summary>
        Off,

        /// <summary>Switched on and working.</summary>
        On,

        /// <summary>Switched on with no power.</summary>
        NoPower,

        /// <summary>A door that is open.</summary>
        Open,

        /// <summary>A door that is shut.</summary>
        Shut,

        /// <summary>A door on its way open.</summary>
        Opening,

        /// <summary>A door on its way shut.</summary>
        Closing,

        /// <summary>An air vent's room full of air.</summary>
        Pressurized,

        /// <summary>An air vent's room, a battery or a tank with nothing in it.</summary>
        Empty,

        /// <summary>An air vent or a tank filling.</summary>
        Filling,

        /// <summary>An air vent emptying its room.</summary>
        Emptying,

        /// <summary>A battery or tank that is full.</summary>
        Full,

        /// <summary>A battery charging.</summary>
        Charging,

        /// <summary>A piston all the way out.</summary>
        Extended,

        /// <summary>A piston all the way in.</summary>
        Retracted,

        /// <summary>A piston on its way out.</summary>
        Extending,

        /// <summary>A piston on its way in.</summary>
        Retracting,

        /// <summary>A button panel none of whose buttons does anything.</summary>
        NotWired
    }
}
