using System;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Copied text kept in a file, so a player can paste text in from outside the game and copy it back out.</summary>
    /// <remarks>Read on every paste and written on every copy.</remarks>
    public sealed class ClipboardFile : IClipboard
    {
        const string File = "sprocket-clipboard.txt";

        readonly IFiles _files;

        readonly Action<string> _say;

        readonly Action _pasted;

        string _held = string.Empty;

        /// <summary>The clipboard file in <paramref name="files" />, saying through <paramref name="say" /> what would not open or write, and telling <paramref name="pasted" /> of each paste.</summary>
        public ClipboardFile(IFiles files, Action<string> say, Action pasted)
        {
            _files = files;
            _say = say;
            _pasted = pasted;
        }

        /// <inheritdoc />
        /// <remarks>Where the file will not open, what was last copied this session.</remarks>
        public string Get()
        {
            if (_pasted != null)
                _pasted();

            try
            {
                if (_files.Has(File))
                    return _files.Read(File);
            }
            catch (Exception failed)
            {
                _say($"{File} will not open - {failed.Message}");
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
                _say($"{File} will not write - {failed.Message}");
            }
        }
    }
}
