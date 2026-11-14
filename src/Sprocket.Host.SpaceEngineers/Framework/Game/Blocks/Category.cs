
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>
    /// What a member of a block is about, which is how the organized view groups them.
    /// </summary>
    /// <remarks>The order they are declared in is the order they are listed in.</remarks>
    public enum Category
    {
        /// <summary>Running or not, and what it costs to run.</summary>
        Power,

        /// <summary>Going somewhere: turning, extending, locking, and how fast.</summary>
        Movement,

        /// <summary>Letting something through: doors, hatches and the like.</summary>
        Opening,

        /// <summary>Holding fast to something else: a landing gear, a connector.</summary>
        Locking,

        /// <summary>Flying the construct, and what helps it fly.</summary>
        Flight,

        /// <summary>What it holds and what it does with it.</summary>
        Stock,

        /// <summary>The air in a room: filling it, emptying it, and how much there is.</summary>
        Air,

        /// <summary>The air kept for whoever sits or sleeps in it.</summary>
        LifeSupport,

        /// <summary>Gas held or made: a tank's store, a generator's ice, a farm's output.</summary>
        Gas,

        /// <summary>What a block burns to run, and how much it has left.</summary>
        Fuel,

        /// <summary>Making things.</summary>
        Production,

        /// <summary>How whole it is, and the parts it is built from.</summary>
        Repair,

        /// <summary>Shooting.</summary>
        Weapons,

        /// <summary>Seeing what is out there.</summary>
        Detection,

        /// <summary>Setting something else going: a timer, a script, a watcher.</summary>
        Automation,

        /// <summary>Talking to other constructs and to people.</summary>
        Signals,

        /// <summary>What it looks like: color, brightness, how it is drawn.</summary>
        Appearance,

        /// <summary>What it writes on itself.</summary>
        Screen,

        /// <summary>What it makes a noise about.</summary>
        Sound,

        /// <summary>What it is called.</summary>
        Identity,

        /// <summary>How it and its construct stand, counted and weighed: what is fitted to it, how heavy the construct is.</summary>
        State,

        /// <summary>
        /// How the machine is set up to behave: what it does when nobody tells it, and what it was
        /// tuned to when it was built.
        /// </summary>
        Behaviour,

        /// <summary>Who is using it now.</summary>
        Usage,

        /// <summary>Who may use it.</summary>
        Access,

        /// <summary>What belongs under no other heading, and anything nobody has placed.</summary>
        Other
    }
}
