using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// One block a test reads, worked out once: the member as the block has it, ready to be read, and what it is compared
    /// with.
    /// </summary>
    /// <remarks>
    /// Made when a program is bound and kept until it changes, so a look asks nothing by name: a delegate call or a
    /// property read, and a comparison.
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

        // What Of made it from, so the value it compares with can be changed without making it again.
        EntityAction _written;
        OwnValue _ownValue;
        ValueKind _kind;
        bool _totals;
        bool _readsWord;

        /// <summary>Whether the value it compares with was refused, so it holds on no look until another is given.</summary>
        public bool Refused { get; private set; }

        /// <summary>Whether the block can't answer what is asked of it, so a reading of several blocks leaves it out.</summary>
        public bool PassedOver { get; private set; }

        /// <summary>The block read.</summary>
        public GridEntity Entity { get; private set; }

        /// <summary>The value read, ready to read; null for a signal.</summary>
        public Gauge Gauge { get; private set; }

        /// <summary>The signal watched for, Sprocket's own change on the block; null for a value.</summary>
        public EntityEvent Signal { get; private set; }

        /// <summary>Sprocket's own value read, which says how a change in it is heard; null for the block's own value or a signal.</summary>
        public EntityAction Own { get; private set; }

        /// <summary>What a signal remembers between looks at this block, or null where it remembers nothing.</summary>
        public ReaderMemory Seen { get; private set; }

        /// <summary>How the value is compared.</summary>
        public Relation Relation { get; private set; }

        /// <summary>What the value is compared with, as the kind the block holds it.</summary>
        public Literal Operand { get; private set; }

        /// <summary>Whether the block says when what this reads may have changed.</summary>
        public bool Announced => Signal?.Announced ?? Own != null && Own.Announced;

        /// <summary>The flag that, while it reads true, rules out a change in what this reads; null where nothing does.</summary>
        public Reading RuledOutBy => Signal != null ? Signal.RuledOutBy : Own?.RuledOutBy;

        /// <summary>
        /// The value <paramref name="key" /> names as <paramref name="entity" /> has it,
        /// given <paramref name="arguments" /> and compared by <paramref name="relation" /> with <paramref name="value" />; null
        /// where the block has no such value, or it cannot be compared that way or read the way <paramref name="quantifier" />
        /// asks.
        /// </summary>
        /// <remarks>
        /// Only numbers are ordered or summed. A value of a type the host provides reads the facet numbered
        /// <paramref name="facet" />, the first where it names none.
        /// </remarks>
        public static Probe Of(GridEntity entity, MemberKey key, IList<Literal> arguments, Quantifier quantifier,
            Relation relation, Literal value, ReadingMemory kept = null, Func<int> clock = null, int facet = 0)
        {
            if (entity == null)
                return null;

            var written = Value(entity.Kind, key);
            if (written == null)
                return null;

            var own = written.OwnValue;
            if (own == null ? arguments.Count > 0 : own.Missing(arguments.Count) != null || own.Refused(arguments) != null)
                return null;

            Literal operand;
            if (!written.Compares(value, out operand))
                return null;

            var kind = own?.Kind ?? ValueKind.Flag;
            Gauge gauge;
            var passedOver = false;
            if (own == null)
            {
                gauge = entity.Gauge(written);
                if (gauge == null)
                    return null;

                kind = gauge.Kind;
            }
            else
            {
                var given = written.Type == null ? arguments : HostType.Arguments(facet, arguments);
                gauge = own.Gauge(entity.Block, given, own.Remembers ? Remembered(kept, true, clock) : null);
                passedOver = !own.Answers(entity.Block, given);
                if (kind == ValueKind.Name)
                {
                    var at = own.Position(operand.Text);
                    if (at < 0)
                        return null;

                    operand = Literal.Of(at);
                }
            }

            var same = relation == Relation.Is || relation == Relation.IsNot;
            var number = kind == ValueKind.Integer || kind == ValueKind.Real;
            var totals = Quantifiers.Totals(quantifier);
            if ((!same || totals) && !number)
                return null;

            if (totals)
                operand = value;

            return new Probe(entity, gauge, relation, operand, null, own == null ? null : written, kept, clock)
            {
                PassedOver = passedOver,
                _written = written,
                _ownValue = own,
                _kind = kind,
                _totals = totals
            };
        }

        /// <summary>
        /// The value <paramref name="key" /> names as <paramref name="entity" /> has it, given <paramref name="arguments" />, to be read as
        /// it is; null where the block has no such value or does not take those arguments.
        /// </summary>
        /// <remarks>A value of a type the host provides reads the facet numbered <paramref name="facet" />. Made as a step reaches it, never on a tick.</remarks>
        public static Probe Reading(GridEntity entity, MemberKey key, IList<Literal> arguments, Func<int> clock, int facet = 0)
        {
            if (entity == null)
                return null;

            var written = Value(entity.Kind, key);
            if (written == null)
                return null;

            var own = written.OwnValue;
            if (own == null ? arguments.Count > 0 : own.Missing(arguments.Count) != null || own.Refused(arguments) != null)
                return null;

            Gauge gauge;
            var passedOver = false;
            if (own == null)
            {
                gauge = entity.Gauge(written);
                if (gauge == null)
                    return null;
            }
            else
            {
                var given = written.Type == null ? arguments : HostType.Arguments(facet, arguments);
                gauge = own.Gauge(entity.Block, given, own.Remembers ? Remembered(null, true, clock) : null);
                passedOver = !own.Answers(entity.Block, given);
            }

            return new Probe(entity, gauge, Relation.Is, default(Literal), null, own == null ? null : written, null, clock)
            {
                PassedOver = passedOver,
                _readsWord = own != null && own.Kind == ValueKind.Name
            };
        }

        /// <summary>The kind of value <paramref name="key" /> names as <paramref name="entity" /> has it, or null where the block has no such value.</summary>
        public static ValueKind? KindOf(GridEntity entity, MemberKey key)
        {
            var written = entity == null ? null : Value(entity.Kind, key);
            if (written == null)
                return null;

            if (written.OwnValue != null)
                return written.OwnValue.Kind;

            return entity.Gauge(written)?.Kind;
        }

        /// <summary>
        /// What a reader remembers of the block between looks: the recall <paramref name="kept" /> holds for the value read or the
        /// signal watched, as <paramref name="gauge" /> says, made and kept there where it holds none; a fresh one where nothing
        /// keeps it; told the tick by <paramref name="clock" />.
        /// </summary>
        static ReaderMemory Remembered(ReadingMemory kept, bool gauge, Func<int> clock)
        {
            if (kept == null)
                return new ReaderMemory { Clock = clock };

            var recall = gauge ? kept.Gauge : kept.Seen;
            if (recall != null)
            {
                recall.Clock = clock;
                return recall;
            }

            recall = new ReaderMemory { Clock = clock };
            if (gauge)
                kept.Gauge = recall;
            else
                kept.Seen = recall;

            return recall;
        }

        /// <summary>
        /// The signal <paramref name="key" /> names as <paramref name="entity" /> sends it,
        /// watched for by Sprocket; null where the block sends no such signal, or one only a toolbar sends.
        /// </summary>
        public static Probe Watching(GridEntity entity, MemberKey key, ReadingMemory kept = null, Func<int> clock = null)
        {
            if (entity == null)
                return null;

            var signal = entity.Kind.FindEvent(key);
            if (signal == null || signal.Flag == null)
                return null;

            return new Probe(entity, null, Relation.Is, Literal.Of(true), signal, null, kept, clock);
        }

        /// <summary>The value a block of <paramref name="kind" /> has that <paramref name="key" /> names, or null where it has none, or a bare name meets more than one.</summary>
        public static EntityAction Value(EntityKind kind, MemberKey key)
        {
            return EntityAction.Value(kind.Actions, key);
        }

        /// <summary>Whether the comparison holds on the block now, or, for a signal, whether the block is in what it changes to.</summary>
        public bool Holds()
        {
            if (Refused)
                return false;

            if (Signal != null)
                return Entity.In(Signal, Seen);

            return Gauge.Holds(Relation, Operand);
        }

        /// <summary>Has it compare with <paramref name="value" /> from now on, as <see cref="Of" /> would have made it; where the value cannot be compared so it holds on no look.</summary>
        /// <remarks>Allocates nothing; asked on a look of a watcher whose value is an expression.</remarks>
        public void Retarget(Literal value)
        {
            Refused = true;
            if (_written == null)
                return;

            Literal operand;
            if (!_written.Compares(value, out operand))
                return;

            if (_ownValue != null && _kind == ValueKind.Name)
            {
                var at = _ownValue.Position(operand.Text);
                if (at < 0)
                    return;

                operand = Literal.Of(at);
            }

            Operand = _totals ? value : operand;
            Refused = false;
        }

        /// <summary>What the value reads now as an expression takes it: a value that is one of a list by its name, and any other as it is.</summary>
        public Literal Current()
        {
            return _readsWord ? Literal.Name(Gauge.Said()) : Gauge.Read();
        }

        /// <summary>What the value reads now as a number, for a sum.</summary>
        public double Number()
        {
            return Gauge?.Number() ?? 0d;
        }
    }
}
