using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>Joins two automations into one, and divides one between the halves of a construct that parted.</summary>
    public static class Meeting
    {
        /// <summary>The program <paramref name="kept" /> holds with <paramref name="other" />'s logic systems joined in, adding each pair of differing versions to <paramref name="conflicts" />.</summary>
        public static string Join(SprocketUnit kept, SprocketUnit other, List<ConflictMemory> conflicts, out string switches)
        {
            var values = kept.World.Values;
            var program = Read(kept);

            // Plain systems become blueprints first, as the editor makes them.
            foreach (var system in new List<AutomationSystem>(other.Program.Systems))
            {
                if (!system.IsBlueprint)
                    other.Editor.Blueprinted(system);
            }

            foreach (var system in other.Program.Systems)
            {
                var here = program.FindSystem(system.Identity);
                if (here != null && ProgramText.Said(here) == ProgramText.Said(system))
                {
                    Bring(system, here, other, program, kept.World);
                    continue;
                }

                var identity = here == null ? system.Identity : Guid.NewGuid();
                var trouble = ProgramText.AddBlueprint(ProgramText.Blueprint(system, identity), program, values);
                var arrived = program.FindSystem(identity);
                if (trouble.Length > 0 || arrived == null)
                {
                    Told.Say($"{system.Name} could not join the automation it met, and stays where it was: {trouble}");
                    continue;
                }

                arrived.Changed = system.Changed;
                arrived.Origin = system.Origin;
                arrived.OriginChanged = system.OriginChanged;
                arrived.Enabled = system.Enabled;
                Bring(system, arrived, other, program, kept.World);

                if (here == null)
                    continue;

                here.Enabled = false;
                arrived.Enabled = false;
                conflicts.Add(new ConflictMemory { System = here.Identity, Waiting = identity });
            }

            switches = StateText.Save(program, null, false);
            return ProgramText.Save(program, values);
        }

        /// <summary>The program <paramref name="unit" /> keeps after its construct parted from <paramref name="away" />.</summary>
        public static string Part(SprocketUnit unit, GridWorld away, out string switches)
        {
            var program = Read(unit);
            foreach (var system in new List<AutomationSystem>(program.Systems))
            {
                if (!system.IsBlueprint || system.Instances.Count == 0)
                    continue;

                foreach (var instance in new List<SystemInstance>(system.Instances))
                {
                    if (Count(instance, program, unit.World) == 0 && Count(instance, program, away) > 0)
                        system.RemoveInstance(instance);
                }

                if (system.Instances.Count == 0)
                    program.RemoveSystem(system);
            }

            Rekey(program, unit.World);
            switches = StateText.Save(program, null, false);
            return ProgramText.Save(program, unit.World.Values);
        }

        /// <summary>Adds a copy of <paramref name="instance" /> to <paramref name="into" />, unless an identical installation is there already.</summary>
        public static void Bring(SystemInstance instance, AutomationSystem into, Func<string, string> named)
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
                    return;
                }
            }
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

                world.Identify(found, fresh);
                var place = fresh.FindIndex(key => key.Cells != null);
                if (place < 0)
                    keys.RemoveAt(at);
                else
                    keys[at] = fresh[place];
            }
        }

        static Automation Read(SprocketUnit unit)
        {
            string text;
            string state;
            unit.Written(out text, out state);

            var program = new Automation();
            ProgramText.Load(text, program, unit.World.Values);
            StateText.Load(StateText.Save(unit.Program, null, false), program, null);
            return program;
        }

        static void Bring(AutomationSystem from, AutomationSystem into, SprocketUnit other, Automation program, GridWorld world)
        {
            foreach (var instance in from.Instances)
                Bring(instance, into, name => Declared(name, other, program, world));
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
            var found = world.Match(declared.Keys, out matched);
            return found != null && (declared.KindId.Length == 0 || found.Kind.Id == declared.KindId) ? found : null;
        }
    }
}
