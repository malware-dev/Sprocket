using System;
using System.Collections.Generic;
using Castle.DynamicProxy;

namespace Sprocket.Sim.Fakes
{
    /// <summary>
    /// Stands in for one of the game's own objects, answering only what the code under test asks.
    /// </summary>
    /// <remarks>
    /// The game's interfaces are enormous - a door is 375 members, a grid 354 - and the harness needs a
    /// handful of them. So they are answered at runtime rather than implemented: what is named here is
    /// answered, and anything else throws saying which member it was, which is how the harness reports
    /// that the mod has started using something new.
    /// <para>
    /// Several interfaces on one object, because a block is several: a door is also a toolbar provider,
    /// and the mod casts between them.
    /// </para>
    /// </remarks>
    public static class Pretend
    {
        /// <summary>
        /// How many questions the mod has asked the game, in total.
        /// </summary>
        /// <remarks>
        /// Every call to the game's interfaces comes through here, so this is the one place that can
        /// count them. The bytes a call costs in this harness are the proxy's and say nothing about the
        /// game, but the number of calls is exactly what the game would see - and a mod that asks its
        /// host a hundred questions on a frame where nothing has happened is the same fault as one
        /// that builds a hundred strings.
        /// <para>
        /// A static, which is banned in the mod and fine here: the harness runs one screen at a time
        /// (see Serially) and this is never shipped.
        /// </para>
        /// </remarks>
        public static int Asked;

        /// <summary>Where every question is written down by name, as it is asked, or null for nowhere.</summary>
        /// <remarks>For a test that has to say which questions a look costs, not only how many. A static for the reason <see cref="Asked" /> is.</remarks>
        public static List<string> Heard;

        /// <summary>One generator, because each one keeps a cache of the types it has made.</summary>
        public static readonly ProxyGenerator Generator = new ProxyGenerator();

        /// <summary>A stand-in for a <typeparamref name="T" />, answering nothing until it is told what.</summary>
        public static Stand<T> To<T>() where T : class
        {
            return new Stand<T>();
        }
    }

    /// <summary>A stand-in being told what it answers, which <see cref="Done" /> makes.</summary>
    public sealed class Stand<T> where T : class
    {
        readonly List<Type> _also = new List<Type>();
        readonly Dictionary<string, Func<object[], object>> _answers =
            new Dictionary<string, Func<object[], object>>(StringComparer.Ordinal);

        readonly Dictionary<string, Func<IInvocation, object>> _whole =
            new Dictionary<string, Func<IInvocation, object>>(StringComparer.Ordinal);

        /// <summary>Another interface the same object answers to.</summary>
        public Stand<T> Also<TOther>() where TOther : class
        {
            _also.Add(typeof(TOther));
            return this;
        }

        /// <summary>What a property reads as, asked every time rather than captured once.</summary>
        public Stand<T> Gives(string property, Func<object> value)
        {
            _answers.Add("get_" + property, _ => value());
            return this;
        }

        /// <summary>A method whose answer does not depend on what it was passed.</summary>
        public Stand<T> Does(string method, Func<object> answer)
        {
            _answers.Add(method, _ => answer());
            return this;
        }

        /// <summary>What a property is set to.</summary>
        public Stand<T> Takes(string property, Action<object> value)
        {
            _answers.Add("set_" + property, arguments =>
            {
                value(arguments[0]);
                return null;
            });
            return this;
        }

        /// <summary>A method whose answer depends on what it was passed.</summary>
        public Stand<T> Does(string method, Func<object[], object> answer)
        {
            _answers.Add(method, answer);
            return this;
        }

        /// <summary>
        /// A method answered from the whole call rather than from its arguments.
        /// </summary>
        /// <remarks>
        /// For the two shapes arguments alone cannot carry: a generic method, where which type was
        /// asked for is the question, and an <c>out</c> parameter, which is answered by writing back
        /// into the call.
        /// </remarks>
        public Stand<T> Answers(string method, Func<IInvocation, object> answer)
        {
            _whole.Add(method, answer);
            return this;
        }

        /// <summary>A method whose answer nobody reads.</summary>
        public Stand<T> Does(string method, Action<object[]> what)
        {
            _answers.Add(method, arguments =>
            {
                what(arguments);
                return null;
            });
            return this;
        }

        /// <summary>The stand-in, answering what it was told.</summary>
        public T Done()
        {
            var answering = new Answering(_answers, _whole);
            return (T)Pretend.Generator.CreateInterfaceProxyWithoutTarget(
                typeof(T), _also.ToArray(), answering);
        }

        sealed class Answering : IInterceptor
        {
            readonly Dictionary<string, Func<object[], object>> _answers;
            readonly Dictionary<string, Func<IInvocation, object>> _whole;

            public Answering(Dictionary<string, Func<object[], object>> answers,
                Dictionary<string, Func<IInvocation, object>> whole)
            {
                _answers = answers;
                _whole = whole;
            }

            static string Named(IInvocation invocation)
            {
                Type owner = invocation.Method.DeclaringType;
                return (owner == null ? "" : owner.Name + ".") + invocation.Method.Name;
            }

            public void Intercept(IInvocation invocation)
            {
                Pretend.Asked++;
                if (Pretend.Heard != null)
                    Pretend.Heard.Add(Named(invocation));

                Func<IInvocation, object> everything;
                if (_whole.TryGetValue(invocation.Method.Name, out everything))
                {
                    invocation.ReturnValue = everything(invocation);
                    return;
                }

                Func<object[], object> answer;
                if (!_answers.TryGetValue(invocation.Method.Name, out answer))
                    throw new NotSupportedException("the harness has no answer for " + Named(invocation));

                invocation.ReturnValue = answer(invocation.Arguments);
            }
        }
    }
}
