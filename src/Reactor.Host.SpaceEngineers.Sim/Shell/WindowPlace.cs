using System;
using System.IO;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;

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

            var at = new PixelPoint(saved[0], saved[1]);
            if (OnAScreen(window, at, saved[2], saved[3]))
            {
                // Manual placement, or the window manager centres it and the position is overwritten by
                // the thing that was supposed to be the fallback.
                window.WindowStartupLocation = WindowStartupLocation.Manual;
                window.Position = at;
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

                // A maximised window's own position is the whole screen, which is not what anybody wants
                // restored when they un-maximise it. Avalonia does not keep the pre-maximise rectangle, so
                // this keeps the last size it saw as a normal window instead.
                int[] previous = Read();
                PixelPoint at = maximised && previous != null
                    ? new PixelPoint(previous[0], previous[1])
                    : window.Position;

                double width = maximised && previous != null ? previous[2] : window.Width;
                double height = maximised && previous != null ? previous[3] : window.Height;

                string path = File;
                Directory.CreateDirectory(Path.GetDirectoryName(path));

                System.IO.File.WriteAllText(path, string.Join(" ", new[]
                {
                    at.X.ToString(CultureInfo.InvariantCulture),
                    at.Y.ToString(CultureInfo.InvariantCulture),
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

        /// <summary>Whether enough of the window would land somewhere a person could see it.</summary>
        static bool OnAScreen(Window window, PixelPoint at, int width, int height)
        {
            if (window.Screens == null || window.Screens.ScreenCount == 0)
                return false;

            var screen = window.Screens.ScreenFromPoint(at);
            double density = screen != null ? screen.Scaling : 1d;

            // Position is in physical pixels and the saved size is in the window's own units, so one of
            // the two has to be converted before they can be compared.
            var box = new PixelRect(at,
                new PixelSize((int)(width * density), (int)(height * density)));

            for (int i = 0; i < window.Screens.All.Count; i++)
            {
                PixelRect visible = window.Screens.All[i].WorkingArea.Intersect(box);
                if (visible.Width >= MustBeVisible && visible.Height >= MustBeVisible)
                    return true;
            }

            return false;
        }
    }
}
