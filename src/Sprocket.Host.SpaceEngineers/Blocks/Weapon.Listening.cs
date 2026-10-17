using System;
using Sandbox.Game.Weapons;
using VRage.Game.Entity;

namespace Sprocket.Host.SpaceEngineers.Blocks
{
    partial class Weapon
    {
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
