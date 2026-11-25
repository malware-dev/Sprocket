using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Castle.DynamicProxy;

namespace Sprocket.Sim.Fakes
{
    /// <summary>
    /// What the stand-ins for the game's objects allocate while some work runs, so a test counts the work's own bytes apart
    /// from them: the game's own objects answer without allocating, and a stand-in allocates on every question.
    /// </summary>
    /// <remarks>
    /// A question costs bytes in two places: the proxy's record of the call, made before the stand-in is asked, which is the
    /// same on every call to one member and is measured once for it; and the stand-in's answer, measured on every call.
    /// </remarks>
    public sealed class StandInLedger
    {
        // What a call to each member allocates before the stand-in is asked. A static, as Pretend's counts are: the harness
        // runs one screen at a time, and what a proxy allocates for a member never changes while it runs.
        static readonly Dictionary<MethodInfo, long> Recorded = new Dictionary<MethodInfo, long>();

        readonly Dictionary<MethodInfo, int> _asked = new Dictionary<MethodInfo, int>();
        readonly Dictionary<MethodInfo, object> _askedOf = new Dictionary<MethodInfo, object>();
        int _depth;
        long _answering;
        long _recorded;
        bool _measuringRecord;
        long _recordStart;
        long _record;

        /// <summary>The bytes <paramref name="work" /> allocates of its own, without what the stand-ins it asks allocate.</summary>
        public static long Own(Action work)
        {
            var ledger = new StandInLedger();
            long total;
            Pretend.Ledger = ledger;
            try
            {
                var before = GC.GetAllocatedBytesForCurrentThread();
                work();
                total = GC.GetAllocatedBytesForCurrentThread() - before;
            }
            finally
            {
                Pretend.Ledger = null;
            }

            return total - ledger._answering - ledger.Records();
        }

        /// <summary>
        /// The bytes allocated on this thread so far, less what the stand-ins have allocated while <see cref="Own" /> measures,
        /// for telling apart the parts of the work it measures.
        /// </summary>
        /// <remarks>Exact once each member asked has been measured by an earlier <see cref="Own" />.</remarks>
        public static long Clock()
        {
            var ledger = Pretend.Ledger;
            var now = GC.GetAllocatedBytesForCurrentThread();
            return ledger == null ? now : now - ledger._answering - ledger._recorded;
        }

        /// <summary>Notes a question as the stand-in is asked it, and gives the bytes allocated so far, for <see cref="Answered" />.</summary>
        public long Asking(IInvocation invocation)
        {
            var entry = GC.GetAllocatedBytesForCurrentThread();
            if (_depth++ > 0)
                return entry;

            if (_measuringRecord)
            {
                _record = entry - _recordStart;
                return entry;
            }

            long each;
            if (Recorded.TryGetValue(invocation.Method, out each))
                _recorded += each;

            int asked;
            _asked.TryGetValue(invocation.Method, out asked);
            _asked[invocation.Method] = asked + 1;
            if (asked == 0)
                _askedOf[invocation.Method] = invocation.Proxy;

            return entry;
        }

        /// <summary>Notes that the question asked when <paramref name="entry" /> bytes had been allocated is answered.</summary>
        public void Answered(long entry)
        {
            if (--_depth == 0 && !_measuringRecord)
                _answering += GC.GetAllocatedBytesForCurrentThread() - entry;
        }

        // What the proxy's records of every question asked allocated.
        long Records()
        {
            long bytes = 0;
            foreach (var asked in _asked)
            {
                long each;
                if (!Recorded.TryGetValue(asked.Key, out each))
                {
                    each = Record(asked.Key, _askedOf[asked.Key]);
                    Recorded[asked.Key] = each;
                }

                bytes += each * asked.Value;
            }

            return bytes;
        }

        // What the proxy allocates for a call to member of proxy before the stand-in is asked, measured on a call with every
        // argument its default.
        long Record(MethodInfo member, object proxy)
        {
            var call = Call(member, proxy);
            Ask(call);
            Pretend.Ledger = this;
            _measuringRecord = true;
            try
            {
                _recordStart = GC.GetAllocatedBytesForCurrentThread();
                Ask(call);
            }
            finally
            {
                _measuringRecord = false;
                Pretend.Ledger = null;
            }

            return _record;
        }

        // A call to member of proxy with every argument its default, answering whatever it answers.
        static Action Call(MethodInfo member, object proxy)
        {
            var locals = new List<ParameterExpression>();
            var arguments = new List<Expression>();
            foreach (var parameter in member.GetParameters())
            {
                var type = parameter.ParameterType;
                if (!type.IsByRef)
                {
                    arguments.Add(Expression.Default(type));
                    continue;
                }

                var local = Expression.Variable(type.GetElementType());
                locals.Add(local);
                arguments.Add(local);
            }

            var target = Expression.Convert(Expression.Constant(proxy), member.DeclaringType);
            var body = Expression.Block(locals, Expression.Call(target, member, arguments), Expression.Empty());
            return Expression.Lambda<Action>(body).Compile();
        }

        // The call made, a stand-in's refusal of its default arguments included, which comes after the record is made.
        static void Ask(Action call)
        {
            try
            {
                call();
            }
            catch (Exception)
            {
                // What the stand-in answers, or how it refuses, comes after the record measured.
            }
        }
    }
}
