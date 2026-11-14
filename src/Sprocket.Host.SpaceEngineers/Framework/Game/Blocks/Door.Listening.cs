using System;
using Sandbox.ModAPI;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Blocks
{
    partial class Door
    {
        /// <summary>A door being listened to, until disposed.</summary>
        sealed class Listening : IDisposable
        {
            readonly IMyDoor _door;
            readonly Action _told;
            readonly bool _hangar;

            public Listening(IMyDoor door, Action told)
            {
                _door = door;
                _told = told;
                _hangar = door is IMyAirtightHangarDoor;
                _door.OnDoorOpened += Moved;
                _door.OnDoorClosed += Moved;
                _door.OnDoorStateChanged += Turned;
                if (_hangar)
                    _door.PropertiesChanged += Changed;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _door.OnDoorOpened -= Moved;
                _door.OnDoorClosed -= Moved;
                _door.OnDoorStateChanged -= Turned;
                if (_hangar)
                    _door.PropertiesChanged -= Changed;
            }

            void Moved(IMyDoor door)
            {
                _told();
            }

            void Turned(IMyDoor door, bool open)
            {
                _told();
            }

            void Changed(IMyTerminalBlock block)
            {
                _told();
            }
        }
    }
}
