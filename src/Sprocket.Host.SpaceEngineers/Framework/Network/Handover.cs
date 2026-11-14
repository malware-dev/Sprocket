using Sprocket.Host.SpaceEngineers.Framework.Game.Grids;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Mal.MdkModMixin.Network;

namespace Sprocket.Host.SpaceEngineers.Framework.Network
{
    /// <summary>
    /// Hands a Sprocket to the machine that edits it: the server lends each one to a single editor at a
    /// time, with the program to edit, and a client asks for one and is answered.
    /// </summary>
    /// <remarks>
    /// The same object on every machine: the server answers claims and keeps the leases, a client claims and hears the
    /// answer, and a host editing its own world holds a lease as a client does.
    /// </remarks>
    public sealed class Handover
    {
        readonly Func<long, SprocketUnit> _find;
        readonly Action<long, string, string, string> _given;
        readonly Action<long, string, string, string, string> _refused;
        readonly Action<long> _foreign;
        readonly Action<long, string, string> _changed;
        readonly Action<long, string, string> _stored;
        readonly Func<long, PlayerMemory> _player;
        readonly Func<string> _settings;
        readonly Action<string> _settled;
        readonly Action<string> _ruled;

        readonly Dictionary<long, Sent> _sent = new Dictionary<long, Sent>();

        readonly Dictionary<long, Holder> _held = new Dictionary<long, Holder>();

        readonly Dictionary<long, Sent> _told = new Dictionary<long, Sent>();

        readonly List<long> _gone = new List<long>();

        readonly List<long> _wiring = new List<long>();

        NetworkChannel _channel;

        struct Holder
        {
            public ulong Id;
            public string Name;
        }

        struct Sent
        {
            public string Program;
            public string Switches;
        }

        /// <summary>
        /// A handover finding a block's Sprocket with <paramref name="find" /> where it is the server,
        /// and telling <paramref name="given" /> or <paramref name="refused" /> what the server answered
        /// where it is a client.
        /// </summary>
        /// <param name="find">The Sprocket a block carries, by entity id, made where it has none; or null.</param>
        /// <param name="given">A claim answered: the block, the program and what is switched off.</param>
        /// <param name="refused">
        /// A claim turned down: the block, who holds it or empty, and the program and switches where somebody holds it.
        /// </param>
        /// <param name="foreign">A claim turned down because the one who asked is none of the construct's big owners: the block.</param>
        /// <param name="changed">On the server, the holder's program and switches after a change of theirs.</param>
        /// <param name="stored">On a client, the program and switches the server wrote into a block.</param>
        /// <param name="player">A player's settings in this world, by identity: anybody's on the server, a client's own on a client.</param>
        /// <param name="settings">On the server, the settings the world runs by, written as one text.</param>
        /// <param name="settled">On a client, the server's settings arriving, which it takes as its own.</param>
        /// <param name="ruled">On the server, settings an admin's client proposed, which it takes and keeps before telling every client.</param>
        public Handover(Func<long, SprocketUnit> find, Action<long, string, string, string> given,
            Action<long, string, string, string, string> refused, Action<long> foreign, Action<long, string, string> changed,
            Action<long, string, string> stored, Func<long, PlayerMemory> player, Func<string> settings, Action<string> settled,
            Action<string> ruled)
        {
            _find = find;
            _given = given;
            _refused = refused;
            _foreign = foreign;
            _changed = changed;
            _stored = stored;
            _player = player;
            _settings = settings;
            _settled = settled;
            _ruled = ruled;
        }

        /// <summary>Registers what a handover sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder
                .UsingModel<EditClaim>(EditClaim.SizeOf, EditClaim.Read, EditClaim.Write)
                .UsingModel<EditGrant>(EditGrant.SizeOf, EditGrant.Read, EditGrant.Write)
                .UsingModel<EditRelease>(EditRelease.SizeOf, EditRelease.Read, EditRelease.Write)
                .UsingModel<ProgramChange>(ProgramChange.SizeOf, ProgramChange.Read, ProgramChange.Write)
                .UsingModel<RunOrder>(RunOrder.SizeOf, RunOrder.Read, RunOrder.Write)
                .UsingModel<WiringRequest>(WiringRequest.SizeOf, WiringRequest.Read, WiringRequest.Write)
                .UsingModel<UnwiringRequest>(UnwiringRequest.SizeOf, UnwiringRequest.Read, UnwiringRequest.Write)
                .UsingModel<PlayerPreference>(PlayerPreference.SizeOf, PlayerPreference.Read, PlayerPreference.Write)
                .UsingModel<SettingsRuling>(SettingsRuling.SizeOf, SettingsRuling.Read, SettingsRuling.Write)
                .UsingModel<ResumeOrder>(ResumeOrder.SizeOf, ResumeOrder.Read, ResumeOrder.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<EditClaim>(Claimed);
            _channel.ListenFor<EditGrant>(Answered);
            _channel.ListenFor<EditRelease>(Released);
            _channel.ListenFor<ProgramChange>(Changed);
            _channel.ListenFor<RunOrder>(Ordered);
            _channel.ListenFor<WiringRequest>(Wired);
            _channel.ListenFor<UnwiringRequest>(Unwired);
            _channel.ListenFor<PlayerPreference>(Preferred);
            _channel.ListenFor<SettingsRuling>(Ruled);
            _channel.ListenFor<ResumeOrder>(Resumed);
        }

