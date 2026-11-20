using System;
using System.Collections.Generic;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Players
{
    /// <summary>What the player at this machine has chosen for themselves, for every world they play: whether the screens show tips, and which callouts they have dismissed.</summary>
    /// <remarks>Stored in a section of its own in the global file on the player's own machine, read there and never sent to a server.</remarks>
    public sealed class PlayerSettings
    {
        // Persisted: the section of the global file the player's own choices are stored in, and the key of each.
        const string Section = "Player";

        const string TipsKey = "tips";

        // The number of the highest release whose callouts have all been dismissed.
        const string SeenKey = "seen";

        // The ids of the callouts the player has dismissed, separated by commas.
        const string DismissedKey = "dismissed";

        const char Separator = ',';

        static readonly string[] NoIds = new string[0];

        readonly string[] _dismissed;

        /// <summary>The player's choices: tips shown where <paramref name="tips" /> says, callouts dismissed up to release <paramref name="seen" /> where it has a value, and those named by <paramref name="dismissed" /> dismissed.</summary>
        public PlayerSettings(bool tips, int? seen = null, IEnumerable<string> dismissed = null)
        {
            Tips = tips;
            Seen = seen;
            _dismissed = dismissed == null ? NoIds : new List<string>(dismissed).ToArray();
        }

        /// <summary>Whether the screens show tips, and terms look like terms; on until the player turns them off.</summary>
        public bool Tips { get; }

        /// <summary>The number of the highest release whose callouts have all been dismissed, or null where none has.</summary>
        public int? Seen { get; }

        /// <summary>The ids of the callouts the player has dismissed.</summary>
        public IEnumerable<string> Dismissed => _dismissed;

        /// <summary>What <paramref name="ini" /> says, or every default where it says nothing or is null.</summary>
        public static PlayerSettings From(MyIni ini)
        {
            bool tips;
            if (ini == null || !ini.Get(Section, TipsKey).TryGetBoolean(out tips))
                tips = true;

            int? seen = null;
            int number;
            if (ini != null && ini.Get(Section, SeenKey).TryGetInt32(out number))
                seen = number;

            var dismissed = NoIds;
            if (ini != null)
                dismissed = ini.Get(Section, DismissedKey).ToString("").Split(new[] { Separator }, StringSplitOptions.RemoveEmptyEntries);

            return new PlayerSettings(tips, seen, dismissed);
        }

        /// <summary>What the global file on this machine says.</summary>
        public static PlayerSettings Read()
        {
            return From(Settings.GlobalFile());
        }

        /// <summary>These choices with tips shown where <paramref name="on" /> says.</summary>
        public PlayerSettings WithTips(bool on)
        {
            return new PlayerSettings(on, Seen, _dismissed);
        }

        /// <summary>These choices with the callouts of every release up to <paramref name="release" /> dismissed.</summary>
        public PlayerSettings WithSeen(int release)
        {
            return new PlayerSettings(Tips, release, _dismissed);
        }

        /// <summary>Whether the callout <paramref name="id" /> has been dismissed.</summary>
        public bool HasDismissed(string id)
        {
            return Array.IndexOf(_dismissed, id) >= 0;
        }

        /// <summary>These choices with the callout <paramref name="id" /> dismissed.</summary>
        public PlayerSettings WithDismissed(string id)
        {
            if (HasDismissed(id))
                return this;

            var more = new List<string>(_dismissed) { id };
            return new PlayerSettings(Tips, Seen, more);
        }

        /// <summary>Writes these choices into <paramref name="ini" />, leaving every other section as it was.</summary>
        public void Write(MyIni ini)
        {
            ini.Set(Section, TipsKey, Tips);

            if (Seen.HasValue)
                ini.Set(Section, SeenKey, Seen.Value);
            else
                ini.Delete(Section, SeenKey);

            if (_dismissed.Length > 0)
                ini.Set(Section, DismissedKey, string.Join(Separator.ToString(), _dismissed));
            else
                ini.Delete(Section, DismissedKey);
        }

        /// <summary>Saves these choices in the global file on this machine, leaving the rest of the file as it was.</summary>
        public void Save()
        {
            var ini = Settings.GlobalFile() ?? new MyIni();
            Write(ini);
            Settings.SaveGlobal(ini);
        }
    }
}
