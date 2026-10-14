using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Storage
{
    /// <summary>The upgrades that carry a program written in an earlier format into the one this build writes.</summary>
    /// <remarks>
    /// An upgrade goes from one released format to the next and is never removed, since every program behind it reads
    /// through it. A draft of the format on the way to a release gets one only where one is chosen; every other draft is
    /// refused. Each runs on what the parser read, never on the text, so a name that happens to hold a keyword is left as
    /// it is, and the program is written in the current format at its next save.
    /// </remarks>
    public static class Upgrades
    {
        // Sprocket's own switch actions as alpha 14 programs persist them, and the value that took them over.
        const string SwitchOn = "on";

        const string SwitchOff = "off";

        const string Power = "power";

        /// <summary>Whether a program written for <paramref name="draft" /> of version <paramref name="version" /> is upgraded rather than refused.</summary>
        public static bool From(int version, int draft)
        {
            return version == 1 && draft == 14;
        }

        /// <summary>Carries <paramref name="program" />, read as written for <paramref name="draft" />, into the current format.</summary>
        /// <returns>What was upgraded, for the log, or empty where nothing needed it.</returns>
        public static string Apply(int draft, Automation program)
        {
            var switched = 0;
            if (draft == 14)
            {
                var systems = program.Systems;
                for (var i = 0; i < systems.Count; i++)
                    switched += Switches(systems[i]);
            }

            return switched == 0 ? "" : "alpha " + draft + ": " + switched + " switch step" + (switched == 1 ? "" : "s") + " set power.";
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
            act.Form = Form.Value;
            act.Provider = BlockKinds.Own;
            act.Arguments.Clear();
            act.Arguments.Add(Literal.Of(on));
            return true;
        }
    }
}
