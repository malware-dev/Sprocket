using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Grids
{
    /// <summary>
    /// Says, in the game's own log, what was made of what somebody wrote.
    /// </summary>
    public static partial class GameLog
    {
        const string Ours = "[Sprocket] ";

        /// <summary>Writes <paramref name="what" /> to the game's log, marked as Sprocket's.</summary>
        public static void Say(string what)
        {
            Heard(what);

            if (MyLog.Default != null)
                MyLog.Default.WriteLineAndConsole(Ours + what);
        }

        /// <summary>What one mod's file got wrong, named so its author can find it.</summary>
        public static void Say(string mod, string what)
        {
            Say(string.IsNullOrEmpty(mod) ? what : $"{mod}: {what}");
        }

        /// <summary>Every line as it is said, for a local build to keep.</summary>
        /// <remarks>Has no body in anything packed for the game, where the call compiles to nothing.</remarks>
        static partial void Heard(string what);
    }
}
