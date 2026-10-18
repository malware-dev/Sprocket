using System;
using System.Globalization;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.DoorAutoClose
{
    /// <summary>How many ticks were left of a door's coming close when it was saved, kept in the door.</summary>
    public static class CloseDue
    {
        // Persisted in a door's mod storage and claimed in Content/Data/EntityComponents.sbc, without which the game drops it at
        // save.
        static readonly Guid Key = new Guid("D57CD7DB-E906-4874-B671-38DF6ED56E98");

        /// <summary>The ticks <paramref name="block" /> had left to close in, taken out of it; -1 where none were kept.</summary>
        public static int Take(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block?.Storage;
            string text;
            int ticks;
            if (storage == null || !storage.TryGetValue(Key, out text))
                return -1;

            storage.RemoveValue(Key);
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out ticks) && ticks >= 0 ? ticks : -1;
        }

        /// <summary>Keeps <paramref name="ticks" /> in <paramref name="block" /> as what is left of its close; -1 keeps none.</summary>
        public static void Store(IMyTerminalBlock block, int ticks)
        {
            MyModStorageComponentBase storage = block.Storage;
            if (ticks < 0)
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
                storage.SetValue(Key, ticks.ToString(CultureInfo.InvariantCulture));
        }
    }
}
