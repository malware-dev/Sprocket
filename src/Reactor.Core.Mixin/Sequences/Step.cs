using System.Collections.Generic;

namespace Reactor.Sequences
{
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
            Annotation = "";
        }

        /// <summary>
        /// What somebody wrote about this, in their own words, or empty.
        /// </summary>
        /// <remarks>
        /// Data somebody typed, not a comment: it is part of the model, round-tripped like a name, and
        /// shown by whatever is drawing. What a target labels it on screen - "description", "notes",
        /// anything, in any language - is the host's word and has no bearing on this one.
        /// </remarks>
        public string Annotation { get; set; }

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
        public IList<long> Targets => _targets;

        /// <summary>The <see cref="World.EntityAction"/> id to invoke.</summary>
        public string ActionId { get; set; }

        public bool HasAction => Targets.Count > 0 && !string.IsNullOrEmpty(ActionId);
    }
}
