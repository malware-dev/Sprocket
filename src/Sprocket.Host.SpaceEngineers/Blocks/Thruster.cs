using System;
using Sandbox.Definitions;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>A thruster: its override set, and read for how hard it pushes and how much of its thrust the air it is in leaves it.</summary>
    /// <remarks>
    /// In percent, so one number means the same on every size of thruster; the game's own thrust is in newtons, which
    /// differ per size.
    /// </remarks>
    public sealed class Thruster : Capability
    {
        const string Unheard = "A thruster's thrust changes many times a second in flight, too often to listen to.";

        const string Unaired = "Nothing announces a change in the air a thruster is in.";

        static readonly Being Override = Being.Number<IMyThrust>(thruster => thruster.ThrustOverridePercentage * 100.0)
            .Writing<IMyThrust>((thruster, value) => thruster.ThrustOverridePercentage = (float)(value.Number / 100.0));

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
        /// Override is the game's own in percent, from 0, which is no override, to 100, as the game's own set override takes
        /// it; its override in newtons and the steps made out of that are hidden behind it. Effectiveness only where the air
        /// changes it, as on an atmospheric or an ion thruster; anything else is always at 100. Maximum thrust is fixed by
        /// the block, so nothing need announce it.
        /// </remarks>
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("override", "override", Override).Measured(" %").Announced().Between("0", "100").In(Category.Flight)
                .WithValue("thrust", "thrust", Thrust).Percent().Looked(Unheard).In(Category.Flight);

            if (Aired(block))
                kind.WithValue("effectiveness", "effectiveness", Effectiveness).Percent().Looked(Unaired).In(Category.Flight);

            kind.WithValue("maximum-thrust", "maximum thrust", MostThrust).Measured(" N").Announced().In(Category.Flight)
                .Hiding("Override", "SetOverride", "IncreaseOverride", "DecreaseOverride");
        }

        /// <summary>Told as the thruster's override changes, which it announces.</summary>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var thruster = block as IMyThrust;
            return thruster == null ? null : new Overriding(thruster, told);
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

        /// <summary>A thruster being listened to for a change in its override, until disposed.</summary>
        /// <remarks>The game raises it on every machine as the override's synced value changes.</remarks>
        sealed class Overriding : IDisposable
        {
            readonly IMyThrust _thruster;
            readonly Action _told;

            public Overriding(IMyThrust thruster, Action told)
            {
                _thruster = thruster;
                _told = told;
                _thruster.ThrustOverrideChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _thruster.ThrustOverrideChanged -= Changed;
            }

            void Changed(IMyThrust thruster, float newtons)
            {
                _told();
            }
        }
    }
}
