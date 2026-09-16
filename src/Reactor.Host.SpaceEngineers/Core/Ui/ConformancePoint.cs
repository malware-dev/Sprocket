namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>Something every target has to answer for.</summary>
    public sealed class ConformancePoint
    {
        public ConformancePoint(string id, string must, string why)
        {
            Id = id;
            Must = must;
            Why = why;
        }

        /// <summary>Stable name, used by a host to say it has answered this one.</summary>
        public string Id { get; private set; }

        /// <summary>What a target has to provide.</summary>
        public string Must { get; private set; }

        /// <summary>What goes wrong if it does not.</summary>
        public string Why { get; private set; }
    }
}
