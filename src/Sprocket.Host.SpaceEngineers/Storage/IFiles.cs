namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Files in one folder, known by name, which a player can also open outside the game.</summary>
    /// <remarks>
    /// The game's mod storage offers no listing, so a file is found only by a name already known; a name holds no
    /// folder.
    /// </remarks>
    public interface IFiles
    {
        /// <summary>Whether there is a file called <paramref name="name" />.</summary>
        bool Has(string name);

        /// <summary>The whole of the file <paramref name="name" />; throws where it will not open.</summary>
        string Read(string name);

        /// <summary>Puts <paramref name="text" /> in the file <paramref name="name" />, in place of whatever it held.</summary>
        void Write(string name, string text);

        /// <summary>Deletes the file <paramref name="name" />, where there is one.</summary>
        void Delete(string name);
    }
}
