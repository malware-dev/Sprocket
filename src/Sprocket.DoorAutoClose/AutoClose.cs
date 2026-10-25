using System;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;
using VRage.Utils;

namespace Sprocket.DoorAutoClose
{
    /// <summary>Every door's auto-close delay: set from the terminal on any machine, kept in the door, and carried out by the server.</summary>
    /// <remarks>The same object on every machine; only the server closes doors.</remarks>
    public sealed class AutoClose
    {
        readonly Func<IMyDoor, DoorCloser> _closers;

        NetworkChannel _channel;

        /// <summary>Auto-close finding each door's closer through <paramref name="closers" />, which answers null where the door has none.</summary>
        public AutoClose(Func<IMyDoor, DoorCloser> closers)
        {
            _closers = closers;
        }

        /// <summary>Registers what auto-close sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder.UsingModel<CloseDelayUpdate>(CloseDelayUpdate.SizeOf, CloseDelayUpdate.Read, CloseDelayUpdate.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<CloseDelayUpdate>(Received);
        }

        /// <summary>Sets <paramref name="door" />'s delay to <paramref name="seconds" />, as its terminal slider does on whichever machine it was moved.</summary>
        /// <remarks>On a client, keeps it in its own copy of the door at once and asks the server, which has the last word.</remarks>
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
                _channel.SendToServer(new CloseDelayUpdate { Door = door.EntityId, Seconds = seconds }, true);
        }

        /// <summary>Keeps <paramref name="seconds" /> in <paramref name="door" />, closes it by that, and tells every client.</summary>
        /// <remarks>On the server.</remarks>
        void Keep(IMyDoor door, int seconds)
        {
            CloseDelay.Store(door, seconds);
            _closers(door)?.Set(seconds);
            if (_channel != null)
                _channel.Broadcast(new CloseDelayUpdate { Door = door.EntityId, Seconds = seconds }, true);
        }

        /// <summary>
        /// A delay arriving: at the server, taken from a player who may change the door; at a client, what the server's door
        /// keeps now.
        /// </summary>
        void Received(ushort channel, CloseDelayUpdate closing, ulong sender, bool fromServer)
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
                Say($"player {sender} set auto-close on door {closing.Door}, which is not here");
                return;
            }

            var identity = MyAPIGateway.Players == null ? 0L : MyAPIGateway.Players.TryGetIdentityId(sender);
            if (identity == 0L || !door.HasPlayerAccess(identity))
            {
                Say($"player {sender} set auto-close on {door.CustomName}, and was ignored: they may not change it");
                return;
            }

            Keep(door, closing.Seconds);
        }

        /// <summary>Writes <paramref name="what" /> to the game's log and a dedicated server's console.</summary>
        static void Say(string what)
        {
            MyLog.Default.WriteLineAndConsole("[Door Auto-Close] " + what);
        }
    }
}
