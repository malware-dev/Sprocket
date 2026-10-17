using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    // What the mod has said, kept for the harness, which has no game log to read it from. A .debug.cs file, compiled into
    // local builds only and never packed for the game.
    public static partial class GameLog
    {
        /// <summary>Every line said since the process began, without the prefix the game's log gives it.</summary>
        public static readonly List<string> Lines = new List<string>();

        static partial void Heard(string what)
        {
            Lines.Add(what);
        }
    }
}
