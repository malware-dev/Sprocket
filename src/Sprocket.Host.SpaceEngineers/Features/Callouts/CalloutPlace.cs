namespace Sprocket.Host.SpaceEngineers.Features.Callouts
{
    /// <summary>A place on a screen a callout can hang from.</summary>
    public enum CalloutPlace
    {
        /// <summary>The Share mark in the editor's header, on a page with something to share.</summary>
        HeaderShare,

        /// <summary>The Share mark on a logic system's row on the home page.</summary>
        HomeShare,

        /// <summary>The Share mark on the details of the blueprint chosen in the library.</summary>
        LibraryShare,

        /// <summary>The Import mark beside the library's search.</summary>
        LibraryImport,

        /// <summary>The plus that adds a slot, in a logic system's installation panel.</summary>
        SlotAdd
    }
}
