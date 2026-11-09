using Sprocket.Host.SpaceEngineers.Blocks;
using Sprocket.Host.SpaceEngineers.Features.Look;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Sentences
{
    /// <summary>Which mark each kind of member is shown under, and what a member under it says as part of its row.</summary>
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

        /// <summary>The mark a slot is shown with wherever it is named: a signal's, a screen's, or a block's.</summary>
        public static string OfSlot(Part part) => part == null ? Symbols.Block : OfSlot(part.IsSignal, HostTypes.Named(part.Holds));

        /// <summary>The mark of a slot that a signal fills where <paramref name="signal" />, and otherwise one holding <paramref name="holds" />, a type the host provides, or blocks for null.</summary>
        public static string OfSlot(bool signal, HostType holds) => signal ? Symbols.Signal : holds is ScreenType ? Symbols.Screen : holds is InventoryType ? Symbols.Inventory : Symbols.Block;

        /// <summary>The mark of each slot of the blueprint <paramref name="system" /> these names are, for a line naming them; null where the system names blocks.</summary>
        public static List<string> OfSlots(AutomationSystem system, IList<string> names)
        {
            if (system == null || !system.IsBlueprint)
                return null;

            var marks = new List<string>(names.Count);
            foreach (var name in names)
                marks.Add(OfSlot(system.FindPart(name)));

            return marks;
        }

        /// <summary>What a member under <paramref name="mark" /> says as part of the whole it is shown in, or null where it says nothing.</summary>
        public static string About(Texts texts, string mark)
        {
            return mark == Symbols.Lasting ? texts.Codex.MemberWaits : null;
        }
    }
}
