using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage.Upgrades
{
    /// <summary>
    /// Alpha 19: a block slot every step uses the screen of, where every block in it has one screen, holds that screen; alpha 16
    /// moved only the steps that named their screen.
    /// </summary>
    /// <remarks>
    /// Where there is no world, a block has more than one screen or none, or anything uses the slot for the block itself, the slot
    /// is left as it is, for the repair to mend.
    /// </remarks>
    public sealed class OneScreenSlots : FormatChange
    {
        // The members of a block's screens as alpha 18 programs persist them.
        static readonly string[] ScreenMembers =
        {
            ScreenType.ShowText, ScreenType.SetUp, "content", "text-color", "background-color", "font", "font-size", "alignment",
            "padding", "app-color", "app-background-color", "image-interval", "keep-aspect-ratio"
        };

        /// <inheritdoc />
        public override int Arrived => 19;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            if (world == null)
                return "";

            var bindings = new Bindings();
            bindings.Refresh(world, program);

            var held = 0;
            var type = HostTypes.Returned("screen");
            foreach (var system in program.Systems)
            {
                if (!system.IsBlueprint)
                    continue;

                foreach (var part in system.Parts)
                {
                    if (!part.IsSignal && part.Holds.Length == 0 && OnlyScreens(system, part.Id) && Hold(system, part, type, bindings))
                        held++;
                }
            }

            return held == 0 ? "" : $"alpha 18: {held} block slot{(held == 1 ? "" : "s")} of one screen each made a screen slot.";
        }

        /// <summary>Whether something in <paramref name="system" /> names <paramref name="slot" />, and everything that does is a step or a trigger's act on a screen's member.</summary>
        static bool OnlyScreens(AutomationSystem system, string slot)
        {
            var any = false;
            foreach (var trigger in system.Triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null && moment.Sources.Contains(slot))
                    return false;

                var does = trigger.Acts;
                if (does != null && does.Targets.Contains(slot))
                {
                    if (!Screened(does.Provider, does.ActionId))
                        return false;

                    any = true;
                }
            }

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    if (!step.Targets.Contains(slot))
                        continue;

                    var act = step as Act;
                    var test = (step as Branch)?.Test as Comparison;
                    if (act != null ? !Screened(act.Provider, act.ActionId) : test == null || !Screened(test.Provider, test.MemberId))
                        return false;

                    any = true;
                }
            }

            return any;
        }

        /// <summary>Whether <paramref name="member" />, from <paramref name="provider" />, is a member of a block's screen.</summary>
        static bool Screened(string provider, string member)
        {
            return provider == BlockKinds.Own && System.Array.IndexOf(ScreenMembers, member) >= 0;
        }

        /// <summary>
        /// Has <paramref name="part" /> hold <paramref name="type" />, each installation filling it with its blocks on their one
        /// screen, where every block in it has exactly one; whether it did.
        /// </summary>
        static bool Hold(AutomationSystem system, Part part, HostType type, Bindings bindings)
        {
            var filled = new List<List<string>>();
            var numbers = new List<List<int>>();
            foreach (var instance in system.Instances)
            {
                var blocks = instance.FilledWith(part.Id);
                var held = new List<int>();
                if (blocks != null)
                {
                    foreach (var block in blocks)
                    {
                        var facets = bindings.Entity(block)?.Kind.Facets(type);
                        if (facets == null || facets.Count != 1)
                            return false;

                        held.Add(facets[0].Number);
                    }
                }

                filled.Add(blocks == null ? null : new List<string>(blocks));
                numbers.Add(held);
            }

            part.Holds = type.Id;
            for (var i = 0; i < system.Instances.Count; i++)
            {
                if (filled[i] != null && filled[i].Count > 0)
                    system.Instances[i].Fill(part.Id, filled[i], numbers[i]);
            }

            return true;
        }
    }
}
