using System.IO;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>The game's world storage: a folder of the mod's own beside the world's save, on the machine that runs the world.</summary>
    /// <remarks>
    /// Written the moment a file is written, apart from the world's save, so a world loaded again without saving keeps what
    /// was written; and left out of the game's own backups of a world, which copy only the save's top folder.
    /// </remarks>
    public sealed class WorldFiles : IFiles
    {
        /// <inheritdoc />
        public bool Has(string name)
        {
            return MyAPIGateway.Utilities.FileExistsInWorldStorage(name, typeof(WorldFiles));
        }

        /// <inheritdoc />
        public string Read(string name)
        {
            using (TextReader reader = MyAPIGateway.Utilities.ReadFileInWorldStorage(name, typeof(WorldFiles)))
                return reader.ReadToEnd();
        }

        /// <inheritdoc />
        public void Write(string name, string text)
        {
            using (TextWriter writer = MyAPIGateway.Utilities.WriteFileInWorldStorage(name, typeof(WorldFiles)))
                writer.Write(text);
        }

        /// <inheritdoc />
        public void Delete(string name)
        {
            MyAPIGateway.Utilities.DeleteFileInWorldStorage(name, typeof(WorldFiles));
        }
    }
}
