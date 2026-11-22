using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;

namespace Sprocket.Host.SpaceEngineers.Features.Expressions
{
    /// <summary>The box of the expression builder each of the language's calculations is shown as.</summary>
    public static class JoinKinds
    {
        /// <summary>The box the operator <paramref name="op" /> is shown as.</summary>
        public static JoinKind Of(Operator op)
        {
            switch (op)
            {
                case Operator.Add: return JoinKind.Add;
                case Operator.Subtract: return JoinKind.Subtract;
                case Operator.Multiply: return JoinKind.Multiply;
                default: return JoinKind.Divide;
            }
        }

        /// <summary>The box the function <paramref name="function" /> is shown as.</summary>
        public static JoinKind Of(FunctionKind function)
        {
            switch (function)
            {
                case FunctionKind.Min: return JoinKind.Lowest;
                case FunctionKind.Max: return JoinKind.Highest;
                case FunctionKind.Round: return JoinKind.Rounded;
                default: return JoinKind.WithoutSign;
            }
        }
    }
}
