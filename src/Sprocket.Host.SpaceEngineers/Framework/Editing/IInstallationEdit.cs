using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Editing
{
    /// <summary>A change to one installation of a logic system: its blocks or its name.</summary>
    public interface IInstallationEdit
    {
        /// <summary>The installation changed.</summary>
        SystemInstance Instance { get; }
    }
}