        /// <summary>Asks the server for the Sprocket on <paramref name="block" />, as <paramref name="name" />.</summary>
        public void Ask(long block, string name)
        {
            GameLog.Say($"asking the server for the Sprocket on block {block}");
            _channel.SendToServer(new EditClaim { Block = block, Name = name ?? "" }, true);
        }

        /// <summary>Tells the server this client's player's settings in this world, as <paramref name="mine" /> holds them now.</summary>
        public void Prefer(PlayerMemory mine)
        {
            _channel.SendToServer(new PlayerPreference
            {
                Ticks = mine.Ticks, ColorTab = mine.ColorTab, ShowsHidden = mine.ShowsHidden, RecentColors = string.Join(" ", mine.RecentColors)
            }, true);
        }

        /// <summary>Proposes <paramref name="settings" />, written as one text, to the server, which takes them where this client's player is an admin.</summary>
        public void Propose(string settings)
        {
            _channel.SendToServer(new SettingsRuling { Settings = settings }, true);
        }

        /// <summary>Tells every client the settings the server runs the world by now.</summary>
        /// <remarks>On the server, after an admin changed them.</remarks>
        public void Rule()
        {
            _channel.Broadcast(new SettingsRuling { Settings = _settings() }, true);
        }

        /// <summary>Hands back the Sprocket on <paramref name="block" />, finished with.</summary>
        public void Hand(long block)
        {
            GameLog.Say($"handing back the Sprocket on block {block}");
            _sent.Remove(block);
            _channel.SendToServer(new EditRelease { Block = block }, true);
        }

        /// <summary>
        /// Sends the server the Sprocket on <paramref name="block" /> as <paramref name="copy" /> has it
        /// now, where that differs from what was last sent.
        /// </summary>
        /// <remarks>
        /// Compared as written, so only committed changes are sent; the first send after a copy arrives is measured against
        /// what arrived.
        /// </remarks>
        public void Keep(long block, SprocketUnit copy)
        {
            string program;
            string state;
            copy.Written(out program, out state);

            var switches = state;

            Sent sent;
            if (_sent.TryGetValue(block, out sent) && sent.Program == program && sent.Switches == switches)
                return;

            _sent[block] = new Sent { Program = program, Switches = switches };
            GameLog.Say($"sending the server a change to block {block}: {Compared(sent.Program, program, sent.Switches, switches)}");
            _channel.SendToServer(new ProgramChange { Block = block, Program = program, Switches = switches }, true);
        }

        /// <summary>
        /// Asks the server to run <paramref name="sequence" /> by hand on the Sprocket on
        /// <paramref name="block" />, or to stop it.
        /// </summary>
        public void Run(long block, Sequence sequence, SystemInstance instance, bool stops)
        {
            if (sequence.System == null || instance == null)
                return;

            GameLog.Say($"asking the server to {(stops ? "stop" : "run")} sequence {sequence.Number} of {sequence.System.Identity:N} #{instance.Number} on block {block}");
            _channel.SendToServer(new RunOrder
            {
                Block = block,
                System = sequence.System.Identity,
                Instance = instance.Number,
                Sequence = sequence.Number,
                Stops = stops
            }, true);
        }

        /// <summary>Asks the server to resume the paused automation of the Sprocket on <paramref name="block" />.</summary>
        public void Resume(long block)
        {
            GameLog.Say($"asking the server to resume the automation on block {block}");
            _channel.SendToServer(new ResumeOrder { Block = block }, true);
        }

        /// <summary>Remembers what the server lent, so sending starts from it rather than from nothing.</summary>
        public void Kept(long block, SprocketUnit copy)
        {
            string program;
            string state;
            copy.Written(out program, out state);
            _sent[block] = new Sent { Program = program, Switches = state };
        }

