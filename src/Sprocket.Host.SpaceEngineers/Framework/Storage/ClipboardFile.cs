using System;
using Lumel.Input;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>Copied text stored in a file, so a player can paste text in from outside the game and copy it back out.</summary>
    /// <remarks>
    /// Read on every paste and written on every copy. A copy also reaches the computer's own clipboard where one is given;
    /// a paste comes only from the file, since a mod cannot read that clipboard.
    /// </remarks>
    public sealed class ClipboardFile : IClipboard
    {
        const string File = "sprocket-clipboard.txt";

        readonly IFiles _files;

        readonly Action<string> _say;

        readonly Action _pasted;

        readonly Action<string> _system;

        string _held = string.Empty;

        /// <summary>
        /// The clipboard file in <paramref name="files" />, saying through <paramref name="say" /> what would not open or write,
        /// telling <paramref name="pasted" /> of each paste, and giving each copy to <paramref name="system" />, the computer's
        /// own clipboard, where it is not null.
        /// </summary>
        public ClipboardFile(IFiles files, Action<string> say, Action pasted, Action<string> system = null)
        {
            _files = files;
            _say = say;
            _pasted = pasted;
            _system = system;
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

            if (_system == null)
                return;

            try
            {
                _system(_held);
            }
            catch (Exception failed)
            {
                _say($"the clipboard will not take the copy - {failed.Message}");
            }
        }
    }
}
