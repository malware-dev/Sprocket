using System;
using System.Collections.Generic;
using System.Globalization;
using Reactor.Host.SpaceEngineers.Sequences;
using Reactor.Host.SpaceEngineers.World;

namespace Reactor.Host.SpaceEngineers.Storage
{
    /// <summary>
    /// Writes a program out as a script, and reads one back.
    /// </summary>
    /// <remarks>
    /// Core never touches a file - the banned list says so, because Space Engineers does not whitelist
    /// IO and a mod stores its data in the save. So this turns a program into a string and back, and
    /// whoever is hosting decides where the string lives: a file beside the prototype, a section of the
    /// world save, a datapad.
    /// <para>
    /// <b>The script is the program.</b> There is nothing else to project it from - the model lives in
    /// memory in a mod the game may unload at any moment - so this is not a view of the truth, it is
    /// the truth written down. Three things follow and all three are load-bearing: it stays readable so
    /// a save that breaks can be looked at, a construct nobody recognises is dropped whole rather than
    /// taking its neighbours with it, and a program that did not read cleanly is marked
    /// <see cref="Automation.Intact" /> false so that the next save cannot write the damage over what
    /// is still on disk.
    /// </para>
    /// <para>
    /// It is a language and not prose, and that is the line to hold: keywords here are syntax, the way
    /// <c>if</c> and <c>while</c> are, and nobody localises those. The moment it drifts towards English
    /// it puts word order and vocabulary back into core and freezes them in a file format, where they
    /// become a compatibility promise. Every word a person reads is the host's; every word in here is
    /// the format's.
    /// </para>
    /// </remarks>
    public static class ProgramText
    {
        /// <summary>
        /// What this build writes, and the only thing it reads.
        /// </summary>
        /// <remarks>
        /// <b>Not backwards compatible, on purpose, while the language is still being designed.</b> A save
        /// saying any other number is read as far as it goes and marked not to be written back, so nothing
        /// on disk is lost - it simply does not load cleanly.
        /// </remarks>
        const int Version = 4;

        /// <summary>
        /// How a <c>changed</c> time may be written: ISO 8601, and nothing else.
        /// </summary>
        /// <remarks>
        /// Quoted rather than bare because a bare date is two subtractions once expressions exist. A time with
        /// no offset is UTC. The writer always uses the first.
        /// </remarks>
        static readonly string[] Times =
        {
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm':'sszzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFzzz",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss",
            "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFF",
            "yyyy'-'MM'-'dd'T'HH':'mm'Z'",
            "yyyy'-'MM'-'dd'T'HH':'mm",
            "yyyy'-'MM'-'dd"
        };

        public static string Save(Automation program, Values values)
        {
            var pen = new Pen();
            pen.Start(Keyword.Reactor).Whole(Version);
            pen.End();

            // Before anything that names one, so a reader has them in hand by the time it meets a step.
            var refs = program.Refs;
            if (refs.Count > 0)
                pen.Blank();

            for (var i = 0; i < refs.Count; i++)
                WriteRef(pen, values, refs[i]);

            var systems = program.Systems;
            for (var i = 0; i < systems.Count; i++)
                WriteSystem(pen, systems[i]);

            // After every system, because an instance names the system it is one of and a reader
            // should have met it by then.
            for (var i = 0; i < systems.Count; i++)
            {
                var instances = systems[i].Instances;
                for (var n = 0; n < instances.Count; n++)
                    WriteInstance(pen, systems[i], instances[n]);
            }

            return pen.ToString();
        }

        /// <summary>
        /// Reads a program back into an empty one, and answers what was wrong with it.
        /// </summary>
        /// <remarks>
        /// An empty answer means everything that was written was read. Anything else is for the host to
        /// log or show; the program itself carries the same news as <see cref="Automation.Intact" />,
        /// because the decision it forces - do not save over this - is taken somewhere else entirely.
        /// </remarks>
        public static string Load(string saved, Automation program, Values values)
        {
            string warning;
            return Load(saved, program, values, out warning);
        }

