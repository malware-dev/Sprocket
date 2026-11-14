namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>What sort of moment starts a sequence.</summary>
    public enum TriggerKind
    {
        /// <summary>Something happened to something.</summary>
        When,

        /// <summary>A length of time went by.</summary>
        Every,

        /// <summary>A signal, sent from a place the construct offers: a button, a timer, a sensor.</summary>
        Signal
    }
}
