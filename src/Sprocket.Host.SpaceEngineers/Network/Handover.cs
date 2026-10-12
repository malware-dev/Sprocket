using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Sequences;
using Sprocket.Host.SpaceEngineers.Storage;
using Sprocket.Host.SpaceEngineers.World;

namespace Sprocket.Host.SpaceEngineers.Network
{
    /// <summary>
    /// Hands a Sprocket to the machine that edits it: the server lends each one to a single editor at a
    /// time, with the program to edit, and a client asks for one and is answered.
    /// </summary>
    /// <remarks>
    /// One editor at a time is what lets a client apply its own edits the moment it makes them. With
    /// nobody else changing the same program, what it shows cannot be contradicted by the server, so
    /// nothing has to be merged or taken back when the server catches up.
    /// <para>
    /// The same object on every machine. The server answers claims and keeps the leases, a client
    /// claims and hears the answer, and a host editing its own world holds a lease the same way a
    /// client does, so a client cannot edit what the host has open.
    /// </para>
    /// </remarks>
    public sealed class Handover
    {
        readonly Func<long, SprocketUnit> _find;
        readonly Action<long, string, string> _given;
        readonly Action<long, string, string, string> _refused;
        readonly Action<long, string, string> _changed;
        readonly Action<long, string, string> _stored;

        readonly Dictionary<long, Sent> _sent = new Dictionary<long, Sent>();

        readonly Dictionary<long, Holder> _held = new Dictionary<long, Holder>();

        readonly Dictionary<long, Sent> _told = new Dictionary<long, Sent>();

        readonly List<long> _gone = new List<long>();

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
        /// A claim turned down: the block, who holds it or empty where nobody can have it, and the
        /// program and what is switched off where somebody holds it, to look at.
        /// </param>
        /// <param name="changed">On the server, the holder's program and switches after a change of theirs.</param>
        /// <param name="stored">On a client, the program and switches the server wrote into a block.</param>
        public Handover(Func<long, SprocketUnit> find, Action<long, string, string> given,
            Action<long, string, string, string> refused, Action<long, string, string> changed,
            Action<long, string, string> stored)
        {
            _find = find;
            _given = given;
            _refused = refused;
            _changed = changed;
            _stored = stored;
        }

        /// <summary>Registers what a handover sends on the channel being built, and answers the builder.</summary>
        public static NetworkChannel.Builder Carry(NetworkChannel.Builder builder)
        {
            return builder
                .UsingModel<Claim>(Claim.SizeOf, Claim.Read, Claim.Write)
                .UsingModel<Grant>(Grant.SizeOf, Grant.Read, Grant.Write)
                .UsingModel<Release>(Release.SizeOf, Release.Read, Release.Write)
                .UsingModel<Change>(Change.SizeOf, Change.Read, Change.Write)
                .UsingModel<Order>(Order.SizeOf, Order.Read, Order.Write)
                .UsingModel<Wiring>(Wiring.SizeOf, Wiring.Read, Wiring.Write);
        }

        /// <summary>Starts hearing what arrives on <paramref name="channel" />, which is built with <see cref="Carry" />.</summary>
        public void Listen(NetworkChannel channel)
        {
            _channel = channel;
            _channel.ListenFor<Claim>(Claimed);
            _channel.ListenFor<Grant>(Answered);
            _channel.ListenFor<Release>(Released);
            _channel.ListenFor<Change>(Changed);
            _channel.ListenFor<Order>(Ordered);
            _channel.ListenFor<Wiring>(Wired);
        }

        /// <summary>Asks the server for the Sprocket on <paramref name="block" />, as <paramref name="name" />.</summary>
        public void Ask(long block, string name)
        {
            Told.Say("asking the server for the Sprocket on block " + block);
            _channel.SendToServer(new Claim { Block = block, Name = name ?? "" }, true);
        }

        /// <summary>Hands back the Sprocket on <paramref name="block" />, finished with.</summary>
        public void Hand(long block)
        {
            Told.Say("handing back the Sprocket on block " + block);
            _sent.Remove(block);
            _channel.SendToServer(new Release { Block = block }, true);
        }

