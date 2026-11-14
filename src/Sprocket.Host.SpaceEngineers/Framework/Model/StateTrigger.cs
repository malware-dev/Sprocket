namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A trigger started as a step sets one of the logic system's states so that a test of it comes to hold: <c>when where == outside</c>.</summary>
    public sealed class StateTrigger : Trigger
    {
        /// <summary>A trigger started as <paramref name="test" /> comes to hold.</summary>
        public StateTrigger(StateTest test)
        {
            Test = test;
        }

        /// <summary>The test that starts it as it comes to hold.</summary>
        public StateTest Test { get; set; }
    }
}
