using System;
using System.Collections.Generic;
using Lumel.Layout;
using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Sequences;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Sequences
{
    /// <summary>Which wait on a sequence's screen covers which steps that take time, and the rows lit for what is under the pointer.</summary>
    public sealed class WaitCoverage
    {
        // One brightening and fading of the ring: a second at 60 ticks a second.
        const int PulseTicks = 60;

        readonly Func<Step, bool> _lasts;
        readonly List<int> _waiter = new List<int>();
        readonly List<int> _covered = new List<int>();
        readonly List<bool> _lasting = new List<bool>();
        int _hot = -1;
        bool _hotWait;

        /// <summary>The ties between the rows of a sequence, a step taking time where <paramref name="lasts" /> says so.</summary>
        public WaitCoverage(Func<Step, bool> lasts)
        {
            _lasts = lasts;
        }

        /// <summary>The ring's color at <paramref name="tick" /> while something is lit, and none while nothing is.</summary>
        public Color Glow(int tick)
        {
            return _hot < 0 ? Palette.None : Pulse(tick);
        }

        /// <summary>Works out which wait covers each step that takes time, reading paused steps as the run reads them.</summary>
        public void Cover(List<object> keys, KeyedChildren<object, StepRow> rows)
        {
            _waiter.Clear();
            _covered.Clear();
            _lasting.Clear();
            for (var i = 0; i < keys.Count; i++)
            {
                _waiter.Add(-1);
                _covered.Add(-1);
                var step = keys[i] as Step;
                _lasting.Add(step != null && _lasts(step) && !rows[keys[i]].IsOff);
            }

            _hot = -1;
            var open = -1;
            for (var i = 0; i < keys.Count; i++)
            {
                if (!(keys[i] is Step))
                    continue;

                if (_lasting[i] && open < 0)
                    open = i;

                // A paused wait is read as though it were not there, its steps going to the next.
                if (!(keys[i] is Await) || rows[keys[i]].IsOff || open < 0)
                    continue;

                _covered[i] = open;
                for (var j = open; j <= i; j++)
                {
                    if (_lasting[j] || j == i)
                        _waiter[j] = i;
                }

                open = -1;
            }
        }

        /// <summary>Lights the rows tied to the row under the pointer.</summary>
        /// <remarks>Called every tick; it changes rows only when the row under the pointer changes.</remarks>
        public void Light(List<object> keys, KeyedChildren<object, StepRow> rows, bool carrying)
        {
            var hot = -1;
            var onWait = false;

            for (var i = 0; !carrying && i < keys.Count && i < _waiter.Count; i++)
            {
                var row = rows[keys[i]];
                if (row.Kind == StepKind.Await && row.IsUnderPointer)
                {
                    hot = i;
                    onWait = true;
                    break;
                }

                if (hot < 0 && row.IsUnderPointer)
                    hot = i;
            }

            if (hot == _hot && onWait == _hotWait)
                return;

            _hot = hot;
            _hotWait = onWait;

            var waiter = hot < 0 ? -1 : onWait ? _covered[hot] >= 0 ? hot : -1 : _waiter[hot];
            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = rows[keys[i]];
                var tied = waiter >= 0 && _waiter[i] == waiter && _lasting[i];
                row.MarksLit = onWait ? tied : waiter >= 0 && i == hot;
                row.ClockLit = i == waiter;
            }

            Join(keys, rows);
        }

        /// <summary>Runs the spine joining lit lines from the first of them to the last.</summary>
        void Join(List<object> keys, KeyedChildren<object, StepRow> rows)
        {
            var first = -1;
            var last = -1;
            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = rows[keys[i]];
                if (!row.MarksLit && !row.ClockLit)
                    continue;

                if (first < 0)
                    first = i;

                last = i;
            }

            for (var i = 0; i < _waiter.Count; i++)
            {
                var row = rows[keys[i]];
                row.Joins = first >= 0 && i >= first && i <= last;
                row.JoinFirst = i == first;
                row.JoinLast = i == last;
            }
        }

        /// <summary>The ring's color at <paramref name="tick" />, brightening and fading once a second.</summary>
        static Color Pulse(int tick)
        {
            var phase = tick % PulseTicks * MathHelper.TwoPi / PulseTicks;
            return Palette.Link * (0.6f - 0.4f * (float)Math.Cos(phase));
        }
    }
}
