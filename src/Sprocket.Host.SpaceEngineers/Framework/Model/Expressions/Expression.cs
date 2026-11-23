using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>
    /// A value a step works out when it is reached: operands joined by operators and functions.
    /// </summary>
    /// <remarks>Immutable but for the lists of names its readings hold, which edits to a slot's name reach.</remarks>
    public abstract class Expression
    {
        /// <summary>How tightly an atom binds, which nothing needs parentheses around.</summary>
        public const int AtomPrecedence = 7;

        string _written;

        /// <summary>How tightly this binds its operands: an atom 7, a negation or a not 6, a product or quotient 5, a sum or difference 4, a comparison 3, an and 2, an or 1.</summary>
        public abstract int Precedence { get; }

        /// <summary>This as a program writes it, with only the parentheses its precedence needs.</summary>
        public string Written => _written ?? (_written = ExpressionWriter.Of(this));

        /// <summary>Works this out against <paramref name="scope" /> into <paramref name="value" />, or says why it has no value.</summary>
        /// <remarks>Allocates only where text is joined or a reading takes its blocks' values; asked as a step is reached, never on a tick.</remarks>
        public abstract ExpressionFailure Evaluate(IExpressionScope scope, out Literal value);

        /// <summary>
        /// The kind of value this works out to, into <paramref name="kind" />, null where an operand's kind is not known yet; false
        /// where an operator is given operands it does not take.
        /// </summary>
        public abstract bool TypeOf(ITypeScope scope, out ValueKind? kind);

        /// <summary>Adds to <paramref name="into" /> every list of names this reads blocks by, the lists themselves.</summary>
        public abstract void NameLists(List<IList<string>> into);

        /// <summary>Adds to <paramref name="into" /> every setting this names, each once.</summary>
        public abstract void Settings(List<string> into);

        /// <summary>This with the setting <paramref name="was" /> named <paramref name="now" />, or the state called so where <paramref name="state" /> says it is one.</summary>
        public abstract Expression Renamed(string was, string now, bool state);

        /// <summary>This again with lists of names of its own, so editing the copy cannot reach it.</summary>
        public abstract Expression Copied();

        /// <summary>Calls <paramref name="visit" /> on this and each expression it holds.</summary>
        public abstract void Walk(Action<Expression> visit);

        /// <summary>Whether <paramref name="kind" /> is a number: an integer or a real.</summary>
        public static bool IsNumber(ValueKind kind)
        {
            return kind == ValueKind.Integer || kind == ValueKind.Real;
        }
    }
}
