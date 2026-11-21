using Sprocket.Host.SpaceEngineers.Framework.Game.Members;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>A test of one of the logic system's states, or one of its settings, against a value or a state: <c>where == inside</c>, <c>where != home</c>, <c>'Outer light' == on</c>.</summary>
    public sealed class StateTest : BranchTest
    {
        /// <summary>The name of the state tested, or empty where a setting is.</summary>
        public string State { get; set; } = "";

        /// <summary>The name of the setting tested in place of a state, its value in each installation, or empty where a state is: <c>'Outer light' == on</c>.</summary>
        public string Setting { get; set; } = "";

        /// <summary>How the state is compared: equal to, or not.</summary>
        public Relation Relation { get; set; }

        /// <summary>
        /// What it is compared with where <see cref="Other" /> is empty: a flag, or the name of a choice, or for a setting a value
        /// of its type; or a setting standing in for one.
        /// </summary>
        public Literal Value { get; set; }

        /// <summary>The name of the state it is compared with, or empty where it is compared with <see cref="Value" />.</summary>
        public string Other { get; set; } = "";

        /// <summary>A copy of this test, to change one thing of as an edit.</summary>
        public StateTest Copy()
        {
            return new StateTest { State = State, Setting = Setting, Relation = Relation, Value = Value, Other = Other };
        }
    }
}
