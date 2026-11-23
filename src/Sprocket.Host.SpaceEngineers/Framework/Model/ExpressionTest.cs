using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>What an if tests where it is an expression giving a flag: <c>all gearbox.pressurized on [ vent ] == on and not cycling</c>.</summary>
    public sealed class ExpressionTest : BranchTest
    {
        /// <summary>A test of <paramref name="condition" />, which gives a flag.</summary>
        public ExpressionTest(Expression condition)
        {
            Condition = condition;
        }

        /// <summary>The expression tested, which holds where it comes to on.</summary>
        public Expression Condition { get; set; }

        /// <summary>A copy of this test, to change one thing of as an edit.</summary>
        public ExpressionTest Copy()
        {
            return new ExpressionTest(Condition.Copied());
        }
    }
}
