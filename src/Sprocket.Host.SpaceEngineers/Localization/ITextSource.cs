
namespace Sprocket.Host.SpaceEngineers.Localization
{
    /// <summary>Where the texts in the player's language are read from.</summary>
    public interface ITextSource
    {
        /// <summary>The text keyed <paramref name="key" />, or null where there is none.</summary>
        string Find(string key);
    }
}
