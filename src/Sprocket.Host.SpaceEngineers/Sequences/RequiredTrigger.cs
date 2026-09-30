
namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// A moment the construct announces, which Sprocket is started by rather than watching for.
    /// </summary>
    /// <remarks>
    /// A button, a timer, a sensor, an event controller: the game already starts things from these,
    /// and it starts them through a toolbar slot. So Sprocket puts its own action in the slot and
    /// waits to be called, which is the game's own arrangement and not a second one beside it.
    /// <para>
    /// <b>Nothing is sampled.</b> A button raises no property a rule could read, and a Sprocket that
    /// polled one would be inventing a signal the game does not have - and firing twice on the days
    /// it guessed right.
    /// </para>
    /// <para>
    /// With no sources it says only that something on the construct has to start this, and which is
    /// then whatever somebody puts Sprocket's action into. Written that way by hand, or left over
    /// from a system somebody sent; the editor always says which block it meant.
    /// </para>
    /// </remarks>
    public sealed class RequiredTrigger : MomentTrigger
    {
        public RequiredTrigger() : base("")
        {
        }

        public RequiredTrigger(string eventId) : base(eventId)
        {
        }

        /// <summary>The trigger that is the trigger part <paramref name="part" />, answered in each instance by its toolbar slots.</summary>
        public static RequiredTrigger Named(string part)
        {
            var trigger = new RequiredTrigger();
            trigger.Sources.Add(part);
            return trigger;
        }

        /// <summary>The trigger part this is, or empty where it names none.</summary>
        public string Part => Sources.Count > 0 ? Sources[0] : "";
    }
}
