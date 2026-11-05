using System;
using Mal.MdkModMixin.Network;
using Sandbox.ModAPI;
using Sprocket.Host.SpaceEngineers.Shares;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>Carries orders about shared blueprints to the server's desk and its news back to the player who asked.</summary>
    public sealed class ShareCourier
    {
        readonly Func<ShareDesk> _desk;
        readonly ShareBoard _board;
        readonly Action<ShareTiding> _told;

        NetworkChannel _channel;

        /// <summary>A courier between the desk <paramref name="desk" /> gives on the server and this machine's board, handing this machine's player what they are told to <paramref name="told" />.</summary>
        public ShareCourier(Func<ShareDesk> desk, Action<ShareTiding> told)
        {
            _desk = desk;
            _told = told;
            _board = new ShareBoard(Post);
        }

        /// <summary>What this machine's player knows of shared blueprints.</summary>
        public ShareBoard Board => _board;

        /// <summary>Registers what sharing sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder
                .UsingModel<ShareOrder>(ShareOrder.SizeOf, ShareOrder.Read, ShareOrder.Write)
                .UsingModel<ShareNews>(ShareNews.SizeOf, ShareNews.Read, ShareNews.Write)
                .UsingModel<ShareTiding>(ShareTiding.SizeOf, ShareTiding.Read, ShareTiding.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<ShareOrder>(Ordered);
            _channel.ListenFor<ShareNews>(Told);
            _channel.ListenFor<ShareTiding>(Heard);
        }

        /// <summary>Tells the player <paramref name="player" /> <paramref name="tiding" />, on the machine they play on.</summary>
        public void Tell(long player, ShareTiding tiding)
        {
            if (MyAPIGateway.Session?.Player != null && MyAPIGateway.Session.Player.IdentityId == player)
            {
                _told(tiding);
                return;
            }

            var recipient = MyAPIGateway.Players?.TryGetSteamId(player) ?? 0;
            if (recipient != 0)
                _channel.Send(tiding, recipient, true);
        }

        void Heard(ushort channel, ShareTiding tiding, ulong sender, bool fromServer)
        {
            if (fromServer)
                _told(tiding);
        }

        void Post(ShareOrder order)
        {
            if (!NetworkChannel.Network.IsServer)
            {
                _channel.SendToServer(order, true);
                return;
            }

            // A hosting player's order goes straight to the desk on this machine.
            var player = MyAPIGateway.Session?.Player;
            var desk = _desk();
            if (player != null && desk != null)
                _board.Heard(desk.Hear(order, player.IdentityId, MyAPIGateway.Session.IsUserAdmin(player.SteamUserId)));
        }

        void Ordered(ushort channel, ShareOrder order, ulong sender, bool fromServer)
        {
            var desk = _desk();
            if (!NetworkChannel.Network.IsServer || desk == null || MyAPIGateway.Players == null)
                return;

            var player = MyAPIGateway.Players.TryGetIdentityId(sender);
            if (player != 0)
                _channel.Send(desk.Hear(order, player, MyAPIGateway.Session.IsUserAdmin(sender)), sender, true);
        }

        void Told(ushort channel, ShareNews news, ulong sender, bool fromServer)
        {
            if (fromServer)
                _board.Heard(news);
        }
    }
}
