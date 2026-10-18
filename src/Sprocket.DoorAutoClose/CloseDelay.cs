using System;
using System.Globalization;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>How long a door stays open before it closes by itself: as the slider sets it, as it is written beside it, and as the door keeps it.</summary>
    /// <remarks>The slider follows the timer block's delay as <c>MyTimerBlock</c> has it: a logarithmic scale in whole seconds.</remarks>
    public static class CloseDelay
    {
        /// <summary>The slider's left end, in seconds, which is off.</summary>
        public const float Least = 0.5f;

        /// <summary>The longest delay, in seconds: ten minutes.</summary>
        public const int Most = 10 * 60;

        /// <summary>How many of the game's ticks make a second.</summary>
        public const int TicksPerSecond = 60;

        // The logarithmic scale lands its right end a fraction short of a whole number, as it lands the timer block's hour at 3599.9998.
        const float Slack = 0.001f;

        // Persisted in a door's mod storage and claimed in Content/Data/EntityComponents.sbc, without which the game drops it at
        // save; another key reads as every door in the world set to stay open.
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

            // Shared with every other mod on the block. Assigning a new one registers it with the block, without which the game does
            // not save it.
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
