using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model.Expressions
{
    /// <summary>What an expression is worked out against: the installation's settings and states, and the blocks it reads.</summary>
    public interface IExpressionScope
    {
        /// <summary>The value the setting <paramref name="name" /> has, and whether it has one.</summary>
        bool Setting(string name, out Literal value);

        /// <summary>The value the state <paramref name="name" /> holds, and whether there is such a state.</summary>
        bool State(string name, out Literal value);

        /// <summary>An empty list to work values in, taken from a pool so that working out numbers allocates nothing; given back by <see cref="Return" />.</summary>
        List<Literal> Borrow();

        /// <summary>Gives back a list <see cref="Borrow" /> lent.</summary>
        void Return(List<Literal> list);

        /// <summary>Appends to <paramref name="into" /> the value <paramref name="reading" /> reads on each block it names, given <paramref name="arguments" />.</summary>
        void Read(BlockReading reading, IList<Literal> arguments, List<Literal> into);
    }
}
