namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>How loudly an abort says why the sequence ended.</summary>
    public enum AbortLevel
    {
        /// <summary>A quiet note.</summary>
        Note,

        /// <summary>A warning, in the warnings' look.</summary>
        Warning,

        /// <summary>A problem, which also switches off the installation the sequence ran on.</summary>
        Problem
    }
}