        /// <summary>
        /// Reads a program back into an empty one, and answers what was wrong with it and what only looked
        /// wrong.
        /// </summary>
        /// <remarks>
        /// A warning is something that reads and runs but is probably a mistake, such as two systems with
        /// the same name. It does not mark the program damaged.
        /// </remarks>
        public static string Load(string saved, Automation program, Values values, out string warning)
        {
            if (string.IsNullOrEmpty(saved))
            {
                warning = "";
                return "";
            }

            var parser = new Parser(saved);
            var constructs = parser.Read();
            var trouble = new Trouble(saved, parser.Trouble);

            if (constructs.Count == 0 || constructs[0].Key != Keyword.Reactor)
                trouble.Say("this is not a Reactor program");
            else if (constructs[0].Whole(0) != Version)
                trouble.Say("this was written by a different version of Reactor");

            for (var i = 0; i < constructs.Count; i++)
            {
                var construct = constructs[i];
                if (construct.Key == Keyword.Block)
                    ReadRef(construct, program, values, trouble);
                else if (construct.Key == Keyword.System)
                    ReadSystem(construct, program, trouble);
                else if (construct.Key == Keyword.Instance)
                    ReadInstance(construct, program, trouble);
            }

            if (trouble.What.Length > 0)
                program.Damaged(trouble.What);

            warning = trouble.Warning;
            return trouble.What;
        }

        static void WriteRef(Pen pen, Values values, EntityRef declared)
        {
            pen.Start(Keyword.Block).Word(BlockKinds.Sort(declared.KindId)).Word(declared.Name);
            pen.OpenList();

            var keys = declared.Keys;
            for (var i = 0; i < keys.Count; i++)
                WriteKey(pen, values, keys[i]);

            pen.CloseList();
            pen.End();
        }

        /// <summary>
        /// One of a declaration's keys, written as whatever sort of value it is.
        /// </summary>
        /// <remarks>
        /// The syntax carries the kind, so nothing here tags anything: a whole number reads back as a
        /// whole number because that is what it looks like. Which is the point - core does not know what
        /// a key means, only what it is, and the host that minted the list is the only thing that reads
        /// it.
        /// </remarks>
        static void WriteKey(Pen pen, Values values, EntityKey key)
        {
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

        static void ReadRef(Statement statement, Automation program, Values values, Trouble trouble)
        {
            var declared = new EntityRef(statement.Word(1), BlockKinds.Definition(statement.Word(0)));
            var keys = statement.Items(2);
            for (var i = 0; i < keys.Count; i++)
                ReadKey(values, keys[i], declared.Keys, statement, trouble);

            program.Restore(declared);
        }

        /// <summary>
        /// A key, or trouble where this build has no kind that could hold it.
        /// </summary>
        /// <remarks>
        /// A vector is the one the language has and the model does not - a position survives
        /// blueprinting where an id does not, so it is the key worth having, and it is waiting on a
        /// kind wide enough to hold it. Said rather than dropped, because a key is how a block is
        /// found again: dropping one quietly leaves the next save writing a block that matches less
        /// than the one it replaces.
        /// </remarks>
        static void ReadKey(Values values, Value key, IList<EntityKey> into, Statement statement,
            Trouble trouble)
        {
            switch (key.Form)
            {
                case ValueForm.Integer:
                    into.Add(new EntityKey(ValueKind.Integer, EntityValue.Of(key.Whole)));
                    return;

                case ValueForm.Real:
                    into.Add(new EntityKey(ValueKind.Real, EntityValue.Of(key.Real)));
                    return;

                case ValueForm.Text:
                    into.Add(new EntityKey(ValueKind.Text, values.Text(key.Word)));
                    return;

                case ValueForm.Color:
                    byte r, g, b, a;
                    key.Colour(out r, out g, out b, out a);
                    into.Add(new EntityKey(ValueKind.Color, EntityValue.Of(r, g, b, a)));
                    return;

                case ValueForm.Keyword:
                    if (key.Keyword == Keyword.On || key.Keyword == Keyword.Off)
                    {
                        into.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(key.Keyword == Keyword.On)));
                        return;
                    }

                    break;
            }

