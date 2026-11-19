using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Format.Upgrades
{
    /// <summary>Alpha 16: a screen's members no longer name their screen; a slot of screens holds it, filled by each installation.</summary>
    /// <remarks>
    /// Where there is no world, or a block has no screen of that name, the installation's slot of screens is left empty.
    /// </remarks>
    public sealed class ScreensOnSlots : FormatChange
    {
        // The members of a block's screens as alpha 15 programs persist them on a block with several, each taking the name of
        // its screen first; a block with one screen took them without the name.
        static readonly string[] ScreenMembers =
        {
            "show-text", "set-up-screen", "content", "text-color", "background-color", "font", "font-size", "alignment",
            "padding", "app-color", "app-background-color", "image-interval", "keep-aspect-ratio"
        };

        // How many values each of ScreenMembers took in alpha 15, its screen's name among them.
        static readonly int[] ScreenTook = { 2, 12, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        /// <inheritdoc />
        public override int Arrived => 16;

        /// <inheritdoc />
        public override string Program(Automation program, GridWorld world)
        {
            Bindings bindings = null;
            if (world != null)
            {
                bindings = new Bindings();
                bindings.Refresh(world, program);
            }

            var held = 0;
            foreach (var system in program.Systems)
                held += Screens(system, bindings);

            return held == 0 ? "" : $"alpha 15: {held} screen step{(held == 1 ? "" : "s")} on a slot of screens.";
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

                    var test = (step as Branch)?.Test as Comparison;
                    if (test != null && Screened(system, test.Provider, test.MemberId, test.Arguments, test.Targets, bindings))
                        count++;
                }
            }

            foreach (var trigger in system.Triggers)
            {
                var when = trigger as WhenTrigger;
                if (when != null && Screened(system, when.Provider, when.EventId, when.Arguments, when.Sources, bindings))
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
            if (member == "set-up-screen")
                arguments.Insert(1, Literal.Of(""));

            if (!system.IsBlueprint)
                return true;

            var type = HostTypes.Named(ScreenType.TypeId);
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
    }
}
