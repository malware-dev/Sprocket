namespace Sprocket.Host.SpaceEngineers.Framework.Running
{
    /// <summary>Why a run that was asked for did not start, as the debug log says it.</summary>
    public enum RunRefusal
    {
        /// <summary>A run of it is already under way.</summary>
        Busy,
        /// <summary>The block Sprocket runs on is unpowered or switched off.</summary>
        Unpowered,
        /// <summary>The sequence, its logic system or its installation is switched off.</summary>
        Off,
        /// <summary>A slot it needs is empty.</summary>
        EmptySlot,
        /// <summary>Another sequence with the same <c>exclusive</c> id is running in the same instance.</summary>
        Held,
        /// <summary>One of its steps needs a namespace the host lacks.</summary>
        Uninstalled
    }
}
