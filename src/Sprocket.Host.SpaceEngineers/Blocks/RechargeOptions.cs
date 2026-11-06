using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>What a step told a battery or a jump drive to recharge was given: whether it waits until the block is charged.</summary>
    public sealed class RechargeOptions
    {
        const string Wait = "wait";

        const string MoveOn = "move on";

        /// <summary>Whether the step waits until the block is charged, rather than setting it recharging and carrying on.</summary>
        public bool Waits;

        /// <summary>What a step's options come to: waiting unless it says not to.</summary>
        public static RechargeOptions Read(IList<Literal> arguments)
        {
            return new RechargeOptions
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

            return field;
        }
    }
}
