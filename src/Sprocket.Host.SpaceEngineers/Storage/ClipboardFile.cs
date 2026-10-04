using System;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Copied text kept in a file, so a player can paste text in from outside the game and copy it back out.</summary>
    /// <remarks>
    /// The game gives a mod no way to the operating system's clipboard, so the file stands in for it. Read on every paste
    /// and written on every copy, each of which a person does by hand.
    /// </remarks>
    public sealed class ClipboardFile : IClipboard
    {
        const string File = "sprocket-clipboard.txt";

        readonly IFiles _files;

        readonly Action<string> _say;

        string _held = string.Empty;

        /// <summary>The clipboard file in <paramref name="files" />, saying through <paramref name="say" /> what would not open or write.</summary>
        public ClipboardFile(IFiles files, Action<string> say)
        {
            _files = files;
            _say = say;
        }

        /// <inheritdoc />
        /// <remarks>Where the file will not open, what was last copied this session.</remarks>
        public string Get()
        {
            try
            {
                if (_files.Has(File))
                    return _files.Read(File);
            }
            catch (Exception failed)
            {
                _say(File + " will not open - " + failed.Message);
            }

            return _held;
        }

        /// <inheritdoc />
        public void Set(string text)
        {
            _held = text ?? string.Empty;
            try
            {
                _files.Write(File, _held);
            }
            catch (Exception failed)
            {
                _say(File + " will not write - " + failed.Message);
            }
        }
    }
}
