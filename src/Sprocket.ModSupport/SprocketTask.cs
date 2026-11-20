using System;
using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Sprocket.ModSupport
{
    /// <summary>
    /// One run of a mod's awaitable action on one block, which a step waits for until the mod ends it: completed, failed,
    /// conflicted or cancelled.
    /// </summary>
    /// <remarks>
    /// Made and ended on the game's main thread. A task ends once; whatever it is told after that is passed over.
    /// </remarks>
    public sealed partial class SprocketTask : ISprocketWired
    {
        static readonly ISprocketWiring Wiring = new Key();
        readonly List<Action<SprocketTask>> _ending = new List<Action<SprocketTask>>();
        Action<string, string, string> _heard;
        Action _poll;
        Action<SprocketCancel> _forward;
        string _stuck;
        bool _stopped;

        /// <summary>Raised when Sprocket stops the task. Stop it at once, as fast as it can possibly be done.</summary>
        /// <remarks>
        /// A stop is never optional: Sprocket has already let go, and nothing reported after it reaches the run. Say the
        /// task has stopped with <see cref="Cancel" />; a task that has not said so a second later is logged as breaking the
        /// mod support guidelines. Once Sprocket has stopped it, the task ends as cancelled however it ends, so what
        /// the mod does to stop may raise its own events in any order.
        /// </remarks>
        public event Action<SprocketCancel> Cancelling;

        /// <summary>Whether the task has ended, one way or another.</summary>
        public bool IsEnded => Outcome != null;

        /// <summary>How the task ended, one of the outcome words of <see cref="SprocketWire" />, or null while it runs.</summary>
        public string Outcome { get; private set; }

        /// <summary>The failure it ended with, where it failed or was conflicted.</summary>
        public SprocketFailure Failure { get; private set; }

        /// <summary>Ends the task: it got there.</summary>
        public void Complete()
        {
            End(SprocketWire.Completed, default(SprocketFailure));
        }

        /// <summary>Ends the task: it went wrong, as <paramref name="failure" /> says, and a sequence can catch it.</summary>
        public void Fail(SprocketFailure failure)
        {
            End(SprocketWire.Failed, failure);
        }

        /// <summary>Ends the task: something took it out of the mod's hands, as <paramref name="failure" /> says, which players see as a warning.</summary>
        public void Conflicted(SprocketFailure failure)
        {
            End(SprocketWire.Conflicted, failure);
        }

        /// <summary>Ends the task: the mod agreed to stop when Sprocket asked.</summary>
        public void Cancel()
        {
            End(SprocketWire.Cancelled, default(SprocketFailure));
        }

        /// <summary>Has <paramref name="ending" /> called once the task ends, however it ends, at once where it has: where a mod unsubscribes what it completed the task from.</summary>
        public void Ended(Action ending)
        {
            Ended(task => ending());
        }

        /// <summary>Says the task is not getting there now, as <paramref name="message" /> says, without giving up; null says it is moving again.</summary>
        public void Stuck(string message)
        {
            if (IsEnded || _stuck == message)
                return;

            _stuck = message;
            _heard?.Invoke(SprocketWire.Stuck, null, message);
        }

        /// <summary>A task that runs this one, then the one <paramref name="next" /> makes once this has completed; ending as either fails, conflicts or is cancelled.</summary>
        public SprocketTask Then(Func<SprocketTask> next)
        {
            var chain = new SprocketTask();
            chain.Follow(this);
            Ended(done =>
            {
                if (done.Outcome != SprocketWire.Completed)
                {
                    chain.EndAs(done);
                    return;
                }

                var following = next() ?? Completed;
                chain.Follow(following);
                following.Ended(chain.EndAs);
            });
            return chain;
        }

        /// <summary>A task that completes once all of <paramref name="tasks" /> have, and ends as the first of them that does not.</summary>
        public static SprocketTask WhenAll(IEnumerable<SprocketTask> tasks)
        {
            var all = new SprocketTask();
            var waiting = new List<SprocketTask>(tasks);
            var left = waiting.Count;
            all.Waiting(waiting);
            if (left == 0)
                all.Complete();

            foreach (var task in waiting)
            {
                task.Ended(done =>
                {
                    if (done.Outcome != SprocketWire.Completed)
                        all.EndAs(done);
                    else if (--left == 0)
                        all.Complete();
                });
            }

            return all;
        }

        /// <summary>A task that ends as the first of <paramref name="tasks" /> to end, asking the others to stop.</summary>
        public static SprocketTask WhenAny(IEnumerable<SprocketTask> tasks)
        {
            var any = new SprocketTask();
            var waiting = new List<SprocketTask>(tasks);
            any.Waiting(waiting);
            foreach (var task in waiting)
            {
                task.Ended(done =>
                {
                    if (any.IsEnded)
                        return;

                    any.EndAs(done);
                    foreach (var other in waiting)
                    {
                        if (!other.IsEnded)
                            other.Request(new SprocketCancel(Wiring, SprocketCancelCause.Aborted));
                    }
                });
            }

            return any;
        }

        /// <summary>A task that completes <paramref name="seconds" /> of game time from now.</summary>
        public static SprocketTask Delay(double seconds)
        {
            var until = MyAPIGateway.Session.ElapsedPlayTime + TimeSpan.FromSeconds(seconds);
            return Polled(() => MyAPIGateway.Session.ElapsedPlayTime >= until);
        }

        /// <summary>A task that completes once <paramref name="condition" /> holds, asked over and over on Sprocket's own cadence while a step waits on it.</summary>
        /// <remarks>
        /// For a block with no event to complete a task from. Where it has one, complete a <c>new SprocketTask()</c> from that
        /// event instead, and unsubscribe in <see cref="Ended(Action)" />: it costs nothing while it waits, and the step moves on
        /// at once rather than at the next look. It says it has stopped by itself when Sprocket stops it, before any other
        /// <see cref="Cancelling" /> handler runs; a handler added to it only has to stop what the mod itself does.
        /// </remarks>
        public static SprocketTask Polled(Func<bool> condition)
        {
            return Polled(condition, null);
        }

        /// <summary>
        /// A task that completes once <paramref name="condition" /> holds, or fails as <paramref name="failing" /> says once it
        /// names a failure, null while nothing is wrong; both looked at on Sprocket's own cadence while a step waits on it.
        /// </summary>
        /// <remarks>For a block with no events to end a task from, as <see cref="Polled(Func{bool})" /> says.</remarks>
        public static SprocketTask Polled(Func<bool> condition, Func<SprocketFailure?> failing)
        {
            var task = new SprocketTask();
            task._poll = () =>
            {
                var failure = failing?.Invoke();
                if (failure != null)
                    task.Fail(failure.Value);
                else if (condition())
                    task.Complete();
            };
            task.Cancelling += cancel => task.Cancel();
            return task;
        }

        /// <summary>A task that has already completed.</summary>
        public static SprocketTask Completed
        {
            get
            {
                var task = new SprocketTask();
                task.Complete();
                return task;
            }
        }

        /// <summary>A task that has already failed as <paramref name="failure" /> says.</summary>
        public static SprocketTask Failed(SprocketFailure failure)
        {
            var task = new SprocketTask();
            task.Fail(failure);
            return task;
        }

        /// <inheritdoc />
        Dictionary<string, object> ISprocketWired.Wire()
        {
            return new Dictionary<string, object>
            {
                { SprocketWire.Hear, (Action<Action<string, string, string>>)Heard },
                { SprocketWire.Cancel, (Action<bool, string>)((final, cause) => Request(new SprocketCancel(Wiring, Cause(cause)))) },
                { SprocketWire.Poll, (Action)(() => _poll?.Invoke()) }
            };
        }

        /// <summary>Has <paramref name="heard" /> told how the task ends, at once where it has already ended.</summary>
        void Heard(Action<string, string, string> heard)
        {
            _heard = heard;
            if (IsEnded)
                Tell();
            else if (_stuck != null)
                heard(SprocketWire.Stuck, null, _stuck);
        }

        /// <summary>Stops the task, through whatever it is waiting on now.</summary>
        void Request(SprocketCancel cancel)
        {
            if (IsEnded)
                return;

            // Sprocket has let go: however the task ends from here, even by what the mod does to stop it, it ended by being stopped.
            _stopped = true;
            if (_forward != null)
                _forward(cancel);
            else
                Cancelling?.Invoke(cancel);
        }

        /// <summary>Has this task stand for <paramref name="part" />: polled through it, and stopped through it.</summary>
        void Follow(SprocketTask part)
        {
            _poll = () => part._poll?.Invoke();
            _forward = part.Request;
        }

        /// <summary>Has this task stand for every one of <paramref name="parts" />.</summary>
        void Waiting(List<SprocketTask> parts)
        {
            _poll = () =>
            {
                for (var i = 0; i < parts.Count; i++)
                    parts[i]._poll?.Invoke();
            };
            _forward = cancel =>
            {
                foreach (var part in parts)
                    part.Request(cancel);
            };
        }

        /// <summary>Has <paramref name="ending" /> called once the task ends, at once where it has.</summary>
        void Ended(Action<SprocketTask> ending)
        {
            if (IsEnded)
                ending(this);
            else
                _ending.Add(ending);
        }

        /// <summary>Ends this task the way <paramref name="other" /> ended.</summary>
        void EndAs(SprocketTask other)
        {
            End(other.Outcome, other.Failure);
        }

        /// <summary>Ends the task with <paramref name="outcome" />, unless it already has.</summary>
        void End(string outcome, SprocketFailure failure)
        {
            if (IsEnded)
                return;

            if (_stopped)
            {
                outcome = SprocketWire.Cancelled;
                failure = default(SprocketFailure);
            }

            Outcome = outcome;
            Failure = failure;
            _poll = null;
            _forward = null;
            Tell();
            foreach (var ending in _ending)
                ending(this);
            _ending.Clear();
        }

        /// <summary>Tells Sprocket how the task ended, where it is listening.</summary>
        void Tell()
        {
            _heard?.Invoke(Outcome, Failure.Id, Failure.Message);
        }

        /// <summary>The cause the word <paramref name="cause" /> names, as Sprocket sends it.</summary>
        static SprocketCancelCause Cause(string cause)
        {
            SprocketCancelCause read;
            return Enum.TryParse(cause, out read) ? read : SprocketCancelCause.Aborted;
        }
    }
}
