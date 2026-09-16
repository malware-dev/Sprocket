using System.Collections.Generic;
using Sandbox.ModAPI;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// One <see cref="EntityKind" /> per block definition, built the first time one of that block is seen.
    /// </summary>
    /// <remarks>
    /// Per definition rather than one kind for everything, because the ship list groups by kind and a list
    /// where every row says "Block" is not a list anybody can use. The vocabulary is identical across all of
    /// them for now; what differs is the name the game gives the type.
    /// </remarks>
    sealed class BlockKinds
    {
        internal const string SwitchOn = "on";
        internal const string SwitchOff = "off";

        readonly Dictionary<string, EntityKind> _kinds = new Dictionary<string, EntityKind>();

        public EntityKind For(IMyTerminalBlock block)
        {
            string id = block.BlockDefinition.TypeIdString + "/" + block.BlockDefinition.SubtypeName;

            EntityKind kind;
            if (_kinds.TryGetValue(id, out kind))
                return kind;

            string name = string.IsNullOrEmpty(block.DefinitionDisplayNameText)
                ? block.BlockDefinition.SubtypeName
                : block.DefinitionDisplayNameText;

            // The block's own definition id doubles as its icon: the screen-space sprite API resolves
            // one straight to that block's picture, so nothing has to be declared, extracted or shipped.
            // It is the same string as the kind id, which is not a coincidence - both are "which block
            // type is this".
            kind = new EntityKind(id, name).WithIcon(id);

            // What this block is, before what every block is, so a door offers "open" above "switch on".
            IBlockExtension extension = BlockExtensions.For(block);
            if (extension != null)
                extension.Describe(kind, block);

            kind.WithProperty(GridEntity.WorkingProperty, "working")
                .WithState("is-working", GridEntity.WorkingProperty, true, "is working")
                .WithState("is-not-working", GridEntity.WorkingProperty, false, "is not working");

            // Only a block with a switch gets the words for one. A cargo container has no on and off, and
            // offering them would be offering a sentence that cannot come true.
            if (block is IMyFunctionalBlock)
            {
                kind.WithProperty(GridEntity.OnProperty, "switched on")
                    .WithEvent("switched-on", GridEntity.OnProperty, Edge.Rising, "is switched on")
                    .WithEvent("switched-off", GridEntity.OnProperty, Edge.Falling, "is switched off")
                    .WithState("is-on", GridEntity.OnProperty, true, "is on")
                    .WithState("is-off", GridEntity.OnProperty, false, "is off")
                    .WithAction(SwitchOn, "switch on", "is-on")
                    .WithAction(SwitchOff, "switch off", "is-off");
            }

            _kinds.Add(id, kind);
            return kind;
        }
    }
}
