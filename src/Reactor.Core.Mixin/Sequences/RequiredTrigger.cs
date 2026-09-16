namespace Reactor.Sequences
{
    /// <summary>
    /// A trigger the construct has to provide: something on the ship starts the sequence.
    /// </summary>
    /// <remarks>
    /// A button, a timer, a sensor, an event controller - all of them are toolbar slots, so the system
    /// says only that it needs one and the construct's toolbars say which. Reactor never fires this
    /// itself; it is fired by whatever answers it.
    /// <para>
    /// Which slot answers which declaration is not recorded anywhere. Two of these running one
    /// sequence need two slots running it, and counting is how a missing one is found.
    /// </para>
    /// </remarks>
    public sealed class RequiredTrigger : Trigger
    {
    }
}
