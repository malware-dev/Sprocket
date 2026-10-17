using System;
using System.Collections.Generic;
using System.Globalization;
using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>Writes a program out as its script, whole, or one logic system or blueprint alone.</summary>
    public static class Scribe
    {
        static readonly Literal[] NoArguments = new Literal[0];

        /// <summary>The program <paramref name="program" /> written out, as the block keeps it.</summary>
        public static string Save(Automation program, Values values)
        {
            return Save(program, values, null);
        }

        /// <summary>
        /// The program <paramref name="program" /> written out with <paramref name="with" /> in the place of the system of its
        /// identity, that system's instances keeping their answers to every slot <paramref name="with" /> uses.
        /// </summary>
        /// <remarks>For putting a newer copy of a system blueprint in the place of the one on the construct.</remarks>
        public static string Replacing(Automation program, Values values, AutomationSystem with)
        {
            return Save(program, values, with);
        }

        /// <summary>The program written out, with <paramref name="with" /> in the place of the system of its identity where it is not null.</summary>
        static string Save(Automation program, Values values, AutomationSystem with)
        {
            var pen = new Pen();
            pen.Note(program.Annotation);
            pen.Start(Keyword.Sprocket).Whole(Format.Version).Says(Keyword.Alpha).Whole(Format.Draft);
            pen.End();

            // Before anything that names one, so a reader has them in hand by the time it meets a step. A variable
            // nothing names is left out.
            var blocks = new HashSet<string>(StringComparer.Ordinal);
            var triggers = new HashSet<string>(StringComparer.Ordinal);
            Used(program, blocks, triggers);

            var refs = program.Refs;
            var first = true;
            for (var i = 0; i < refs.Count; i++)
            {
                if (!blocks.Contains(refs[i].Name))
                    continue;

                if (first)
                    pen.Blank();

                first = false;
                WriteRef(pen, values, refs[i]);
            }

            // After the blocks, since each is on one of them.
            var held = program.SignalRefs;
            for (var i = 0; i < held.Count; i++)
            {
                if (triggers.Contains(held[i].Name))
                    WriteSignalRef(pen, held[i]);
            }

            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, Written(systems[i], with), Written(systems[i], with).Identity, true, true, true);

            // After every system, because an instance names the system it is one of and a reader
            // should have met it by then.
            for (var i = 0; i < systems.Count; i++)
            {
                var written = Written(systems[i], with);
                var instances = systems[i].Instances;
                for (var n = 0; n < instances.Count; n++)
                    WriteInstance(pen, program, written, instances[n], written != systems[i]);
            }

            return pen.ToString();
        }

        /// <summary><paramref name="with" /> where it is the same system as <paramref name="system" />, by its identity, and <paramref name="system" /> otherwise.</summary>
        static AutomationSystem Written(AutomationSystem system, AutomationSystem with)
        {
            return with != null && with.Identity == system.Identity ? with : system;
        }

        /// <summary>The blueprint <paramref name="system" /> written out alone, as a player's library keeps it.</summary>
        /// <remarks>A blueprint names nothing outside itself, so the system is the whole of it; its instances are the construct's.</remarks>
        public static string Blueprint(AutomationSystem system)
        {
            return Blueprint(system, system.Identity);
        }

        /// <summary>The blueprint <paramref name="system" /> written out alone under the identity <paramref name="identity" />, as the library keeps the blueprint it is linked to.</summary>
        /// <remarks>A blueprint as it is stored carries no link of its own.</remarks>
        public static string Blueprint(AutomationSystem system, Guid identity)
        {
            var pen = new Pen();
            pen.Start(Keyword.Sprocket).Whole(Format.Version).Says(Keyword.Alpha).Whole(Format.Draft);
            pen.End();
            WriteSystem(pen, system, identity, true, false, false);
            return pen.ToString();
        }

        /// <summary>The logic system <paramref name="system" /> written out alone with its own identity and the blueprint it is linked to, as a backup keeps it.</summary>
        public static string Linked(AutomationSystem system)
        {
            var pen = new Pen();
            pen.Start(Keyword.Sprocket).Whole(Format.Version).Says(Keyword.Alpha).Whole(Format.Draft);
            pen.End();
            WriteSystem(pen, system, system.Identity, true, false, true);
            return pen.ToString();
        }

        /// <summary>The blueprints <paramref name="systems" /> written out together, as a backup keeps them: no blocks, and no installations.</summary>
        public static string Blueprints(IList<AutomationSystem> systems)
        {
            var pen = new Pen();
            pen.Start(Keyword.Sprocket).Whole(Format.Version).Says(Keyword.Alpha).Whole(Format.Draft);
            pen.End();
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i], systems[i].Identity, true, false, false);

            return pen.ToString();
        }

        /// <summary>What <paramref name="system" /> says, written out alone without when it last changed: what tells whether it has.</summary>
        public static string Said(AutomationSystem system)
        {
            var pen = new Pen();
            WriteSystem(pen, system, system.Identity, false, false, false);
            return pen.ToString();
        }

        /// <summary>
        /// The variables something in <paramref name="program" /> names: in <paramref name="blocks" /> those holding a
        /// block, in <paramref name="triggers" /> those holding a trigger.
        /// </summary>
        /// <remarks>
        /// A system names variables in its steps and triggers; a blueprint names only its slots, and its instances name the
        /// variables answering them. A signal variable names the block it is on.
        /// </remarks>
        static void Used(Automation program, HashSet<string> blocks, HashSet<string> triggers)
        {
            var named = new List<string>();
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if (!system.IsBlueprint)
                {
                    system.Acted(named);
                    foreach (var name in named)
                    {
                        if (program.FindSignalRef(name) != null)
                            triggers.Add(name);
                        else
                            blocks.Add(name);
                    }

                    continue;
                }

                var instances = system.Instances;
                for (var n = 0; n < instances.Count; n++)
                {
                    foreach (var slot in instances[n].Filled)
                    {
                        var filled = instances[n].FilledWith(slot);
                        var at = instances[n].Positions(slot);
                        for (var f = 0; f < filled.Count; f++)
                        {
                            blocks.Add(filled[f]);
                            var declared = Held(program, filled, at, f);
                            if (declared != null)
                                triggers.Add(declared.Name);
                        }
                    }
                }
            }

            var held = program.SignalRefs;
            for (var i = 0; i < held.Count; i++)
            {
                for (var n = 0; triggers.Contains(held[i].Name) && n < held[i].Blocks.Count; n++)
                    blocks.Add(held[i].Blocks[n]);
            }
        }

        /// <summary><c>let door1 = block 'Door/LargeBlockDoor' [ 7485…, "Airlock Inner Door" ]</c></summary>
        static void WriteRef(Pen pen, Values values, EntityRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Start(Keyword.Let).Word(declared.Name).Says(Keyword.Equals).Says(Keyword.Block).Word(BlockKinds.Sort(declared.KindId));
            pen.OpenList();

            var keys = declared.Keys;
            foreach (var key in keys)
                WriteKey(pen, values, key);

            pen.CloseList();
            pen.End();
        }

        /// <summary>
        /// One of a declaration's keys, written as whatever sort of value it is.
        /// </summary>
        /// <remarks>
        /// The syntax carries the kind, so nothing here tags anything: a whole number reads back as a
        /// whole number because that is what it looks like. Which is the point - this reader does not know what
        /// a key means, only what it is, and the host that minted the list is the only thing that reads
        /// it.
        /// </remarks>
        static void WriteKey(Pen pen, Values values, EntityKey key)
        {
            if (key.Cells != null)
            {
                pen.OpenList();
                for (var i = 0; i < key.Cells.Length; i++)
                    pen.Vector(key.Cells[i].X, key.Cells[i].Y, key.Cells[i].Z);

                pen.CloseList();
                return;
            }

            if (key.Kind == ValueKind.Text)
            {
                string text;
                values.TryText(key.Kind, key.Value, out text);
                pen.Text(text);
                return;
            }

            if (key.Kind == ValueKind.Integer)
            {
                long whole;
                values.TryInteger(key.Kind, key.Value, out whole);
                pen.Whole(whole);
                return;
            }

            if (key.Kind == ValueKind.Flag)
            {
                pen.Says(values.Flag(key.Kind, key.Value) ? Keyword.On : Keyword.Off);
                return;
            }

            if (key.Kind == ValueKind.Color)
            {
                byte r, g, b, a;
                values.TryColor(key.Kind, key.Value, out r, out g, out b, out a);
                pen.Colour(r, g, b, a);
                return;
            }

            double real;
            values.TryReal(ValueKind.Real, key.Value, out real);
            pen.Real(real);
        }

        /// <summary><c>let button = ext.toolbar(1, 1, 4) on [ panel1 ]</c>: a signal, sent from a slot on the toolbars of blocks above.</summary>
        static void WriteSignalRef(Pen pen, SignalRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Start(Keyword.Let).Word(declared.Name).Says(Keyword.Equals).Signal(declared.Blocks, declared.At);
            pen.End();
        }

        /// <summary>
        /// Writes <paramref name="system" /> as <paramref name="identity" />, with when it last changed where
        /// <paramref name="dated" />, where <paramref name="kept" /> the slots nothing in it uses that an installation holds
        /// blocks in, and where <paramref name="linked" /> the blueprint it was made from.
        /// </summary>
        static void WriteSystem(Pen pen, AutomationSystem system, Guid identity, bool dated, bool kept, bool linked)
        {
            pen.Blank();
            pen.Note(system.Annotation);
            pen.Start(Keyword.System);
            if (system.IsBlueprint)
                pen.Says(Keyword.Blueprint);

            pen.Text(identity.ToString("D")).Says(Keyword.As).Qualified(system.Namespace, system.Name);

            if (dated && system.Changed.HasValue)
            {
                pen.Says(Keyword.Changed)
                    .Text(system.Changed.Value.ToString(Format.Time, CultureInfo.InvariantCulture));
            }

            if (system.LastSequenceNumber > system.HighestSequenceNumber())
                pen.Says(Keyword.Last).Says(Keyword.Sequence).Whole(system.LastSequenceNumber);

            // Last, so the date before it is read as the system's own and the one after it as the blueprint's.
            if (linked && system.HasOrigin)
            {
                pen.Says(Keyword.From).Text(system.Origin.ToString("D"));
                if (system.OriginChanged.HasValue)
                    pen.Text(system.OriginChanged.Value.ToString(Format.Time, CultureInfo.InvariantCulture));
            }

            pen.Body();

            // The slots before what uses them, so a reader meets the vocabulary before the answers.
            var used = new List<string>();
            system.Acted(used);

            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (used.Contains(parts[i].Id) || kept && Held(system, parts[i].Id))
                    WriteSlot(pen, parts[i]);
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                WriteTrigger(pen, triggers[i]);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                WriteSequence(pen, sequences[i]);

            pen.Shut();
        }

        /// <summary>
        /// <c>required slot inner expects Door</c>: something the construct has to provide, or may leave out where it is
        /// <c>optional</c>.
        /// </summary>
        /// <remarks>
        /// <c>required</c> or <c>optional</c> heads it, so every line that starts with either word is something the ship
        /// answers rather than something Sprocket does. What a slot for a block expects is written only when the author said
        /// it; nothing means any block may fill it.
        /// </remarks>
        static void WriteSlot(Pen pen, Part part)
        {
            if (part.Annotation.Length > 0)
                pen.Blank();

            pen.Note(part.Annotation);
            pen.Start(part.Optional ? Keyword.Optional : Keyword.Required).Says(Keyword.Slot).Word(part.Id);

            if (part.IsSignal)
                pen.Says(Keyword.Expects).Says(Keyword.Signal);
            else if (part.Holds.Length > 0)
                pen.Says(Keyword.Expects).Qualified(Space(part.Holds), Name(part.Holds));
            else if (part.Expects.Length > 0)
                pen.Says(Keyword.Expects).Word(part.Expects);

            pen.End();
        }

        /// <summary>The namespace of a qualified id: <c>ext</c> of <c>ext.screen</c>.</summary>
        static string Space(string id)
        {
            var dot = id.IndexOf('.');
            return dot < 0 ? "" : id.Substring(0, dot);
        }

        /// <summary>The name of a qualified id: <c>screen</c> of <c>ext.screen</c>.</summary>
        static string Name(string id)
        {
            return id.Substring(id.IndexOf('.') + 1);
        }

        /// <summary>
        /// Writes <paramref name="instance" /> of <paramref name="system" />; where <paramref name="replacing" />, it is an
        /// instance of another copy of the system, and keeps only its answers to the slots <paramref name="system" /> uses.
        /// </summary>
        /// <remarks>
        /// <c>instance 1 of "3f2a…" as 'Port airlock' { inner = [ door1 ] }</c>: one use of a system blueprint, on this
        /// construct. It sits outside the blueprint rather than inside it, because the blueprint is what is shared and which
        /// of somebody's blocks answer its slots is not.
        /// </remarks>
        static void WriteInstance(Pen pen, Automation program, AutomationSystem system, SystemInstance instance, bool replacing)
        {
            pen.Blank();
            pen.Note(instance.Annotation);
            pen.Start(Keyword.Instance).Whole(instance.Number)
                .Says(Keyword.Of).Text(system.Identity.ToString("D"));

            if (instance.HasName)
                pen.Says(Keyword.As).Word(instance.Name);

            var parts = Sorted(system, instance, replacing);
            if (parts.Count == 0)
            {
                pen.End();
                return;
            }

            pen.Body();
            foreach (var part in parts)
            {
                pen.Note(instance.NoteOn(part));
                pen.Head(part).Says(Keyword.Equals);

                var positions = instance.Positions(part);
                var facets = instance.Facets(part);
                var type = facets == null ? null : HostTypes.Named(system.FindPart(part)?.Holds);
                if (positions != null)
                    Slots(pen, program, instance.FilledWith(part), positions);
                else if (type != null)
                    Facets(pen, type, instance.FilledWith(part), facets);
                else
                    WriteNames(pen, instance.FilledWith(part));

                pen.End();
            }

            pen.Shut();
        }

        /// <summary>
        /// Which parts <paramref name="instance" /> fills, in a settled order, leaving out a part nothing in the system uses and no installation
        /// holds blocks in.
        /// </summary>
        /// <remarks>
        /// Sorted, so a saved file does not churn between runs for no reason: a dictionary hands its
        /// keys back in whatever order it likes, and a save that differs every time is one that gets
        /// written every time and reads as a change in a diff.
        /// </remarks>
        static List<string> Sorted(AutomationSystem system, SystemInstance instance, bool replacing)
        {
            var used = new List<string>();
            system.Acted(used);

            // An answer to a slot the system declares nothing about is kept as it is, except for an instance of another copy
            // of the system, where it answers a slot the copy has done away with.
            var parts = new List<string>();
            foreach (var slot in instance.Filled)
            {
                if (used.Contains(slot) || !replacing && (system.FindPart(slot) == null || Held(system, slot)))
                    parts.Add(slot);
            }

            parts.Sort(StringComparer.Ordinal);
            return parts;
        }

        /// <summary>Whether an installation of <paramref name="system" /> holds blocks in <paramref name="slot" />.</summary>
        static bool Held(AutomationSystem system, string slot)
        {
            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
            {
                var filled = instances[i].FilledWith(slot);
                if (filled != null && filled.Count > 0)
                    return true;
            }

            return false;
        }

        /// <summary>The answers to a slot of the type <paramref name="type" /> as a list, each block's facet written in place on it.</summary>
        static void Facets(Pen pen, HostType type, IList<string> blocks, IList<int> facets)
        {
            pen.OpenList();
            for (var i = 0; blocks != null && i < blocks.Count && i < facets.Count; i++)
                pen.Facet(type, new[] { blocks[i] }, facets[i]);

            pen.CloseList();
        }

        /// <summary>
        /// A signal slot's answers as a list: by the variable holding them where one holds every block it names among them,
        /// once, and otherwise each written in place on its block.
        /// </summary>
        static void Slots(Pen pen, Automation program, IList<string> blocks, IList<ToolbarSlot> at)
        {
            var written = new List<SignalRef>();
            pen.OpenList();
            for (var i = 0; blocks != null && i < blocks.Count && i < at.Count; i++)
            {
                var declared = Held(program, blocks, at, i);
                if (declared == null)
                    pen.Signal(new[] { blocks[i] }, at[i]);
                else if (!written.Contains(declared))
                {
                    written.Add(declared);
                    pen.Word(declared.Name);
                }
            }

            pen.CloseList();
        }

        /// <summary>
        /// The signal variable answer <paramref name="index" /> of a slot is written as: one holding that slot on that block
        /// and on every other block it names among the answers, or null where none does.
        /// </summary>
        /// <remarks>Every block, because a variable reads back as all of its blocks.</remarks>
        static SignalRef Held(Automation program, IList<string> blocks, IList<ToolbarSlot> at, int index)
        {
            if (at == null || index >= at.Count)
                return null;

            var declared = program.FindSignalRef(blocks[index], at[index]);
            if (declared == null)
                return null;

            for (var i = 0; i < declared.Blocks.Count; i++)
            {
                var found = false;
                for (var n = 0; n < blocks.Count && n < at.Count && !found; n++)
                    found = blocks[n] == declared.Blocks[i] && at[n].Same(declared.At);

                if (!found)
                    return null;
            }

            return declared;
        }

        static void WriteSequence(Pen pen, Sequence sequence)
        {
            pen.Blank();
            pen.Note(sequence.Annotation);
            pen.Start(Keyword.Sequence).Whole(sequence.Number);
            if (sequence.HasName)
                pen.Says(Keyword.As).Word(sequence.Name);

            pen.Body();

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
            {
                var opens = steps[i] as If;
                var branch = steps[i] as Else;
                if (opens != null)
                    WriteIf(pen, opens);
                else if (branch != null)
                    WriteElse(pen, branch);
                else if (steps[i] is EndIf)
                    pen.Shut();
                else
                    WriteStep(pen, steps[i]);
            }

            pen.Shut();
        }

        /// <summary><c>if all ext.filled on [ vent ] == on then {</c>: the line an if starts, and its block opened.</summary>
        static void WriteIf(Pen pen, If step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Start(Keyword.If, step.IsOff);
            WriteTest(pen, step.Test);
            pen.Body();
        }

        /// <summary><c>} else if … then {</c> or <c>} else {</c>: the branch before closed, and the next one opened.</summary>
        static void WriteElse(Pen pen, Else step)
        {
            pen.Shut();
            pen.Note(step.Annotation);
            pen.Start(Keyword.Else);
            if (step.Test != null)
            {
                pen.Says(Keyword.If);
                WriteTest(pen, step.Test);
            }

            pen.Body();
        }

        /// <summary><c>all ext.filled on [ vent ] == on then</c>.</summary>
        static void WriteTest(Pen pen, Comparison test)
        {
            pen.Says(Said(test.Quantifier));
            WriteMember(pen, test.Provider, test.MemberId, test.Arguments, test.Targets);
            pen.Says(Said(test.Relation));
            Written(pen, test.Value);
            pen.Says(Keyword.Then);
        }

        /// <summary><c>ext.amount("Ingot/Iron") on [ cargo ]</c>: a member, bare or qualified, with its arguments where it has any, and its blocks.</summary>
        static void WriteMember(Pen pen, string provider, string id, IList<Literal> arguments, IList<string> targets)
        {
            pen.Qualified(provider, id);
            if (arguments.Count > 0)
            {
                pen.Open();
                for (var i = 0; i < arguments.Count; i++)
                    Written(pen, arguments[i]);

                pen.Close();
            }

            pen.Says(Keyword.On);
            WriteNames(pen, targets);
        }

        /// <summary>How <paramref name="quantifier" /> is written.</summary>
        public static Keyword Said(Quantifier quantifier)
        {
            switch (quantifier)
            {
                case Quantifier.All: return Keyword.All;
                case Quantifier.Sum: return Keyword.Sum;
                case Quantifier.Average: return Keyword.Average;
            }

            return Keyword.Any;
        }

        /// <summary>How <paramref name="relation" /> is written.</summary>
        public static Keyword Said(Relation relation)
        {
            switch (relation)
            {
                case Relation.IsNot: return Keyword.NotEqualTo;
                case Relation.Above: return Keyword.Above;
                case Relation.AtLeast: return Keyword.AtLeast;
                case Relation.Below: return Keyword.Below;
                case Relation.AtMost: return Keyword.AtMost;
            }

            return Keyword.EqualTo;
        }

        /// <summary>
        /// <c>whenever button then run 1</c>, <c>when all ext.status on [ inner ] == closed then run 2</c>, or <c>every 600t stop 3</c>.
        /// </summary>
        /// <remarks>
        /// A trigger says the step it does, written as a sequence writes it, because it belongs to the system rather than
        /// to one sequence.
        /// </remarks>
        static void WriteTrigger(Pen pen, Trigger trigger)
        {
            pen.Blank();
            pen.Note(trigger.Annotation);

            var needed = trigger as SignalTrigger;
            if (needed != null)
            {
                pen.Start(Keyword.Whenever);
                if (!needed.IsWritten)
                    pen.Word(needed.Part);
                else if (needed.At != null)
                    pen.Signal(needed.Sources, needed.At);
                else
                    WriteMember(pen, needed.Provider, needed.EventId, NoArguments, needed.Sources);

                pen.Says(Keyword.Then);
                WriteDoes(pen, trigger.Does);
                pen.End();
                return;
            }

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(Said(when.Quantifier));
                WriteMember(pen, when.Provider, when.EventId, when.Arguments, when.Sources);
                pen.Says(Said(when.Relation));
                Written(pen, when.Value);
                if (when.IntervalTicks > 0)
                    Lasting(pen.Says(Keyword.Every), when.Interval);

                pen.Says(Keyword.Then);
                WriteDoes(pen, trigger.Does);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                Lasting(pen.Start(Keyword.Every), every.Interval);
                WriteDoes(pen, trigger.Does);
                pen.End();
            }
        }

        /// <summary>The step a trigger does, on the line the trigger is written on.</summary>
        static void WriteDoes(Pen pen, Step step)
        {
            pen.Says(Head(step));
            Rest(pen, step);
        }

        /// <summary>Writes <paramref name="length" /> as the script had it: in seconds, or in ticks.</summary>
        static void Lasting(Pen pen, Duration length)
        {
            if (length.InSeconds)
                pen.Seconds(length.Seconds);
            else
                pen.Ticks(length.Ticks);
        }

        /// <summary>A step: an action on some parts, a value set on them, a sequence run or stopped, an await, or a length of time.</summary>
        static void WriteStep(Pen pen, Step step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Start(Head(step), step.IsOff);
            Rest(pen, step);
            pen.End();
        }

        /// <summary>The word a step starts with.</summary>
        static Keyword Head(Step step)
        {
            // A value is a set; an action is a call, bare or qualified by its namespace.
            var act = step as Act;
            if (act != null)
                return act.Form == Nature.Value ? Keyword.Set : Keyword.Call;

            if (step is Wait)
                return Keyword.Wait;

            var run = step as Run;
            if (run != null)
                return run.Stops ? Keyword.Stop : Keyword.Run;

            return Keyword.Await;
        }

        /// <summary>Everything in a step after the word it starts with.</summary>
        static void Rest(Pen pen, Step step)
        {
            var act = step as Act;
            if (act != null)
            {
                if (act.Form == Nature.Value)
                {
                    WriteMember(pen, act.Provider, act.ActionId, NoArguments, act.Targets);
                    if (act.Arguments.Count > 0)
                    {
                        pen.Says(Keyword.To);
                        Written(pen, act.Arguments[0]);
                    }
                }
                else
                {
                    WriteMember(pen, act.Form == Nature.Native ? act.Provider : "", act.ActionId, act.Arguments, act.Targets);
                }

                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                Lasting(pen, wait.Length);
                return;
            }

            var run = step as Run;
            if (run != null)
                pen.Whole(run.Number);
        }

        /// <summary>Writes <paramref name="value" /> back as the kind it is.</summary>
        static void Written(Pen pen, Literal value)
        {
            if (value.Kind == ValueKind.Text)
            {
                pen.Text(value.Text);
                return;
            }

            if (value.Kind == ValueKind.Name)
            {
                pen.Word(value.Text);
                return;
            }

            if (value.Kind == ValueKind.Flag)
            {
                pen.Says(value.Flag ? Keyword.On : Keyword.Off);
                return;
            }

            if (value.Kind == ValueKind.Integer)
            {
                pen.Whole(value.Whole);
                return;
            }

            if (value.Kind == ValueKind.Real)
            {
                pen.Real(value.Real);
                return;
            }

            pen.Colour(value.Red, value.Green, value.Blue, value.Alpha);
        }

        static void WriteNames(Pen pen, IList<string> names)
        {
            pen.OpenList();
            for (var i = 0; i < names.Count; i++)
                pen.Word(names[i]);

            pen.CloseList();
        }
    }
}
