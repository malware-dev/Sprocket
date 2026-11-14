using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A line that opens a branch of an if: the <see cref="If" /> itself, or an <see cref="Else" /> after it.</summary>
    public abstract class Branch : Step
    {
        /// <summary>A branch testing <paramref name="test" />, or testing nothing where it is null.</summary>
        protected Branch(BranchTest test)
        {
            Test = test;
        }

        /// <summary>What it tests, or null for an <c>else</c>, which runs where nothing before it held.</summary>
        public BranchTest Test { get; set; }

        /// <summary>The comparison of a member read on blocks it tests, or null where it tests a state or nothing.</summary>
        public Comparison Compared => Test as Comparison;

        /// <inheritdoc />
        public override IList<string> Targets => Test == null ? base.Targets : Test.Targets;

        /// <inheritdoc />
        public override IList<Literal> Arguments => Compared == null ? base.Arguments : Compared.Arguments;
    }
}