            trouble.Say(statement, "this build cannot keep one of this block's keys");
        }

        static void WriteSystem(Pen pen, AutomationSystem system)
        {
            pen.Blank();
            pen.Note(system.Annotation);
            pen.Start(Keyword.System).Text(system.Identity.ToString("D"))
                .Says(Keyword.As).Qualified(system.Namespace, system.Name);

            if (system.Changed.HasValue)
            {
                pen.Says(Keyword.Changed)
                    .Text(system.Changed.Value.ToString(Times[0], CultureInfo.InvariantCulture));
            }

            if (system.LastSequenceNumber > system.HighestSequenceNumber())
                pen.Says(Keyword.Last).Says(Keyword.Sequence).Whole(system.LastSequenceNumber);

            pen.Body();

            // The parts before the ones that fill them in, so a reader meets the vocabulary before
            // the answers.
            var parts = system.Parts;
            for (var i = 0; i < parts.Count; i++)
                WritePart(pen, parts[i]);

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
                WriteTrigger(pen, triggers[i]);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                WriteSequence(pen, sequences[i]);

            pen.Shut();
        }

        /// <summary>
        /// <c>system "3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43" as MalTek.Airlock changed "…" last sequence 4 { … }</c>
        /// </summary>
        /// <remarks>
        /// Strict where it matters. A system is its identity, so one without a readable identity, or a second
        /// with the same one, is trouble and not read; so is one with no name, which the editor never makes. A
        /// second system with the same name loads, with a warning, because the name decides nothing but a
        /// person still has to tell the two apart. Whether it is switched on is not here at all - that is
        /// state, and <see cref="StateText" /> holds it.
        /// </remarks>
        static void ReadSystem(Statement statement, Automation program, Trouble trouble)
        {
            Guid identity;
            if (!Guid.TryParseExact(statement.Text(0), "D", out identity) || identity == Guid.Empty)
            {
                trouble.Say(statement, "a system starts with its identity, like \"3f2a9c1e-7b4d-4e8a-9f61-2c5d8e0b7a43\"");
                return;
            }

            var name = statement.Parts(2);
            if (!statement.Says(1, Keyword.As) || name.Count == 0 || name.Count > 2
                || name[name.Count - 1].Word.Length == 0)
            {
                trouble.Say(statement, "a system is named after 'as', as a name or a namespace and a name");
                return;
            }

            if (program.FindSystem(identity) != null)
            {
                trouble.Say(statement, "two systems are both " + identity.ToString("D"));
                return;
            }

            var space = name.Count == 2 ? name[0].Word : "";
            var called = name[name.Count - 1].Word;
            if (program.FindSystem(space, called) != null)
                trouble.Warn(statement, "two systems are both called " + Pen.Spelling(space, called));

            var system = program.Restore(identity, space, called);
            system.Annotation = statement.Annotation;

            var changed = statement.Find(Keyword.Changed);
            if (changed >= 0)
                system.Changed = Time(statement, changed + 1, trouble);

            var last = statement.Find(Keyword.Last);
            if (last >= 0)
                LastSequence(statement, last, system, trouble);

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.Key == Keyword.Required && inner.Says(0, Keyword.Trigger))
                    ReadTrigger(inner, system, trouble);
                else if (inner.Key == Keyword.Required)
                    ReadPart(inner, system, trouble);
                else if (inner.Key == Keyword.When || inner.Key == Keyword.Every)
                    ReadTrigger(inner, system, trouble);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program, trouble);
            }

            Resolve(system, statement, trouble);
        }

        /// <summary>
        /// <c>last sequence 4</c>: the highest number the system has handed out, kept only as a failsafe.
        /// </summary>
        /// <remarks>
        /// Whichever is higher wins, this or the sequences in the block, so a sequence somebody added by hand
        /// with a higher number is never undercut by a stale one.
        /// </remarks>
        static void LastSequence(Statement statement, int at, AutomationSystem system, Trouble trouble)
        {
            var number = statement.ValueAt(at + 2);
            if (!statement.Says(at + 1, Keyword.Sequence) || number.Form != ValueForm.Integer || number.Whole < 1
                || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, "'last' is followed by 'sequence' and a number, like last sequence 4");
                return;
            }

            system.Used((int)number.Whole);
        }

        /// <summary>A <c>changed</c> time, or null and trouble when it is not ISO 8601.</summary>
        static DateTime? Time(Statement statement, int at, Trouble trouble)
        {
            DateTime time;
            if (DateTime.TryParseExact(statement.Text(at), Times, CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out time))
                return time;

            trouble.Say(statement, "a changed time is written as ISO 8601, like \"2026-09-14T19:02:11Z\"");
            return null;
        }

        /// <summary>
        /// <c>required block inner expects Door</c>: something the construct has to provide.
        /// </summary>
        /// <remarks>
        /// <c>required</c> heads it, so every line that starts with that word is something the ship
        /// answers rather than something Reactor does. What a part expects is written only when the
        /// author said it; nothing means anything may fill it.
        /// </remarks>
        static void WritePart(Pen pen, Part part)
        {
            if (part.Annotation.Length > 0)
                pen.Blank();

            pen.Note(part.Annotation);
            pen.Start(Keyword.Required).Says(Keyword.Block).Word(part.Id);

            if (part.Expects.Count == 1)
                pen.Says(Keyword.Expects).Word(part.Expects[0]);
            else if (part.Expects.Count > 1)
                Names(pen.Says(Keyword.Expects), part.Expects);

            pen.End();
        }

        static void ReadPart(Statement statement, AutomationSystem system, Trouble trouble)
        {
            if (!statement.Says(0, Keyword.Block))
            {
                trouble.Say(statement,
                    "'required' is followed by what sort of thing is required: block, trigger");
                return;
            }

            var part = system.PartFor(statement.Word(1));
            part.Annotation = statement.Annotation;

            var expects = statement.Find(Keyword.Expects);
            if (expects < 0)
                return;

            var one = statement.ValueAt(expects + 1);
            if (one.Form == ValueForm.List)
                Words(statement.Items(expects + 1), part.Expects);
            else if (one.IsWord && one.Word.Length > 0)
                part.Expects.Add(one.Word);
            else
                trouble.Say(statement, "'expects' is followed by a block type, or a list of them");
        }

        /// <summary>
        /// <c>instance 1 of "3f2a…" as 'Port airlock' { inner = [ door1 ] }</c>
        /// </summary>
        /// <remarks>
        /// One of a system, on this construct. It sits outside the system rather than inside it, because
        /// the system is what is shared and which of somebody's blocks answer its parts is not.
        /// </remarks>
        static void WriteInstance(Pen pen, AutomationSystem system, SystemInstance instance)
        {
            pen.Blank();
            pen.Start(Keyword.Instance).Whole(instance.Number)
                .Says(Keyword.Of).Text(system.Identity.ToString("D"));

            if (instance.HasName)
                pen.Says(Keyword.As).Word(instance.Name);

            var parts = Sorted(instance);
            if (parts.Count == 0)
            {
                pen.End();
                return;
            }

            pen.Body();
            for (var i = 0; i < parts.Count; i++)
            {
                pen.Head(parts[i]).Says(Keyword.Equals);
                Names(pen, instance.FilledWith(parts[i]));
                pen.End();
            }

            pen.Shut();
        }

        /// <summary>
        /// Which parts this one fills, in a settled order.
        /// </summary>
        /// <remarks>
        /// Sorted, so a saved file does not churn between runs for no reason: a dictionary hands its
        /// keys back in whatever order it likes, and a save that differs every time is one that gets
        /// written every time and reads as a change in a diff.
        /// </remarks>
        static List<string> Sorted(SystemInstance instance)
        {
            var parts = new List<string>();
            foreach (var slot in instance.Filled)
                parts.Add(slot);

            parts.Sort(StringComparer.Ordinal);
            return parts;
        }

        /// <summary>
        /// One of a system, or nothing where it does not say which system it is one of.
        /// </summary>
        /// <remarks>
        /// The system may be named by its identity or by its name, because a person writing one by hand
        /// has the name in front of them and Reactor always writes the identity.
        /// </remarks>
        static void ReadInstance(Statement statement, Automation program, Trouble trouble)
        {
            var number = statement.ValueAt(0);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue
                || !statement.Says(1, Keyword.Of))
            {
                trouble.Say(statement, "one of a system is written like instance 1 of MalTek.Airlock");
                return;
            }

            var system = Named(program, statement, 2);
            if (system == null)
            {
                trouble.Say(statement, "there is no system for this one to be one of");
                return;
            }

            if (system.FindInstance((int)number.Whole) != null)
            {
                trouble.Say(statement, "this system already has a number " + number.Whole);
                return;
            }

            var name = statement.Says(3, Keyword.As) ? statement.Word(4) : "";
            var instance = system.Restore((int)number.Whole, name);

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.Key != Keyword.None || !inner.Says(0, Keyword.Equals))
                {
                    trouble.Say(inner, "one of a system says what a part is, like inner = [ door1 ]");
                    continue;
                }

                var filled = new List<string>();
                Words(inner.Items(1), filled);
                instance.Fill(inner.Head, filled);
            }
        }

        /// <summary>The system a statement names by identity or by name, or null.</summary>
        static AutomationSystem Named(Automation program, Statement statement, int at)
        {
            Guid identity;
            if (Guid.TryParseExact(statement.Text(at), "D", out identity))
                return program.FindSystem(identity);

            var name = statement.Parts(at);
            if (name.Count == 0 || name.Count > 2)
                return null;

            return program.FindSystem(name.Count == 2 ? name[0].Word : "", name[name.Count - 1].Word);
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
                WriteStep(pen, steps[i]);

            pen.Shut();
        }

        /// <summary>
        /// <c>sequence 3 as 'Let me out' { … }</c>
        /// </summary>
        /// <remarks>
        /// The number is what the sequence is, within its system, so one without a number or with a number its
        /// system already has is trouble and not read. The name is optional.
        /// </remarks>
        static void ReadSequence(Statement statement, AutomationSystem system, Automation program, Trouble trouble)
        {
            var number = statement.ValueAt(0);
            if (number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, "a sequence starts with its number within its system");
                return;
            }

            if (system.FindSequence((int)number.Whole) != null)
            {
                trouble.Say(statement, "two sequences of one system are both number " + number.Whole);
                return;
            }

            var sequence = program.Restore(system, (int)number.Whole, null);
            sequence.Annotation = statement.Annotation;

            if (statement.Says(1, Keyword.As))
            {
                sequence.Name = statement.Word(2);
                if (sequence.Name.Length == 0)
                    trouble.Say(statement, "'as' is followed by the sequence's name");
            }

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var step = ReadStep(body[i], trouble);
                if (step != null)
                    sequence.Steps.Add(step);
            }
        }

        /// <summary>
        /// <c>when any pressed [ call ] then run 1</c>, or <c>every 600t run 2</c>.
        /// </summary>
        /// <remarks>
        /// A trigger says what it runs, because it belongs to the system rather than to one sequence. The
        /// condition a <c>when</c> takes is as much as the language can say today: an event, and the parts
        /// to watch for it.
        /// </remarks>
        static void WriteTrigger(Pen pen, Trigger trigger)
        {
            // The ones a construct answers sit in a list of their own and only break it up where one
            // carries words; the rest each start a paragraph.
            if (!(trigger is RequiredTrigger) || trigger.Annotation.Length > 0)
                pen.Blank();

            pen.Note(trigger.Annotation);

            var needed = trigger as RequiredTrigger;
            if (needed != null)
            {
                pen.Start(Keyword.Required).Says(Keyword.Trigger);

                // Which block and which moment, where the editor said: a person reading their own
                // program should see the button they picked, not just that something has to press.
                if (needed.Sources.Count > 0)
                {
                    pen.Word(needed.EventId);
                    Names(pen, needed.Sources);
                }

                pen.Says(Keyword.To).Says(Doing(trigger)).Whole(trigger.Runs);
                pen.End();
                return;
            }

            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(when.RequiresAll ? Keyword.All : Keyword.Any).Word(when.EventId);
                Names(pen, when.Sources);
                pen.Says(Keyword.Then).Says(Doing(trigger)).Whole(trigger.Runs);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                pen.Start(Keyword.Every).Ticks(every.IntervalTicks).Says(Keyword.Run).Whole(trigger.Runs);
                pen.End();
            }
        }

        /// <summary>Which word says what a trigger does with its sequence.</summary>
        static Keyword Doing(Trigger trigger)
        {
            return trigger.Stops ? Keyword.Stop : Keyword.Run;
        }

        static void ReadTrigger(Statement statement, AutomationSystem system, Trouble trouble)
        {
            var trigger = Kind(statement, trouble);
            if (trigger == null)
                return;

            trigger.Annotation = statement.Annotation;

            var at = statement.Find(Keyword.Run);
            if (at < 0)
            {
                at = statement.Find(Keyword.Stop);
                trigger.Stops = at >= 0;
            }

            var number = statement.ValueAt(at + 1);
            if (at < 0 || number.Form != ValueForm.Integer || number.Whole < 1 || number.Whole > int.MaxValue)
            {
                trouble.Say(statement, "a trigger says which sequence it runs or stops, like run 1");
                return;
            }

            trigger.Runs = (int)number.Whole;
            system.AddTrigger(trigger);
        }

        static Trigger Kind(Statement statement, Trouble trouble)
        {
            if (statement.Key == Keyword.When)
                return When(statement, trouble);

            if (statement.Key == Keyword.Every)
                return Every(statement, trouble);

            // required trigger to run 1: something on the construct answers it, and nothing here
            // says which. Legal, and what a hand-written program or an imported system may say.
            if (statement.Says(1, Keyword.To))
                return new RequiredTrigger();

            // required trigger pressed-1 [Airlock Button] to run 1: the same, saying which block
            // and which of its moments, which is what the editor always writes.
            if (!statement.Says(3, Keyword.To))
            {
                trouble.Say(statement, "a required trigger says what it runs, like required trigger to run 1");
                return null;
            }

            var needed = new RequiredTrigger(statement.Word(1));
            Words(statement.Items(2), needed.Sources);
            return needed;
        }

        static Trigger When(Statement statement, Trouble trouble)
        {
            if (!statement.Says(0, Keyword.All) && !statement.Says(0, Keyword.Any))
            {
                trouble.Say(statement, "'when' says whether it watches any of the parts or all of them");
                return null;
            }

            if (!statement.Says(statement.Find(Keyword.Then), Keyword.Then))
            {
                trouble.Say(statement, "what a 'when' watches for is followed by 'then'");
                return null;
            }

            var when = new WhenTrigger(statement.Word(1));
            when.RequiresAll = statement.Says(0, Keyword.All);
            Words(statement.Items(2), when.Sources);
            return when;
        }

        static Trigger Every(Statement statement, Trouble trouble)
        {
            int ticks;
            if (!Ticks(statement.ValueAt(0), 1, out ticks))
            {
                trouble.Say(statement, "'every' is followed by how often, like every 600t");
                return null;
            }

            return new EveryTrigger(ticks);
        }

        /// <summary>
        /// A duration as a count of ticks, or false where it was no length of time that can be kept.
        /// </summary>
        /// <remarks>
        /// The text reads into a long and ticks are counted in an int, so the bound is the language's
        /// as much as the reader's: <c>every 4294967296t</c> is not an interval, it is a number that
        /// wraps onto one, and <c>every -600t</c> is not a rate at all.
        /// </remarks>
        static bool Ticks(Value value, long least, out int ticks)
        {
            ticks = 0;
            if (value.Form != ValueForm.Duration || value.Whole < least || value.Whole > int.MaxValue)
                return false;

            ticks = (int)value.Whole;
            return true;
        }

        /// <summary>
        /// A step: an action on some parts, an await, or a length of time.
        /// </summary>
        /// <remarks>
        /// An action's id is the head, so a step reads as what it does, and an id that collides with one
        /// of the language's words is quoted.
        /// </remarks>
        static void WriteStep(Pen pen, Step step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);

            var act = step as Act;
            if (act != null)
            {
                // One that has to be given values says so in the language's own words, because the
                // values have to hang off something and a bare head has nowhere to put them. One
                // that needs none is written as what it is: a verb and the things it acts on.
                if (act.Values.Count > 0)
                {
                    pen.Start(Keyword.Action).Word(act.ActionId).Says(Keyword.On);
                    Names(pen, act.Targets);
                    pen.Says(Keyword.With);

                    for (var i = 0; i < act.Values.Count; i++)
                        Written(pen, act.Values[i]);
                }
                else
                {
                    pen.Head(act.ActionId);
                    Names(pen, act.Targets);
                }

                pen.End();
                return;
            }

            var wait = step as Wait;
            if (wait != null)
            {
                pen.Start(Keyword.Wait).Ticks(wait.Ticks);
                pen.End();
                return;
            }

            var run = step as Run;
            if (run != null)
            {
                pen.Start(Keyword.Run).Whole(run.Number);
                pen.End();
                return;
            }

            pen.Start(Keyword.Await);
            pen.End();
        }

        /// <summary>
        /// A step, or nothing where the statement was not one.
        /// </summary>
        /// <remarks>
        /// Only <c>await</c> and <c>wait</c> are the language speaking here; any other bare head is an
        /// action id, which is why a statement a later build adds has to carry a word of its own to be
        /// dropped whole rather than read as a step.
        /// </remarks>
        static Step ReadStep(Statement statement, Trouble trouble)
        {
            if (statement.Key == Keyword.Await)
                return Noted(new Await(), statement);

            if (statement.Key == Keyword.Run)
                return Called(statement, trouble);

            if (statement.Key == Keyword.Wait)
            {
                int ticks;
                if (Ticks(statement.ValueAt(0), 0, out ticks))
                    return Noted(new Wait(ticks), statement);

                // Said rather than dropped, because a wait without its unit is somebody writing a
                // length of time and Reactor is the only thing that can tell them it read as nothing.
                trouble.Say(statement, "a wait is written with its unit, like wait 60t");
                return null;
            }

            if (statement.Key == Keyword.Action)
                return Given(statement);

            if (statement.Key != Keyword.None)
                return null;

            var act = new Act(statement.Head);
            for (var i = 0; i < statement.Count; i++)
            {
                if (statement.ValueAt(i).Form != ValueForm.List)
                    continue;

                Words(statement.Items(i), act.Targets);
                break;
            }

            return Noted(act, statement);
        }

        /// <summary>
        /// <c>action 'SetColor' on [ light ] with 255 0 0</c>: one of the game's own, given what it
        /// asked for.
        /// </summary>
        /// <remarks>
        /// Everything after <c>with</c> is a value, in the order it lies, because order is all the
        /// game says about them - there are no names to match and nothing is optional. Taken by
        /// position rather than by what it looks like, so a flag written <c>on</c> is a value and
        /// not mistaken for one of the language's own words.
        /// </remarks>
        static Step Given(Statement statement)
        {
            var act = new Act(statement.Word(0));

            for (var at = 1; at < statement.Count; at++)
            {
                if (statement.ValueAt(at).Form != ValueForm.List)
                    continue;

                Words(statement.Items(at), act.Targets);
                break;
            }

            var from = -1;
            for (var at = 1; at < statement.Count; at++)
            {
                if (!statement.Says(at, Keyword.With))
                    continue;

                from = at + 1;
                break;
            }

            for (var at = from; at >= 0 && at < statement.Count; at++)
                act.Values.Add(Given(statement.ValueAt(at)));

            return Noted(act, statement);
        }

        /// <summary>One value of an action, as whatever kind it was written as.</summary>
        /// <remarks>
        /// A bare word that is not a flag is kept as text. Nothing else could be done with it, and
        /// losing it would turn a step somebody wrote into one that quietly does something else.
        /// </remarks>
        static Literal Given(Value value)
        {
            if (value.Form == ValueForm.Text)
                return Literal.Of(value.Word);

            if (value.Form == ValueForm.Integer || value.Form == ValueForm.Duration)
                return Literal.Of(value.Whole);

            if (value.Form == ValueForm.Real)
                return Literal.Of(value.Real);

            if (value.Form == ValueForm.Color)
            {
                return Literal.Colour((byte)(value.Whole >> 24), (byte)(value.Whole >> 16),
                    (byte)(value.Whole >> 8), (byte)value.Whole);
            }

            if (value.Says(Keyword.On))
                return Literal.Of(true);

            if (value.Says(Keyword.Off))
                return Literal.Of(false);

            return Literal.Of(value.Word);
        }

        /// <summary>The same, written back as the kind it is.</summary>
        static void Written(Pen pen, Literal value)
        {
            if (value.Kind == ValueKind.Text)
            {
                pen.Text(value.Text);
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

        static Step Noted(Step step, Statement statement)
        {
            step.Annotation = statement.Annotation;
            return step;
        }

        /// <summary>
        /// <c>run 2</c>, or <c>run 'Let me out'</c>: another sequence of the same system.
        /// </summary>
        static Step Called(Statement statement, Trouble trouble)
        {
            var named = statement.ValueAt(0);
            if (named.Form == ValueForm.Integer && named.Whole >= 1 && named.Whole <= int.MaxValue)
                return Noted(new Run((int)named.Whole), statement);

            if (named.IsWord && named.Word.Length > 0)
            {
                var run = new Run(0);
                run.Named = named.Word;
                return Noted(run, statement);
            }

            trouble.Say(statement, "'run' is followed by a sequence's number or its name");
            return null;
        }

        /// <summary>
        /// Matches what the system's <c>run</c> steps and triggers name, now the whole system is read.
        /// </summary>
        /// <remarks>
        /// Late, because a sequence may be named or numbered before it is written. Anything naming a
        /// sequence the system has not got is trouble: it can never run, nothing else would say so, and
        /// a number that is not there would take the runner down on the tick that reached it.
        /// <para>
        /// Reported against the system's own line rather than the step's, because a step does not keep
        /// where it was written and the system is what the name was looked for in.
        /// </para>
        /// </remarks>
        static void Resolve(AutomationSystem system, Statement statement, Trouble trouble)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                var steps = sequences[i].Steps;
                for (var s = 0; s < steps.Count; s++)
                {
                    var run = steps[s] as Run;
                    if (run == null)
                        continue;

                    if (!string.IsNullOrEmpty(run.Named))
                    {
                        var named = Called(system, run.Named);
                        if (named == null)
                            trouble.Say(statement, "no sequence of this system is called " + run.Named);
                        else
                            run.Number = named.Number;

                        run.Named = "";
                        continue;
                    }

                    if (system.FindSequence(run.Number) == null)
                        trouble.Say(statement, "this system has no sequence " + run.Number + " to run");
                }
            }

            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                if (system.FindSequence(triggers[i].Runs) == null)
                    trouble.Say(statement, "this system has no sequence " + triggers[i].Runs + " to run");
            }
        }

        static Sequence Called(AutomationSystem system, string name)
        {
            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
            {
                if (string.Equals(sequences[i].Name, name, StringComparison.Ordinal))
                    return sequences[i];
            }

            return null;
        }

        static void Names(Pen pen, IList<string> names)
        {
            pen.OpenList();
            for (var i = 0; i < names.Count; i++)
                pen.Word(names[i]);

            pen.CloseList();
        }

        static void Words(ValueRange values, IList<string> into)
        {
            into.Clear();
            for (var i = 0; i < values.Count; i++)
            {
                var value = values[i];
                if (value.IsWord && value.Word.Length > 0)
                    into.Add(value.Word);
            }
        }

    }
}
