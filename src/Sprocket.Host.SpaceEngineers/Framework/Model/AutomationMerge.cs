using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Language.Writing;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;

namespace Sprocket.Host.SpaceEngineers.Framework.Model
{
    /// <summary>Joins two automations into one, and divides one between the halves of a construct that parted.</summary>
    public static class AutomationMerge
    {
        /// <summary>
        /// A copy of the program <paramref name="kept" /> holds with <paramref name="other" />'s logic systems joined in,
        /// adding each pair of differing versions to <paramref name="conflicts" /> and each installation brought in to
        /// <paramref name="moves" />.
        /// </summary>
        /// <remarks>The program <paramref name="kept" /> holds is left as it is, and the copy's values are its own.</remarks>
        public static Automation Join(SprocketUnit kept, SprocketUnit other, List<ConflictMemory> conflicts, List<InstanceMove> moves)
        {
            var values = kept.World.Values;
            var program = new Automation();
            ProgramCopy.Into(kept.Program, values, program, values);

            // Plain systems become blueprints first, as the editor makes them.
            foreach (var system in new List<AutomationSystem>(other.Program.Systems))
            {
                if (!system.IsBlueprint)
                    other.Editor.Picks.Blueprinted(system);
            }

            foreach (var system in other.Program.Systems)
            {
                var here = program.FindSystem(system.Identity);
                if (here != null && ProgramWriter.Said(here) == ProgramWriter.Said(system))
                {
                    Bring(system, here, other, program, kept.World, moves);
                    continue;
                }

                var identity = here == null ? system.Identity : Guid.NewGuid();
                var trouble = ProgramReader.AddBlueprint(ProgramWriter.Blueprint(system, identity), program, values);
                var arrived = program.FindSystem(identity);
                if (trouble.Length > 0 || arrived == null)
                {
                    GameLog.Say($"{system.Name} could not join the automation it met, and stays where it was: {trouble}");
                    continue;
                }

                arrived.Changed = system.Changed;
                arrived.Origin = system.Origin;
                arrived.OriginChanged = system.OriginChanged;
                arrived.Enabled = system.Enabled;
                Bring(system, arrived, other, program, kept.World, moves);

                if (here == null)
                    continue;

                here.Enabled = false;
                arrived.Enabled = false;
                conflicts.Add(new ConflictMemory { System = here.Identity, Waiting = identity });
            }

            return program;
        }

        /// <summary>
        /// Brings <paramref name="program" />, which runs on <paramref name="world" />, to what it keeps after its construct
        /// parted from <paramref name="away" />: an installation whose blocks all went away goes, a blueprint left with none goes
        /// with it, and every block's place is found again on <paramref name="world" />.
        /// </summary>
        /// <remarks>Changes <paramref name="program" /> in place; the caller says it changed.</remarks>
        public static void Part(Automation program, GridWorld world, GridWorld away)
        {
            foreach (var system in new List<AutomationSystem>(program.Systems))
            {
                if (!system.IsBlueprint || system.Instances.Count == 0)
                    continue;

                foreach (var instance in new List<SystemInstance>(system.Instances))
                {
                    if (Count(instance, program, world) == 0 && Count(instance, program, away) > 0)
                        system.RemoveInstance(instance);
                }

                if (system.Instances.Count == 0)
                    program.RemoveSystem(system);
            }

            Rekey(program, world);
        }

