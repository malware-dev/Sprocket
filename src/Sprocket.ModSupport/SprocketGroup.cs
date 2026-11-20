namespace Sprocket.ModSupport
{
    /// <summary>The groups of Sprocket's own a mod's members may stand in, beside the game's members of the same sort.</summary>
    /// <remarks>The ids are sent to Sprocket and never change; a group Sprocket does not know puts the member under Other.</remarks>
    public struct SprocketGroup
    {
        /// <summary>The group Sprocket knows by <paramref name="id" />.</summary>
        SprocketGroup(string id)
        {
            Id = id;
        }

        /// <summary>The id Sprocket knows the group by.</summary>
        public string Id { get; }

        /// <summary>Making, storing and using power.</summary>
        public static readonly SprocketGroup Power = new SprocketGroup("power");

        /// <summary>Moving parts: pistons, rotors, hinges.</summary>
        public static readonly SprocketGroup Movement = new SprocketGroup("movement");

        /// <summary>Opening and closing: doors, hatches.</summary>
        public static readonly SprocketGroup Opening = new SprocketGroup("opening");

        /// <summary>Locking on: connectors, landing gear, merge blocks.</summary>
        public static readonly SprocketGroup Locking = new SprocketGroup("locking");

        /// <summary>Flying: thrusters, gyroscopes, dampeners.</summary>
        public static readonly SprocketGroup Flight = new SprocketGroup("flight");

        /// <summary>Items held and moved.</summary>
        public static readonly SprocketGroup Stock = new SprocketGroup("stock");

        /// <summary>Air in rooms.</summary>
        public static readonly SprocketGroup Air = new SprocketGroup("air");

        /// <summary>Keeping people alive.</summary>
        public static readonly SprocketGroup LifeSupport = new SprocketGroup("life_support");

        /// <summary>Gases held and moved.</summary>
        public static readonly SprocketGroup Gas = new SprocketGroup("gas");

        /// <summary>Fuel.</summary>
        public static readonly SprocketGroup Fuel = new SprocketGroup("fuel");

        /// <summary>Making and refining.</summary>
        public static readonly SprocketGroup Production = new SprocketGroup("production");

        /// <summary>Building and repairing.</summary>
        public static readonly SprocketGroup Repair = new SprocketGroup("repair");

        /// <summary>Weapons.</summary>
        public static readonly SprocketGroup Weapons = new SprocketGroup("weapons");

        /// <summary>Sensing and detecting.</summary>
        public static readonly SprocketGroup Detection = new SprocketGroup("detection");

        /// <summary>Automation and timing.</summary>
        public static readonly SprocketGroup Automation = new SprocketGroup("automation");

        /// <summary>Signals.</summary>
        public static readonly SprocketGroup Signals = new SprocketGroup("signals");

        /// <summary>How a block looks: lights, colors.</summary>
        public static readonly SprocketGroup Appearance = new SprocketGroup("appearance");

        /// <summary>Screens.</summary>
        public static readonly SprocketGroup Screen = new SprocketGroup("screen");

        /// <summary>Sound.</summary>
        public static readonly SprocketGroup Sound = new SprocketGroup("sound");

        /// <summary>Names and identity.</summary>
        public static readonly SprocketGroup Identity = new SprocketGroup("identity");

        /// <summary>A block's state.</summary>
        public static readonly SprocketGroup State = new SprocketGroup("state");

        /// <summary>How a block behaves.</summary>
        public static readonly SprocketGroup Behavior = new SprocketGroup("behavior");

        /// <summary>Using a block.</summary>
        public static readonly SprocketGroup Usage = new SprocketGroup("usage");

        /// <summary>Who may use a block.</summary>
        public static readonly SprocketGroup Access = new SprocketGroup("access");

        /// <summary>Everything else.</summary>
        public static readonly SprocketGroup Other = new SprocketGroup("other");
    }
}
