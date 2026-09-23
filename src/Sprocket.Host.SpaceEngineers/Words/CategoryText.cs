using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Words
{
    /// <summary>What each category is headed on screen.</summary>
    /// <remarks>
    /// A plain noun for the heading over a group of rows, not a sentence: the rows underneath say
    /// what they do, and a heading that explained itself would be read once and then be in the way.
    /// </remarks>
    public static class CategoryText
    {
        public static string Of(Category category)
        {
            switch (category)
            {
                case Category.Power: return "Power";
                case Category.Movement: return "Movement";
                case Category.Opening: return "Opening";
                case Category.Locking: return "Locking";
                case Category.Flight: return "Flight";
                case Category.Stock: return "Cargo";
                case Category.Air: return "Air";
                case Category.Making: return "Production";
                case Category.Weapons: return "Weapons";
                case Category.Detection: return "Detection";
                case Category.Automation: return "Automation";
                case Category.Behaviour: return "Behaviour";
                case Category.Signals: return "Signals";
                case Category.Appearance: return "Appearance";
                case Category.Screen: return "Screens";
                case Category.Sound: return "Sound";
                case Category.Identity: return "Identity";
                case Category.Access: return "Access";
                default: return "Other";
            }
        }
    }
}
