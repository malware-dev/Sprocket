using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Sequences
{
    /// <summary>A line that opens a branch of an if: the <see cref="If" /> itself, or an <see cref="Else" /> after it.</summary>
    /// <remarks>
    /// The branch is the lines after it, up to the next branch or the <see cref="EndIf" />; see <see cref="Sequence.BranchAfter" />.
    /// </remarks>
    public abstract class Branch : Step
    {
        /// <summary>A branch testing <paramref name="test" />, or testing nothing where it is null.</summary>
        protected Branch(Comparison test)
        {
            Test = test;
        }

        /// <summary>What it tests, or null for an <c>else</c>, which runs where nothing before it held.</summary>
        public Comparison Test { get; set; }

        /// <inheritdoc />
        public override IList<string> Targets => Test == null ? base.Targets : Test.Targets;
    }
}
