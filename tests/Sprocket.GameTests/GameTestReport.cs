using System;
using System.IO;

namespace Sprocket.GameTests
{
    /// <summary>The results of one run of game tests, written line by line to a file as each check is answered.</summary>
    public sealed class GameTestReport
    {
        readonly string _path;
        int _passed;
        int _failed;

        /// <summary>A report written to <paramref name="path" />, emptied first.</summary>
        public GameTestReport(string path)
        {
            _path = path;
            File.WriteAllText(_path, $"started {DateTime.Now:yyyy-MM-dd HH:mm:ss}{Environment.NewLine}");
        }

        /// <summary>Whether every check so far passed.</summary>
        public bool IsClean => _failed == 0;

        /// <summary>Records that <paramref name="check" /> passed, with <paramref name="detail" />.</summary>
        public void Pass(string check, string detail)
        {
            _passed++;
            Write($"PASS {check}: {detail}");
        }

        /// <summary>Records that <paramref name="check" /> failed, with <paramref name="detail" />.</summary>
        public void Fail(string check, string detail)
        {
            _failed++;
            Write($"FAIL {check}: {detail}");
        }

        /// <summary>Records <paramref name="check" /> as passed where <paramref name="passed" /> is true, and failed otherwise.</summary>
        public void Check(string check, bool passed, string detail)
        {
            if (passed)
                Pass(check, detail);
            else
                Fail(check, detail);
        }

        /// <summary>Records a line that is no verdict, such as what a check found on the way.</summary>
        public void Note(string text)
        {
            Write($"NOTE {text}");
        }

        /// <summary>Writes the totals as the last line.</summary>
        public void Finish()
        {
            Write($"finished {DateTime.Now:yyyy-MM-dd HH:mm:ss}: {_passed} passed, {_failed} failed");
        }

        void Write(string line)
        {
            File.AppendAllText(_path, line + Environment.NewLine);
        }
    }
}
