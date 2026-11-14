using System.IO;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>The game's global mod storage: the one <c>Storage</c> folder every mod shares, on the player's own machine.</summary>
    /// <remarks>Shared with every other mod, so every name Sprocket gives a file there starts <c>sprocket</c>.</remarks>
    public sealed class GlobalFiles : IFiles
    {
        /// <inheritdoc />
        public bool Has(string name)
        {
            return MyAPIGateway.Utilities.FileExistsInGlobalStorage(name);
        }

        /// <inheritdoc />
        public string Read(string name)
        {
            using (TextReader reader = MyAPIGateway.Utilities.ReadFileInGlobalStorage(name))
                return reader.ReadToEnd();
        }

        /// <inheritdoc />
        public void Write(string name, string text)
        {
            using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInGlobalStorage(name))
                writer.Write(text);
        }

        /// <inheritdoc />
        public void Delete(string name)
        {
            MyAPIGateway.Utilities.DeleteFileInGlobalStorage(name);
        }
    }
}
