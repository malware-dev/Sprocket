using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Picking
{
    /// <summary>What each category is headed on screen.</summary>
    /// <remarks>
    /// A plain noun for the heading over a group of rows, not a sentence: the rows underneath say
    /// what they do, and a heading that explained itself would be read once and then be in the way.
    /// </remarks>
    public static class CategoryText
    {
        /// <summary>What <paramref name="category" /> is headed, in the words <paramref name="texts" /> give.</summary>
        public static string Of(Texts texts, Category category)
        {
            switch (category)
            {
                case Category.Power: return texts.CategoryPower;
                case Category.Movement: return texts.CategoryMovement;
                case Category.Opening: return texts.CategoryOpening;
                case Category.Locking: return texts.CategoryLocking;
                case Category.Flight: return texts.CategoryFlight;
                case Category.Stock: return texts.CategoryCargo;
                case Category.Air: return texts.CategoryAir;
                case Category.LifeSupport: return texts.CategoryLifeSupport;
                case Category.Gas: return texts.CategoryGas;
                case Category.Fuel: return texts.CategoryFuel;
                case Category.Production: return texts.CategoryProduction;
                case Category.Repair: return texts.CategoryRepair;
                case Category.Weapons: return texts.CategoryWeapons;
                case Category.Detection: return texts.CategoryDetection;
                case Category.Automation: return texts.CategoryAutomation;
                case Category.Behaviour: return texts.CategoryBehavior;
                case Category.Signals: return texts.CategorySignals;
                case Category.Appearance: return texts.CategoryAppearance;
                case Category.Screen: return texts.CategoryScreens;
                case Category.Sound: return texts.CategorySound;
                case Category.Identity: return texts.CategoryIdentity;
                case Category.State: return texts.CategoryState;
                case Category.Usage: return texts.CategoryUsage;
                case Category.Access: return texts.CategoryAccess;
                default: return texts.CategoryOther;
            }
        }
    }
}
