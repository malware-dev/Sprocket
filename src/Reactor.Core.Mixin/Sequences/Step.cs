using System.Collections.Generic;

namespace Reactor.Sequences
{
    /// <summary>
    /// When a step is allowed to start.
    /// </summary>
    public abstract class StepGate
    {
    }

    /// <summary>
    /// "once the inner door is shut - …"
    /// </summary>
    /// <remarks>
    /// The default kind of gate, and the important one. A duration is a programmer's
    /// substitute for a completion signal: people do not think "wait two seconds",
    /// they think "once the door is actually shut". Guessing a duration produces an
    /// airlock that works until the door is obstructed or the server lags, and then
    /// vents the ship.
    /// </remarks>
    public sealed class OnceGate : StepGate
    {
        public OnceGate(Condition condition)
        {
            Condition = condition;
        }

        public Condition Condition { get; set; }
    }

    /// <summary>
    /// "only if the inner door is shut - …"
    /// </summary>
    /// <remarks>
    /// A guard, not a wait. If the situation does not hold the step is passed over
    /// and the sequence carries on - which is the difference between "hold here until
    /// it is safe" and "don't do this unless it is safe".
    ///
    /// Being passed over is recorded, because a step that quietly does nothing is
    /// indistinguishable from one that worked.
    /// </remarks>
    public sealed class OnlyIfGate : StepGate
    {
        public OnlyIfGate(Condition condition)
        {
            Condition = condition;
        }

        public Condition Condition { get; set; }
    }

    /// <summary>
    /// "after 30 ticks - …"
    /// </summary>
    /// <remarks>
    /// Still needed for things that genuinely have no completion signal, such as
    /// flashing a beacon. Offered second, never first.
    /// </remarks>
    public sealed class AfterGate : StepGate
    {
        public AfterGate(int ticks)
        {
            Ticks = ticks;
        }

        public int Ticks { get; set; }
    }

    /// <summary>
    /// One numbered line of a sequence.
    /// </summary>
    /// <remarks>
    /// Flat and numbered. Nesting is what defeats novices, so there isn't any - every
    /// line sits at the same level and reads top to bottom.
    ///
    /// A step with a gate and nothing to do is simply a wait.
    /// </remarks>
    public sealed class Step
    {
        readonly List<long> _targets = new List<long>();

        public Step(string actionId)
        {
            ActionId = actionId;
            WaitsForCompletion = true;
        }

        /// <summary>When this step may start, or null to start immediately.</summary>
        /// <remarks>
        /// Usually null. Because steps wait for their own action to finish, a gate is
        /// only needed to wait on something this sequence did <em>not</em> cause -
        /// "once the tank is full" - or to guard a step with "only if".
        /// </remarks>
        public StepGate Gate { get; set; }

        /// <summary>
        /// Whether the sequence holds here until the action has actually taken effect.
        /// </summary>
        /// <remarks>
        /// True by default, and this is the single most important default in the
        /// language. A numbered list already means "and then" to anyone who has
        /// followed a recipe: "close the inner door, depressurize, open the outer
        /// door" needs no further explanation of the ordering, because the ordering
        /// is what a list <em>is</em>.
        ///
        /// The alternative - stating "once the inner door is shut" on every step -
        /// makes the user write down something the reader already assumed, and pushes
        /// them towards guessing a duration instead, which is how an airlock ends up
        /// venting a ship the day a door sticks.
        ///
        /// Doing several things at once is therefore the thing you have to ask for.
        /// Note that acting on several entities in one step was always simultaneous,
        /// so this is only needed for genuinely different actions overlapping.
        /// </remarks>
        public bool WaitsForCompletion { get; set; }

        /// <summary>Entities affected. All of them, together, in this step.</summary>
        public IList<long> Targets { get { return _targets; } }

        /// <summary>The <see cref="World.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        public bool HasAction
        {
            get { return Targets.Count > 0 && !string.IsNullOrEmpty(ActionId); }
        }
    }
}
