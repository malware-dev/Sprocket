using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Files kept in the world's save, packed together as one text for the world's carrier to hold.</summary>
    /// <remarks>
    /// The game saves only the storage keys a mod has claimed in advance, so every file shares the carrier's one key. The
    /// text is a line saying what it is, then each file as a line <c>= name</c> followed by its lines; a line of a file
    /// that starts with <c>=</c> is written with one more <c>=</c> in front, so no line of a file starts a file:
    /// <code>
    /// files 1
    /// = state
    /// running 3
    /// == a line that started with =
    /// = another
    /// </code>
    /// <para>
    /// The game holds the whole text from the moment the world loads, so the files are unpacked once, as it starts, and
    /// packed again only for a save that follows a change. Where they will not read, nothing is written over them.
    /// </para>
    /// </remarks>
    public sealed class CarrierFiles : IFiles
    {
        const string Head = "files 1";

        const char Mark = '=';

        readonly Dictionary<string, string> _files = new Dictionary<string, string>();

        /// <summary>Whether what was unpacked was read whole; where it was not, the files are never packed over it.</summary>
        public bool Intact { get; private set; } = true;

        /// <summary>Whether a file has been written or deleted since the files were last unpacked or packed.</summary>
        public bool Changed { get; private set; }

        /// <summary>How many files there are.</summary>
        public int Count => _files.Count;

        /// <inheritdoc />
        public bool Has(string name)
        {
            return _files.ContainsKey(name);
        }

        /// <inheritdoc />
        public string Read(string name)
        {
            string text;
            if (!_files.TryGetValue(name, out text))
                throw new FileNotFoundException(name);

            return text;
        }

        /// <inheritdoc />
        public void Write(string name, string text)
        {
            _files[name] = text;
            Changed = true;
        }

        /// <inheritdoc />
        public void Delete(string name)
        {
            if (_files.Remove(name))
                Changed = true;
        }

        /// <summary>Takes the files packed in <paramref name="text" />, answering what was wrong with it, or empty.</summary>
        /// <remarks>Whole or not at all: where any of it will not read, no file is taken and none is ever packed over it.</remarks>
        public string Unpack(string text)
        {
            _files.Clear();
            Changed = false;
            Intact = true;
            if (string.IsNullOrEmpty(text))
                return "";

            var lines = text.Split('\n');
            if (lines[0] != Head)
            {
                Intact = false;
                return "the carrier's files do not start with \"" + Head + "\"";
            }

            // A packed text ends every line with a newline, so the last piece is empty and stands for nothing.
            var last = lines.Length - 1;
            string name = null;
            var file = new StringBuilder();
            var first = true;
            for (var i = 1; i < last; i++)
            {
                var line = lines[i];
                if (line.Length >= 2 && line[0] == Mark && line[1] == ' ')
                {
                    if (name != null)
                        _files[name] = file.ToString();

                    name = line.Substring(2);
                    file.Clear();
                    first = true;
                    continue;
                }

                if (name == null)
                {
                    _files.Clear();
                    Intact = false;
                    return "a line of the carrier's files is in no file: " + line;
                }

                if (!first)
                    file.Append('\n');

                var skip = line.Length > 0 && line[0] == Mark ? 1 : 0;
                file.Append(line, skip, line.Length - skip);
                first = false;
            }

            if (name != null)
                _files[name] = file.ToString();

            return "";
        }

        /// <summary>The files packed as one text, for the carrier to hold; marks them unchanged.</summary>
        public string Pack()
        {
            var text = new StringBuilder(Head).Append('\n');
            foreach (var pair in _files)
            {
                text.Append(Mark).Append(' ').Append(pair.Key).Append('\n');
                var file = pair.Value;
                var at = 0;
                while (true)
                {
                    var end = file.IndexOf('\n', at);
                    if (end < 0)
                        end = file.Length;

                    if (end > at && file[at] == Mark)
                        text.Append(Mark);

                    text.Append(file, at, end - at).Append('\n');
                    if (end == file.Length)
                        break;

                    at = end + 1;
                }
            }

            Changed = false;
            return text.ToString();
        }
    }
}
