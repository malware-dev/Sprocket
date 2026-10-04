namespace Lumel.Input
{
    /// <summary>Where copied text is kept, for whatever pastes it next.</summary>
    /// <remarks>Not the operating system's clipboard, which the mod API does not offer; the host decides where it is.</remarks>
    public interface IClipboard
    {
        /// <summary>The copied text, or empty when nothing has been copied.</summary>
        string Get();

        /// <summary>Replaces the copied text with <paramref name="text" />.</summary>
        void Set(string text);
    }
}
