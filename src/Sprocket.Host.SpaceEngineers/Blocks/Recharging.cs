using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>What a step told a battery or a jump drive to recharge was given: whether it waits until the block is charged.</summary>
    /// <remarks>
    /// One option for both, because a battery and a jump drive recharge under the same word. Waiting is what recharging
    /// does unless told otherwise, so a sentence leaves it out and says "and move on" where it moves on.
    /// </remarks>
    public sealed class Recharging
    {
        const string Wait = "wait";

        const string MoveOn = "move on";

        /// <summary>Whether the step waits until the block is charged, rather than setting it recharging and carrying on.</summary>
        public bool Waits;

        /// <summary>What a step's options come to: waiting unless it says not to.</summary>
        public static Recharging Read(IList<Literal> arguments)
        {
            return new Recharging
            {
                Waits = arguments == null || arguments.Count == 0 || arguments[0].Kind != ValueKind.Flag || arguments[0].Flag
            };
        }

        /// <summary>The field a recharge asks whether it waits until charged by.</summary>
        public static Field Asked()
        {
            var field = Field.Among("wait until charged", new[] { Wait, MoveOn },
                new[] { Literal.Of(true).Said, Literal.Of(false).Said });

            field.Default = Wait;
            field.Quiet = Wait;
            field.Lead = "and";
            field.About = "**Wait** holds the sequence at the next await until the block is charged.\n\n"
                + "**Move on** sets it recharging and lets the sequence carry on.";

            return field;
        }
    }
}
