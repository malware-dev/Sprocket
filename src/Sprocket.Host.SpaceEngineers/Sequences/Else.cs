using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>
    /// The next branch of an <see cref="If" />: one that tests again, an <c>else if</c>, or one that runs where nothing held,
    /// an <c>else</c>.
    /// </summary>
    /// <remarks>Reached from the branch before it, it ends that branch, and the sequence goes on after the whole <c>if</c>.</remarks>
    public sealed class Else : Step
    {
        /// <summary>An <c>else if</c> testing <paramref name="test" />, or an <c>else</c> where it is null.</summary>
        public Else(Comparison test)
        {
            Test = test;
        }

        /// <summary>What it tests, or null for an <c>else</c>, which runs where nothing before it held.</summary>
        public Comparison Test { get; set; }

        /// <inheritdoc />
        public override IList<string> Targets => Test == null ? base.Targets : Test.Targets;
    }
}
