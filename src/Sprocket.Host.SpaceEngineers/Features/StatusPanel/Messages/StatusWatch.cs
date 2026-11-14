using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Running;
using System;
using System.Collections.Generic;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Features.StatusPanel.Messages
{
    /// <summary>
    /// How a construct's automation stands, worked out once on the server for every status panel showing it: a client asks
    /// once for a construct a panel shows, the server answers and then sends every change, and the client lets go once no
    /// panel has shown it for five seconds. On a host the panels take what the server works out, with nothing sent.
    /// </summary>
    /// <remarks>
    /// The same object on every machine. Looks at what it watches once in a hundred ticks, and only while anything is
    /// watched; a construct whose automation does not change sends nothing and redraws nothing.
    /// </remarks>
    public sealed class StatusWatch
    {
        /// <summary>How long a client keeps watching a construct no panel has shown, in ticks: three of a panel's runs.</summary>
        public const int Unseen = 300;

        readonly Rota _rota;
        readonly Func<int> _now;
        readonly Func<long, List<SprocketUnit>, string> _find;
        readonly Func<ulong, bool> _connected;
        readonly Shift _shift;
        readonly Dictionary<long, Served> _served = new Dictionary<long, Served>();
        readonly Dictionary<long, Watched> _watched = new Dictionary<long, Watched>();
        readonly List<SprocketUnit> _units = new List<SprocketUnit>();
        readonly List<long> _done = new List<long>();

        NetworkChannel _channel;
        bool _turning;

        /// <summary>A construct the server works out, and which other machines it tells.</summary>
        sealed class Served
        {
            public readonly StatusSource Source = new StatusSource();
            public readonly List<ulong> Clients = new List<ulong>();
            public int Sent = -1;
        }

        /// <summary>A construct this machine's panels show, what they last heard of it, and the panels.</summary>
        sealed class Watched
        {
            public readonly ConstructStatus Status = new ConstructStatus();
            public readonly List<Action> Panels = new List<Action>();
            public int Seen;
            public int Taken = -1;
        }

        /// <summary>
        /// A watch looking on <paramref name="rota" />, at the tick <paramref name="now" /> says, that finds a construct's
        /// automation with <paramref name="find" />, which fills its units and answers its name, or null where the construct
        /// is gone, and asks <paramref name="connected" /> whether a client is still there.
        /// </summary>
        public StatusWatch(Rota rota, Func<int> now, Func<long, List<SprocketUnit>, string> find, Func<ulong, bool> connected)
        {
            _rota = rota;
            _now = now;
            _find = find;
            _connected = connected;
            _shift = new Shift(rota.Join(), Turn);
        }

        /// <summary>Registers what the watch sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder
                .UsingModel<WatchRequest>(WatchRequest.SizeOf, WatchRequest.Read, WatchRequest.Write)
                .UsingModel<StatusReport>(StatusReport.SizeOf, StatusReport.Read, StatusReport.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<WatchRequest>(Asked);
            _channel.ListenFor<StatusReport>(Told);
        }

        /// <summary>
        /// How the automation of <paramref name="construct" /> stands as the server last said, for a panel that redraws itself
        /// with <paramref name="panel" /> when the server says more; asks the server where nothing here watches it yet, and on
        /// a host works it out at once.
        /// </summary>
        /// <remarks>A panel asks every time it is drawn, which keeps the watch. Called on the game's thread only.</remarks>
        public ConstructStatus Watch(long construct, Action panel)
        {
            Watched watched;
            if (!_watched.TryGetValue(construct, out watched))
            {
                watched = new Watched();
                _watched.Add(construct, watched);
                if (NetworkChannel.Network.IsServer)
                {
                    // The panel asking draws what this answers, so no panel is told to redraw.
                    var served = Serve(construct);
                    if (Look(construct, served))
                        Take(watched, served);
                }
                else
                    _channel.SendToServer(new WatchRequest { Construct = construct, Watching = true }, true);

                Turning();
            }

            if (!watched.Panels.Contains(panel))
                watched.Panels.Add(panel);

            watched.Seen = _now();
            return watched.Status;
        }

        /// <summary>Stops <paramref name="panel" /> being drawn again for <paramref name="construct" />; the watch itself lasts until unseen.</summary>
        public void Unwatch(long construct, Action panel)
        {
            Watched watched;
            if (_watched.TryGetValue(construct, out watched))
                watched.Panels.Remove(panel);
        }

        /// <summary>A client asking to watch a construct, or to stop.</summary>
        void Asked(ushort channel, WatchRequest request, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            Served served;
            if (!request.Watching)
            {
                if (_served.TryGetValue(request.Construct, out served))
                {
                    served.Clients.Remove(sender);
                    if (served.Clients.Count == 0)
                        _served.Remove(request.Construct);
                }

                return;
            }

            served = Serve(request.Construct);
            if (!served.Clients.Contains(sender))
                served.Clients.Add(sender);

            // Answered at once, so a panel coming into view is not left waiting for the next look.
            if (Look(request.Construct, served))
            {
                served.Sent = served.Source.Status.Revision;
                _channel.Send(Report(request.Construct, served.Source.Status), sender, true);
            }

            Turning();
        }

        /// <summary>The server saying how a construct stands, which every panel showing it draws at once.</summary>
        void Told(ushort channel, StatusReport report, ulong sender, bool fromServer)
        {
            Watched watched;
            if (NetworkChannel.Network.IsServer || !_watched.TryGetValue(report.Construct, out watched))
                return;

            watched.Status.Hear(report.Name, report.Systems, report.Normal, report.Items, report.KeptBlocks, report.KeptCounts);
            Redraw(watched);
        }

        /// <summary>
        /// Once in a hundred ticks while anything is watched: on the server, what changed is sent to the clients watching it
        /// and taken by a host's own panels, and a client that has gone is forgotten; on any machine, a construct no panel has
        /// shown for <see cref="Unseen" /> ticks is let go.
        /// </summary>
        void Turn(int tick)
        {
            _done.Clear();
            foreach (var pair in _served)
            {
                var served = pair.Value;
                for (var i = served.Clients.Count - 1; i >= 0; i--)
                {
                    if (!_connected(served.Clients[i]))
                        served.Clients.RemoveAt(i);
                }

                Watched here;
                _watched.TryGetValue(pair.Key, out here);
                if ((served.Clients.Count == 0 && here == null) || !Look(pair.Key, served))
                {
                    _done.Add(pair.Key);
                    continue;
                }

                var status = served.Source.Status;
                if (here != null && here.Taken != status.Revision)
                {
                    Take(here, served);
                    Redraw(here);
                }

                if (served.Clients.Count == 0 || status.Revision == served.Sent)
                    continue;

                served.Sent = status.Revision;
                var report = Report(pair.Key, status);
                foreach (var client in served.Clients)
                    _channel.Send(report, client, true);
            }

            foreach (var construct in _done)
                _served.Remove(construct);

            _done.Clear();
            foreach (var pair in _watched)
            {
                if (tick - pair.Value.Seen >= Unseen)
                    _done.Add(pair.Key);
            }

            foreach (var construct in _done)
            {
                _watched.Remove(construct);
                if (!NetworkChannel.Network.IsServer)
                    _channel.SendToServer(new WatchRequest { Construct = construct, Watching = false }, true);
            }

            if (_served.Count == 0 && _watched.Count == 0)
            {
                _rota.Leave(_shift);
                _turning = false;
            }
        }

        /// <summary>Works out how <paramref name="construct" /> stands for <paramref name="served" />, and answers false where it is gone.</summary>
        bool Look(long construct, Served served)
        {
            var name = _find(construct, _units);
            if (name == null)
                return false;

            served.Source.Look(_units, name, _now());
            return true;
        }

        /// <summary>The server's working out of <paramref name="construct" />, made where there is none yet.</summary>
        Served Serve(long construct)
        {
            Served served;
            if (!_served.TryGetValue(construct, out served))
            {
                served = new Served();
                _served.Add(construct, served);
            }

            return served;
        }

        /// <summary>Gives a host's panels on <paramref name="watched" /> what <paramref name="served" /> worked out.</summary>
        /// <remarks>Copies the installations, which happens only when they changed.</remarks>
        static void Take(Watched watched, Served served)
        {
            var status = served.Source.Status;
            watched.Taken = status.Revision;
            var kept = status.KeptBlocks();
            watched.Status.Hear(status.Construct, status.Systems, status.Normal, status.Copy(), kept, status.KeptCounts(kept));
        }

        /// <summary>Has every panel on <paramref name="watched" /> draw what it now holds.</summary>
        static void Redraw(Watched watched)
        {
            for (var i = 0; i < watched.Panels.Count; i++)
                watched.Panels[i]();
        }

        /// <summary>A report of how <paramref name="construct" /> stands, as <paramref name="status" /> says.</summary>
        static StatusReport Report(long construct, ConstructStatus status)
        {
            var kept = status.KeptBlocks();
            return new StatusReport
            {
                KeptBlocks = kept,
                KeptCounts = status.KeptCounts(kept),
                Construct = construct,
                Name = status.Construct,
                Systems = status.Systems,
                Normal = status.Normal,
                Items = status.Copy()
            };
        }

        /// <summary>Puts the watch on the rota, where it is not on it already.</summary>
        void Turning()
        {
            if (_turning)
                return;

            _turning = true;
            _rota.Every(_shift, Rota.Hundred);
        }
    }
}
