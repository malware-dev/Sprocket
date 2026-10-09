using System;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.ModAPI;

namespace RichTextPanels
{
    /// <summary>Whether a panel's text is fitted to its screen, as the block keeps it: grown or shrunk to fill it, in place of the panel's font size.</summary>
    /// <remarks>
    ///     Kept in the block's mod storage as <c>fit</c>; a block keeping nothing uses its font size. One setting for the
    ///     whole block, so every screen of it running the app follows it.
    /// </remarks>
    public static class Fitting
    {
        const string On = "fit";

        /// <summary>Where the setting sits in a block's mod storage.</summary>
        /// <remarks>
        ///     Claimed in <c>Content/Data/EntityComponents.sbc</c>, without which the game drops it at save, and never to
        ///     change: another key reads as every panel set back to its font size.
        /// </remarks>
        static readonly Guid Key = new Guid("BEBE4495-8A4E-4444-8594-A534F5B5A0FF");

        /// <summary>Whether <paramref name="block" /> has its text fitted to its screen.</summary>
        /// <remarks>Read by every panel running the app each time it looks for changes, so it allocates nothing.</remarks>
        public static bool Stored(IMyEntity block)
        {
            MyModStorageComponentBase storage = block == null ? null : block.Storage;
            string text;
            return storage != null && storage.TryGetValue(Key, out text) && text == On;
        }

        /// <summary>Keeps whether <paramref name="block" /> has its text fitted to its screen; not fitted keeps nothing.</summary>
        public static void Store(IMyEntity block, bool fit)
        {
            MyModStorageComponentBase storage = block.Storage;
            if (!fit)
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

            storage.SetValue(Key, On);
        }
    }
}
