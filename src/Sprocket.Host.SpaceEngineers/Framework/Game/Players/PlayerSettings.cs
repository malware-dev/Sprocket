using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Players
{
    /// <summary>What the player at this machine has chosen for themselves, for every world they play: whether the screens show tips.</summary>
    /// <remarks>Kept in a section of its own in the global file on the player's own machine, read there and never sent to a server.</remarks>
    public sealed class PlayerSettings
    {
        // Persisted: the section of the global file the player's own choices are kept in, and the key of each.
        const string Section = "Player";

        const string TipsKey = "tips";

        /// <summary>The player's choices: tips shown where <paramref name="tips" /> says.</summary>
        public PlayerSettings(bool tips)
        {
            Tips = tips;
        }

        /// <summary>Whether the screens show tips, and terms look like terms; on until the player turns them off.</summary>
        public bool Tips { get; }

        /// <summary>What <paramref name="ini" /> says, or every default where it says nothing or is null.</summary>
        public static PlayerSettings From(MyIni ini)
        {
            bool tips;
            if (ini == null || !ini.Get(Section, TipsKey).TryGetBoolean(out tips))
                tips = true;

            return new PlayerSettings(tips);
        }

        /// <summary>What the global file on this machine says.</summary>
        public static PlayerSettings Read()
        {
            return From(Settings.GlobalFile());
        }

        /// <summary>These choices with tips shown where <paramref name="on" /> says.</summary>
        public PlayerSettings WithTips(bool on)
        {
            return new PlayerSettings(on);
        }

        /// <summary>Writes these choices into <paramref name="ini" />, leaving every other section as it was.</summary>
        public void Write(MyIni ini)
        {
            ini.Set(Section, TipsKey, Tips);
        }

        /// <summary>Keeps these choices in the global file on this machine, leaving the rest of the file as it was.</summary>
        public void Keep()
        {
            var ini = Settings.GlobalFile() ?? new MyIni();
            Write(ini);
            Settings.KeepGlobal(ini);
        }
    }
}
