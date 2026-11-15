using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Language.Format;
using Sprocket.Host.SpaceEngineers.Framework.Model;

namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>Reads what starts a logic system's sequences out of the script: whenever, when and every.</summary>
    public static class TriggerReader
    {
        /// <summary><c>whenever</c>, <c>when</c> or <c>every</c>: a trigger of <paramref name="system" /> and the step it does, or trouble where it is none of those.</summary>
        public static void ReadTrigger(Statement statement, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            var trigger = Kind(statement, system, trouble);
            if (trigger == null)
                return;

            trigger.Does = Does(statement, Doing(statement), system, trouble);
            if (trigger.Does == null)
                return;

            SystemReader.Uses(statement, trigger.Acts, system, program, trouble);

            var when = trigger as WhenTrigger;
            for (var i = 0; when != null && i < when.Sources.Count; i++)
                SystemReader.Uses(statement, when.Sources[i], system, program, false, trouble);

            var needed = trigger as SignalTrigger;
            if (needed != null && needed.IsWritten)
            {
                for (var i = 0; i < needed.Sources.Count; i++)
                    SystemReader.Uses(statement, needed.Sources[i], system, program, false, trouble);

                for (var i = 0; needed.At != null && !system.IsBlueprint && i < needed.Sources.Count; i++)
                    trouble.Takes(statement, needed.Sources[i], needed.At);
            }
            else if (needed != null)
            {
                SystemReader.Uses(statement, needed.Part, system, program, true, trouble);
                Takes(statement, needed, system, program, trouble);
            }

            trigger.Annotation = statement.Annotation;
            trigger.Attributes = ScriptAttributes.Unused(statement, ScriptAttributes.Category);
            SystemReader.Category(statement, trigger, trouble);
            system.AddTrigger(trigger);
        }

        /// <summary>
        /// Trouble where the trigger <paramref name="needed" /> names already starts something: a slot of a system blueprint
        /// another <c>whenever</c> names, or the toolbar slot a system's signal variable holds.
        /// </summary>
        /// <remarks>A toolbar slot holds one action, so a second <c>whenever</c> on it could never be wired.</remarks>
        static void Takes(Statement statement, SignalTrigger needed, AutomationSystem system, Automation program, ScriptTrouble trouble)
        {
            if (system.IsBlueprint)
            {
                if (Started(system, needed.Part))
                    trouble.Say(statement, $"slot '{needed.Part}' already starts something; a toolbar slot holds one action.");

                return;
            }

            var declared = program.FindSignalRef(needed.Part);
            for (var i = 0; declared != null && i < declared.Blocks.Count; i++)
                trouble.Takes(statement, declared.Blocks[i], declared.At);
        }

        /// <summary>Whether a <c>whenever</c> of <paramref name="system" /> names <paramref name="slot" />.</summary>
        public static bool Started(AutomationSystem system, string slot)
        {
            var triggers = system.Triggers;
            for (var i = 0; i < triggers.Count; i++)
            {
                var needed = triggers[i] as SignalTrigger;
                if (needed != null && needed.Part == slot)
                    return true;
            }

            return false;
        }

        /// <summary>Where the step a trigger does starts: after <c>then</c>, or after how often.</summary>
        static int Doing(Statement statement)
        {
            if (statement.Key == Keyword.When)
                return statement.Find(Keyword.Then) + 1;

            return statement.Key == Keyword.Every ? 1 : 2;
        }

        /// <summary>The step a trigger does, which is one a sequence could hold and that starts something: run, stop, call or set.</summary>
        static Step Does(Statement statement, int at, AutomationSystem system, ScriptTrouble trouble)
        {
            var step = statement.Rest(at);
            if (step.Key != Keyword.Run && step.Key != Keyword.Stop && step.Key != Keyword.Call && step.Key != Keyword.Set)
            {
                trouble.Say(statement, "expected run, stop, call or set.");
                return null;
            }

            var does = StepReader.ReadStep(step, system, trouble);
            if (does != null)
                does.Annotation = "";

            return does;
        }

        static Trigger Kind(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            if (statement.Key == Keyword.When && ScriptQuotes.Named(statement.ValueAt(0)))
                return Watched(statement, system, trouble);

            if (statement.Key == Keyword.When)
                return When(statement, trouble);

            if (statement.Key == Keyword.Every)
                return Every(statement, trouble);

            if (!statement.Says(1, Keyword.Then) || (!ScriptQuotes.Named(statement.ValueAt(0)) && !statement.IsMember(0)))
            {
                trouble.Say(statement, "expected whenever <signal> then <step>.");
                return null;
            }

            // whenever button then run 1: the signal button, a variable of a system's or a signal slot of a blueprint's.
            if (ScriptQuotes.Named(statement.ValueAt(0)))
                return SignalTrigger.Named(statement.Word(0));

            return Written(statement, trouble);
        }

        /// <summary>
        /// <c>whenever ext.finishes_closing on [ inner ] then</c> or <c>whenever ext.toolbar(1, 1, 4) on [ panel1 ] then</c>: a signal
        /// written in place, a block's own change or a slot on the blocks' toolbars; or null and trouble where it is neither.
        /// </summary>
        static Trigger Written(Statement statement, ScriptTrouble trouble)
        {
            var member = statement.Parts(0);
            if (member.Count != 2 || member[0].Word.Length == 0 || member[1].Word.Length == 0)
            {
                trouble.Say(statement, "expected a signal qualified by its namespace, like ext.finishes_closing on [ inner ].");
                return null;
            }

            if (member[0].Word != BlockKinds.Own)
            {
                trouble.Say(statement, $"no namespace '{member[0].Word}'; expected a signal qualified by ext.");
                return null;
            }

            var written = SignalTrigger.Written(member[0].Word, member[1].Word);
            var given = statement.Arguments(0);
            if (member[1].Word == ScriptFormat.Toolbar)
            {
                if (given.Count != 3 || Statement.Counted(given[0]) == 0 || Statement.Counted(given[1]) == 0
                    || Statement.Counted(given[2]) == 0)
                {
                    trouble.Say(statement, "expected ext.toolbar(<toolbar>, <page>, <slot>), each a whole number from 1.");
                    return null;
                }

                written.At = new ToolbarSlot(Statement.Counted(given[0]), Statement.Counted(given[1]), Statement.Counted(given[2]));
            }
            else if (given.Count > 0)
            {
                trouble.Say(statement, "unexpected arguments; a block's own signal takes none.");
                return null;
            }

            return StepReader.Names(statement.On(0), written.Sources, statement, trouble) ? written : null;
        }

        static Trigger When(Statement statement, ScriptTrouble trouble)
        {
            Quantifier quantifier;
            if (!StepReader.Quantified(statement, 0, "when", trouble, out quantifier))
                return null;

            var then = statement.Find(Keyword.Then);
            if (then < 0)
            {
                trouble.Say(statement, "expected 'then'.");
                return null;
            }

            var when = new WhenTrigger("");
            string provider, id;
            if (!StepReader.Member(statement, 1, "a value on a list of names, like ext.closed on [ inner ]", trouble, out provider, out id,
                    when.Arguments, when.Sources))
                return null;

            Relation relation;
            Literal value;
            if (!StepReader.Compared(statement, 2, quantifier, trouble, out relation, out value))
                return null;

            when.EventId = id;
            when.Provider = provider;
            when.Quantifier = quantifier;
            when.Relation = relation;
            when.Value = value;

            var at = 4;
            if (statement.Says(at, Keyword.Every))
            {
                Duration interval;
                if (!StepReader.Lasting(statement.ValueAt(at + 1), 1, out interval))
                {
                    trouble.Say(statement, "expected a duration after 'every', like 30t.");
                    return null;
                }

                when.Interval = interval;
                at += 2;
            }

            if (at != then)
            {
                trouble.Say(statement, "expected 'then'.");
                return null;
            }

            return when;
        }

        /// <summary>
        /// <c>when where == outside then</c>: a trigger started as a step sets a state so that the test comes to hold; or
        /// null and trouble where it is not that.
        /// </summary>
        /// <remarks>Nothing is looked at on a cadence, so it takes no <c>every</c>.</remarks>
        static Trigger Watched(Statement statement, AutomationSystem system, ScriptTrouble trouble)
        {
            var test = StateReader.Test(statement, 0, system, trouble);
            if (test == null)
                return null;

            if (statement.Says(3, Keyword.Every))
            {
                trouble.Say(statement, "unexpected 'every'; a when on a state is started by the step that sets it, and takes no every.");
                return null;
            }

            if (!statement.Says(3, Keyword.Then))
            {
                trouble.Say(statement, "expected 'then' after the value.");
                return null;
            }

            return new StateTrigger(test);
        }

        static Trigger Every(Statement statement, ScriptTrouble trouble)
        {
            Duration interval;
            if (!StepReader.Lasting(statement.ValueAt(0), 1, out interval))
            {
                trouble.Say(statement, "expected a duration after 'every', like 600t.");
                return null;
            }

            return new EveryTrigger(interval);
        }
    }
}
