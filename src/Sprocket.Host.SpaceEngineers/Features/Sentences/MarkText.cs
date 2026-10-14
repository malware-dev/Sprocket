using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>Which mark each kind of member is shown under, and what a member under it says as part of its row.</summary>
    /// <remarks>
    /// One mark to a row. A value carries its own mark wherever it is named, set as well as checked, since setting one is
    /// telling a block what one of its properties is, a different thing from telling it to do something; an action that
    /// takes time has an arrow into a clock where an action has an arrow into a block.
    /// </remarks>
    public static class MarkText
    {
        /// <summary>The mark of <paramref name="action" />: a value's where it sets a value, the arrow into a clock where it takes time, and the arrow into a block otherwise.</summary>
        public static string Of(EntityAction action)
        {
            if (action.Sets)
                return Symbols.Value;

            return action.Lasts ? Symbols.Lasting : Symbols.Action;
        }

        /// <summary>The mark of <paramref name="moment" />, a signal the block sends, from a toolbar slot or of Sprocket's own alike.</summary>
        public static string Of(EntityEvent moment)
        {
            return Symbols.Signal;
        }

        /// <summary>What a member under <paramref name="mark" /> says as part of the whole it is shown in, or null where it says nothing.</summary>
        /// <remarks>A mark carries no note of its own: what it stands for is told by the row or the part it is in.</remarks>
        public static string About(Texts texts, string mark)
        {
            return mark == Symbols.Lasting ? texts.MemberWaits : null;
        }
    }
}
