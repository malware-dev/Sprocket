using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using System;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>Where Sprocket stores its text in a block's mod storage: the program and its switches, each under a key of its own.</summary>
    public static class BlockStorage
    {
        /// <summary>
        /// Where the program sits in the block's mod storage.
        /// </summary>
        /// <remarks>
        /// Persisted: a new key would read every saved block as holding no program. Claimed in
        /// <c>Content/Data/EntityComponents.sbc</c>, and the two must match: the game drops an unclaimed key at save.
        /// </remarks>
        public static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

        /// <summary>The key of the switches beside the program.</summary>
        public static readonly Guid StateKey = new Guid("C4B7D2F6-1A85-4E39-8D07-6B92F5C1E4A3");

        /// <summary>
        /// Whether this block already carries a program, without building anything to find out.
        /// </summary>
        /// <remarks>Asked of every programmable block in the world as it comes alive, so it is one lookup and allocates nothing.</remarks>
        public static bool HasProgram(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block?.Storage;
            if (storage == null)
                return false;

            string text;
            return storage.TryGetValue(ProgramKey, out text) && !string.IsNullOrEmpty(text);
        }

        /// <summary>What a block holds under one of Sprocket's keys, or empty.</summary>
        public static string Stored(IMyTerminalBlock block, Guid key)
        {
            MyModStorageComponentBase storage = block.Storage;
            string text;
            return storage != null && storage.TryGetValue(key, out text) && text != null ? text : "";
        }

        /// <summary>
        /// Writes a program and its switches into a client's copy of a block, as the server wrote them.
        /// </summary>
        /// <remarks>
        /// The game copies, pastes and blueprints a grid from the copy on the machine doing it, and does not carry a block's
        /// storage from the server to the clients.
        /// </remarks>
        public static void Store(IMyTerminalBlock block, string program, string switches)
        {
            MyModStorageComponentBase storage = Attached(block);
            if (storage == null)
                return;

            storage.SetValue(ProgramKey, program);
            storage.SetValue(StateKey, switches);
        }

        /// <summary>Takes the program and its switches out of <paramref name="block" />, leaving it none of the automation.</summary>
        public static void Clear(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block.Storage;
            if (storage == null)
                return;

            storage.RemoveValue(ProgramKey);
            storage.RemoveValue(StateKey);
        }

        /// <summary>The storage on a block, attached where it has none yet; or null, which is logged.</summary>
        public static MyModStorageComponentBase Attached(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block.Storage;

            // Shared with every other mod on this block. Assigning it registers it with the block's component container, which
            // is what makes the game save it, and the getter answers only once that has happened.
            if (storage == null)
            {
                block.Storage = new MyModStorageComponent();
                storage = block.Storage;
            }

            // Logged rather than thrown: this runs on the way out of a screen and from the world save.
            if (storage == null)
                GameLog.Say($"mod storage would not attach to {SprocketUnit.Logged(block)}; program not saved");

            return storage;
        }
    }
}
