
namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// How a reading is measured against something.
    /// </summary>
    public enum Relation
    {
        /// <summary>Equal to.</summary>
        Is,
        /// <summary>Not equal to.</summary>
        IsNot,
        /// <summary>Greater than.</summary>
        Above,
        /// <summary>Greater than or equal to.</summary>
        AtLeast,
        /// <summary>Less than.</summary>
        Below,
        /// <summary>Less than or equal to.</summary>
        AtMost
    }
}
