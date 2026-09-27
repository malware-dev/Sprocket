
namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// What a member of a block is about, which is how the organized view groups them.
    /// </summary>
    /// <remarks>
    /// One set for every block rather than one per kind, so a player who has learnt where a light's
    /// colour sits knows where a sorter's is. A block shows only the categories it has anything in.
    /// <para>
    /// The order they are declared in is the order they are listed in, and <see cref="Other" /> is
    /// last because it is where anything nobody has placed ends up.
    /// </para>
    /// </remarks>
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

        /// <summary>Air: holding it, making it, letting it out.</summary>
        Air,

        /// <summary>Making things.</summary>
        Making,

        /// <summary>Shooting.</summary>
        Weapons,

        /// <summary>Seeing what is out there.</summary>
        Detection,

        /// <summary>Setting something else going: a timer, a script, a watcher.</summary>
        Automation,

        /// <summary>Talking to other constructs and to people.</summary>
        Signals,

        /// <summary>What it looks like: colour, brightness, how it is drawn.</summary>
        Appearance,

        /// <summary>What it writes on itself.</summary>
        Screen,

        /// <summary>What it makes a noise about.</summary>
        Sound,

        /// <summary>What it is called, and what it carries written on it.</summary>
        Identity,

        /// <summary>
        /// How the machine is set up to behave: what it does when nobody tells it, and what it was
        /// tuned to when it was built.
        /// </summary>
        /// <remarks>
        /// Low in the list because none of it belongs in a sequence twice. A landing gear is told to
        /// lock, and locks by itself because somebody set autolock; a rotor is told to turn, and
        /// turns with the torque it was built with. Neither is something an automation revisits.
        /// </remarks>
        Behaviour,

        /// <summary>Who may use it.</summary>
        Access,

        /// <summary>Everything nothing has been said about.</summary>
        Other
    }
}
