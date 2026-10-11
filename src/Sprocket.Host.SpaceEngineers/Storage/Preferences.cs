using System;
using VRage.Game.ModAPI.Ingame.Utilities;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>How the player likes Sprocket's screens to look, the same in every world and kept from one game to the next.</summary>
    /// <remarks>
    /// Kept in a file of its own, apart from the settings a player writes by hand, because Sprocket writes this one each
    /// time something in it changes:
    /// <code>
    /// [View]
    /// showHidden=true
    /// </code>
    /// Read once, as the world starts. A file that will not read is said out loud, leaves every preference at its default,
    /// and is never written over.
    /// </remarks>
    public sealed class Preferences
    {
        const string File = "sprocket-view.ini";

        const string Section = "View";

        const string HiddenKey = "showHidden";

        readonly IFiles _files;

        readonly Action<string> _say;

        bool _read;

        bool _intact = true;

        bool _showsHidden;

        /// <summary>Preferences kept in <paramref name="files" />, saying through <paramref name="say" /> what would not read or write.</summary>
        public Preferences(IFiles files, Action<string> say)
        {
            _files = files;
            _say = say;
        }

        /// <summary>Whether the pickers list the blocks hidden from toolbars: their eye open.</summary>
        public bool ShowsHidden
        {
            get { return _showsHidden; }
            set
            {
                if (_showsHidden == value)
                    return;

                _showsHidden = value;
                Write();
            }
        }

        /// <summary>Reads the preferences out of their file, where there is one.</summary>
        public void Read()
        {
            _read = true;
            _intact = true;

            string text;
            try
            {
                if (!_files.Has(File))
                    return;

                text = _files.Read(File);
            }
            catch (Exception failed)
            {
                _intact = false;
                _say(File + " will not open - " + failed.Message);
                return;
            }

            var ini = new MyIni();
            MyIniParseResult parsed;
            if (!ini.TryParse(text, out parsed))
            {
                _intact = false;
                _say(File + " has a problem on line " + parsed.LineNo + " - " + parsed.Error);
                return;
            }

            _showsHidden = ini.Get(Section, HiddenKey).ToBoolean();
        }

        void Write()
        {
            if (!_read || !_intact)
                return;

            var ini = new MyIni();
            ini.Set(Section, HiddenKey, _showsHidden);
            try
            {
                _files.Write(File, ini.ToString());
            }
            catch (Exception failed)
            {
                _say(File + " will not write - " + failed.Message);
            }
        }
    }
}
