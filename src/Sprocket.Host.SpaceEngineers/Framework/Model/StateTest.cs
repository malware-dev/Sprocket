using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A test of one of the logic system's states against a value or another state: <c>where == inside</c>, <c>where != home</c>.</summary>
    public sealed class StateTest : BranchTest
    {
        /// <summary>The name of the state tested.</summary>
        public string State { get; set; } = "";

        /// <summary>How the state is compared: equal to, or not.</summary>
        public Relation Relation { get; set; }

        /// <summary>What it is compared with where <see cref="Other" /> is empty: a flag, or the name of a choice.</summary>
        public Literal Value { get; set; }

        /// <summary>The name of the state it is compared with, or empty where it is compared with <see cref="Value" />.</summary>
        public string Other { get; set; } = "";

        /// <summary>A copy of this test, to change one thing of as an edit.</summary>
        public StateTest Copy()
        {
            return new StateTest { State = State, Relation = Relation, Value = Value, Other = Other };
        }
    }
}
