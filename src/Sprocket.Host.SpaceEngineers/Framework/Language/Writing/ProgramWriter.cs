using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Language.Reading;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Writing
{
    /// <summary>Writes a program out as its script, whole, or one logic system or blueprint alone.</summary>
    public static class ProgramWriter
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
        public static string Replacing(Automation program, Values values, AutomationSystem with)
        {
            return Save(program, values, with);
        }

        /// <summary>The program written out, with <paramref name="with" /> in the place of the system of its identity where it is not null.</summary>
        static string Save(Automation program, Values values, AutomationSystem with)
        {
            var pen = new ScriptWriter();
            pen.Note(program.Annotation);
            pen.Attributes(program.Attributes);
            Head(pen);
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

            // After every system, since an instance names the system it is one of.
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
        /// <remarks>The system alone, without its instances.</remarks>
        public static string Blueprint(AutomationSystem system)
        {
            return Blueprint(system, system.Identity);
        }

        /// <summary>Writes the version line this build writes, with its draft where the version is unfinished.</summary>
        static void Head(ScriptWriter pen)
        {
            pen.Start(ScriptFormat.Header, false).Whole(ScriptFormat.Version);
            if (ScriptFormat.IsDraft)
                pen.Says(Keyword.Alpha).Whole(ScriptFormat.Draft);
        }

        /// <summary>The blueprint <paramref name="system" /> written out alone under the identity <paramref name="identity" />, as the library keeps the blueprint it is linked to.</summary>
        /// <remarks>A blueprint as it is stored carries no link of its own.</remarks>
        public static string Blueprint(AutomationSystem system, Guid identity)
        {
            var pen = new ScriptWriter();
            Head(pen);
            pen.End();
            WriteSystem(pen, system, identity, true, false, false);
            return pen.ToString();
        }

        /// <summary>The logic system <paramref name="system" /> written out alone with its own identity and the blueprint it is linked to, as a backup keeps it.</summary>
        public static string Linked(AutomationSystem system)
        {
            var pen = new ScriptWriter();
            Head(pen);
            pen.End();
            WriteSystem(pen, system, system.Identity, true, false, true);
            return pen.ToString();
        }

        /// <summary>The blueprints <paramref name="systems" /> written out together, as a backup keeps them: no blocks, and no installations.</summary>
        public static string Blueprints(IList<AutomationSystem> systems)
        {
            var pen = new ScriptWriter();
            Head(pen);
            pen.End();
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i], systems[i].Identity, true, false, false);

            return pen.ToString();
        }

        /// <summary>What <paramref name="system" /> says, written out alone without when it last changed: what tells whether it has.</summary>
        public static string Said(AutomationSystem system)
        {
            var pen = new ScriptWriter();
            WriteSystem(pen, system, system.Identity, false, false, false);
            return pen.ToString();
        }

        /// <summary>
        /// The variables something in <paramref name="program" /> names: in <paramref name="blocks" /> those holding a
        /// block, in <paramref name="triggers" /> those holding a trigger.
        /// </summary>
        static void Used(Automation program, HashSet<string> blocks, HashSet<string> triggers)
        {
            var named = new List<string>();
            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
            {
                var system = systems[i];
                if (!string.IsNullOrEmpty(system.ListedOn))
                    blocks.Add(system.ListedOn);

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
        static void WriteRef(ScriptWriter pen, Values values, EntityRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Attributes(declared.Attributes);
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
        static void WriteKey(ScriptWriter pen, Values values, EntityKey key)
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
                pen.Color(r, g, b, a);
                return;
            }

            double real;
            values.TryReal(ValueKind.Real, key.Value, out real);
            pen.Real(real);
        }

        /// <summary><c>let button = sprocket.toolbar(1, 1, 4) on [ panel1 ]</c>: a signal, sent from a slot on the toolbars of blocks above.</summary>
        static void WriteSignalRef(ScriptWriter pen, SignalRef declared)
        {
            pen.Note(declared.Annotation);
            pen.Attributes(declared.Attributes);
            pen.Start(Keyword.Let).Word(declared.Name).Says(Keyword.Equals).Signal(declared.Blocks, declared.At);
            pen.End();
        }

        /// <summary>
        /// Writes <paramref name="system" /> as <paramref name="identity" />, with when it last changed where
        /// <paramref name="dated" />, where <paramref name="kept" /> the slots nothing in it uses that an installation holds
        /// blocks in, and where <paramref name="linked" /> the blueprint it was made from.
        /// </summary>
        static void WriteSystem(ScriptWriter pen, AutomationSystem system, Guid identity, bool dated, bool kept, bool linked)
        {
            pen.Blank();
            pen.Note(system.Annotation);

            // Sprocket's own: the highest number it has handed out, worth writing only where it is higher than every sequence.
            if (system.LastSequenceNumber > system.HighestSequenceNumber())
                pen.Attribute(ScriptAttributes.LastSequence, system.LastSequenceNumber.ToString(CultureInfo.InvariantCulture));

            if (kept && !string.IsNullOrEmpty(system.ListedOn))
                pen.AttributeText(ScriptAttributes.ListedOn, system.ListedOn);

            pen.Attributes(system.Attributes);
            pen.Start(Keyword.System);
            if (system.IsBlueprint)
                pen.Says(Keyword.Blueprint);

            pen.Text(identity.ToString("D")).Says(Keyword.As).Qualified(system.Namespace, system.Name);

            if (dated && system.Changed.HasValue)
                pen.Line(Keyword.Changed).Text(system.Changed.Value.ToString(ScriptFormat.Time, CultureInfo.InvariantCulture));

            if (linked && system.HasOrigin)
            {
                pen.Line(Keyword.From).Says(Keyword.Blueprint).Text(system.Origin.ToString("D"));
                if (system.OriginChanged.HasValue)
                    pen.Text(system.OriginChanged.Value.ToString(ScriptFormat.Time, CultureInfo.InvariantCulture));
            }

            pen.BodyBelow();

            // The slots before what uses them, so a reader meets the vocabulary before the answers. A setting is written
            // whether anything uses it or not, since every installation's answer to it is stored with it.
            var used = new List<string>();
            system.Acted(used);

            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
            {
                if (parts[i].IsSetting || used.Contains(parts[i].Id) || kept && Held(system, parts[i].Id))
                    WriteSlot(pen, system, parts[i]);
            }

            if (system.Types.Count > 0)
                pen.Blank();

            foreach (var type in system.Types)
                WriteType(pen, type);

            if (system.States.Count > 0)
                pen.Blank();

            foreach (var state in system.States)
                WriteState(pen, system, state);

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                WriteTrigger(pen, system, triggers[i]);

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
        /// What a slot for a block expects is written only where the author said it; nothing means any block. A setting is
        /// written with its type, and its default where it has one: <c>optional slot 'Open angle' expects real = 45</c>.
        /// </remarks>
        static void WriteSlot(ScriptWriter pen, AutomationSystem system, Part part)
        {
            if (part.Annotation.Length > 0)
                pen.Blank();

            pen.Note(part.Annotation);
            if (part.Short.Length > 0)
                pen.AttributeText(ScriptAttributes.Short, part.Short);

            if (part.Category.Length > 0)
                pen.AttributeText(ScriptAttributes.Category, part.Category);

            if (part.Editor.Length > 0)
                pen.AttributeText(ScriptAttributes.Editor, part.Editor);

            pen.Attributes(part.Attributes);
            pen.Start(part.Optional ? Keyword.Optional : Keyword.Required).Says(Keyword.Slot).Word(part.Id);

            if (part.IsSetting)
            {
                if (!SettingReader.IsBuiltIn(part.Setting) && (BlockKinds.IsGameType(part.Setting) || Meanings(system, part.Setting) > 1))
                    pen.Says(Keyword.Expects).Kinded("", Keywords.Name(Keyword.Type), part.Setting);
                else
                    pen.Says(Keyword.Expects).Expected(part.Setting);

                if (part.HasDefault)
                {
                    pen.Says(Keyword.Equals);
                    Written(pen, part.Default);
                }
            }
            else if (part.IsSignal)
                pen.Says(Keyword.Expects).Says(Keyword.Signal);
            else if (part.Holds.Length > 0)
                pen.Says(Keyword.Expects).HostQualified(Space(part.Holds), Name(part.Holds));
            else if (part.Expects.Length > 0 && system.FindType(part.Expects) != null)
                pen.Says(Keyword.Expects).Kinded("", Kinds.BlockType, part.Expects);
            else if (part.Expects.Length > 0)
                pen.Says(Keyword.Expects).Expected(part.Expects);

            pen.End();
        }

        /// <summary><c>type side = inside | outside</c>: a type the system declares, its choices in order.</summary>
        static void WriteType(ScriptWriter pen, DeclaredType type)
        {
            pen.Note(type.Annotation);
            pen.Attributes(type.Attributes);
            pen.Start(Keyword.Type).Word(type.Name).Says(Keyword.Equals);
            for (var i = 0; i < type.Choices.Count; i++)
            {
                if (i > 0)
                    pen.Says(Keyword.Bar);

                pen.Word(type.Choices[i]);
            }

            pen.End();
        }

        /// <summary><c>let where = inside</c>: a state the system declares, and the value it starts at.</summary>
        static void WriteState(ScriptWriter pen, AutomationSystem system, State state)
        {
            pen.Note(state.Annotation);
            pen.Attributes(state.Attributes);
            pen.Start(Keyword.Let).Word(state.Name).Says(Keyword.Equals);
            Choice(pen, system, state.Type, state.Start, true);
            pen.End();
        }

        /// <summary>
        /// A state's or a setting's value: a flag, a choice of <paramref name="type" />, or a setting standing in for one, each
        /// written with its kind exactly where alone it could also mean something else.
        /// </summary>
        static void Choice(ScriptWriter pen, AutomationSystem system, string type, Literal value, bool starting)
        {
            if (system != null && value.Kind == ValueKind.Flag && FlagShared(system, Flag(value), starting))
            {
                pen.Kinded("", SettingReader.Flag, Flag(value));
                return;
            }

            if (system != null && value.Kind == ValueKind.Name && Shared(system, type, value.Text, starting))
            {
                if (Meanings(system, type) > 1)
                    pen.Typed(type, value.Text);
                else
                    pen.Kinded("", type, value.Text);

                return;
            }

            if (system != null && value.IsSetting && Shared(system, type, value.Text, starting))
            {
                pen.Kinded("", Kinds.Setting, value.Text);
                return;
            }

            Written(pen, value);
        }

        /// <summary>
        /// Whether <paramref name="name" />, written bare where a value of <paramref name="type" /> stands, could mean more than
        /// one thing: a slot, setting, type, state or sequence of <paramref name="system" />, or a choice of that type, or, where
        /// it is a <paramref name="starting" /> value, of any type, or the flag where it is spelled like one and a flag could stand.
        /// </summary>
        static bool Shared(AutomationSystem system, string type, string name, bool starting)
        {
            var meanings = Meanings(system, name);
            foreach (var declared in system.Types)
            {
                if ((starting || declared.Name == type) && declared.Has(name))
                    meanings++;
            }

            if (Kinds.SpelledLikeFlag(name) && (starting || type.Length == 0 || type == SettingReader.Flag))
                meanings++;

            return meanings > 1;
        }

        /// <summary>
        /// Whether the flag <paramref name="word" />, <c>on</c> or <c>off</c>, written bare where a value stands, could also mean a
        /// thing of <paramref name="system" /> so named, or, where it is a <paramref name="starting" /> value, a choice of any type.
        /// </summary>
        static bool FlagShared(AutomationSystem system, string word, bool starting)
        {
            if (Meanings(system, word) > 0)
                return true;

            foreach (var declared in system.Types)
            {
                if (starting && declared.Has(word))
                    return true;
            }

            return false;
        }

        /// <summary>How <paramref name="value" />, a flag, is spelled: <c>on</c> or <c>off</c>.</summary>
        static string Flag(Literal value)
        {
            return Keywords.Name(value.Flag ? Keyword.On : Keyword.Off);
        }

        /// <summary>How many things of <paramref name="system" /> are called <paramref name="name" />: its slot or setting, type, state and sequence.</summary>
        static int Meanings(AutomationSystem system, string name)
        {
            var meanings = new List<string>();
            Kinds.Meanings(system, name, meanings, new List<string>());
            return meanings.Count;
        }

        /// <summary>
        /// The state <paramref name="state" />, a state or a setting holding <paramref name="type" /> is set to or compared with,
        /// written with its kind, <c>state(home)</c>, where alone it could also mean another thing.
        /// </summary>
        static void State(ScriptWriter pen, AutomationSystem system, string type, string state)
        {
            if (system != null && Shared(system, type, state, false))
                pen.Kinded("", Kinds.State, state);
            else
                pen.Word(state);
        }

        /// <summary>
        /// The state or setting <paramref name="name" />, of the kind <paramref name="kind" />, written with it where alone it could
        /// also mean another thing; where it <paramref name="tests" />, it stands where a test starts.
        /// </summary>
        static void Named(ScriptWriter pen, AutomationSystem system, string kind, string name, bool tests)
        {
            if (system != null && Meanings(system, name) > 1)
                pen.Kinded("", kind, name);
            else if (tests)
                pen.Tested(name);
            else
                pen.Word(name);
        }

        /// <summary>The slots <paramref name="names" />, in brackets, each written <c>slot(side)</c> where alone it could also mean another thing.</summary>
        static void WriteSlots(ScriptWriter pen, AutomationSystem system, IList<string> names)
        {
            pen.OpenList();
            foreach (var name in names)
            {
                if (system != null && system.FindPart(name) != null && Meanings(system, name) > 1)
                    pen.Kinded("", Keywords.Name(Keyword.Slot), name);
                else
                    pen.Word(name);
            }

            pen.CloseList();
        }

        /// <summary>The name of the type of the state called <paramref name="name" />, or empty where it is a flag or not found.</summary>
        static string TypeOf(AutomationSystem system, string name)
        {
            var state = system?.FindState(name);
            return state == null ? "" : state.Type;
        }

        /// <summary>The type the setting called <paramref name="name" /> holds, or empty where it is not found.</summary>
        static string SettingType(AutomationSystem system, string name)
        {
            var part = system?.FindPart(name);
            return part == null ? "" : part.Setting;
        }

        /// <summary>The namespace of a qualified id: <c>sprocket</c> of <c>sprocket.screen</c>.</summary>
        static string Space(string id)
        {
            var dot = id.IndexOf('.');
            return dot < 0 ? "" : id.Substring(0, dot);
        }

        /// <summary>The name of a qualified id: <c>screen</c> of <c>sprocket.screen</c>.</summary>
        static string Name(string id)
        {
            return id.Substring(id.IndexOf('.') + 1);
        }

        /// <summary>
        /// Writes <paramref name="instance" /> of <paramref name="system" />; where <paramref name="replacing" />, it is an
        /// instance of another copy of the system, and keeps only its answers to the slots <paramref name="system" /> uses.
        /// </summary>
        /// <remarks>Written <c>instance 1 of "3f2a…" as 'Port airlock' { inner = [ door1 ] }</c>, outside the blueprint.</remarks>
        static void WriteInstance(ScriptWriter pen, Automation program, AutomationSystem system, SystemInstance instance, bool replacing)
        {
            pen.Blank();
            pen.Note(instance.Annotation);
            pen.Attributes(instance.Attributes);
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
                pen.Attributes(instance.KeptOn(part));
                pen.Head(part).Says(Keyword.Equals);

                Literal answer;
                if (instance.AnswerTo(part, out answer))
                {
                    Written(pen, answer);
                    pen.End();
                    continue;
                }

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
        /// Which parts <paramref name="instance" /> fills or answers, in a settled order, leaving out a part nothing in the system uses and no installation
        /// holds blocks in, and an answer to a setting the system does not have.
        /// </summary>
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

            // An answer to a setting is written where the system has the setting.
            foreach (var setting in instance.Answered)
            {
                var part = system.FindPart(setting);
                if (part != null && part.IsSetting)
                    parts.Add(setting);
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
        static void Facets(ScriptWriter pen, HostType type, IList<string> blocks, IList<int> facets)
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
        static void Slots(ScriptWriter pen, Automation program, IList<string> blocks, IList<ToolbarSlot> at)
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

        static void WriteSequence(ScriptWriter pen, Sequence sequence)
        {
            pen.Blank();
            pen.Note(sequence.Annotation);
            if (sequence.Category.Length > 0)
                pen.AttributeText(ScriptAttributes.Category, sequence.Category);

            pen.Attributes(sequence.Attributes);
            pen.Start(Keyword.Sequence).Whole(sequence.Number);
            if (sequence.HasName)
                pen.Says(Keyword.As).Word(sequence.Name);

            if (sequence.Exclusive.Length > 0)
                pen.Word(SequenceWords.Exclusive).Word(sequence.Exclusive);

            pen.Body();

            var steps = sequence.Steps;
            var handling = false;
            for (var i = 0; i < steps.Count; i++)
            {
                var opens = steps[i] as If;
                var branch = steps[i] as Else;
                var handler = steps[i] as Handler;
                if (opens != null)
                    WriteIf(pen, sequence.System, opens);
                else if (branch != null)
                    WriteElse(pen, sequence.System, branch);
                else if (steps[i] is EndIf)
                    pen.Shut();
                else if (handler != null)
                {
                    if (handling)
                        pen.Shut();

                    WriteHandler(pen, handler);
                    handling = true;
                }
                else
                    WriteStep(pen, sequence.System, steps[i]);
            }

            if (handling)
                pen.Shut();

            pen.Shut();
        }

        /// <summary><c>catch {</c> or <c>finally {</c>: the line a part run as the run ends starts, and its block opened.</summary>
        static void WriteHandler(ScriptWriter pen, Handler step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Attributes(step.Attributes);
            pen.Head(step is Catch ? SequenceWords.Catch : SequenceWords.Finally);
            pen.Body();
        }

        /// <summary><c>if all sprocket.filled on [ vent ] == on then {</c>: the line an if starts, and its block opened.</summary>
        static void WriteIf(ScriptWriter pen, AutomationSystem system, If step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Attributes(step.Attributes);
            pen.Start(Keyword.If, step.IsOff);
            WriteTest(pen, system, step.Test);
            pen.Body();
        }

        /// <summary><c>} else if … then {</c> or <c>} else {</c>: the branch before closed, and the next one opened.</summary>
        static void WriteElse(ScriptWriter pen, AutomationSystem system, Else step)
        {
            pen.Shut();
            pen.Note(step.Annotation);
            pen.Attributes(step.Attributes);
            pen.Start(Keyword.Else);
            if (step.Test != null)
            {
                pen.Says(Keyword.If);
                WriteTest(pen, system, step.Test);
            }

            pen.Body();
        }

        /// <summary>What a branch tests, then <c>then</c>: a member read on blocks, one of the system's states, or the host.</summary>
        static void WriteTest(ScriptWriter pen, AutomationSystem system, BranchTest test)
        {
            var compared = test as Comparison;
            var host = test as HostTest;
            if (compared != null)
                WriteComparison(pen, system, compared);
            else if (host != null)
                WriteHostTest(pen, host);
            else
                WriteStateTest(pen, system, (StateTest)test);
        }

        /// <summary><c>gearbox.lifts is installed then</c> or <c>gearbox.lifts is not installed then</c>: whether the host has a namespace.</summary>
        static void WriteHostTest(ScriptWriter pen, HostTest test)
        {
            pen.Namespace(test.Namespace).Says(Keyword.Is);
            if (!test.Installed)
                pen.Says(Keyword.Not);

            pen.Says(Keyword.Installed).Says(Keyword.Then);
        }

        /// <summary>
        /// <c>where == inside then</c>, <c>where != home then</c> or <c>'Outer light' == on then</c>: a state or a
        /// setting compared with a value or a state.
        /// </summary>
        static void WriteStateTest(ScriptWriter pen, AutomationSystem system, StateTest test)
        {
            if (test.Setting.Length > 0)
                Named(pen, system, Kinds.Setting, test.Setting, true);
            else
                Named(pen, system, Kinds.State, test.State, true);

            pen.Says(test.Relation == Relation.IsNot ? Keyword.NotEqualTo : Keyword.EqualTo);
            if (test.Other.Length > 0)
                State(pen, system, test.Setting.Length > 0 ? SettingType(system, test.Setting) : TypeOf(system, test.State), test.Other);
            else
                Choice(pen, system, test.Setting.Length > 0 ? SettingType(system, test.Setting) : TypeOf(system, test.State), test.Value, false);

            pen.Says(Keyword.Then);
        }

        static void WriteComparison(ScriptWriter pen, AutomationSystem system, Comparison test)
        {
            pen.Says(Said(test.Quantifier));
            WriteMember(pen, system, test.Provider, test.MemberId, test.Arguments, test.Targets);
            pen.Says(Said(test.Relation));
            Word(pen, system, test.Value);
            pen.Says(Keyword.Then);
        }

        /// <summary><c>sprocket.amount("Ingot/Iron") on [ cargo ]</c>: a member, bare or qualified, with its arguments where it has any, and its blocks.</summary>
        static void WriteMember(ScriptWriter pen, AutomationSystem system, string provider, string id, IList<Literal> arguments, IList<string> targets)
        {
            pen.HostQualified(provider, id);
            if (arguments.Count > 0)
            {
                pen.Open();
                for (var i = 0; i < arguments.Count; i++)
                    Word(pen, system, arguments[i]);

                pen.Close();
            }

            pen.Says(Keyword.On);
            WriteSlots(pen, system, targets);
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
        /// <c>whenever button then run 1</c>, <c>when all sprocket.status on [ inner ] == closed then run 2</c>, or <c>every 600t stop 3</c>.
        /// </summary>
        /// <remarks>The step it does is written as a sequence writes it.</remarks>
        static void WriteTrigger(ScriptWriter pen, AutomationSystem system, Trigger trigger)
        {
            pen.Blank();
            pen.Note(trigger.Annotation);
            if (trigger.Category.Length > 0)
                pen.AttributeText(ScriptAttributes.Category, trigger.Category);

            pen.Attributes(trigger.Attributes);

            var needed = trigger as SignalTrigger;
            if (needed != null)
            {
                pen.Start(Keyword.Whenever);
                if (!needed.IsWritten && system != null && system.FindPart(needed.Part) != null && Meanings(system, needed.Part) > 1)
                    pen.Kinded("", Keywords.Name(Keyword.Slot), needed.Part);
                else if (!needed.IsWritten)
                    pen.Word(needed.Part);
                else if (needed.At != null)
                    pen.Signal(needed.Sources, needed.At);
                else
                    WriteMember(pen, system, needed.Provider, needed.EventId, NoArguments, needed.Sources);

                pen.Says(Keyword.Then);
                WriteDoes(pen, system, trigger.Does);
                pen.End();
                return;
            }

            var watched = trigger as StateTrigger;
            if (watched != null)
            {
                pen.Start(Keyword.When);
                WriteStateTest(pen, system, watched.Test);
                WriteDoes(pen, system, trigger.Does);
                pen.End();
                return;
            }

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(Said(when.Quantifier));
                WriteMember(pen, system, when.Provider, when.EventId, when.Arguments, when.Sources);
                pen.Says(Said(when.Relation));
                Word(pen, system, when.Value);
                if (when.IntervalSetting.Length > 0)
                    pen.Says(Keyword.Every).Word(when.IntervalSetting);
                else if (when.IntervalTicks > 0)
                    Lasting(pen.Says(Keyword.Every), when.Interval);

                pen.Says(Keyword.Then);
                WriteDoes(pen, system, trigger.Does);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every == null)
                throw new InvalidOperationException($"a {trigger.GetType().Name} has no way to be written.");

            if (every.IntervalSetting.Length > 0)
                pen.Start(Keyword.Every).Word(every.IntervalSetting);
            else
                Lasting(pen.Start(Keyword.Every), every.Interval);
            WriteDoes(pen, system, trigger.Does);
            pen.End();
        }

        /// <summary>The step a trigger does, on the line the trigger is written on.</summary>
        static void WriteDoes(ScriptWriter pen, AutomationSystem system, Step step)
        {
            pen.Says(Head(step));
            Rest(pen, system, step);
        }

        /// <summary>Writes <paramref name="length" /> as the script had it: in seconds, or in ticks.</summary>
        static void Lasting(ScriptWriter pen, Duration length)
        {
            if (length.InSeconds)
                pen.Seconds(length.Seconds);
            else
                pen.Ticks(length.Ticks);
        }

        /// <summary>A step: an action on some parts, a value set on them, a sequence run or stopped, an await, or a length of time.</summary>
        static void WriteStep(ScriptWriter pen, AutomationSystem system, Step step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);
            pen.Attributes(step.Attributes);
            Started(pen, step);
            Rest(pen, system, step);
            pen.End();
        }

        /// <summary>Starts <paramref name="step" />'s line with the word it starts with: a keyword, or <c>abort</c>, which is none.</summary>
        static void Started(ScriptWriter pen, Step step)
        {
            if (step is Abort)
                pen.Start(AbortWords.Abort, step.IsOff);
            else
                pen.Start(Head(step), step.IsOff);
        }

        /// <summary><c>call open on [ door ]</c>: one step as its line of script, without its note or its attributes, for a log to name it by.</summary>
        /// <remarks>An if or an otherwise-if is its test, and the end of an if its closing brace.</remarks>
        public static string Line(Step step)
        {
            var pen = new ScriptWriter();
            var opens = step as If;
            var branch = step as Else;
            if (opens != null)
            {
                pen.Start(Keyword.If, step.IsOff);
                WriteTest(pen, null, opens.Test);
            }
            else if (branch != null)
            {
                pen.Start(Keyword.Else);
                if (branch.Test != null)
                {
                    pen.Says(Keyword.If);
                    WriteTest(pen, null, branch.Test);
                }
            }
            else if (step is EndIf)
                return "}";
            else
            {
                Started(pen, step);
                Rest(pen, null, step);
            }

            return pen.ToString().Trim();
        }

        /// <summary>The word a step starts with.</summary>
        static Keyword Head(Step step)
        {
            // A value is a set; an action is a call, bare or qualified by its namespace.
            var act = step as Act;
            if (act != null)
                return act.Form == ReachKind.Value ? Keyword.Set : Keyword.Call;

            if (step is Wait)
                return Keyword.Wait;

            var run = step as Run;
            if (run != null)
                return run.Stops ? Keyword.Stop : Keyword.Run;

            if (step is SetState)
                return Keyword.Set;

            if (step is Await)
                return Keyword.Await;

            throw new InvalidOperationException($"a {step.GetType().Name} has no way to be written.");
        }

        /// <summary>Everything in a step after the word it starts with.</summary>
        static void Rest(ScriptWriter pen, AutomationSystem system, Step step)
        {
            var abort = step as Abort;
            if (abort != null)
            {
                if (abort.Setting.Length > 0)
                    pen.Word(AbortWords.Saying).Word(abort.Setting);
                else if (abort.Message.Length > 0)
                    pen.Word(AbortWords.Saying).Text(abort.Message);
                if (abort.Level != AbortLevel.Note)
                    pen.Says(Keyword.As).Word(AbortWords.Name(abort.Level));

                return;
            }

            var sets = step as SetState;
            if (sets != null)
            {
                Named(pen, system, Kinds.State, sets.State, false);
                pen.Says(Keyword.To);
                if (sets.From.Length > 0)
                    State(pen, system, TypeOf(system, sets.State), sets.From);
                else
                    Choice(pen, system, TypeOf(system, sets.State), sets.Value, false);

                return;
            }

            var act = step as Act;
            if (act != null)
            {
                if (act.Form == ReachKind.Value)
                {
                    WriteMember(pen, system, act.Provider, act.ActionId, NoArguments, act.Targets);
                    if (act.Arguments.Count > 0)
                    {
                        pen.Says(Keyword.To);
                        Word(pen, system, act.Arguments[0]);
                    }
                }
                else
                {
                    WriteMember(pen, system, act.Provider, act.ActionId, act.Arguments, act.Targets);
                }

                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                if (wait.Setting.Length > 0)
                    pen.Word(wait.Setting);
                else
                    Lasting(pen, wait.Length);

                return;
            }

            var run = step as Run;
            if (run != null)
                pen.Whole(run.Number);
        }

        /// <summary>
        /// Writes <paramref name="value" />, given to or compared with a member,
        /// back as the kind it is; a word of the member spelled like a thing of <paramref name="system" />'s written with its host type,
        /// <c>door_status(closed)</c>, the type the word was read with or the one type that has it, so it is read back as the word.
        /// </summary>
        static void Word(ScriptWriter pen, AutomationSystem system, Literal value)
        {
            if (value.Kind == ValueKind.Flag && system != null && Meanings(system, Flag(value)) > 0)
            {
                pen.Kinded("", SettingReader.Flag, Flag(value));
                return;
            }

            var type = value.Kind == ValueKind.Name && ((system != null && Meanings(system, value.Text) > 0) || Kinds.SpelledLikeFlag(value.Text)) ? value.Typed ?? ChoiceTypes.Having(value.Text) : null;
            if (type != null)
                pen.Kinded(system != null && Meanings(system, type.Id) > 0 ? type.Namespace : "", type.Id, value.Text);
            else
                Written(pen, value);
        }

        /// <summary>Writes <paramref name="value" /> back as the kind it is.</summary>
        static void Written(ScriptWriter pen, Literal value)
        {
            if (value.Kind == ValueKind.Slots)
            {
                WriteNames(pen, value.Names);
                return;
            }

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

            if (value.Kind == ValueKind.Duration)
            {
                Lasting(pen, value.Length);
                return;
            }

            if (value.IsKinded)
            {
                pen.Kinded("", Kinds.Setting, value.Text);
                return;
            }

            // A setting spelled like a flag is written with its kind, since alone where a value stands it could be the flag.
            if (value.Kind == ValueKind.Setting && Kinds.SpelledLikeFlag(value.Text))
            {
                pen.Kinded("", Kinds.Setting, value.Text);
                return;
            }

            if (value.Kind == ValueKind.Setting)
            {
                pen.Word(value.Text);
                return;
            }

            pen.Color(value.Red, value.Green, value.Blue, value.Alpha);
        }

        static void WriteNames(ScriptWriter pen, IList<string> names)
        {
            pen.OpenList();
            for (var i = 0; i < names.Count; i++)
                pen.Word(names[i]);

            pen.CloseList();
        }
    }
}
