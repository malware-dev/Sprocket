
namespace Reactor.Host.SpaceEngineers.Core.Ui
{
    /// <summary>One way of starting something.</summary>
    public sealed class StartOption
    {
        public StartOption(string label, int command)
        {
            Label = label;
            Question = "";
            Command = command;
        }

        public string Label { get; set; }

        /// <summary>
        /// What to ask first, or empty when there is nothing to ask.
        /// </summary>
        public string Question { get; set; }

        public bool Asks => !string.IsNullOrEmpty(Question);

        /// <summary>
        /// Where the answer goes, or what to press when there is no question.
        /// </summary>
        /// <remarks>
        /// An empty answer is ignored rather than refused, so a target that lets somebody
        /// confirm nothing does no harm - though it should not offer that in the first place.
        /// </remarks>
        public int Command { get; set; }
    }
}
