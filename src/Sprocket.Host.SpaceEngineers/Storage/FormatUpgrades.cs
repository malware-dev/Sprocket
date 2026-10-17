using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The upgrades that carry a program written in an earlier format into the one this build writes.</summary>
    /// <remarks>
    /// An upgrade is never removed, since every program behind it reads through it. The upgraded program is written in the
    /// current format at its next save.
    /// </remarks>
    public static class FormatUpgrades
    {
        // Sprocket's own switch actions as alpha 14 programs persist them, and the value that took them over.
        const string SwitchOn = "on";

        const string SwitchOff = "off";

        const string Power = "power";

        // The power value's id as alpha 14 programs written before it was named power persist it.
        const string SwitchedOn = "switched-on";

        // A sensor's count of what it detects, as alpha 15 programs persist it, and its id since.
        const string ThingsDetected = "things-detected";
        const string Detections = "detections";

        // The members of a block's screens as alpha 15 programs persist them on a block with several, each taking the name of
        // its screen first; a block with one screen took them without the name.
        static readonly string[] ScreenMembers =
        {
            ScreenType.ShowText, ScreenType.SetUp, "content", "text-color", "background-color", "font", "font-size", "alignment",
            "padding", "app-color", "app-background-color", "image-interval", "keep-aspect-ratio"
        };

        // How many values each of ScreenMembers took in alpha 15, its screen's name among them.
        static readonly int[] ScreenTook = { 2, 12, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        /// <summary>Whether a program written for <paramref name="draft" /> of version <paramref name="version" /> is upgraded rather than refused.</summary>
        public static bool From(int version, int draft)
        {
            return version == 1 && (draft == 14 || draft == 15);
        }

        /// <summary>
        /// Carries <paramref name="program" />, read as written for <paramref name="draft" />, into the current format, the
        /// screens its steps name found on the blocks of <paramref name="world" />.
        /// </summary>
        /// <remarks>
        /// Where there is no world, or a block has no screen of that name, the installation's slot of screens is left empty.
        /// </remarks>
        /// <returns>What was upgraded, for the log, or empty where nothing needed it.</returns>
        public static string Apply(int draft, Automation program, GridWorld world)
        {
            var said = new List<string>();
            var systems = program.Systems;
            if (draft == 14)
            {
                var switched = 0;
                var renamed = 0;
                foreach (var system in systems)
                {
                    switched += Switches(system);
                    renamed += Renames(system);
                }

                if (switched + renamed > 0)
                    said.Add($"alpha 14: {switched} switch step{(switched == 1 ? "" : "s")} set power, {renamed} use{(renamed == 1 ? "" : "s")} of switched-on named power.");
            }

            if (draft <= 15)
            {
                Bindings bindings = null;
                if (world != null)
                {
                    bindings = new Bindings();
                    bindings.Refresh(world, program);
                }

                var counted = 0;
                var held = 0;
                foreach (var system in systems)
                {
                    counted += Counts(system);
                    held += Screens(system, bindings);
                }

                if (counted + held > 0)
                    said.Add($"alpha 15: {counted} use{(counted == 1 ? "" : "s")} of things-detected named detections, {held} screen step{(held == 1 ? "" : "s")} on a slot of screens.");
            }

            return string.Join(" ", said);
        }

        /// <summary>Names a sensor's count <c>detections</c> wherever <paramref name="system" /> names it <c>things-detected</c>.</summary>
        static int Counts(AutomationSystem system)
        {
            var count = 0;
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var act = step as Act;
                    if (act != null && act.Provider == BlockKinds.Own && act.ActionId == ThingsDetected)
                    {
                        act.ActionId = Detections;
                        count++;
                    }

                    var test = (step as Branch)?.Test;
                    if (test != null && test.Provider == BlockKinds.Own && test.MemberId == ThingsDetected)
                    {
                        test.MemberId = Detections;
                        count++;
                    }
                }
            }

            foreach (var trigger in system.Triggers)
            {
                var when = trigger as WhenTrigger;
                if (when != null && when.Provider == BlockKinds.Own && when.EventId == ThingsDetected)
                {
                    when.EventId = Detections;
                    count++;
                }
            }

            return count;
        }

        /// <summary>Moves every step, test and watch of <paramref name="system" /> naming a screen onto a slot of screens of its own.</summary>
        static int Screens(AutomationSystem system, Bindings bindings)
        {
            var count = 0;
            foreach (var sequence in system.Sequences)
            {
                foreach (var step in sequence.Steps)
                {
                    var act = step as Act;
                    if (act != null && Screened(system, act.Provider, act.ActionId, act.Arguments, act.Targets, bindings))
                        count++;

                    var test = (step as Branch)?.Test;
                    if (test != null && Screened(system, test.Provider, test.MemberId, test.Arguments, test.Targets, bindings))
                        count++;
                }
            }

            foreach (var trigger in system.Triggers)
            {
                var when = trigger as WhenTrigger;
                if (when != null && Screened(system, when.Provider, when.EventId, when.Arguments, when.Sources, bindings))
                    count++;

                var does = trigger.Does as Act;
                if (does != null && Screened(system, does.Provider, does.ActionId, does.Arguments, does.Targets, bindings))
                    count++;
            }

            return count;
        }

        /// <summary>
        /// Takes the screen's name off <paramref name="arguments" />, where <paramref name="member" /> is a screen's and names
        /// one, and has <paramref name="names" /> name a new slot of screens in its place, each installation filling it with the
        /// blocks the names stood for and the screen of that name on each.
        /// </summary>
        static bool Screened(AutomationSystem system, string provider, string member, IList<Literal> arguments, IList<string> names,
            Bindings bindings)
        {
            var at = System.Array.IndexOf(ScreenMembers, member);
            if (provider != BlockKinds.Own || at < 0 || arguments.Count != ScreenTook[at])
                return false;

            var screen = arguments[0].Text;
            arguments.RemoveAt(0);

            // Setting a screen up asks which app it runs since, after its content: left empty, so the app is left as it is.
            if (member == ScreenType.SetUp)
                arguments.Insert(1, Literal.Of(""));

            if (!system.IsBlueprint)
                return true;

            var type = HostTypes.Returned("screen");
            var slot = system.Free(names.Count == 1 ? names[0] : "Screens");
            system.PartFor(slot).Holds = type.Id;

            foreach (var instance in system.Instances)
            {
                var blocks = SystemInstance.Through(instance, names);
                var numbers = new List<int>();
                foreach (var block in blocks)
                {
                    var number = Numbered(bindings?.Entity(block), type, screen);
                    if (number == 0)
                        break;

                    numbers.Add(number);
                }

                // Left empty where any block lacks the screen, so the installation panel asks for it rather than one being guessed.
                if (blocks.Count > 0 && numbers.Count == blocks.Count)
                    instance.Fill(slot, blocks, numbers);
            }

            names.Clear();
            names.Add(slot);
            return true;
        }

        /// <summary>The number of the facet of <paramref name="type" /> called <paramref name="name" /> that <paramref name="entity" /> has, or 0 where it has none.</summary>
        static int Numbered(GridEntity entity, HostType type, string name)
        {
            if (entity == null)
                return 0;

            foreach (var facet in entity.Kind.Facets(type))
            {
                if (facet.Name == name)
                    return facet.Number;
            }

            return 0;
        }

        /// <summary>Names the power value <c>power</c> wherever <paramref name="system" /> names it <c>switched-on</c>: set, checked or watched.</summary>
        static int Renames(AutomationSystem system)
        {
            var count = 0;
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    if (Rename(steps[s]))
                        count++;
                }
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var when = triggers[i] as WhenTrigger;
                if (when != null && when.Provider == BlockKinds.Own && when.EventId == SwitchedOn)
                {
                    when.EventId = Power;
                    count++;
                }

                if (Rename(triggers[i].Does))
                    count++;
            }

            return count;
        }

        /// <summary>Names the power value <c>power</c> where <paramref name="step" /> sets or checks it as <c>switched-on</c>.</summary>
        static bool Rename(Step step)
        {
            var act = step as Act;
            if (act != null && act.Provider == BlockKinds.Own && act.ActionId == SwitchedOn)
            {
                act.ActionId = Power;
                return true;
            }

            var branch = step as Branch;
            if (branch == null || branch.Test == null || branch.Test.Provider != BlockKinds.Own || branch.Test.MemberId != SwitchedOn)
                return false;

            branch.Test.MemberId = Power;
            return true;
        }

        /// <summary>Turns every <c>call ext.on</c> and <c>call ext.off</c> of <paramref name="system" /> into the setting of its power value.</summary>
        static int Switches(AutomationSystem system)
        {
            var count = 0;
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    if (Switch(steps[s]))
                        count++;
                }
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (Switch(triggers[i].Does))
                    count++;
            }

            return count;
        }

        /// <summary>Makes <paramref name="step" /> set the power value where it called Sprocket's own switch action.</summary>
        static bool Switch(Step step)
        {
            var act = step as Act;
            if (act == null || act.Sets || act.Provider != BlockKinds.Own || (act.ActionId != SwitchOn && act.ActionId != SwitchOff))
                return false;

            var on = act.ActionId == SwitchOn;
            act.ActionId = Power;
            act.Form = ReachKind.Value;
            act.Provider = BlockKinds.Own;
            act.Arguments.Clear();
            act.Arguments.Add(Literal.Of(on));
            return true;
        }
    }
}
