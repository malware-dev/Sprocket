using System;
using System.Collections.Generic;
using Reactor.Sequences;
using Reactor.World;

namespace Reactor.Storage
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
        /// What this build writes.
        /// </summary>
        /// <remarks>
        /// 1 was one construct per bar-separated line, which <see cref="Lines" /> still reads. A higher
        /// number than this is a save from a later build: what it says is read as far as it goes, and
        /// the program is marked not to be written back.
        /// </remarks>
        const int Version = 2;

        const string Language = "reactor";

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
            if (string.IsNullOrEmpty(saved))
                return "";

            if (First(saved) == Language + " 1")
            {
                Lines.Load(saved, program, values);
                return "";
            }

            var parser = new Parser(saved);
            var constructs = parser.Read();
            var trouble = parser.Trouble;

            if (constructs.Count == 0 || constructs[0].Key != Keyword.Reactor)
                trouble = Pick(trouble, "this is not a Reactor program");
            else if (constructs[0].Whole(0) > Version)
                trouble = Pick(trouble, "this was written by a later version of Reactor");

            for (var i = 0; i < constructs.Count; i++)
            {
                var construct = constructs[i];
                if (construct.Key == Keyword.Block)
                    ReadRef(construct, program, values);
                else if (construct.Key == Keyword.System)
                    ReadSystem(construct, program);
            }

            if (trouble.Length > 0)
                program.Damaged();

            return trouble;
        }

        /// <summary>The first line, however the file ends its lines.</summary>
        static string First(string saved)
        {
            var to = saved.IndexOf('\n');
            if (to < 0)
                to = saved.Length;

            if (to > 0 && saved[to - 1] == '\r')
                to--;

            return saved.Substring(0, to);
        }

        static string Pick(string trouble, string instead)
        {
            return trouble.Length > 0 ? trouble : instead;
        }

        static void WriteRef(Pen pen, Values values, EntityRef declared)
        {
            pen.Start(Keyword.Block).Word(declared.KindId).Word(declared.Name);
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

        static void ReadRef(Statement statement, Automation program, Values values)
        {
            var declared = new EntityRef(statement.Word(1), statement.Word(0));
            var keys = statement.Items(2);
            for (var i = 0; i < keys.Count; i++)
                ReadKey(values, keys[i], declared.Keys);

            program.Restore(declared);
        }

        /// <summary>
        /// A key, or nothing where this build has no kind that could hold it.
        /// </summary>
        /// <remarks>
        /// A vector is the one the language has and the model does not, which is why it is named here
        /// rather than falling through: a position survives blueprinting where an id does not, so it is
        /// the key worth having, and it is waiting on a kind wide enough to hold it.
        /// </remarks>
        static void ReadKey(Values values, Value key, IList<EntityKey> into)
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
                        into.Add(new EntityKey(ValueKind.Flag, EntityValue.Of(key.Keyword == Keyword.On)));
                    else
                        into.Add(new EntityKey(ValueKind.Text, values.Text(key.Word)));
                    return;

                case ValueForm.Identifier:
                    into.Add(new EntityKey(ValueKind.Text, values.Text(key.Word)));
                    return;
            }
        }

        static void WriteSystem(Pen pen, AutomationSystem system)
        {
            pen.Blank();
            pen.Note(system.Annotation);
            pen.Start(Keyword.System).Whole(system.Id).Says(OnOff(system.Enabled));
            Named(pen, system.Name);
            pen.Body();

            // What the author wrote about the parts, before the ones that fill them in, so a reader
            // meets the vocabulary before the answers.
            var roles = system.Roles;
            for (var i = 0; i < roles.Count; i++)
                WriteRole(pen, roles[i]);

            var instances = system.Instances;
            for (var i = 0; i < instances.Count; i++)
                WriteInstance(pen, instances[i]);

            var sequences = system.Sequences;
            for (var i = 0; i < sequences.Count; i++)
                WriteSequence(pen, sequences[i]);

            pen.Shut();
        }

        static void ReadSystem(Statement statement, Automation program)
        {
            var system = program.Restore(statement.Whole(0));
            system.Enabled = !statement.Says(1, Keyword.Off);
            system.Name = statement.Word(2);
            system.Annotation = statement.Annotation;

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.Key == Keyword.Role)
                    ReadRole(inner, system);
                else if (inner.Key == Keyword.Instance)
                    ReadInstance(inner, system);
                else if (inner.Key == Keyword.Sequence)
                    ReadSequence(inner, system, program);
            }
        }

        static void WriteRole(Pen pen, SystemRole role)
        {
            if (role.Instructions.Length > 0)
                pen.Blank();

            pen.Note(role.Instructions);
            pen.Start(Keyword.Role).Word(role.Id).Says(role.Optional ? Keyword.Optional : Keyword.Required);
            Named(pen, role.Name);
            pen.End();
        }

        static void ReadRole(Statement statement, AutomationSystem system)
        {
            var role = system.RoleFor(statement.Word(0));
            role.Optional = statement.Says(1, Keyword.Optional);
            role.Name = statement.Word(2);
            role.Instructions = statement.Annotation;
        }

        static void WriteInstance(Pen pen, SystemInstance instance)
        {
            pen.Blank();
            pen.Start(Keyword.Instance).Whole(instance.Number).Says(OnOff(instance.Enabled));
            Named(pen, instance.Name);

            var parts = Sorted(instance);
            if (parts.Count == 0)
            {
                pen.End();
                return;
            }

            pen.Body();
            for (var i = 0; i < parts.Count; i++)
            {
                pen.Start(Keyword.Fill).Word(parts[i]);
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
            foreach (var role in instance.Filled)
                parts.Add(role);

            parts.Sort(StringComparer.Ordinal);
            return parts;
        }

        static void ReadInstance(Statement statement, AutomationSystem system)
        {
            var instance = system.Restore(statement.Whole(0), statement.Word(2));
            instance.Enabled = !statement.Says(1, Keyword.Off);

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (inner.Key != Keyword.Fill)
                    continue;

                var filled = new List<string>();
                Words(inner.Items(1), filled);
                instance.Fill(inner.Word(0), filled);
            }
        }

        static void WriteSequence(Pen pen, Sequence sequence)
        {
            pen.Blank();
            pen.Note(sequence.Annotation);
            pen.Start(Keyword.Sequence).Whole(sequence.Id).Says(OnOff(sequence.Enabled));
            Named(pen, sequence.Name);
            pen.Body();

            WriteTrigger(pen, sequence.Trigger);

            var steps = sequence.Steps;
            for (var i = 0; i < steps.Count; i++)
                WriteStep(pen, steps[i]);

            pen.Shut();
        }

        static void ReadSequence(Statement statement, AutomationSystem system, Automation program)
        {
            var sequence = program.Restore(system, statement.Whole(0), new CalledTrigger());
            sequence.Enabled = !statement.Says(1, Keyword.Off);
            sequence.Name = statement.Word(2);
            sequence.Annotation = statement.Annotation;

            var body = statement.Body;
            for (var i = 0; i < body.Count; i++)
            {
                var inner = body[i];
                if (ReadTrigger(inner, sequence))
                    continue;

                var step = ReadStep(inner);
                if (step != null)
                    sequence.Steps.Add(step);
            }
        }

        static void WriteTrigger(Pen pen, Trigger trigger)
        {
            var when = trigger as WhenTrigger;
            if (when != null)
            {
                pen.Start(Keyword.When).Says(when.RequiresAll ? Keyword.All : Keyword.Any).Word(when.EventId);
                Names(pen, when.Sources);
                pen.End();
                return;
            }

            var every = trigger as EveryTrigger;
            if (every != null)
            {
                pen.Start(Keyword.Every).Whole(every.IntervalTicks);
                pen.End();
                return;
            }

            if (trigger is CalledTrigger)
            {
                pen.Start(Keyword.Called);
                pen.End();
            }
        }

        /// <summary>Whether the statement was a trigger, which is what tells a step from one.</summary>
        static bool ReadTrigger(Statement statement, Sequence sequence)
        {
            if (statement.Key == Keyword.When)
            {
                var when = new WhenTrigger(statement.Word(1));
                when.RequiresAll = statement.Says(0, Keyword.All);
                Words(statement.Items(2), when.Sources);
                sequence.Trigger = when;
                return true;
            }

            if (statement.Key == Keyword.Every)
            {
                sequence.Trigger = new EveryTrigger(statement.Whole(0));
                return true;
            }

            if (statement.Key == Keyword.Called)
            {
                sequence.Trigger = new CalledTrigger();
                return true;
            }

            return false;
        }

        /// <summary>
        /// A step: what it does, to what, and whether anything holds it up.
        /// </summary>
        /// <remarks>
        /// The action id is the head, so a step reads as what it does. A step with no action yet - one
        /// somebody is halfway through building, or a bare wait - says <c>step</c> instead, and an
        /// action that collides with one of the format's words is quoted.
        /// </remarks>
        static void WriteStep(Pen pen, Step step)
        {
            if (step.Annotation.Length > 0)
                pen.Blank();

            pen.Note(step.Annotation);

            if (step.ActionId.Length == 0)
                pen.Start(Keyword.Step);
            else
                pen.Head(step.ActionId);

            Names(pen, step.Targets);
            pen.Says(step.WaitsForCompletion ? Keyword.Wait : Keyword.Now);
            WriteGate(pen, step.Gate);
            pen.End();
        }

        /// <summary>
        /// A step, or nothing where the statement was not one.
        /// </summary>
        /// <remarks>
        /// <b>A step always says <c>wait</c> or <c>now</c>, and that is what makes the reading safe.</b>
        /// Any bare word can be an action id, so a construct a later build adds - <c>run</c>, <c>set</c>
        /// - would otherwise be read as a step doing something called "run". Requiring the word means an
        /// unknown statement is dropped whole instead, which is the promise the format makes.
        /// </remarks>
        static Step ReadStep(Statement statement)
        {
            var waits = statement.Find(Keyword.Wait);
            var now = statement.Find(Keyword.Now);
            if (waits < 0 && now < 0)
                return null;

            // A keyword head is the format speaking, so the only one a step may start with is the word
            // for a step with no action yet. Anything else is data: an id, quoted or not.
            if (statement.Key != Keyword.None && statement.Key != Keyword.Step)
                return null;

            var action = statement.Head;

            var step = new Step(action);
            step.Annotation = statement.Annotation;
            step.WaitsForCompletion = now < 0;
            ReadGate(statement, step);

            // The targets are the list before whatever holds the step up, because a gate brings a list
            // of its own and both are plain lists of declaration names.
            var gate = Gate(statement);
            for (var i = 0; i < gate; i++)
            {
                if (statement.At(i).Form != ValueForm.List)
                    continue;

                Words(statement.Items(i), step.Targets);
                break;
            }

            return step;
        }

        static void WriteGate(Pen pen, StepGate gate)
        {
            var after = gate as AfterGate;
            if (after != null)
            {
                pen.Says(Keyword.After).Whole(after.Ticks);
                return;
            }

            var once = gate as OnceGate;
            if (once != null)
            {
                WriteCondition(pen, Keyword.Once, once.Condition);
                return;
            }

            var onlyIf = gate as OnlyIfGate;
            if (onlyIf != null)
                WriteCondition(pen, Keyword.OnlyIf, onlyIf.Condition);
        }

        static void ReadGate(Statement statement, Step step)
        {
            var after = statement.Find(Keyword.After);
            if (after >= 0)
            {
                step.Gate = new AfterGate(statement.Whole(after + 1));
                return;
            }

            var once = statement.Find(Keyword.Once);
            if (once >= 0)
            {
                step.Gate = new OnceGate(ReadCondition(statement, once));
                return;
            }

            var onlyIf = statement.Find(Keyword.OnlyIf);
            if (onlyIf >= 0)
                step.Gate = new OnlyIfGate(ReadCondition(statement, onlyIf));
        }

        /// <summary>Where a gate starts among a step's values, or past the end when it has none.</summary>
        static int Gate(Statement statement)
        {
            var at = statement.Count;
            at = Earlier(at, statement.Find(Keyword.After));
            at = Earlier(at, statement.Find(Keyword.Once));
            return Earlier(at, statement.Find(Keyword.OnlyIf));
        }

        static int Earlier(int at, int found)
        {
            return found >= 0 && found < at ? found : at;
        }

        static void WriteCondition(Pen pen, Keyword keyword, Condition condition)
        {
            if (condition == null)
                return;

            pen.Says(keyword).Word(condition.StateId);
            Names(pen, condition.Entities);
        }

        static Condition ReadCondition(Statement statement, int at)
        {
            var condition = new Condition(statement.Word(at + 1));
            Words(statement.Items(at + 2), condition.Entities);
            return condition;
        }

        /// <summary>A name, written only when there is one, because a trailing empty value reads the same.</summary>
        static void Named(Pen pen, string name)
        {
            if (!string.IsNullOrEmpty(name))
                pen.Text(name);
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

        static Keyword OnOff(bool on)
        {
            return on ? Keyword.On : Keyword.Off;
        }
    }
}