        /// <summary>
        /// Sends the server the Sprocket on <paramref name="block" /> as <paramref name="copy" /> has it
        /// now, where that differs from what was last sent.
        /// </summary>
        /// <remarks>
        /// Compared as written, which is what makes it the committed changes and nothing else: a picker
        /// half filled in changes nothing in the program until it is done, so nothing is sent for it.
        /// The first send after a copy arrives is measured against what arrived.
        /// </remarks>
        public void Keep(long block, SprocketUnit copy)
        {
            string program;
            string state;
            copy.Written(out program, out state);

            var switches = StateText.Save(copy.Program, null);

            Sent sent;
            if (_sent.TryGetValue(block, out sent) && sent.Program == program && sent.Switches == switches)
                return;

            _sent[block] = new Sent { Program = program, Switches = switches };
            Told.Say("sending the server a change to block " + block + ": "
                + Compared(sent.Program, program, sent.Switches, switches));
            _channel.SendToServer(new Change { Block = block, Program = program, Switches = switches }, true);
        }

        /// <summary>
        /// Asks the server to run <paramref name="sequence" /> by hand on the Sprocket on
        /// <paramref name="block" />, or to stop it.
        /// </summary>
        public void Run(long block, Sequence sequence, SystemInstance instance, bool stops)
        {
            if (sequence.System == null || instance == null)
                return;

            Told.Say("asking the server to " + (stops ? "stop" : "run") + " sequence " + sequence.Number
                + " of " + sequence.System.Identity.ToString("N") + " #" + instance.Number + " on block " + block);
            _channel.SendToServer(new Order
            {
                Block = block,
                System = sequence.System.Identity,
                Instance = instance.Number,
                Sequence = sequence.Number,
                Stops = stops
            }, true);
        }

        /// <summary>Remembers what the server lent, so sending starts from it rather than from nothing.</summary>
        public void Kept(long block, SprocketUnit copy)
        {
            string program;
            string state;
            copy.Written(out program, out state);
            _sent[block] = new Sent { Program = program, Switches = StateText.Save(copy.Program, null) };
        }

        /// <summary>
        /// Tells every client what the server wrote into <paramref name="block" />, where that differs
        /// from what they were last told.
        /// </summary>
        /// <remarks>On the server. See <see cref="Change" /> for why they need it.</remarks>
        public void Tell(long block, string program, string switches)
        {
            Sent told;
            if (_told.TryGetValue(block, out told) && told.Program == program && told.Switches == switches)
                return;

            Told.Say("telling every client what block " + block + " holds: "
                + Compared(told.Program, program, told.Switches, switches));
            _told[block] = new Sent { Program = program, Switches = switches };
            _channel.Broadcast(new Change { Block = block, Program = program, Switches = switches }, true);
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
            return true;
        }

        /// <summary>Takes back every Sprocket held by somebody <paramref name="connected" /> says has gone.</summary>
        /// <remarks>
        /// On the server. A holder who crashed or lost the connection never hands back, and what they
        /// held would otherwise stay theirs until the server restarts.
        /// </remarks>
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

