namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>One of a block's own values made ready to be read, found once and read as often as a test asks.</summary>
    /// <remarks>
    /// Made when a program is bound, as an <see cref="Order" /> is, so a look reads the block and looks nothing up by name.
    /// </remarks>
    public abstract class Gauge
    {
        /// <summary>The kind of value the block holds.</summary>
        public abstract ValueKind Kind { get; }

        /// <summary>What the block holds now.</summary>
        public abstract Literal Read();

        /// <summary>Whether what the block holds now stands in <paramref name="relation" /> to <paramref name="operand" />, a value of <see cref="Kind" />.</summary>
        public virtual bool Holds(Relation relation, Literal operand)
        {
            return Relations.Holds(Read(), relation, operand);
        }

        /// <summary>What the block holds now as a number, for a sum; 0 for a value that is no number.</summary>
        public double Number()
        {
            var reading = Read();
            return reading.Kind == ValueKind.Integer || reading.Kind == ValueKind.Real ? reading.Number : 0d;
        }
    }
}