        /// <summary>Whether any installation of <paramref name="program" /> has a block on <paramref name="world" />.</summary>
        public static bool Reaches(Automation program, GridWorld world)
        {
            foreach (var system in program.Systems)
            {
                foreach (var instance in system.Uses)
                {
                    if (Count(instance, program, world) > 0)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Adds a copy of <paramref name="instance" /> to <paramref name="into" />, unless an identical installation is there
        /// already; answers the installation of <paramref name="into" /> that stands for it.
        /// </summary>
        public static SystemInstance Bring(SystemInstance instance, AutomationSystem into, Func<string, string> named)
        {
            var made = into.AddInstance();
            made.Name = instance.Name;
            made.Enabled = instance.Enabled;
            made.Annotation = instance.Annotation;

            foreach (var slot in instance.Filled)
            {
                if (into.FindPart(slot) == null)
                    continue;

                var blocks = new List<string>();
                foreach (var name in instance.FilledWith(slot))
                    blocks.Add(named(name));

                var positions = instance.Positions(slot);
                var facets = instance.Facets(slot);
                if (positions != null)
                    made.Fill(slot, blocks, positions);
                else if (facets != null)
                    made.Fill(slot, blocks, facets);
                else
                    made.Fill(slot, blocks);

                made.Note(slot, instance.NoteOn(slot));
            }

            foreach (var here in into.Instances)
            {
                if (here != made && Same(here, made))
                {
                    into.RemoveInstance(made);
                    return here;
                }
            }

            return made;
        }

        /// <summary>
        /// Moves what <paramref name="from" /> remembers of each installation in <paramref name="moves" /> into
        /// <paramref name="into" />, under the logic system and number it has there: its runs, its watches and its states.
        /// </summary>
        /// <remarks>What <paramref name="into" /> remembers already of an installation is kept, and the moved memory of it dropped.</remarks>
        public static void Carry(ConstructMemory from, ConstructMemory into, List<InstanceMove> moves)
        {
            foreach (var run in from.Runs)
            {
                var move = Moved(moves, run.System, run.Instance);
                if (move == null || into.Runs.Exists(here => here.System == move.System && here.Instance == move.Number && here.Sequence == run.Sequence))
                    continue;

                run.System = move.System;
                run.Instance = move.Number;
                into.Runs.Add(run);
            }

            foreach (var watch in from.Watches)
            {
                var move = Moved(moves, watch.System, watch.Instance);
                if (move == null || into.Watches.Exists(here => here.System == move.System && here.Instance == move.Number && here.Trigger == watch.Trigger))
                    continue;

                watch.System = move.System;
                watch.Instance = move.Number;
                into.Watches.Add(watch);
            }

            foreach (var state in from.States)
            {
                var move = Moved(moves, state.System, state.Instance);
                if (move == null || into.States.Exists(here => here.System == move.System && here.Instance == move.Number && here.State == state.State))
                    continue;

                state.System = move.System;
                state.Instance = move.Number;
                into.States.Add(state);
            }

            from.Runs.Clear();
            from.Watches.Clear();
            from.States.Clear();
        }

        /// <remarks>An old place, walked from this half's grid, can land on a different block of the same kind.</remarks>
        static void Rekey(Automation program, GridWorld world)
        {
            var fresh = new List<EntityKey>();
            foreach (var declared in program.Refs)
            {
                var keys = declared.Keys;
                var at = keys.FindIndex(key => key.Cells != null);
                if (at < 0)
                    continue;

                var found = ById(declared, world);
                if (found == null)
                {
                    keys.RemoveAt(at);
                    continue;
                }

                world.Keys.Identify(found, fresh);
                var place = fresh.FindIndex(key => key.Cells != null);
                if (place < 0)
                    keys.RemoveAt(at);
                else
                    keys[at] = fresh[place];
            }
        }

        static void Bring(AutomationSystem from, AutomationSystem into, SprocketUnit other, Automation program, GridWorld world,
            List<InstanceMove> moves)
        {
            foreach (var instance in from.Instances)
            {
                var at = Bring(instance, into, name => Declared(name, other, program, world));
                moves.Add(new InstanceMove { FromSystem = from.Identity, FromNumber = instance.Number, System = into.Identity, Number = at.Number });
            }
        }

        static InstanceMove Moved(List<InstanceMove> moves, Guid system, int number)
        {
            foreach (var move in moves)
            {
                if (move.FromSystem == system && move.FromNumber == number)
                    return move;
            }

            return null;
        }

        static bool Same(SystemInstance one, SystemInstance other)
        {
            var slots = new List<string>(one.Filled);
            if (slots.Count != new List<string>(other.Filled).Count)
                return false;

            foreach (var slot in slots)
            {
                var theirs = other.FilledWith(slot);
                if (theirs == null || !Equal(one.FilledWith(slot), theirs) || !Places(one.Positions(slot), other.Positions(slot))
                    || !Equal(one.Facets(slot), other.Facets(slot)))
                    return false;
            }

            return true;
        }

        static bool Places(IList<ToolbarSlot> one, IList<ToolbarSlot> other)
        {
            if (one == null || other == null)
                return one == null && other == null;

            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (!one[i].Same(other[i]))
                    return false;
            }

            return true;
        }

        static bool Equal<T>(IList<T> one, IList<T> other)
        {
            if (one == null || other == null)
                return one == null && other == null;

            if (one.Count != other.Count)
                return false;

            for (var i = 0; i < one.Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(one[i], other[i]))
                    return false;
            }

            return true;
        }

        /// <remarks>A block <paramref name="other" /> has lost is copied as it was, so it stays lost.</remarks>
        static string Declared(string name, SprocketUnit other, Automation program, GridWorld world)
        {
            var declared = other.Program.FindRef(name);
            if (declared == null)
                return name;

            var found = Find(declared, other.World);
            if (found != null && Find(declared, world) != null)
                return program.Declare(world, found).Name;

            var copy = new EntityRef(program.Free(declared.Name), declared.KindId);
            copy.Keys.AddRange(declared.Keys);
            return program.Restore(copy).Name;
        }

        /// <remarks>By id alone: a split keeps every id, but not what a place points at.</remarks>
        static int Count(SystemInstance instance, Automation program, GridWorld world)
        {
            var count = 0;
            foreach (var slot in instance.Filled)
            {
                foreach (var name in instance.FilledWith(slot))
                {
                    var declared = program.FindRef(name);
                    if (declared != null && ById(declared, world) != null)
                        count++;
                }
            }

            return count;
        }

        static GridEntity ById(EntityRef declared, GridWorld world)
        {
            foreach (var key in declared.Keys)
            {
                if (key.Cells != null || key.Kind != ValueKind.Integer)
                    continue;

                long id;
                world.Values.TryInteger(key.Kind, key.Value, out id);
                return world.FindEntity(id);
            }

            return null;
        }

        static GridEntity Find(EntityRef declared, GridWorld world)
        {
            int matched;
            var found = world.Keys.Match(declared.Keys, out matched);
            return found != null && (declared.KindId.Length == 0 || found.Kind.Id == declared.KindId) ? found : null;
        }
    }
}
