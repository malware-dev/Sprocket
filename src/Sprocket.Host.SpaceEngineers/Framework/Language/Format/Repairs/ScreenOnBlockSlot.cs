using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using Sprocket.Host.SpaceEngineers.Framework.Text;
using System.Collections.Generic;
using Lumel.Text;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Repairs
{
    /// <summary>
    /// A step doing something to a screen through a slot that holds blocks, as a program holds it from before slots held
    /// screens, where the upgrades could not choose its screen: mended by picking the screen, which makes the slot a screen slot
    /// in place where every use of it is of that screen, and gives the step a screen slot of its own where some use is of the
    /// block.
    /// </summary>
    public sealed class ScreenOnBlockSlot : Repair
    {
        /// <inheritdoc />
        public override SequenceProblem Problem => SequenceProblem.ScreenOnBlockSlot;

        /// <inheritdoc />
        public override bool IsAboutSystem => true;

        /// <inheritdoc />
        public override SequenceFault Find(Bindings bindings, Sequence sequence, SystemInstance through)
        {
            var system = sequence.System;
            if (system == null || !system.IsBlueprint)
                return SequenceFault.None;

            for (var i = 0; i < sequence.Steps.Count; i++)
            {
                var act = sequence.Steps[i] as Act;
                if (act == null || !act.HasAction || act.IsOff)
                    continue;

                foreach (var slot in act.Targets)
                {
                    var part = system.FindPart(slot);
                    if (part == null || part.IsSignal || part.Holds.Length > 0)
                        continue;

                    foreach (var name in SystemInstance.Through(through, new[] { slot }))
                    {
                        GridEntity entity = bindings.Entity(name);
                        if (entity != null && act.Names(entity.Kind.Actions)?.Type != null)
                            return new SequenceFault(Problem, i, BlockTrouble.None, slot);
                    }
                }
            }

            return SequenceFault.None;
        }

        /// <inheritdoc />
        public override string Warning(Texts texts, SequenceFault fault)
        {
            return Template.Fill(texts.RepairScreenOnBlockSlotWarning, "slot", fault.Which);
        }

        /// <inheritdoc />
        /// <remarks>
        /// Every use of the slot fits what it comes to hold, so it stays one slot, and holding the type mends each of them. An
        /// installation that has not chosen a screen of its blocks acts on them as it did while the slot held blocks.
        /// </remarks>
        public override Part Reslots(Bindings bindings, AutomationSystem system, SystemInstance shown, IList<string> replacing,
            HostType type)
        {
            var part = type == null || shown == null ? null : BlockSlot(system, replacing);
            return part != null && AllUse(bindings, system, shown, part.Id, type) ? part : null;
        }

        /// <inheritdoc />
        public override string Instead(Texts texts, AutomationSystem system, IList<string> replacing, HostType type)
        {
            var part = type == null ? null : BlockSlot(system, replacing);
            return part == null ? "" : Template.Fill(texts.RepairScreenOnBlockSlotInstead, "slot", part.Id);
        }

        /// <inheritdoc />
        /// <remarks>Every other use of the slot's screen goes with it, so the steps that use the screen keep using one slot.</remarks>
        public override List<IList<string>> Carried(Bindings bindings, AutomationSystem system, SystemInstance shown,
            IList<string> replacing, HostType type)
        {
            var carried = new List<IList<string>>();
            var part = type == null || shown == null ? null : BlockSlot(system, replacing);
            var actions = part == null ? null : Actions(bindings, shown, part.Id);
            if (actions == null)
                return carried;

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    if (ReferenceEquals(step.Targets, replacing) || !step.Targets.Contains(part.Id))
                        continue;

                    var act = step as Act;
                    var test = (step as Branch)?.Test as Comparison;
                    var member = act != null ? act.Names(actions) : test?.Names(actions);
                    if (member?.Type == type)
                        carried.Add(step.Targets);
                }
            }

            return carried;
        }

        /// <inheritdoc />
        public override string Ahead(Texts texts, Bindings bindings, AutomationSystem system, SystemInstance shown, Act step)
        {
            if (system == null || !system.IsBlueprint || shown == null)
                return "";

            foreach (var slot in step.Targets)
            {
                var part = system.FindPart(slot);
                if (part == null || part.IsSignal || part.Holds.Length > 0)
                    continue;

                var filled = shown.FilledWith(slot);
                var entity = filled == null || filled.Count == 0 ? null : bindings.Entity(filled[0]);
                var type = entity == null ? null : step.Names(entity.Kind.Actions)?.Type;
                if (type == null)
                    continue;

                var said = AllUse(bindings, system, shown, slot, type) ? texts.RepairScreenOnBlockSlotAheadInPlace : texts.RepairScreenOnBlockSlotAheadSplit;
                return Template.Fill(said, "slot", slot);
            }

            return "";
        }

        /// <inheritdoc />
        public override string Proceed(Texts texts)
        {
            return texts.RepairScreenOnBlockSlotProceed;
        }

        /// <inheritdoc />
        public override string Cancel(Texts texts)
        {
            return texts.RepairScreenOnBlockSlotCancel;
        }

        /// <summary>The slot holding blocks that the list <paramref name="replacing" /> names alone, or null where it names any other.</summary>
        static Part BlockSlot(AutomationSystem system, IList<string> replacing)
        {
            if (system == null || replacing == null || replacing.Count == 0)
                return null;

            var part = system.FindPart(replacing[0]);
            if (part == null || part.IsSignal || part.Holds.Length > 0)
                return null;

            foreach (var name in replacing)
            {
                if (name != part.Id)
                    return null;
            }

            return part;
        }

        /// <summary>What the first block <paramref name="shown" /> fills <paramref name="slot" /> with offers, or null where it fills it with none.</summary>
        static IList<EntityAction> Actions(Bindings bindings, SystemInstance shown, string slot)
        {
            var filled = shown.FilledWith(slot);
            var entity = filled == null || filled.Count == 0 ? null : bindings.Entity(filled[0]);
            return entity?.Kind.Actions;
        }

        /// <summary>Whether every step of <paramref name="system" /> that names <paramref name="slot" /> does something of <paramref name="type" /> to its blocks, as <paramref name="shown" /> fills it.</summary>
        /// <remarks>A trigger that watches the slot reads its blocks themselves, so it never does.</remarks>
        static bool AllUse(Bindings bindings, AutomationSystem system, SystemInstance shown, string slot, HostType type)
        {
            var actions = Actions(bindings, shown, slot);
            if (actions == null)
                return false;

            foreach (var trigger in system.Triggers)
            {
                var moment = trigger as MomentTrigger;
                if (moment != null && moment.Sources.Contains(slot))
                    return false;
            }

            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    if (!step.Targets.Contains(slot))
                        continue;

                    var act = step as Act;
                    var test = (step as Branch)?.Test as Comparison;
                    var member = act != null ? act.Names(actions) : test?.Names(actions);
                    if (member?.Type != type)
                        return false;
                }
            }

            return true;
        }
    }
}
