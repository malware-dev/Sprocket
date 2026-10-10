using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A thruster: read for how hard it pushes, and how much of its thrust the air it is in leaves it.</summary>
    /// <remarks>
    /// In percent, so one number means the same on every size of thruster; the game's own thrust is in newtons, which
    /// differ per size.
    /// </remarks>
    public sealed class Thruster : Capability
    {
        const string Unheard = "A thruster's thrust changes many times a second in flight, too often to listen to.";

        const string Unaired = "Nothing announces a change in the air a thruster is in.";

        static readonly Being Thrust = Being.Number<IMyThrust>(thruster => thruster.CurrentThrustPercentage);

        static readonly Being Effectiveness = Being.Number<IMyThrust>(thruster =>
            thruster.MaxThrust > 0f ? thruster.MaxEffectiveThrust / thruster.MaxThrust * 100.0 : 0.0);

        static readonly Being MostThrust = Being.Number<IMyThrust>(thruster => thruster.MaxThrust);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyThrust;
        }

        /// <inheritdoc />
        /// <remarks>
        /// Effectiveness only where the air changes it, as on an atmospheric or an ion thruster; anything else is always at
        /// 100. Maximum thrust is fixed by the block, so nothing need announce it.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("thrust", "thrust", Thrust).Looked(Unheard).In(Category.Flight);

            if (Aired(block))
                kind.WithValue("effectiveness", "effectiveness", Effectiveness).Looked(Unaired).In(Category.Flight);

            kind.WithValue("maximum-thrust", "maximum thrust", MostThrust).Announced().In(Category.Flight);
        }

        /// <summary>Whether the air a thruster of <paramref name="block" />'s kind is in changes what it can push.</summary>
        /// <remarks>Asked of the definition, which says how effective it is at either end of the air it works in.</remarks>
        static bool Aired(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            var definition = definitions == null
                ? null
                : definitions.GetCubeBlockDefinition(block.BlockDefinition) as MyThrustDefinition;

            return definition != null && definition.EffectivenessAtMinInfluence != definition.EffectivenessAtMaxInfluence;
        }
    }
}
