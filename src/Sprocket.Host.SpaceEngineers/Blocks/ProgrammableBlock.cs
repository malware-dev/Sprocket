using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>
    /// A programmable block: whether its script failed to compile, and the argument its own run uses, said in Sprocket's
    /// words.
    /// </summary>
    /// <remarks>Reading whether it has compile errors compiles nothing.</remarks>
    public sealed class ProgrammableBlock : Capability
    {
        static readonly Being Broken = Being.Flag(Reading.Of<IMyProgrammableBlock>(block => block.HasCompileErrors));

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyProgrammableBlock;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("has-compile-errors", "has compile errors", Broken)
                .Looked("A programmable block raises no event when its script is compiled.")
                .Renaming("ConsoleCommand", true, "default argument");
        }
    }
}
