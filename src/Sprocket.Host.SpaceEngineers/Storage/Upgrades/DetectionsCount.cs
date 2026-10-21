using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage.Upgrades
{
    /// <summary>Alpha 16: a sensor's count of what it detects was named <c>detections</c>.</summary>
    public sealed class DetectionsCount : FormatChange
    {
        // A sensor's count of what it detects, as alpha 15 programs persist it, and its id since.
        const string ThingsDetected = "things-detected";

        const string Detections = "detections";

        /// <inheritdoc />
        public override int Arrived => 16;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            var counted = 0;
            foreach (var system in program.Systems)
                counted += Counts(system);

            return counted == 0 ? "" : $"alpha 15: {counted} use{(counted == 1 ? "" : "s")} of things-detected named detections.";
        }

        /// <summary>Names a sensor's count <c>detections</c> wherever <paramref name="system" /> names it <c>things-detected</c>.</summary>
        static int Counts(AutomationSystem system)
        {
            var count = 0;
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var act = step as Act;
                    if (act != null && act.Provider == BlockKinds.Own && act.ActionId == ThingsDetected)
                    {
                        act.ActionId = Detections;
                        count++;
                    }

                    var test = (step as Branch)?.Test as Comparison;
                    if (test != null && test.Provider == BlockKinds.Own && test.MemberId == ThingsDetected)
                    {
                        test.MemberId = Detections;
                        count++;
                    }
                }
            }

            foreach (var trigger in system.Triggers)
            {
                var when = trigger as WhenTrigger;
                if (when != null && when.Provider == BlockKinds.Own && when.EventId == ThingsDetected)
                {
                    when.EventId = Detections;
                    count++;
                }
            }

            return count;
        }
    }
}
