using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A programmable block: running its script, and whether the script failed to compile.</summary>
    /// <remarks>Reading whether it has compile errors compiles nothing.</remarks>
    public sealed class ProgrammableBlock : Capability
    {
        /// <summary>Running the block's script, which is Sprocket's own.</summary>
        public const string Run = "run";

        static readonly Being Broken = Being.Flag(Reading.Of<IMyProgrammableBlock>(block => block.HasCompileErrors));

        static readonly OwnAction Runs = OwnAction.Of<IMyProgrammableBlock, Argument>(Argument.Read, Ran, trouble: Refused);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyProgrammableBlock;
        }

        /// <inheritdoc />
        /// <remarks>
        /// The script runs to its end inside the step, on the server, with the argument as given, as the game's own run
        /// does; that run, which takes its argument from a toolbar slot, is left to the terminal view. A script that does not
        /// run ends the run there.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithAction(Run, "run", Runs, Asked()).In(Category.Automation)
                .WithValue("has-compile-errors", "has compile errors", Broken)
                .Looked("A programmable block raises no event when its script is compiled.").In(Category.Automation);
        }

        /// <summary>Runs the script with <paramref name="argument" />, keeping in <paramref name="seen" /> whether it ran.</summary>
        static void Ran(IMyProgrammableBlock block, Argument argument, ReaderMemory seen)
        {
            seen.Number = block.TryRun(argument.Text) ? 1 : 0;
            seen.Frame = 0;
        }

        /// <summary>Why the script did not run, which ends the run there, or nothing where it ran.</summary>
        static BlockTrouble Refused(IMyProgrammableBlock block, Argument argument, ReaderMemory seen)
        {
            return seen.Seen && seen.Number <= 0 ? BlockTrouble.Fatal(Cause.ScriptNotRun) : BlockTrouble.None;
        }

        /// <summary>The argument to run the script with.</summary>
        static Field Asked()
        {
            var argument = Field.Called("argument");
            argument.Entry = FieldInput.Text;
            return argument;
        }

        /// <summary>The argument a step runs a script with.</summary>
        sealed class Argument
        {
            /// <summary>The argument given, which may be empty.</summary>
            public string Text = "";

            /// <summary>The argument a step running a script was given.</summary>
            public static Argument Read(IList<Literal> arguments)
            {
                return new Argument { Text = arguments != null && arguments.Count > 0 ? arguments[0].Text ?? "" : "" };
            }
        }
    }
}
