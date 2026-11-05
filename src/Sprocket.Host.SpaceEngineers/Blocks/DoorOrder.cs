using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Features.Fields;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>How a step tells a door to open or close: as usual, or forced, told again whenever something reverses it.</summary>
    public struct DoorOrder
    {
        const string Normal = "normal";
        const string Forced = "forced";

        /// <summary>Whether the door is told again each time something reverses it, until it arrives.</summary>
        public bool IsForced;

        /// <summary>What a step's options come to: not forced unless it says so.</summary>
        public static DoorOrder Read(IList<Literal> arguments)
        {
            return new DoorOrder
            {
                IsForced = arguments != null && arguments.Count > 0 && arguments[0].Kind == ValueKind.Flag && arguments[0].Flag
            };
        }

        /// <summary>The field that asks whether a door is forced.</summary>
        public static Field Asked()
        {
            var field = Field.Among("force", new[] { Normal, Forced }, new[] { Literal.Of(false).Said, Literal.Of(true).Said });
            field.Default = Normal;
            field.Quiet = Normal;
            field.MayBeLeftOut = true;
            field.About = "**Normal** stops the step with a problem if something reverses the door.\n\n"
                + "**Forced** tells the door again each time something reverses it, until it arrives or stops being able to move.";

            return field;
        }
    }
}
