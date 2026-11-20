using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.Definitions;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>A thruster: its override set, and read for how hard it pushes and how much of its thrust the air it is in leaves it.</summary>
    /// <remarks>Its readings are in percent, which mean the same on every size of thruster.</remarks>
    public sealed partial class Thruster : Capability
    {
        const string Unheard = "A thruster's thrust changes many times a second in flight, too often to listen to.";

        const string Unaired = "Nothing announces a change in the air a thruster is in.";

        static readonly OwnValue Override = OwnValue.Real<IMyThrust>(thruster => thruster.ThrustOverridePercentage * 100.0)
            .Writing<IMyThrust>((thruster, value) => thruster.ThrustOverridePercentage = (float)(value.Number / 100.0));

        static readonly OwnValue Thrust = OwnValue.Real<IMyThrust>(thruster => thruster.CurrentThrustPercentage);

        static readonly OwnValue Effectiveness = OwnValue.Real<IMyThrust>(thruster =>
            thruster.MaxThrust > 0f ? thruster.MaxEffectiveThrust / thruster.MaxThrust * 100.0 : 0.0);

        static readonly OwnValue MostThrust = OwnValue.Real<IMyThrust>(thruster => thruster.MaxThrust);

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyThrust;
        }

        /// <inheritdoc />
        /// <remarks>Override is in percent, from 0, which is no override, to 100.</remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("override", "override", Override).Measured(" %").Announced().Between("0", "100").In(Category.Flight)
                .WithValue("thrust", "thrust", Thrust).Percent().Looked(Unheard).In(Category.Flight);

            if (Aired(block))
                kind.WithValue("effectiveness", "effectiveness", Effectiveness).Percent().Looked(Unaired).In(Category.Flight);

            kind.WithValue("maximum_thrust", "maximum thrust", MostThrust).Measured(" N").Announced().In(Category.Flight)
                .Hiding("Override", "SetOverride", "IncreaseOverride", "DecreaseOverride");
        }

        /// <summary>Told as the thruster's override changes.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var thruster = block as IMyThrust;
            return thruster == null ? null : new Overriding(thruster, told);
        }

        /// <summary>Whether the air a thruster of <paramref name="block" />'s kind is in changes what it can push.</summary>
        static bool Aired(IMyTerminalBlock block)
        {
            var definitions = MyDefinitionManager.Static;
            var definition = definitions?.GetCubeBlockDefinition(block.BlockDefinition) as MyThrustDefinition;

            return definition != null && definition.EffectivenessAtMinInfluence != definition.EffectivenessAtMaxInfluence;
        }
    }
}
