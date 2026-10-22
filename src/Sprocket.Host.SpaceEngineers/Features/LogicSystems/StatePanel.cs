using Lumel.Controls;
using Lumel.Layout;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Features.LogicSystems
{
    /// <summary>What a logic system's State tab shows: its types under one heading, its states under another, each with a way to add one.</summary>
    public partial class StatePanel : VStack
    {
        /// <summary>An empty panel.</summary>
        public StatePanel()
        {
            CreateView();
        }

        /// <summary>What the panel says while the logic system keeps nothing; hidden where it says nothing.</summary>
        public string Empty
        {
            set
            {
                _empty.Text = value ?? "";
                _empty.IsVisible = _empty.Text.Length != 0;
            }
        }

        /// <summary>The heading over the types, saying what a type is where the pointer rests on it.</summary>
        public Label TypesHeading => _typesHeading;

        /// <summary>The types, one row each, in order.</summary>
        public CarryList Types => _types;

        /// <summary>The button that adds a type.</summary>
        public Button AddType => _addType;

        /// <summary>The heading over the states, saying what a state is where the pointer rests on it.</summary>
        public Label StatesHeading => _statesHeading;

        /// <summary>The states, one row each, in order.</summary>
        public CarryList States => _states;

        /// <summary>The button that adds a state.</summary>
        public Button AddState => _addState;

        /// <summary>Whether types and states can be added: the buttons are shown only where they can.</summary>
        public bool Adds
        {
            set
            {
                _addTypes.IsVisible = value;
                _addStates.IsVisible = value;
            }
        }
    }
}
