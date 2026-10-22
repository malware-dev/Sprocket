namespace Sprocket.Host.SpaceEngineers.LcdApps.StatusPanel
{
    /// <summary>What an installation on the status panel wants looking at for, the most wanting first.</summary>
    /// <remarks>A problem is broken; everything after it is a warning.</remarks>
    public enum StatusLevel
    {
        /// <summary>One of its sequences is broken, or it was switched off because it could not work.</summary>
        Problem,

        /// <summary>One of its sequences has a warning.</summary>
        Warning,

        /// <summary>Its automation has stopped because none of the construct's big owners owns the programmable block keeping it, until one does.</summary>
        Stranded,

        /// <summary>Its automation was paused when the construct changed hands, until someone resumes it.</summary>
        Paused,

        /// <summary>A player switched it off.</summary>
        Off
    }
}
