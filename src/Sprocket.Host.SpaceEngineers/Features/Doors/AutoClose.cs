using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Network;
using Sprocket.Host.SpaceEngineers.Runtime;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Features.Doors
{
    /// <summary>Every door's auto-close delay: set from the terminal on any machine, kept in the door, and carried out by the server.</summary>
    /// <remarks>
    /// The delay lives in the door's mod storage, which the game saves with the world and sends to a client with the grid,
    /// and nothing after that: so a client's change goes to the server, and the server tells every client what the door
    /// keeps now. The same object on every machine; only the server closes doors, holding a <see cref="Closer" /> for each
    /// door that has a delay and nothing for the rest.
    /// </remarks>
    public sealed class AutoClose : IDisposable
    {
        readonly Rota _rota;
        readonly Func<int> _now;

        readonly Dictionary<long, Closer> _closers = new Dictionary<long, Closer>();

        NetworkChannel _channel;

        /// <summary>Auto-close counting on <paramref name="rota" />, from the tick <paramref name="now" /> answers.</summary>
        public AutoClose(Rota rota, Func<int> now)
        {
            _rota = rota;
            _now = now;
        }

        /// <summary>Registers what auto-close sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder.UsingModel<Closing>(Closing.SizeOf, Closing.Read, Closing.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<Closing>(Received);
        }

        /// <summary>Sets <paramref name="door" />'s delay to <paramref name="seconds" />, as its terminal slider does on whichever machine it was moved.</summary>
        /// <remarks>
        /// A client keeps it in its own copy of the door at once, so the slider shows what was set, and asks the server, which
        /// has the last word.
        /// </remarks>
        public void Set(IMyDoor door, int seconds)
        {
            seconds = CloseDelay.Clamped(seconds);
            if (NetworkChannel.Network.IsServer)
            {
                Keep(door, seconds);
                return;
            }

            CloseDelay.Store(door, seconds);
            if (_channel != null)
                _channel.SendToServer(new Closing { Door = door.EntityId, Seconds = seconds }, true);
        }

        /// <summary>A door come alive on the server, which starts closing itself where it keeps a delay.</summary>
        public void Arrived(IMyDoor door)
        {
            if (NetworkChannel.Network.IsServer)
                Follow(door, CloseDelay.Stored(door));
        }

        /// <summary>A door gone from the world, whose closer goes with it.</summary>
        /// <remarks>Only the closer of this door: a grid that splits makes its blocks again, and the new door may already be here under the same id.</remarks>
        public void Left(IMyDoor door)
        {
            Closer closer;
            if (!_closers.TryGetValue(door.EntityId, out closer) || closer.Door != door)
                return;

            closer.Dispose();
            _closers.Remove(door.EntityId);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            foreach (Closer closer in _closers.Values)
                closer.Dispose();

            _closers.Clear();
        }

        /// <summary>Keeps <paramref name="seconds" /> in <paramref name="door" />, closes it by that, and tells every client.</summary>
        /// <remarks>On the server.</remarks>
        void Keep(IMyDoor door, int seconds)
        {
            CloseDelay.Store(door, seconds);
            Follow(door, seconds);
            if (_channel != null)
                _channel.Broadcast(new Closing { Door = door.EntityId, Seconds = seconds }, true);
        }

        /// <summary>Closes <paramref name="door" /> <paramref name="seconds" /> after it opens, or never where that is none.</summary>
        void Follow(IMyDoor door, int seconds)
        {
            Closer closer;
            _closers.TryGetValue(door.EntityId, out closer);
            if (closer != null && (seconds == 0 || closer.Door != door))
            {
                closer.Dispose();
                _closers.Remove(door.EntityId);
                closer = null;
            }

            if (seconds == 0)
                return;

            if (closer == null)
            {
                closer = new Closer(door, _rota, _now);
                _closers.Add(door.EntityId, closer);
            }

            closer.Set(seconds);
        }

        /// <summary>
        /// A delay arriving: at the server, taken from a player who may change the door; at a client, what the server's door
        /// keeps now.
        /// </summary>
        void Received(ushort channel, Closing closing, ulong sender, bool fromServer)
        {
            var door = MyAPIGateway.Entities == null ? null : MyAPIGateway.Entities.GetEntityById(closing.Door) as IMyDoor;
            if (!NetworkChannel.Network.IsServer)
            {
                if (fromServer && door != null)
                    CloseDelay.Store(door, closing.Seconds);

                return;
            }

            if (door == null)
            {
                Told.Say("player " + sender + " set auto-close on door " + closing.Door + ", which is not here");
                return;
            }

            var identity = MyAPIGateway.Players == null ? 0L : MyAPIGateway.Players.TryGetIdentityId(sender);
            if (identity == 0L || !door.HasPlayerAccess(identity))
            {
                Told.Say("player " + sender + " set auto-close on " + door.CustomName + ", and was ignored: they may not change it");
                return;
            }

            Keep(door, closing.Seconds);
        }
    }
}
