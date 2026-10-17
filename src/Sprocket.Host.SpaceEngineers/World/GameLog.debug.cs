using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.World
{
    // What the mod has said, kept for the harness, which has no game log to read it from.
    //
    // A .debug.cs file, so this is compiled into local builds only and never packed for the game. A
    // static, which the mod does not have: this one exists only where the harness runs one screen at a
    // time, and the game's own log is a static too.
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
