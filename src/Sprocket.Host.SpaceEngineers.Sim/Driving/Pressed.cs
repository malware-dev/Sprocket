using Sprocket.Host.SpaceEngineers.Sequences;

namespace Sprocket.Sim.Driving
{
    /// <summary>Button triggers written straight into a program, the way the trigger picker makes them.</summary>
    public static class Pressed
    {
        /// <summary>
        /// A trigger part of <paramref name="system" />, answered in its first blockset by button <paramref name="button" /> of
        /// the block <paramref name="declared" />, and the trigger that is it, not yet added to the system.
        /// </summary>
        public static RequiredTrigger On(AutomationSystem system, string declared, int button = 1)
        {
            var part = system.TakeTrigger(system.First, new[] { declared }, new[] { new ToolbarSlot(1, 1, button) },
                "Button Panel", "");
            return RequiredTrigger.Named(part);
        }
    }
}