            for (var i = 0; i < _gone.Count; i++)
            {
                Told.Say(Who(_held[_gone[i]].Name, _held[_gone[i]].Id) + " is gone, so block " + _gone[i]
                    + " is free again");
                _held.Remove(_gone[i]);
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

        void Claimed(ushort channel, Claim claim, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            // Lent by the block that keeps the automation, whichever programmable block on the construct was opened, so
            // one construct has one lease, and the client works with that block from here on.
            SprocketUnit unit = _find(claim.Block);
            long lent = unit == null ? claim.Block : unit.Block.EntityId;
            var grant = new Grant { Block = lent, Holder = "", Program = "", State = "" };

            var holder = "";
            if (unit != null && Lend(lent, sender, claim.Name, out holder))
            {
                grant.Given = true;
                unit.Written(out grant.Program, out grant.State);
                Told.Say(Who(claim.Name, sender) + " asked for block " + claim.Block + ", and was lent it");
            }
            else
            {
                grant.Holder = unit == null ? "" : holder;
                if (unit != null)
                    unit.Written(out grant.Program, out grant.State);

                Told.Say(Who(claim.Name, sender) + " asked for block " + claim.Block + ", and was refused: "
                    + (unit == null ? "it carries no Sprocket here" : holder + " holds it"));
            }

            _channel.Send(grant, sender, true);
        }

        void Answered(ushort channel, Grant grant, ulong sender, bool fromServer)
        {
            if (!fromServer)
                return;

            if (grant.Given)
            {
                Told.Say("the server lent block " + grant.Block + ", " + grant.Program.Length + " characters of program");
                _given(grant.Block, grant.Program, grant.State);
            }
            else
            {
                Told.Say("the server refused block " + grant.Block
                    + (grant.Holder.Length > 0 ? ": " + grant.Holder + " holds it" : ": nobody can have it"));
                _refused(grant.Block, grant.Holder, grant.Program, grant.State);
            }
        }

        void Released(ushort channel, Release release, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            Told.Say(Who(sender) + " handed back block " + release.Block
                + (Holds(release.Block, sender) ? "" : ", which they did not hold"));
            Take(release.Block, sender);
        }

        /// <summary>Asks the server to write the buttons of <paramref name="panel" /> for the Sprocket on <paramref name="block" />, as just picked.</summary>
        public void Wire(long block, long panel)
        {
            Told.Say("asking the server to wire panel " + panel + " for block " + block);
            _channel.SendToServer(new Wiring { Block = block, Panel = panel }, true);
        }

        /// <summary>A panel to wire arriving at the server, which it takes only from whoever holds that Sprocket.</summary>
        void Wired(ushort channel, Wiring wiring, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = Who(sender) + " asked to wire panel " + wiring.Panel + " for block " + wiring.Block;
            if (!Holds(wiring.Block, sender))
            {
                Told.Say(said + ", and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(wiring.Block);
            if (unit == null)
            {
                Told.Say(said + ", which carries no Sprocket here");
                return;
            }

            Told.Say(said);
            unit.Wire(wiring.Panel);
            unit.Revisit();
        }

        /// <summary>An order arriving at the server, which it takes only from whoever holds that Sprocket.</summary>
        void Ordered(ushort channel, Order order, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
                return;

            var said = Who(sender) + " asked to " + (order.Stops ? "stop" : "run") + " sequence " + order.Sequence
                + " of " + order.System.ToString("N") + " #" + order.Instance + " on block " + order.Block;
            if (!Holds(order.Block, sender))
            {
                Told.Say(said + ", and was ignored: they do not hold it");
                return;
            }

            SprocketUnit unit = _find(order.Block);
            if (unit == null)
            {
                Told.Say(said + ", which carries no Sprocket here");
                return;
            }

            Told.Say(said);
            unit.Order(order.System, order.Instance, order.Sequence, order.Stops);
        }

        /// <summary>What a change does to a program and its switches, for the log.</summary>
        /// <remarks>Either side may be null, which is nothing to compare with.</remarks>
        static string Compared(string program, string now, string switches, string switchesNow)
        {
            if (program == null)
                return "nothing to compare with; " + now.Length + " characters of program and "
                    + switchesNow.Length + " of switches";

            var said = program == now
                ? "program unchanged"
                : "program " + program.Length + " to " + now.Length + " characters, " + Difference(program, now);

            if (switches == switchesNow)
                return said + "; switches unchanged";

            return said + "; switches " + (switches == null ? "unknown" : Difference(switches, switchesNow));
        }

        /// <summary>The first line two texts disagree on, as it was and as it is.</summary>
        static string Difference(string was, string now)
        {
            string[] before = was.Split('\n');
            string[] after = now.Split('\n');

            var line = 0;
            while (line < before.Length && line < after.Length && before[line] == after[line])
                line++;

            return "first difference at line " + (line + 1) + " of " + after.Length + ": "
                + Quoted(before, line) + " became " + Quoted(after, line);
        }

        static string Quoted(string[] lines, int line)
        {
            if (line >= lines.Length)
                return "nothing";

            var text = lines[line].Trim();
            return "\"" + (text.Length > 100 ? text.Substring(0, 100) + "..." : text) + "\"";
        }

        /// <summary>A player for the log, by the name they gave where there is one, and always by id.</summary>
        static string Who(string name, ulong id)
        {
            return string.IsNullOrEmpty(name) ? "player " + id : name + " (" + id + ")";
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
        void Changed(ushort channel, Change change, ulong sender, bool fromServer)
        {
            if (!NetworkChannel.Network.IsServer)
            {
                if (!fromServer)
                    return;

                Told.Say("the server wrote block " + change.Block + ", " + change.Program.Length
                    + " characters of program and " + change.Switches.Length + " of switches");
                _stored(change.Block, change.Program, change.Switches);

                return;
            }

            if (!Holds(change.Block, sender))
            {
                Told.Say(Who(sender) + " sent a change to block " + change.Block + ", ignored: they do not hold it");
                return;
            }

            string program = null;
            string switches = null;
            SprocketUnit unit = _find(change.Block);
            if (unit != null)
            {
                string state;
                unit.Written(out program, out state);
                switches = StateText.Save(unit.Program, null);
            }

            Told.Say(Who(sender) + " sent a change to block " + change.Block + ": "
                + Compared(program, change.Program, switches, change.Switches));
            _changed(change.Block, change.Program, change.Switches);
        }
    }
}
