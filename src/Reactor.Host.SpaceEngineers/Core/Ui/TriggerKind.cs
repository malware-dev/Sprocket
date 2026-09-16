namespace Reactor.Ui
{
    /// <summary>What sort of moment starts a sequence.</summary>
    public enum TriggerKind
    {
        /// <summary>Something happened to something.</summary>
        When,

        /// <summary>A length of time went by.</summary>
        Every,

        /// <summary>Something on the construct says so: a button, a timer, a sensor.</summary>
        Required
    }
}