        /// <summary>
        /// Tells every client what the server wrote into <paramref name="block" />, where that differs
        /// from what they were last told.
        /// </summary>
        /// <remarks>On the server.</remarks>
        public void Tell(long block, string program, string switches)
        {
            Sent told;
            if (_told.TryGetValue(block, out told) && told.Program == program && told.Switches == switches)
                return;

            GameLog.Say($"telling every client what block {block} holds: {Compared(told.Program, program, told.Switches, switches)}");
            _told[block] = new Sent { Program = program, Switches = switches };
            _channel.Broadcast(new ProgramChange { Block = block, Program = program, Switches = switches }, true);
        }

        /// <summary>
        /// Lends the Sprocket on <paramref name="block" /> to <paramref name="id" />, and answers whether
        /// it could; where it could not, <paramref name="holder" /> says who has it.
        /// </summary>
        /// <remarks>On the server. Lent again to whoever already holds it, which is a player reopening their own editor.</remarks>
        public bool Lend(long block, ulong id, string name, out string holder)
        {
            Holder held;
            if (_held.TryGetValue(block, out held) && held.Id != id)
            {
                holder = held.Name;
                return false;
            }

            _held[block] = new Holder { Id = id, Name = name ?? "" };
            holder = "";
            if (Lent != null)
                Lent();

            return true;
        }

        /// <summary>Raised on the server as a Sprocket is lent to somebody.</summary>
        public event Action Lent;

        /// <summary>Whether anybody holds a Sprocket lent by this server.</summary>
        public bool Lending => _held.Count > 0;

        /// <summary>Takes back every Sprocket held by somebody <paramref name="connected" /> says has gone.</summary>
        /// <remarks>On the server.</remarks>
        public void Reclaim(Func<ulong, bool> connected)
        {
            if (_held.Count == 0)
                return;

            _gone.Clear();
            foreach (KeyValuePair<long, Holder> held in _held)
            {
                if (!connected(held.Value.Id))
                    _gone.Add(held.Key);
            }

            foreach (var key in _gone)
            {
                GameLog.Say($"{Who(_held[key].Name, _held[key].Id)} is gone, so block {key} is free again");
                _held.Remove(key);
            }
        }

        /// <summary>Takes back the Sprocket on <paramref name="block" /> from <paramref name="id" />, where they hold it.</summary>
        /// <remarks>On the server.</remarks>
        public void Take(long block, ulong id)
        {
            Holder held;
            if (_held.TryGetValue(block, out held) && held.Id == id)
                _held.Remove(block);
        }

        void Claimed(ushort channel, EditClaim claim, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            // Lent by the block that keeps the automation, whichever programmable block on the construct was opened, so
            // one construct has one lease, and the client works with that block from here on.
            SprocketUnit unit = _find(claim.Block);
            long lent = unit == null ? claim.Block : unit.Block.EntityId;
            var identity = Identity(sender);
            var grant = new EditGrant
            {
                Block = lent, Holder = "", Program = "", State = "", Settings = _settings()
            };
            var asker = _player(identity);
            grant.Ticks = asker.Ticks;
            grant.ColorTab = asker.ColorTab;
            grant.ShowsHidden = asker.ShowsHidden;
            grant.RecentColors = string.Join(" ", asker.RecentColors);

            // The automation opens from a programmable block a big owner owns, for whoever may use it, whoever holds it.
            if (unit != null && !unit.MayOpen(claim.Block, identity))
            {
                grant.Foreign = true;
                GameLog.Say($"{Who(claim.Name, sender)} asked for block {claim.Block}, and was refused: no big owner shares it with them");
                _channel.Send(grant, sender, true);
                return;
            }

            var holder = "";
            if (unit != null && Lend(lent, sender, claim.Name, out holder))
            {
                grant.Given = true;
                unit.Written(out grant.Program, out grant.State);
                grant.Memory = unit.Remembered();
                GameLog.Say($"{Who(claim.Name, sender)} asked for block {claim.Block}, and was lent it");
            }
            else
            {
                grant.Holder = unit == null ? "" : holder;
                if (unit != null)
                {
                    unit.Written(out grant.Program, out grant.State);
                    grant.Memory = unit.Remembered();
                }

                GameLog.Say($"{Who(claim.Name, sender)} asked for block {claim.Block}, and was refused: {(unit == null ? "it carries no Sprocket here" : $"{holder} holds it")}");
            }

            _channel.Send(grant, sender, true);
        }

