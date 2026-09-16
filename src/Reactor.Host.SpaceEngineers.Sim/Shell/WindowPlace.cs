using System;
using System.IO;
using System.Globalization;
using System.Windows;

namespace Reactor.Sim.Shell
{
    /// <summary>
    /// Remembers where the window was and puts it back there next time.
    /// </summary>
    /// <remarks>
    /// Kept beside the extracted content in <c>%LOCALAPPDATA%\Reactor</c> rather than in the repository,
    /// for the same reason that is: where somebody likes their window is a property of their desk, not of
    /// the project, and two people working on this would only fight over the file.
    /// <para>
    /// Everything here fails quietly. A window that will not open because a preferences file is malformed
    /// is a worse tool than one that forgets where it was, and there is nothing in here worth an error
    /// message.
    /// </para>
    /// </remarks>
    public static class WindowPlace
    {
        /// <summary>
        /// How much of the window has to land on a screen for the remembered place to be used.
        /// </summary>
        /// <remarks>
        /// Monitors come and go - a laptop leaves its dock, somebody unplugs the second screen - and a
        /// window restored onto one that is no longer there is a window nobody can reach. Checking that a
        /// decent corner of it is visible is cheaper than being clever about which screen it was on.
        /// </remarks>
        const int MustBeVisible = 120;

        static string File
        {
            get
            {
                string root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return Path.Combine(root, "Reactor", "SimWindow.txt");
            }
        }

        /// <summary>Puts <paramref name="window" /> back where it was, if that is still a place it can go.</summary>
        public static void Restore(Window window)
        {
            int[] saved = Read();
            if (saved == null)
                return;

            if (saved[2] > 0 && saved[3] > 0)
            {
                window.Width = saved[2];
                window.Height = saved[3];
            }

            if (OnAScreen(saved[0], saved[1], saved[2], saved[3]))
            {
                // Manual placement, or the window manager centres it and the position is overwritten by
                // the thing that was supposed to be the fallback.
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Left = saved[0];
                window.Top = saved[1];
            }

            if (saved[4] != 0)
                window.WindowState = WindowState.Maximized;
        }

        /// <summary>Writes down where <paramref name="window" /> is now.</summary>
        public static void Remember(Window window)
        {
            try
            {
                bool maximised = window.WindowState == WindowState.Maximized;

                // A maximised window's own position is the whole screen, which is not what anybody
                // wants restored when they un-maximise it. The framework keeps a restore rectangle but
                // only while the window lives, so this keeps the last one it saw as a normal window.
                int[] previous = Read();
                bool kept = maximised && previous != null;

                int left = kept ? previous[0] : (int)window.Left;
                int top = kept ? previous[1] : (int)window.Top;

                double width = kept ? previous[2] : window.Width;
                double height = kept ? previous[3] : window.Height;

                string path = File;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                System.IO.File.WriteAllText(path, string.Join(" ", new[]
                {
                    left.ToString(CultureInfo.InvariantCulture),
                    top.ToString(CultureInfo.InvariantCulture),
                    ((int)width).ToString(CultureInfo.InvariantCulture),
                    ((int)height).ToString(CultureInfo.InvariantCulture),
                    maximised ? "1" : "0"
                }));
            }
            catch (Exception)
            {
                // Nothing to say and nothing to do. See the note on the class.
            }
        }

        /// <summary>The five numbers, or null where there are not five numbers to be had.</summary>
        static int[] Read()
        {
            try
            {
                string path = File;
                if (!System.IO.File.Exists(path))
                    return null;

                string[] words = System.IO.File.ReadAllText(path)
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (words.Length != 5)
                    return null;

                var numbers = new int[5];
                for (int i = 0; i < 5; i++)
                {
                    if (!int.TryParse(words[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out numbers[i]))
                        return null;
                }

                return numbers;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Whether enough of the window would land somewhere a person could see it.
        /// </summary>
        /// <remarks>
        /// Against the whole virtual desktop rather than each monitor in turn, because that is what the
        /// framework offers without reaching for the platform: a rectangle covering every screen. It says
        /// yes to a position over a gap between two monitors, which is a corner nobody has hit, and no to
        /// the one that matters - a window last closed on a monitor that is no longer there.
        /// </remarks>
        static bool OnAScreen(int left, int top, int width, int height)
        {
            var box = new Rect(left, top, width, height);
            var desktop = new Rect(
                SystemParameters.VirtualScreenLeft, SystemParameters.VirtualScreenTop,
                SystemParameters.VirtualScreenWidth, SystemParameters.VirtualScreenHeight);

            box.Intersect(desktop);
            return box.Width >= MustBeVisible && box.Height >= MustBeVisible;
        }
    }
}
