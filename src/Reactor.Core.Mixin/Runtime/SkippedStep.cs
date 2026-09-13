using System.Collections.Generic;

namespace Reactor.Runtime
{
    /// <summary>A step that was passed over because its "only if" was not true.</summary>
    /// <remarks>
    /// Recorded structurally rather than as a sentence, so the editor can word it.
    /// This is the raw material for "why didn't my door open?" - the one question a
    /// non-programmer actually asks, and the one almost no environment answers.
    /// </remarks>
    public sealed class SkippedStep
    {
        public SkippedStep(int stepIndex, int tick)
        {
            StepIndex = stepIndex;
            Tick = tick;
        }

        public int StepIndex { get; private set; }

        public int Tick { get; private set; }
    }
}
