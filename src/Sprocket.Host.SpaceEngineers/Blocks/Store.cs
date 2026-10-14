using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A store, a vending machine or a contract block: who may use it, whether the store is listed, and which item a vending machine shows.</summary>
    /// <remarks>
    /// Words for the game's own settings, and the vending machine's turning through its items, which the game passes on to
    /// every player at the machine.
    /// </remarks>
    public sealed class Store : Capability
    {
        /// <summary>The game's id for who may use the block, and for the act that flips it.</summary>
        public const string Access = "AnyoneCanUse";

        const string Contracts = "MyObjectBuilder_ContractBlock";

        static readonly Doing Next = Doing.Of<IMyVendingMachine>(machine => machine.SelectNextItem());

        static readonly Doing Previous = Doing.Of<IMyVendingMachine>(machine => machine.SelectPreviewsItem());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return Admits(block);
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.Renaming(Access, true, "anyone can use").Renaming(Access, false, "toggle anyone can use");

            if (!(block is IMyStoreBlock))
                return;

            kind.Renaming("Searchable", true, "searchable").Stating("Searchable").Renaming("Searchable", false, "toggle searchable");

            if (block is IMyVendingMachine)
                kind.WithAction("show-next-item", "show next item", Next).In(Category.Screen)
                    .WithAction("show-previous-item", "show previous item", Previous).In(Category.Screen);
        }

        /// <summary>Whether <paramref name="block" /> is one of the blocks these words are for.</summary>
        public static bool Admits(IMyTerminalBlock block)
        {
            return block is IMyStoreBlock || block.BlockDefinition.TypeIdString == Contracts;
        }
    }
}
