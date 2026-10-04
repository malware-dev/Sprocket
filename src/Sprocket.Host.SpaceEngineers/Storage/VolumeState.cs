namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>How a line of the library's index turned out.</summary>
    public enum VolumeState
    {
        /// <summary>The file read as a blueprint.</summary>
        Read,

        /// <summary>No file has the name the line gives.</summary>
        Missing,

        /// <summary>The line names a folder as well as a file, which the library refuses.</summary>
        Path,

        /// <summary>The file is there and does not read as one blueprint.</summary>
        Unreadable,

        /// <summary>The file holds a blueprint an earlier line already gave.</summary>
        Twice
    }
}
