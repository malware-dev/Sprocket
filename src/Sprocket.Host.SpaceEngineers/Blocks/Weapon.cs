using System;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.World;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    /// <summary>Every gun, turret and launcher: firing it, what it is firing, how many rounds it has left, and what it feeds from.</summary>
    /// <remarks>
    /// The game's own on and off for firing is the order a sequence gives, and firing is whether shots are going out, a
    /// turret's own aim included; so the order is offered as its two actions and its toggle, and its value is left out.
    /// </remarks>
    public sealed class Weapon : Capability
    {
        const string Unshot = "A gun raises no event as it starts or stops shooting.";

        static readonly Being Firing = Being.Flag(Reading.Of<IMyUserControllableGun>(gun => gun.IsShooting));

        // Every vanilla weapon block is also the game's gun object, which counts the rounds loaded and the magazines of the
        // chosen kind in its inventory, in rounds.
        static readonly Being Rounds = Being.Whole<IMyGunObject<MyGunBase>>(gun => gun.GetTotalAmmunitionAmount());

        /// <inheritdoc />
        public override bool Claims(IMyTerminalBlock block)
        {
            return block is IMyUserControllableGun;
        }

        /// <inheritdoc />
        public override void Describe(EntityKind kind, IMyTerminalBlock block)
        {
            kind.WithValue("firing", "firing", Firing).Looked(Unshot);

            // A modded gun that is not one of the game's gun objects has no count to give.
            if (block is IMyGunObject<MyGunBase>)
                kind.WithValue("rounds-left", "rounds left", Rounds).Announced();

            kind.Renaming("Shoot_On", false, "open fire")
                .Renaming("Shoot_Off", false, "cease fire")
                .Renaming("Shoot", false, "toggle firing")
                .Renaming("ShootOnce", false, "fire once")
                .Renaming("ForceReload", false, "reload")
                .Renaming("SelectedAmmoType", true, "ammunition")
                .Renaming("UseConveyor", true, "using conveyors")
                .Renaming("UseConveyor", false, "toggle using conveyors")
                .Renaming("TargetingGroup_Selector", true, "aims at")
                .Renaming("RequireTargetLock", true, "requiring target lock")
                .Renaming("RequireTargetLock", false, "toggle requiring target lock")
                .Hiding("CycleSelectedAmmoType", "TargetingGroup_CycleSubsystems");
        }

        /// <summary>Told as a gun's rounds change: a shot, a reload or a change of ammunition, and magazines arriving in its inventory.</summary>
        /// <remarks>
        /// The gun's own event covers what it does with its rounds, and not magazines brought in through the conveyors, which
        /// the inventory's contents event does. The gun's event comes on every shot, which is one look each.
        /// </remarks>
        public override IDisposable Listen(IMyTerminalBlock block, Action told)
        {
            var gun = block as IMyGunObject<MyGunBase>;
            if (gun == null || gun.GunBase == null)
                return null;

            return new Listening(gun.GunBase, block.GetInventory(0) as MyInventoryBase, told);
        }

        /// <summary>A gun being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly MyGunBase _gun;
            readonly MyInventoryBase _inventory;
            readonly Action _told;

            public Listening(MyGunBase gun, MyInventoryBase inventory, Action told)
            {
                _gun = gun;
                _inventory = inventory;
                _told = told;
                _gun.OnAmmoAmountChanged += Fired;
                if (_inventory != null)
                    _inventory.ContentsChanged += Stocked;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _gun.OnAmmoAmountChanged -= Fired;
                if (_inventory != null)
                    _inventory.ContentsChanged -= Stocked;
            }

            void Fired()
            {
                _told();
            }

            void Stocked(MyInventoryBase inventory)
            {
                _told();
            }
        }
    }
}
