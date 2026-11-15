using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>
    /// Alpha 28: the members a block's component gives it are Sprocket's own, so <c>call Detach</c> on a separation charge is
    /// <c>call ext.detach</c>, and a micro emitter's discharge, countdown buttons and countdown are its own likewise.
    /// </summary>
    /// <remarks>
    /// A step, check or trigger is moved only where a block it names is declared the kind its row is for. The same game id
    /// on any other block, such as a rotor's Detach, stays as it was written.
    /// </remarks>
    public sealed class ComponentMembers : FormatChange
    {
        // The block type every row's kind is declared as: a plain functional block, told apart by its subtype.
        static readonly string Functional = BlockKinds.Definition("FunctionalBlock/");

        // Each row: a word of the kind's subtype, the game's id as earlier programs persist it, and Sprocket's own id.
        static readonly ComponentMember[] Rows =
        {
            new ComponentMember("SeparationCharge", "Detach", ReachKind.Action, SeparationCharge.Detach),
            new ComponentMember("MicroEmitter", "Discharge", ReachKind.Action, "discharge"),
            new ComponentMember("MicroEmitter", "StartCountdown", ReachKind.Action, "start_countdown"),
            new ComponentMember("MicroEmitter", "StopCountdown", ReachKind.Action, "stop_countdown"),
            new ComponentMember("MicroEmitter", "Countdown", ReachKind.Value, "countdown"),
        };

        /// <inheritdoc />
        public override int Arrived => 28;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            var count = 0;
            foreach (var system in program.Systems)
            {
                foreach (var sequence in system.Sequences)
                {
                    foreach (var step in sequence.Steps)
                        count += Moves(program, system, step);
                }

                foreach (var trigger in system.Triggers)
                {
                    count += Moves(program, system, trigger as WhenTrigger);
                    count += Moves(program, system, trigger.Does);
                }
            }

            return count == 0 ? "" : $"alpha 27: {count} use{(count == 1 ? "" : "s")} of a block component's member made Sprocket's own.";
        }

        /// <summary>Makes what <paramref name="step" /> does or checks Sprocket's own where a row moves it.</summary>
        static int Moves(Automation program, AutomationSystem system, Step step)
        {
            var act = step as Act;
            if (act != null && act.Provider != BlockKinds.Own)
            {
                var row = Find(program, system, act.ActionId, act.Form, act.Targets);
                if (row == null)
                    return 0;

                act.ActionId = row.Own;
                if (act.Form == ReachKind.Action)
                    act.Form = ReachKind.Native;
                act.Provider = BlockKinds.Own;
                return 1;
            }

            var compared = (step as Branch)?.Test as Comparison;
            if (compared == null || compared.Provider == BlockKinds.Own)
                return 0;

            var checks = Find(program, system, compared.MemberId, ReachKind.Value, compared.Targets);
            if (checks == null)
                return 0;

            compared.MemberId = checks.Own;
            compared.Provider = BlockKinds.Own;
            return 1;
        }

        /// <summary>Makes the value <paramref name="when" /> watches Sprocket's own where a row moves it.</summary>
        static int Moves(Automation program, AutomationSystem system, WhenTrigger when)
        {
            if (when == null || when.Provider == BlockKinds.Own)
                return 0;

            var row = Find(program, system, when.EventId, ReachKind.Value, when.Sources);
            if (row == null)
                return 0;

            when.EventId = row.Own;
            when.Provider = BlockKinds.Own;
            return 1;
        }

        /// <summary>The row for the game's <paramref name="id" />, reached as <paramref name="form" />, on a block among <paramref name="targets" /> it is for, or null.</summary>
        static ComponentMember Find(Automation program, AutomationSystem system, string id, ReachKind form, System.Collections.Generic.IList<string> targets)
        {
            foreach (var row in Rows)
            {
                if (row.Game == id && row.Form == form && Declared(program, system, targets, row.Subtype))
                    return row;
            }

            return null;
        }

        /// <summary>Whether a block <paramref name="targets" /> name is declared a functional block whose subtype holds <paramref name="subtype" />, in any installation of <paramref name="system" />.</summary>
        static bool Declared(Automation program, AutomationSystem system, System.Collections.Generic.IList<string> targets, string subtype)
        {
            foreach (var target in targets)
            {
                if (Is(program.FindRef(target), subtype))
                    return true;

                foreach (var instance in system.Uses)
                {
                    var filled = instance.FilledWith(target);
                    if (filled == null)
                        continue;

                    foreach (var name in filled)
                    {
                        if (Is(program.FindRef(name), subtype))
                            return true;
                    }
                }
            }

            return false;
        }

        static bool Is(EntityRef declared, string subtype)
        {
            return declared != null && declared.KindId.StartsWith(Functional, System.StringComparison.Ordinal)
                && declared.KindId.IndexOf(subtype, System.StringComparison.Ordinal) >= 0;
        }
    }
}
