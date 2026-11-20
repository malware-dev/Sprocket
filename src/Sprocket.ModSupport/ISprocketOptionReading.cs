namespace Sprocket.ModSupport
{
    /// <summary>An option that turns what Sprocket gives for it into what the mod's callback takes.</summary>
    /// <typeparam name="T">What the mod's callback is given for the option.</typeparam>
    /// <remarks>The wrapper's own plumbing, not for mods: a mod reaches it only by casting.</remarks>
    public interface ISprocketOptionReading<out T>
    {
        /// <summary>The value Sprocket gives for the option, as the mod's callback takes it.</summary>
        T Read(object given);
    }
}
