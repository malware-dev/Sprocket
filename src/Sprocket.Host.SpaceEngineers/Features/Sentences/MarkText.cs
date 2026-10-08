using Sprocket.Host.SpaceEngineers.Features.Look;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>Which mark each kind of member is shown under, and what the mark says while the pointer rests on it.</summary>
    /// <remarks>
    /// One mark to a row. A value carries its own mark wherever it is named, set as well as checked, since setting one is
    /// telling a block what one of its properties is, a different thing from telling it to do something; an action that
    /// takes time shows the clock in place of the bolt rather than beside it.
    /// </remarks>
    public static class MarkText
    {
        /// <summary>The mark of <paramref name="action" />: a value's where it sets a value, the clock where a step waits for it, and the bolt otherwise.</summary>
        public static string Of(EntityAction action)
        {
            if (action.Sets)
                return Symbols.Value;

            return action.Lasts ? Symbols.Awaited : Symbols.Action;
        }

        /// <summary>The mark of <paramref name="moment" />: the pulse for a signal a block sends, and the block for one watched on it.</summary>
        public static string Of(EntityEvent moment)
        {
            return moment.Answered ? Symbols.Signal : Symbols.Block;
        }

        /// <summary>What <paramref name="mark" /> says while the pointer rests on it, or null where it is none of these.</summary>
        public static string Note(Texts texts, string mark)
        {
            switch (mark)
            {
                case Symbols.Action: return texts.MemberAction;
                case Symbols.Awaited: return texts.MemberWaits;
                case Symbols.Value: return texts.MemberValue;
                case Symbols.Signal: return texts.MemberSignal;
                case Symbols.Block: return texts.MemberMoment;
                case Symbols.Sequence: return texts.MemberSequence;
                default: return null;
            }
        }
    }
}
