using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.ModSupport;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>The tasks one awaitable action of a mod is running, one for each block a step waits on, and how each has ended.</summary>
    /// <remarks>
    /// A block holds its record until a step starts the action on it again or its run stops waiting, so it is held at most
    /// once per block. What the mod reports is heard on the game's main thread and tells whatever waits on the block to look.
    /// </remarks>
    public sealed class ModTasks
    {
        readonly Func<IMyTerminalBlock, object[], Dictionary<string, object>> _start;
        readonly ModMembers _members;
        readonly string _name;
        readonly Dictionary<long, Run> _runs = new Dictionary<long, Run>();

        /// <summary>The tasks of the action <paramref name="name" /> that <paramref name="start" /> starts, whose ends are told to what listens through <paramref name="members" />.</summary>
        public ModTasks(string name, Func<IMyTerminalBlock, object[], Dictionary<string, object>> start, ModMembers members)
        {
            _name = name;
            _start = start;
            _members = members;
        }

        /// <summary>Starts the action on <paramref name="block" /> with <paramref name="given" />; a task the mod could not start fails at once.</summary>
        public void Start(IMyTerminalBlock block, object[] given)
        {
            var run = new Run();
            _runs[block.EntityId] = run;
            try
            {
                run.Wire = _start(block, given);
                ModWire.Part<Action<Action<string, string, string>>>(run.Wire, SprocketWire.Hear)?.Invoke((outcome, id, message) =>
                {
                    // After Sprocket has let go, only whether it stopped is recorded, for the look a second later.
                    if (run.Let)
                    {
                        if (outcome != SprocketWire.Stuck)
                            run.Outcome = run.Outcome ?? outcome;
                        return;
                    }

                    if (outcome == SprocketWire.Stuck)
                        run.Stuck = message;
                    else
                    {
                        run.Outcome = outcome;
                        run.Message = message ?? "";
                    }

                    _members.Changed(block);
                });
            }
            catch (Exception e)
            {
                GameLog.Say($"A mod's awaitable action on {block.CustomName} failed to start: {e.Message}");
                run.Outcome = SprocketWire.Failed;
                run.Message = "";
            }
        }

        /// <summary>Whether the task on <paramref name="block" /> has completed, having let it look at what it waits for first.</summary>
        /// <remarks>Asked on the pace a step looks at what it waits for, only while it waits.</remarks>
        public bool Completed(IMyTerminalBlock block)
        {
            Run run;
            if (!_runs.TryGetValue(block.EntityId, out run))
                return false;

            if (run.Outcome == null)
                ModWire.Part<Action>(run.Wire, SprocketWire.Poll)?.Invoke();
            return run.Outcome == SprocketWire.Completed;
        }

        /// <summary>Whether the mod agreed to stop the task on <paramref name="block" />, which settles the step though it never got there.</summary>
        public bool Cancelled(IMyTerminalBlock block)
        {
            Run run;
            return _runs.TryGetValue(block.EntityId, out run) && run.Outcome == SprocketWire.Cancelled;
        }

        /// <summary>What the task on <paramref name="block" /> says is wrong: failed or conflicted, which ends the run, or stuck, which waits on.</summary>
        public BlockTrouble Trouble(IMyTerminalBlock block)
        {
            Run run;
            if (!_runs.TryGetValue(block.EntityId, out run))
                return BlockTrouble.None;

            if (run.Outcome == SprocketWire.Failed)
                return BlockTrouble.Failure(Cause.ModFailed, run.Message);
            if (run.Outcome == SprocketWire.Conflicted)
                return BlockTrouble.Fatal(Cause.ModConflicted, run.Message);
            return string.IsNullOrEmpty(run.Stuck) || run.Outcome != null ? BlockTrouble.None : BlockTrouble.Waiting(Cause.ModStuck, run.Stuck);
        }

        /// <summary>Tells the mod its task on <paramref name="block" /> is to stop now, for <paramref name="cause" />, forgets it, and has a second later looked at whether the mod said it stopped.</summary>
        public void Stop(IMyTerminalBlock block, SprocketCancelCause cause)
        {
            Run run;
            if (!_runs.TryGetValue(block.EntityId, out run))
                return;

            _runs.Remove(block.EntityId);
            run.Let = true;
            if (run.Outcome != null)
                return;

            try
            {
                ModWire.Part<Action<bool, string>>(run.Wire, SprocketWire.Cancel)?.Invoke(true, cause.ToString());
            }
            catch (Exception e)
            {
                GameLog.Say($"A mod's awaitable action on {block.CustomName} failed to stop: {e.Message}");
            }

            if (run.Outcome == null)
                _members.Stops?.Watch(() => run.Outcome != null, $"{_name} on {block.CustomName}");
        }

        /// <summary>One task on one block.</summary>
        sealed class Run
        {
            public Dictionary<string, object> Wire;
            public string Outcome;
            public string Message;
            public string Stuck;
            public bool Let;
        }
    }
}
