namespace Sprocket.Host.SpaceEngineers.Framework.Game.Players
{
    /// <summary>How the player at the screen stands: health, suit energy, oxygen and food, each a share from 0 to 1.</summary>
    public interface IVitals
    {
        /// <summary>Whether the player has a body to read, which the rest needs.</summary>
        bool Present { get; }

        /// <summary>Whether the world has oxygen, so the suit's is worth showing.</summary>
        bool HasOxygen { get; }

        /// <summary>Whether the world has hunger, so food is worth showing.</summary>
        bool HasFood { get; }

        /// <summary>The player's health.</summary>
        float Health { get; }

        /// <summary>The suit's energy.</summary>
        float Energy { get; }

        /// <summary>The suit's oxygen.</summary>
        float Oxygen { get; }

        /// <summary>How fed the player is.</summary>
        float Food { get; }
    }
}
