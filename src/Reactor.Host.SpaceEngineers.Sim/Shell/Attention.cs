using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Reactor.Sim.Shell
{
    /// <summary>
    /// Whether the window is asking to be looked at.
    /// </summary>
    /// <remarks>
    /// A window that takes the keyboard away mid-sentence is an interruption, and most of the times this
    /// is opened it has nothing to say - it is being opened to check something, by somebody who will look
    /// at a screenshot instead. So it can be told to come up behind whatever is in front and stay there,
    /// and told separately, later, to come forward when there is actually something to see.
    /// <para>
    /// Win32 rather than anything Avalonia offers, because "show without taking focus" is a thing the
    /// window manager decides and not the toolkit. This is Windows-only, which the simulator already is.
    /// </para>
    /// </remarks>
    public static class Attention
    {
        const int SwShowNoActivate = 4;

        static readonly IntPtr Bottom = new IntPtr(1);
        static readonly IntPtr TopMost = new IntPtr(-1);
        static readonly IntPtr NoTopMost = new IntPtr(-2);

        const uint NoSize = 0x0001;
        const uint NoMove = 0x0002;
        const uint NoActivate = 0x0010;
        const uint ShowWindowFlag = 0x0040;

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(
            IntPtr window, IntPtr after, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetTopWindow(IntPtr parent);

        [DllImport("user32.dll")]
        static extern IntPtr GetWindow(IntPtr window, uint direction);

        [DllImport("user32.dll")]
        static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        static extern int GetWindowTextLength(IntPtr window);

        const uint Next = 2;

        [DllImport("user32.dll")]
        static extern bool ShowWindowAsync(IntPtr window, int command);

        const int Restore = 9;

        /// <summary>Puts <paramref name="window" /> behind everything, without taking the keyboard.</summary>
        public static void StandBack(IntPtr window)
        {
            if (window == IntPtr.Zero)
                return;

            ShowWindow(window, SwShowNoActivate);
            SetWindowPos(window, Bottom, 0, 0, 0, 0, NoSize | NoMove | NoActivate | ShowWindowFlag);
        }

        /// <summary>Sends every running simulator behind again. Answers how many it found.</summary>
        /// <remarks>
        /// Order and focus are two different things, which is worth knowing because putting a window at
        /// the bottom of the pile does not stop it owning the keyboard - it just owns it from behind
        /// everything, which is worse than either state on its own. So whoever is underneath is handed
        /// the keyboard explicitly.
        /// </remarks>
        public static int StandBack()
        {
            bool ours = IsOurs(GetForegroundWindow());
            int sent = Each(StandBack);

            if (sent > 0 && ours)
                HandOver();

            return sent;
        }

        /// <summary>Gives the keyboard to the topmost window that is somebody else's.</summary>
        static void HandOver()
        {
            for (IntPtr window = GetTopWindow(IntPtr.Zero);
                 window != IntPtr.Zero;
                 window = GetWindow(window, Next))
            {
                // Visible, titled and not ours. Untitled top-level windows are the invisible scaffolding
                // every desktop is full of, and handing the keyboard to one of those loses it entirely.
                if (!IsWindowVisible(window) || GetWindowTextLength(window) == 0 || IsOurs(window))
                    continue;

                SetForegroundWindow(window);
                return;
            }
        }

        static bool IsOurs(IntPtr window)
        {
            if (window == IntPtr.Zero)
                return false;

            Process[] running = Process.GetProcessesByName(
                Process.GetCurrentProcess().ProcessName);

            for (int i = 0; i < running.Length; i++)
            {
                if (running[i].MainWindowHandle == window)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Brings every running simulator to the front. Answers how many it found.
        /// </summary>
        /// <remarks>
        /// The topmost flag is set and immediately cleared, which is the long-standing way of getting a
        /// window in front of a foreground application one does not own: Windows will not simply hand
        /// focus over, but it will honour being put at the top of the order and then released.
        /// </remarks>
        public static int ComeForward()
        {
            return Each(window =>
            {
                if (IsIconic(window))
                    ShowWindowAsync(window, Restore);

                SetWindowPos(window, TopMost, 0, 0, 0, 0, NoSize | NoMove | NoActivate);
                SetWindowPos(window, NoTopMost, 0, 0, 0, 0, NoSize | NoMove | NoActivate);
                SetForegroundWindow(window);
            });
        }

        /// <summary>
        /// Does something to every simulator window that is open, this process's own excepted.
        /// </summary>
        /// <remarks>
        /// The run asking is itself a copy of the same executable, and it has no window - it was started
        /// to say one thing and stop. Skipping anything without a window covers that without having to
        /// reason about process ids.
        /// </remarks>
        static int Each(Action<IntPtr> what)
        {
            Process[] running = Process.GetProcessesByName(
                Process.GetCurrentProcess().ProcessName);

            int touched = 0;

            for (int i = 0; i < running.Length; i++)
            {
                IntPtr window = running[i].MainWindowHandle;
                if (window == IntPtr.Zero)
                    continue;

                what(window);
                touched++;
            }

            return touched;
        }
    }
}
