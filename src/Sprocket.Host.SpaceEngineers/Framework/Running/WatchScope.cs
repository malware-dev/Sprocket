using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Model.Expressions;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>
    /// What the expressions of a trigger are worked out against on each look: the installation's settings and states, and the
    /// blocks its readings name, made ready once and read as often as a look asks.
    /// </summary>
    /// <remarks>
    /// A reading's blocks are found when the trigger is compiled, and each that announces a change is listened to, so a look
    /// reads gauges and allocates nothing for numbers. A reading whose options are expressions is made again only where they
    /// come to something else.
    /// </remarks>
    public sealed class WatchScope : IExpressionScope
    {
        readonly AutomationSystem _system;
        readonly SystemInstance _instance;
        readonly AutomationRunner _owner;
        readonly Action _heard;
        readonly Dictionary<BlockReading, ReadingProbes> _read = new Dictionary<BlockReading, ReadingProbes>();
        readonly Stack<List<Literal>> _lent = new Stack<List<Literal>>();
        Bindings _bindings;
        bool _listen;

        /// <summary>The scope of a trigger of <paramref name="system" /> on <paramref name="instance" />, owned by <paramref name="owner" />, told by <paramref name="heard" /> when a block it reads announces a change.</summary>
        public WatchScope(AutomationSystem system, SystemInstance instance, AutomationRunner owner, Action heard)
        {
            _system = system;
            _instance = instance;
            _owner = owner;
            _heard = heard;
        }

        /// <summary>Whether every block its readings name announces a change in what is read, so nothing needs a look on a cadence for them.</summary>
        public bool AllHeard { get; private set; } = true;

        /// <summary>Makes ready the blocks each reading of <paramref name="expressions" /> names, as <paramref name="bindings" /> finds them, listening to the ones that announce where <paramref name="listen" /> says to.</summary>
        /// <remarks>Done again only where the blocks or the program have changed, never on a tick.</remarks>
        public void Prepare(List<Expression> expressions, Bindings bindings, bool listen)
        {
            Forget();
            _bindings = bindings;
            _listen = listen;
            var readings = new List<BlockReading>();
            foreach (var expression in expressions)
                expression.Walk(e => Collect(e, readings));

            // An option that is an expression of readings is worked out from readings made before it, which the walk lists after it.
            for (var i = readings.Count - 1; i >= 0; i--)
                _read[readings[i]] = Build(readings[i], Worked(readings[i]));
        }

        /// <summary>The options of <paramref name="reading" /> as they come to now, an expression worked out and a value that has none standing as nought.</summary>
        List<Literal> Worked(BlockReading reading)
        {
            var worked = new List<Literal>(reading.Arguments.Count);
            foreach (var argument in reading.Arguments)
            {
                Literal value;
                worked.Add(argument.IsExpression && Evaluate(argument.Tree, out value) == ExpressionFailure.None ? value : argument.IsExpression ? Literal.Of(0L) : argument);
            }

            return worked;
        }

        void Collect(Expression tree, List<BlockReading> into)
        {
            var reading = tree as BlockReading;
            if (reading != null)
                into.Add(reading);

            // A state is set by a step, which no block announces, so a watcher reading one keeps its own cadence.
            if (tree is StateOperand)
                AllHeard = false;
        }

        /// <summary>Stops listening to every block, and forgets what was made ready.</summary>
        public void Forget()
        {
            foreach (var made in _read.Values)
                made.Stop();

            _read.Clear();
            AllHeard = true;
        }

        /// <summary>Works out <paramref name="tree" /> into <paramref name="value" />, or says why it has none.</summary>
        public ExpressionFailure Evaluate(Expression tree, out Literal value)
        {
            return tree.Evaluate(this, out value);
        }

        ReadingProbes Build(BlockReading reading, IList<Literal> arguments)
        {
            var settled = SystemInstance.Settled(_instance, arguments);
            var names = new List<string>();
            if (_instance == null)
                names.AddRange(reading.Targets);
            else
                _instance.Resolve(reading.Targets, names);

            _bindings.Distinct(names);
            var made = new ReadingProbes(arguments);
            foreach (var name in names)
            {
                var entity = _bindings.Entity(name);
                var probe = Probe.Reading(entity, reading.Key, settled, _owner.Clock, _instance == null ? 0 : _instance.FacetOf(reading.Targets, name));
                if (probe == null || probe.PassedOver)
                    continue;

                made.Probes.Add(probe);
                // A change the block announces, or whose being ruled out it announces, is heard; only an announced one needs no look on a cadence.
                var listening = _listen && (probe.Announced || probe.RuledOutBy != null) ? entity.Listen(_heard) : null;
                if (listening != null)
                    made.Listening.Add(listening);

                if (listening == null || !probe.Announced)
                    AllHeard = false;
            }

            return made;
        }

        /// <inheritdoc />
        public bool Setting(string name, out Literal value)
        {
            value = default(Literal);
            return _instance != null && _instance.Gives(name, out value);
        }

        /// <inheritdoc />
        public bool State(string name, out Literal value)
        {
            value = default(Literal);
            var state = _system.FindState(name);
            if (state == null)
                return false;

            value = _owner.States.Read(_system, _instance, state);
            return true;
        }

        /// <inheritdoc />
        public List<Literal> Borrow()
        {
            return _lent.Count > 0 ? _lent.Pop() : new List<Literal>();
        }

        /// <inheritdoc />
        public void Return(List<Literal> list)
        {
            list.Clear();
            _lent.Push(list);
        }

        /// <inheritdoc />
        public void Read(BlockReading reading, IList<Literal> arguments, List<Literal> into)
        {
            ReadingProbes made;
            if (!_read.TryGetValue(reading, out made))
                return;

            // Options that came to something else make the reading again, which allocates; nothing else does.
            if (!made.Takes(arguments))
            {
                made.Stop();
                made = Build(reading, arguments);
                _read[reading] = made;
            }

            for (var i = 0; i < made.Probes.Count; i++)
                into.Add(made.Probes[i].Current());
        }
    }
}