        void Answered(ushort channel, EditGrant grant, ulong sender, bool fromServer)
        {
            if (!fromServer)
                return;

            var me = MyAPIGateway.Session == null || MyAPIGateway.Session.Player == null ? 0L : MyAPIGateway.Session.Player.IdentityId;
            Kept(_player(me), grant.Ticks, grant.ColorTab, grant.ShowsHidden, grant.RecentColors);
            _settled(grant.Settings);

            if (grant.Foreign)
            {
                GameLog.Say($"the server refused block {grant.Block}: its automation belongs to its owners");
                _foreign(grant.Block);
                return;
            }

            if (grant.Given)
            {
                GameLog.Say($"the server lent block {grant.Block}, {grant.Program.Length} characters of program");
                _given(grant.Block, grant.Program, grant.State, grant.Memory);
            }
            else
            {
                GameLog.Say($"the server refused block {grant.Block}{(grant.Holder.Length > 0 ? $": {grant.Holder} holds it" : ": nobody can have it")}");
                _refused(grant.Block, grant.Holder, grant.Program, grant.State, grant.Memory);
            }
        }

        void Preferred(ushort channel, PlayerPreference preference, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            Kept(_player(Identity(sender)), preference.Ticks, preference.ColorTab, preference.ShowsHidden, preference.RecentColors);
        }

