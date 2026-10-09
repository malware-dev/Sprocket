using System;
using Lumel;
using Sandbox.Game.EntityComponents;
using VRage.Game.Components;
using VRage.ModAPI;

namespace RichTextPanels
{
    /// <summary>Where the text sits up and down a panel's screen, as the block keeps it: at the top, in the middle or at the bottom.</summary>
    /// <remarks>
    ///     Kept in the block's mod storage as <c>middle</c> or <c>bottom</c>; a block keeping nothing has it at the top. One
    ///     setting for the whole block, so every screen of it running the app follows it.
    /// </remarks>
    public static class Vertical
    {
        const string Middle = "middle";
        const string Bottom = "bottom";

        /// <summary>Where the setting sits in a block's mod storage.</summary>
        /// <remarks>
        ///     Claimed in <c>Content/Data/EntityComponents.sbc</c>, without which the game drops it at save, and never to
        ///     change: another key reads as every panel set to the top.
        /// </remarks>
        static readonly Guid Key = new Guid("5A66D669-D166-43D3-8E74-FF0C7E280C30");

        /// <summary>Where <paramref name="block" /> has its text: <see cref="Align.Top" />, <see cref="Align.Center" /> or <see cref="Align.Bottom" />.</summary>
        /// <remarks>Read by every panel running the app each time it looks for changes, so it allocates nothing.</remarks>
        public static Align Stored(IMyEntity block)
        {
            MyModStorageComponentBase storage = block == null ? null : block.Storage;
            string text;
            if (storage == null || !storage.TryGetValue(Key, out text))
                return Align.Top;

            return text == Middle ? Align.Center : text == Bottom ? Align.Bottom : Align.Top;
        }

        /// <summary>Keeps <paramref name="align" /> as where <paramref name="block" /> has its text; the top keeps nothing.</summary>
        public static void Store(IMyEntity block, Align align)
        {
            MyModStorageComponentBase storage = block.Storage;
            if (align != Align.Center && align != Align.Bottom)
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

            storage.SetValue(Key, align == Align.Center ? Middle : Bottom);
        }
    }
}
