using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Storage
{
    /// <summary>What is switched off, and why, as a construct's block stores it beside its program, and read back.</summary>
    /// <remarks>Persisted as a <see cref="BlockState" /> packed by the game's binary serialization.</remarks>
    public static class BlockSwitches
    {
        /// <summary>What is switched off in <paramref name="program" />, packed by <paramref name="binary" />, or empty where nothing is.</summary>
        public static string Save(Automation program, IBinary binary)
        {
            var state = Of(program);
            return state.Switches.Count == 0 ? "" : binary.Pack(state);
        }

        /// <summary>What is switched off in <paramref name="program" />, and why.</summary>
        public static BlockState Of(Automation program)
        {
            var state = new BlockState();
            foreach (var system in program.Systems)
            {
                if (!system.Enabled)
                    state.Switches.Add(new SwitchMemory { System = system.Identity, Halt = StoredReasons.Stored(system.Halt) });

                foreach (var instance in system.Instances)
                {
                    if (!instance.Enabled)
                        state.Switches.Add(new SwitchMemory { System = system.Identity, Instance = instance.Number, Halt = StoredReasons.Stored(instance.Halt) });
                }
            }

            return state;
        }

        /// <summary>
        /// Switches off in <paramref name="program" /> what <paramref name="text" /> says is off, unpacked by
        /// <paramref name="binary" />; what is wrong with it, or empty.
        /// </summary>
        /// <remarks>
        /// A block written before this form holds the switches as lines of words, and is read as they were written. Switches
        /// that will not read leave the program marked not to be saved over.
        /// </remarks>
        public static string Load(string text, Automation program, IBinary binary)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            // Base 64 holds no space; every line of the earlier form does.
            if (text.IndexOf(' ') >= 0 || text.IndexOf('\n') >= 0)
                return EarlierSwitches.Load(text, program);

            BlockState state;
            try
            {
                state = binary.Unpack<BlockState>(text);
            }
            catch (Exception exception)
            {
                var trouble = $"the switches would not read: {exception.Message}";
                program.Damaged(trouble);
                return trouble;
            }

            Put(state, program);
            return "";
        }

        /// <summary>
        /// Switches <paramref name="program" /> to exactly what <paramref name="text" /> says: everything on, and then off what it
        /// says is off; what is wrong with it, or empty.
        /// </summary>
        /// <remarks>For a program already read, whose switches may have changed elsewhere since.</remarks>
        public static string Reload(string text, Automation program, IBinary binary)
        {
            foreach (var system in program.Systems)
            {
                foreach (var instance in system.Instances)
                    instance.Enabled = true;

                system.Enabled = true;
            }

            return Load(text, program, binary);
        }

        /// <summary>Switches off in <paramref name="program" /> what <paramref name="state" /> says is off, where it is still there.</summary>
        public static void Put(BlockState state, Automation program)
        {
            foreach (var off in state.Switches)
                Switch(program, off);
        }

        /// <summary>Switches off in <paramref name="program" /> what <paramref name="off" /> names, where it is still there.</summary>
        static void Switch(Automation program, SwitchMemory off)
        {
            var system = program.FindSystem(off.System);
            if (system == null)
                return;

            if (off.Instance == 0)
            {
                system.Enabled = false;
                system.Halt = StoredReasons.Halt(off.Halt);
                return;
            }

            var instance = system.FindInstance(off.Instance);
            if (instance == null)
                return;

            instance.Enabled = false;
            instance.Halt = StoredReasons.Halt(off.Halt);
        }
    }
}