        /// <summary>Gives <paramref name="player" /> the settings said, the colors as their codes separated by spaces.</summary>
        static void Kept(PlayerMemory player, bool ticks, int colorTab, bool hidden, string recent)
        {
            player.Ticks = ticks;
            player.ColorTab = colorTab;
            player.ShowsHidden = hidden;
            player.RecentColors.Clear();
            player.RecentColors.AddRange((recent ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

        /// <summary>
        /// Settings arriving: at the server, proposed by a client, taken only from an admin and then told to every client; at a
        /// client, the server's, which it takes as its own.
        /// </summary>
        void Ruled(ushort channel, SettingsRuling ruling, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
            {
                if (fromServer)
                    _settled(ruling.Settings);

                return;
            }

            if (MyAPIGateway.Session == null || !MyAPIGateway.Session.IsUserAdmin(sender))
            {
                GameLog.Say($"{Who(sender)} proposed settings for this world, and was refused: they are no admin");
                return;
            }

            GameLog.Say($"{Who(sender)} changed the settings for this world");
            _ruled(ruling.Settings);
            Rule();
        }

        /// <summary>The identity of the player on the client <paramref name="sender" />, or nought where the game has none.</summary>
        static long Identity(ulong sender)
        {
            return MyAPIGateway.Players == null ? 0L : MyAPIGateway.Players.TryGetIdentityId(sender);
        }

        void Released(ushort channel, EditRelease release, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            GameLog.Say($"{Who(sender)} handed back block {release.Block}{(Holds(release.Block, sender) ? "" : ", which they did not hold")}");
            Take(release.Block, sender);
        }

        /// <summary>Asks the server to write the buttons of <paramref name="panel" /> for the Sprocket on <paramref name="block" />, as just picked.</summary>
        public void Wire(long block, long panel)
        {
            GameLog.Say($"asking the server to wire panel {panel} for block {block}");
            if (!_wiring.Contains(block))
                _wiring.Add(block);

            _channel.SendToServer(new WiringRequest { Block = block, Panel = panel }, true);
        }

        /// <summary>Asks the server to let go of the buttons it was asked to write for the Sprocket on <paramref name="block" />, as a pick is backed out of.</summary>
        /// <remarks>Sent only where something was asked since, because every step back in the editor lets go.</remarks>
        public void Unwire(long block)
        {
            if (!_wiring.Remove(block))
                return;

            GameLog.Say($"asking the server to let go of the buttons picked for block {block}");
            _channel.SendToServer(new UnwiringRequest { Block = block }, true);
        }

        /// <summary>A panel to wire arriving at the server, which it takes only from whoever holds that Sprocket.</summary>
        void Wired(ushort channel, WiringRequest wiring, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = $"{Who(sender)} asked to wire panel {wiring.Panel} for block {wiring.Block}";
            if (!Holds(wiring.Block, sender))
            {
                GameLog.Say($"{said}, and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(wiring.Block);
            if (unit == null)
            {
                GameLog.Say($"{said}, which carries no Sprocket here");
                return;
            }

            GameLog.Say(said);
            unit.Wire(wiring.Panel);
            unit.Revisit();
        }

        /// <summary>A pick backed out of arriving at the server, which it takes only from whoever holds that Sprocket.</summary>
        void Unwired(ushort channel, UnwiringRequest unwiring, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = $"{Who(sender)} let go of the buttons picked for block {unwiring.Block}";
            if (!Holds(unwiring.Block, sender))
            {
                GameLog.Say($"{said}, and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(unwiring.Block);
            if (unit == null)
            {
                GameLog.Say($"{said}, which carries no Sprocket here");
                return;
            }

            GameLog.Say(said);
            unit.Unwire();
        }

        /// <summary>An order arriving at the server, which it takes only from whoever holds that Sprocket.</summary>
        void Ordered(ushort channel, RunOrder order, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = $"{Who(sender)} asked to {(order.Stops ? "stop" : "run")} sequence {order.Sequence} of {order.System:N} #{order.Instance} on block {order.Block}";
            if (!Holds(order.Block, sender))
            {
                GameLog.Say($"{said}, and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(order.Block);
            if (unit == null)
            {
                GameLog.Say($"{said}, which carries no Sprocket here");
                return;
            }

            GameLog.Say(said);
            unit.Order(order.System, order.Instance, order.Sequence, order.Stops);
        }

        void Resumed(ushort channel, ResumeOrder order, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = $"{Who(sender)} asked to resume the automation on block {order.Block}";
            if (!Holds(order.Block, sender))
            {
                GameLog.Say($"{said}, and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(order.Block);
            if (unit == null)
            {
                GameLog.Say($"{said}, which carries no Sprocket here");
                return;
            }

            GameLog.Say(said);
            unit.Resume();
        }

        /// <summary>What a change does to a program and its switches, for the log.</summary>
        /// <remarks>Either side may be null, which is nothing to compare with.</remarks>
        static string Compared(string program, string now, string switches, string switchesNow)
        {
            if (program == null)
                return $"nothing to compare with; {now.Length} characters of program and {switchesNow.Length} of switches";

            var said = program == now
                ? "program unchanged"
                : $"program {program.Length} to {now.Length} characters, {Difference(program, now)}";

            if (switches == switchesNow)
                return $"{said}; switches unchanged";

            return $"{said}; switches {(switches == null ? "unknown" : Difference(switches, switchesNow))}";
        }

        /// <summary>The first line two texts disagree on, as it was and as it is.</summary>
        static string Difference(string was, string now)
        {
            string[] before = was.Split('\n');
            string[] after = now.Split('\n');

            var line = 0;
            while (line < before.Length && line < after.Length && before[line] == after[line])
                line++;

            return $"first difference at line {(line + 1)} of {after.Length}: {Quoted(before, line)} became {Quoted(after, line)}";
        }

        static string Quoted(string[] lines, int line)
        {
            if (line >= lines.Length)
                return "nothing";

            var text = lines[line].Trim();
            return $"\"{(text.Length > 100 ? $"{text.Substring(0, 100)}..." : text)}\"";
        }

        /// <summary>A player for the log, by the name they gave where there is one, and always by id.</summary>
        static string Who(string name, ulong id)
        {
            return string.IsNullOrEmpty(name) ? $"player {id}" : $"{name} ({id})";
        }

        /// <summary>A player for the log, named where they hold something.</summary>
        string Who(ulong id)
        {
            foreach (Holder held in _held.Values)
            {
                if (held.Id == id)
                    return Who(held.Name, id);
            }

            return Who(null, id);
        }

        bool Holds(long block, ulong id)
        {
            Holder held;
            return _held.TryGetValue(block, out held) && held.Id == id;
        }

        /// <summary>
        /// A change arriving: at the server, taken only from whoever holds that Sprocket; at a client,
        /// what the server wrote into the block.
        /// </summary>
        void Changed(ushort channel, ProgramChange change, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
            {
                if (!fromServer)
                    return;

                GameLog.Say($"the server wrote block {change.Block}, {change.Program.Length} characters of program and {change.Switches.Length} of switches");
                _stored(change.Block, change.Program, change.Switches);

                return;
            }

            if (!Holds(change.Block, sender))
            {
                GameLog.Say($"{Who(sender)} sent a change to block {change.Block}, ignored: they do not hold it");
                return;
            }

            string program = null;
            string switches = null;
            SprocketUnit unit = _find(change.Block);
            if (unit != null)
            {
                unit.Written(out program, out switches);
            }

            GameLog.Say($"{Who(sender)} sent a change to block {change.Block}: {Compared(program, change.Program, switches, change.Switches)}");
            _changed(change.Block, change.Program, change.Switches);
        }
    }
}
