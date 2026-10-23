namespace Lumel.Windows
{
    /// <summary>Which side of what it hangs from a popup opens on.</summary>
    public enum PopupSide
    {
        /// <summary>Under it where all of it fits there, otherwise over it where all of it fits there, otherwise on the side with more room, cut to it.</summary>
        Below,

        /// <summary>To its right with the first entry level with it, or to its left where there is no room to the right.</summary>
        Beside
    }
}
