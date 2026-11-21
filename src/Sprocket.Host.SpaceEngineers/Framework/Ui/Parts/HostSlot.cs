using Lumel.Input;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Parts
{
    /// <summary>What an if tests of the host: one of its namespaces, and whether the host has it or lacks it, "gearbox.lifts is installed".</summary>
    public sealed class HostSlot : SentenceSlot
    {
        /// <summary>The part saying <paramref name="test" />, opening the question it was answered with through <paramref name="opens" />, or not pressed for null.</summary>
        public HostSlot(HostTest test, ICommand opens) : base(opens)
        {
            Test = test;
        }

        /// <summary>The test the part says.</summary>
        public HostTest Test { get; private set; }

        /// <summary>Whether it names no namespace, which is a test that cannot hold.</summary>
        public override bool IsEmpty => Test.Namespace.Length == 0;
    }
}
