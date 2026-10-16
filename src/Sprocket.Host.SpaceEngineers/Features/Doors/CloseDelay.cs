using System;
using System.Globalization;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>How long a door stays open before it closes by itself: as the slider sets it, as it is written beside it, and as the door keeps it.</summary>
    /// <remarks>
    /// The slider follows the timer block's delay, a logarithmic scale counted in whole seconds as <c>MyTimerBlock</c> has
    /// it, up to ten minutes and written as minutes and seconds. Its left end reaches below a second, and everything under
    /// a second is off.
    /// </remarks>
    public static class CloseDelay
    {
        /// <summary>The slider's left end, in seconds, which is off.</summary>
        /// <remarks>Half a second, so the stretch of the slider that reads off is about as wide as the one that reads one second.</remarks>
        public const float Least = 0.5f;

        /// <summary>The longest delay, in seconds: ten minutes.</summary>
        public const int Most = 10 * 60;

        /// <summary>How many of the game's ticks make a second.</summary>
        public const int TicksPerSecond = 60;

        /// <summary>How far short of a whole second a slider value may fall and still count as it.</summary>
        /// <remarks>The logarithmic scale lands its right end a fraction short of a whole number, as it lands the timer block's hour at 3599.9998.</remarks>
        const float Slack = 0.001f;

        /// <summary>
        /// Where the delay sits in a door's mod storage.
        /// </summary>
        /// <remarks>
        /// Claimed in <c>Content/Data/EntityComponents.sbc</c>, without which the game drops it at save, and never to change:
        /// another key reads as every door in the world set to stay open.
        /// </remarks>
        static readonly Guid Key = new Guid("687D4592-0806-46CF-9930-7739C4270632");

        /// <summary>The whole seconds a slider value stands for, between none and <see cref="Most" />; none is off.</summary>
        public static int FromSlider(float value)
        {
            return Clamped((int)Math.Floor(value + Slack));
        }

        /// <summary><paramref name="seconds" /> held between none and <see cref="Most" />.</summary>
        public static int Clamped(int seconds)
        {
            return seconds < 0 ? 0 : seconds > Most ? Most : seconds;
        }

        /// <summary>A delay as it is written beside the slider: <paramref name="off" /> for none, otherwise minutes and seconds.</summary>
        public static string Written(int seconds, string off)
        {
            if (seconds <= 0)
                return off;

            return string.Format(CultureInfo.InvariantCulture, "{0:00}:{1:00}", seconds / 60, seconds % 60);
        }

        /// <summary>The delay <paramref name="block" /> keeps, in whole seconds; none where it keeps none, which is off.</summary>
        /// <remarks>Read each time the terminal shows the slider, and each time a door with a delay opens.</remarks>
        public static int Stored(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block?.Storage;
            string text;
            int seconds;
            if (storage == null || !storage.TryGetValue(Key, out text)
                || !int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out seconds))
                return 0;

            return Clamped(seconds);
        }

        /// <summary>Keeps <paramref name="seconds" /> as <paramref name="block" />'s delay; none takes the delay away.</summary>
        public static void Store(IMyTerminalBlock block, int seconds)
        {
            seconds = Clamped(seconds);
            MyModStorageComponentBase storage = block.Storage;
            if (seconds == 0)
            {
                if (storage != null)
                    storage.RemoveValue(Key);

                return;
            }

            // Shared with every other mod on the block, so one already here is the one to use. Assigning a new one is what
            // registers it with the block, which is what makes the game write it out at all.
            if (storage == null)
            {
                block.Storage = new MyModStorageComponent();
                storage = block.Storage;
            }

            if (storage != null)
                storage.SetValue(Key, seconds.ToString(CultureInfo.InvariantCulture));
        }
    }
}
