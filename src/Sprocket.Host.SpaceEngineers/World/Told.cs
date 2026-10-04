using VRage.Utils;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// Says, in the game's own log, what was made of what somebody wrote.
    /// </summary>
    /// <remarks>
    /// A description Sprocket cannot use is worse than no description: the mod author sees their
    /// names missing from a screen and has nothing to go on, and the player sees a form that asks
    /// for three numbers when it could have asked for a colour. So every one that is refused says
    /// which mod it came from and what was wrong with it.
    /// <para>
    /// The log rather than the screen, because it is a mistake in a file somebody ships and not a
    /// thing a player can mend. It happens once as a world comes up.
    /// </para>
    /// </remarks>
    public static partial class Told
    {
        const string Ours = "[Sprocket] ";

        public static void Say(string what)
        {
            Heard(what);

            if (MyLog.Default != null)
                MyLog.Default.WriteLineAndConsole(Ours + what);
        }

        /// <summary>What one mod's file got wrong, named so its author can find it.</summary>
        public static void Say(string mod, string what)
        {
            Say(string.IsNullOrEmpty(mod) ? what : mod + ": " + what);
        }

        /// <summary>Every line as it is said, for a local build to keep.</summary>
        /// <remarks>
        /// Without a body in anything packed for the game, where the call compiles to nothing. The
        /// harness has no game log, so <c>Told.debug.cs</c> gives it one that keeps what was said.
        /// </remarks>
        static partial void Heard(string what);
    }
}
