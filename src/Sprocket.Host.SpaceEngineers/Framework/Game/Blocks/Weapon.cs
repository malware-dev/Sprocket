using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using System;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    /// <summary>Every gun, turret and launcher: firing it, what it is firing, how many rounds it has left, and what it feeds from.</summary>
    public sealed partial class Weapon : Capability
    {
        const string Unshot = "A gun raises no event as it starts or stops shooting.";

        static readonly OwnValue Firing = OwnValue.Flag(Reading.Of<IMyUserControllableGun>(gun => gun.IsShooting));

        // Every vanilla weapon block is also the game's gun object, which counts the rounds loaded and the magazines of the
        // chosen kind in its inventory, in rounds.
        static readonly OwnValue Rounds = OwnValue.Whole<IMyGunObject<MyGunBase>>(gun => gun.GetTotalAmmunitionAmount());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyUserControllableGun;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("firing", "firing", Firing).Status().Looked(Unshot);

            // A modded gun that is not one of the game's gun objects has no count to give.
            if (block is IMyGunObject<MyGunBase>)
                kind.WithValue("rounds_left", "rounds left", Rounds).Announced().In(Category.Weapons);

            kind.Renaming("Shoot_On", false, "open fire")
                .Renaming("Shoot_Off", false, "cease fire")
                .Renaming("Shoot", false, "toggle firing")
                .Renaming("ShootOnce", false, "shoot once")
                .Renaming("ForceReload", false, "force reload")
                .Renaming("SelectedAmmoType", true, "ammunition")
                .Renaming("UseConveyor", true, "automatic push and pull")
                .Renaming("UseConveyor", false, "toggle automatic push and pull")
                .Renaming("TargetingGroup_Selector", true, "aims at")
                .Renaming("RequireTargetLock", true, "require target lock")
                .Renaming("RequireTargetLock", false, "toggle require target lock")
                .Hiding("CycleSelectedAmmoType", "TargetingGroup_CycleSubsystems");
        }

        /// <summary>Told as a gun's rounds change: a shot, a reload or a change of ammunition, and magazines arriving in its inventory.</summary>
        /// <remarks>The gun's event comes on every shot, which is one look each.</remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var gun = block as IMyGunObject<MyGunBase>;
            if (gun == null || gun.GunBase == null)
                return null;

            return new Listening(gun.GunBase, block.GetInventory(0) as MyInventoryBase, told);
        }
    }
}
