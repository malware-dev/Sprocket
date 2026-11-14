using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Text;

namespace Sprocket.Host.SpaceEngineers.Features.Slots
{
    /// <summary>The names a new slot is given, each saying what the slot holds.</summary>
    public static class SlotNames
    {
        /// <summary>The numbered name for a slot a signal fills where <paramref name="signal" />, and otherwise one holding <paramref name="holds" />, or blocks where that is null.</summary>
        public static string Numbered(Texts texts, bool signal, HostType holds)
        {
            if (signal)
                return texts.SlotNumberedSignal;

            if (holds is ScreenType)
                return texts.SlotNumberedScreen;

            return holds is InventoryType ? texts.SlotNumberedInventory : texts.SlotNumbered;
        }
    }
}
