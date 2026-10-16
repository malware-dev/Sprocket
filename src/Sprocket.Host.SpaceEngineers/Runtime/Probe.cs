using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Runtime
{
    /// <summary>
    /// One block a test reads, worked out once: the member as the block has it, ready to be read, and what it is compared
    /// with.
    /// </summary>
    /// <remarks>
    /// Made when a program is bound and kept until it changes, so an <c>if</c> or a <c>when</c> reading the block asks
    /// nothing by name: a look is a delegate call or a property read, and a comparison.
    /// </remarks>
    public sealed class Probe
    {
        Probe(GridEntity entity, Gauge gauge, Relation relation, Literal operand, EntityEvent signal, EntityAction own, ReadingMemory kept,
            Func<int> clock)
        {
            Entity = entity;
            Gauge = gauge;
            Relation = relation;
            Operand = operand;
            Signal = signal;
            Own = own;
            Seen = signal != null && signal.Flag.Remembers ? Remembered(kept, false, clock) : null;
        }

        /// <summary>The block read.</summary>
        public GridEntity Entity { get; private set; }

        /// <summary>The value read, ready to read; null for a signal.</summary>
        public Gauge Gauge { get; private set; }

        /// <summary>The signal watched for, Sprocket's own change on the block; null for a value.</summary>
        public EntityEvent Signal { get; private set; }

        /// <summary>Sprocket's own value read, which says how a change in it is heard; null for the block's own value or a signal.</summary>
        public EntityAction Own { get; private set; }

        /// <summary>What a signal remembers between looks at this block, or null where it remembers nothing.</summary>
        public Recall Seen { get; private set; }

        /// <summary>How the value is compared.</summary>
        public Relation Relation { get; private set; }

        /// <summary>What the value is compared with, as the kind the block holds it.</summary>
        public Literal Operand { get; private set; }

        /// <summary>Whether the block says when what this reads may have changed.</summary>
        public bool Announced => Signal?.Announced ?? Own != null && Own.Announced;

        /// <summary>The flag that, while it reads true, rules out a change in what this reads; null where nothing does.</summary>
        public Reading RuledOutBy => Signal != null ? Signal.RuledOutBy : Own?.RuledOutBy;

        /// <summary>
        /// The value <paramref name="id" /> qualified by <paramref name="provider" /> as <paramref name="entity" /> has it,
        /// given <paramref name="arguments" /> and compared by <paramref name="relation" /> with <paramref name="value" />; null
        /// where the block has no such value, or it cannot be compared that way or read the way <paramref name="quantifier" />
        /// asks.
        /// </summary>
        /// <remarks>
        /// A value of Sprocket's own is read through what declares it; one of the block's own through the property the game
        /// hands out, found once. A one of a list is compared for sameness by its position among the names, worked out here.
        /// Only numbers are ordered or summed. A value of a type the host provides reads the facet numbered <paramref name="facet" />,
        /// the first where it names none.
        /// </remarks>
        public static Probe Of(GridEntity entity, string provider, string id, IList<Literal> arguments, Quantifier quantifier,
            Relation relation, Literal value, ReadingMemory kept = null, Func<int> clock = null, int facet = 0)
        {
            if (entity == null || (provider.Length > 0 && provider != BlockKinds.Own))
                return null;

            var written = Value(entity.Kind, provider, id);
            if (written == null)
                return null;

            var being = written.Being;
            if (being == null ? arguments.Count > 0 : being.Missing(arguments.Count) != null || being.Refused(arguments) != null)
                return null;

            Literal operand;
            if (!written.Compares(value, out operand))
                return null;

            var kind = being?.Kind ?? ValueKind.Flag;
            Gauge gauge;
            if (being == null)
            {
                gauge = entity.Gauge(written);
                if (gauge == null)
                    return null;

                kind = gauge.Kind;
            }
            else
            {
                var given = written.Type == null ? arguments : HostType.Arguments(facet, arguments);
                gauge = being.Gauge(entity.Block, given, being.Remembers ? Remembered(kept, true, clock) : null);
                if (kind == ValueKind.Name)
                {
                    var at = being.Position(operand.Text);
                    if (at < 0)
                        return null;

                    operand = Literal.Of(at);
                }
            }

            var same = relation == Relation.Is || relation == Relation.IsNot;
            var number = kind == ValueKind.Integer || kind == ValueKind.Real;
            var totals = quantifier == Quantifier.Sum || quantifier == Quantifier.Average;
            if ((!same || totals) && !number)
                return null;

            if (totals)
                operand = value;

            return new Probe(entity, gauge, relation, operand, null, being == null ? null : written, kept, clock);
        }

        /// <summary>
        /// What a reader remembers of the block between looks: the recall <paramref name="kept" /> holds for the value read or the
        /// signal watched, as <paramref name="gauge" /> says, made and kept there where it holds none; a fresh one where nothing
        /// keeps it; told the tick by <paramref name="clock" />.
        /// </summary>
        static Recall Remembered(ReadingMemory kept, bool gauge, Func<int> clock)
        {
            if (kept == null)
                return new Recall { Clock = clock };

            var recall = gauge ? kept.Gauge : kept.Seen;
            if (recall != null)
            {
                recall.Clock = clock;
                return recall;
            }

            recall = new Recall { Clock = clock };
            if (gauge)
                kept.Gauge = recall;
            else
                kept.Seen = recall;

            return recall;
        }

        /// <summary>
        /// The signal <paramref name="id" /> qualified by <paramref name="provider" /> as <paramref name="entity" /> sends it,
        /// watched for by Sprocket; null where the block sends no such signal, or one only a toolbar sends.
        /// </summary>
        public static Probe Watching(GridEntity entity, string provider, string id, ReadingMemory kept = null, Func<int> clock = null)
        {
            if (entity == null || provider != BlockKinds.Own)
                return null;

            var signal = entity.Kind.FindEvent(id);
            if (signal == null || signal.Flag == null)
                return null;

            return new Probe(entity, null, Relation.Is, Literal.Of(true), signal, null, kept, clock);
        }

        /// <summary>The value <paramref name="id" /> a block of <paramref name="kind" /> has: Sprocket's own where <paramref name="provider" /> says so, the block's own otherwise.</summary>
        public static EntityAction Value(EntityKind kind, string provider, string id)
        {
            return EntityAction.Value(kind.Actions, provider, id);
        }

        /// <summary>Whether the comparison holds on the block now, or, for a signal, whether the block is in what it changes to.</summary>
        public bool Holds()
        {
            if (Signal != null)
                return Entity.In(Signal, Seen);

            return Gauge.Holds(Relation, Operand);
        }

        /// <summary>What the value reads now as a number, for a sum.</summary>
        public double Number()
        {
            return Gauge?.Number() ?? 0d;
        }
    }
}
