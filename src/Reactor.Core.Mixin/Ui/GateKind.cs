namespace Reactor.Ui
{
    /// <summary>Why a step is held back.</summary>
    public enum GateKind
    {
        /// <summary>Waits for a situation to become true, then goes and never waits again.</summary>
        Once,

        /// <summary>Runs only while a situation is true, and is skipped when it is not.</summary>
        OnlyIf,

        /// <summary>Waits a set length of time.</summary>
        After
    }
}
