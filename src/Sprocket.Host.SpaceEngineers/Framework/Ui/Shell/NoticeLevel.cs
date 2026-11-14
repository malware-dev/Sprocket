namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Shell
{
    /// <summary>How much a remark matters, on a line or on the banner over a screen.</summary>
    public enum NoticeLevel
    {
        /// <summary>Worth reading, nothing is wrong: a note, in the words' own ink.</summary>
        Info,

        /// <summary>Worth acting on, nothing is broken: a warning, in amber.</summary>
        Warning,

        /// <summary>Something is wrong and the user has to know about it: a problem, in red.</summary>
        Problem
    }
}
