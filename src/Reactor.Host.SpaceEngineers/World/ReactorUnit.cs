using System;
using Reactor.Editing;
using Reactor.Runtime;
using Reactor.Sequences;
using Reactor.Storage;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;
using Reactor.Host.SpaceEngineers.Presentation.Controls;
using Reactor.Host.SpaceEngineers.Presentation.Screen;
using Reactor.Host.SpaceEngineers.Presentation.Lists;
using Reactor.Host.SpaceEngineers.Presentation.Look;
using Reactor.Host.SpaceEngineers.Presentation.Picking;
using Reactor.Host.SpaceEngineers.Presentation.Words;
using Reactor.Host.SpaceEngineers.Terminal;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// One Reactor, on one construct: its world, its program, the runtime that runs it, and the editor.
    /// </summary>
    /// <remarks>
    /// The program and its runtime belong to the block, not to the screen: the session ticks them whether
    /// or not anything is open, and the screen is only a way to look at them. Owned by the screen, a
    /// sequence would run only while somebody was watching it, and an airlock is worth nothing on those
    /// terms.
    /// </remarks>
    public sealed class ReactorUnit : IDisposable
    {
        /// <summary>One object answering two interfaces: how a sequence reads, and how a time reads.</summary>
        readonly SequenceText _sequenceWords = new SequenceText();
        readonly TimeText _timeWords = new TimeText();

        /// <summary>
        /// Where the program sits in the block's mod storage.
        /// </summary>
        /// <remarks>
        /// Mod storage is shared with every other mod on the entity, so the key has to be ours and has to
        /// never change: a new one would read as an empty Reactor and quietly discard somebody's ship.
        /// <para>
        /// <b>It also has to be claimed in <c>Content/Data/EntityComponents.sbc</c>, and the two must
        /// match.</b> The game writes out only the keys some mod has registered there and drops the rest
        /// with a line in the log, so an unclaimed key works perfectly all session and vanishes on save.
        /// </para>
        /// </remarks>
        static readonly Guid ProgramKey = new Guid("9F1E4A2C-7D53-4B18-9C6E-2A0D5F3B81E7");

        readonly IMyTerminalBlock _block;
        readonly GridWorld _world;
        readonly Automation _program = new Automation();
        readonly AutomationRunner _runner;
        readonly ReactorEditor _editor;

        /// <summary>
        /// Whether this block already carries a program, without building anything to find out.
        /// </summary>
        /// <remarks>
        /// Asked of every programmable block in the world as it comes alive, so it has to be one lookup and
        /// no allocation. A block that answers false is left entirely alone.
        /// </remarks>
        public static bool HasProgram(IMyTerminalBlock block)
        {
            MyModStorageComponentBase storage = block == null ? null : block.Storage;
            if (storage == null)
                return false;

            string text;
            return storage.TryGetValue(ProgramKey, out text) && !string.IsNullOrEmpty(text);
        }

        public ReactorUnit(IMyTerminalBlock block)
        {
            _block = block;
            _world = new GridWorld(block);
            _runner = new AutomationRunner(_program, _world);

            // Before the editor, which builds its first screen out of whatever the program already says.
            Load();

            _editor = new ReactorEditor(_world, _program, _runner, _sequenceWords, _timeWords);
        }

        public ReactorEditor Editor => _editor;

        public GridWorld World => _world;

        /// <summary>
        /// Advances everything by one frame.
        /// </summary>
        /// <remarks>
        /// Through the editor rather than straight to the runner, because <c>ReactorEditor.Tick</c> ticks
        /// the runner itself and then brings its screen up to date. Doing both would run every sequence
        /// twice on the frames somebody had the editor open, and the runner takes an absolute frame number
        /// rather than ignoring a repeat.
        ///
        /// The cost is that a closed editor still refreshes a screen nobody is looking at. That is cheap by
        /// construction - core compares before it writes and says nothing when nothing differs - and it is
        /// worth more than a flag saying which of two things is doing the ticking this frame.
        /// </remarks>
        public void Tick(int frame)
        {
            _editor.Tick(frame);

            // After, never before: a press is held only until the tick that samples it has seen it.
            _world.ClearMoments();
        }

        /// <summary>
        /// Reads the program back out of the block.
        /// </summary>
        /// <remarks>
        /// A block with no storage, or storage with nothing of ours in it, is a Reactor nobody has written
        /// a program on yet, which is an empty ship rather than an error.
        /// </remarks>
        void Load()
        {
            MyModStorageComponentBase storage = _block.Storage;
            if (storage == null)
                return;

            string text;
            if (storage.TryGetValue(ProgramKey, out text) && !string.IsNullOrEmpty(text))
                ProgramText.Load(text, _program);
        }

        /// <summary>
        /// Writes the program into the block, so the world save carries it.
        /// </summary>
        /// <remarks>
        /// Written whenever the program might have changed rather than only when the world saves. Storage
        /// is a dictionary the game serialises along with the block, so putting the text there early costs
        /// a string and removes any question about whether a session component's save runs before or after
        /// the entities are written out.
        /// </remarks>
        public void Persist()
        {
            MyModStorageComponentBase storage = _block.Storage;

            // Shared with every other mod on this block, so one may already be here - and if it is, it is
            // the one to use rather than one to replace. Assigning it is what registers it with the
            // block's component container, which is what makes the game write it out at all; the getter
            // only answers once that has happened, so it is worth asking again rather than assuming.
            if (storage == null)
            {
                _block.Storage = new MyModStorageComponent();
                storage = _block.Storage;
            }

            if (storage == null)
            {
                // Logged rather than thrown. This runs on the way out of a screen and from the world
                // save, and taking the game down over it would cost far more than the program does.
                MyLog.Default.WriteLineAndConsole(
                    "[Reactor] mod storage would not attach to " + _block.CustomName + "; program not saved");
                return;
            }

            storage.SetValue(ProgramKey, ProgramText.Save(_program));
        }

        public void Dispose()
        {
            _world.Dispose();
        }
    }
}
