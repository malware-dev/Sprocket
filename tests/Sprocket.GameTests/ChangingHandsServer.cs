using System.Collections.Generic;
using System.IO;
using System.Linq;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using SpaceEngineers.Game.Entities.Blocks;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace Sprocket.GameTests
{
    /// <summary>
    /// The dedicated server's side of Testing row #2, a construct changing hands: it pastes Sprocket Forced Door, stands in for its
    /// owner and the owner's faction mate as two NPCs of one faction, and moves the construct and its door between them and the
    /// joined client (ChangingHandsClientTest) step by step, each step begun by a file the client writes and ended by one it writes.
    /// </summary>
    public sealed class ChangingHandsServer
    {
        // The faction the owner and the mate share, and their names, as the paused screen says them.
        const string Tag = "SPT";
        const string Owner = "Sprocket Test Owner";
        const string Mate = "Sprocket Test Mate";

        readonly GameTestReport _report;
        readonly string _signals;
        readonly string _blueprints;

        /// <summary>The server's side, answering into <paramref name="report" />, signalled through <paramref name="signals" />, pasting from <paramref name="blueprints" />.</summary>
        public ChangingHandsServer(GameTestReport report, string signals, string blueprints)
        {
            _report = report;
            _signals = signals;
            _blueprints = blueprints;
        }

        /// <summary>Whether the client has asked for the steps to begin.</summary>
        public bool Asked => File.Exists(Path.Combine(_signals, "hands-1"));

        /// <summary>The steps, each yielding the plugin updates to wait before the next, for the client called <paramref name="player" />, near <paramref name="near" />.</summary>
        public IEnumerator<int> Run(IMyPlayer player, Vector3D near)
        {
            var pasted = new PastedBlueprint(Path.Combine(_blueprints, "Sprocket Forced Door", "bp.sbc"), near);
            var block = pasted.Find("Forced Automation");
            var door = pasted.Find("Forced Door") as IMyDoor;
            var button = pasted.Find("Force Button") as MyButtonPanel;
            if (block == null || door == null || button == null)
            {
                _report.Fail("hands paste", "Forced Automation, Forced Door or Force Button is missing after the paste");
                yield break;
            }

            yield return 5 * PluginUpdates.PerSecond;
            var factions = MyAPIGateway.Session.Factions;
            factions.CreateNPCFaction(Tag, "Sprocket Test Faction", "", "");
            yield return PluginUpdates.PerSecond;
            var faction = factions.TryGetFactionByTag(Tag);
            factions.AddNewNPCToFaction(faction.FactionId, Owner);
            factions.AddNewNPCToFaction(faction.FactionId, Mate);
            yield return PluginUpdates.PerSecond;
            var owner = Identity(Owner);
            var mate = Identity(Mate);
            if (owner == 0 || mate == 0)
            {
                _report.Fail("hands faction", $"the NPCs {Owner} and {Mate} were not made in {Tag}");
                yield break;
            }

            // Step 1: the owner's, shared with the faction, handed to the mate, and its button pressed.
            block.CubeGrid.ChangeGridOwnership(owner, MyOwnershipShareModeEnum.Faction);
            yield return 3 * PluginUpdates.PerSecond;
            block.CubeGrid.ChangeGridOwnership(mate, MyOwnershipShareModeEnum.Faction);
            yield return 3 * PluginUpdates.PerSecond;
            var opened = Pressed(door, button);
            while (opened.MoveNext())
                yield return opened.Current;
            _report.Check("hands to a faction mate keeps running", Opening(door), $"after the construct went to {Mate}, Force Button left Forced Door {door.Status}");
            Signal("hands-2");

            // Step 3: handed to the client, outside the faction.
            var wait = Waiting("hands-3");
            while (wait.MoveNext())
                yield return wait.Current;
            block.CubeGrid.ChangeGridOwnership(player.IdentityId, MyOwnershipShareModeEnum.None);
            yield return 3 * PluginUpdates.PerSecond;
            Signal("hands-4");

            // Step 4: the client resumed it; its button pressed again.
            wait = Waiting("hands-5");
            while (wait.MoveNext())
                yield return wait.Current;
            opened = Pressed(door, button);
            while (opened.MoveNext())
                yield return opened.Current;
            _report.Check("hands resumed runs", Opening(door), $"after the client resumed, Force Button left Forced Door {door.Status}");

            // Step 5: the door given back to the owner, shared with nobody, while the construct is the client's.
            ((MyCubeBlock)door).ChangeBlockOwnerRequest(0, MyOwnershipShareModeEnum.None);
            yield return 2 * PluginUpdates.PerSecond;
            ((MyCubeBlock)door).ChangeBlockOwnerRequest(owner, MyOwnershipShareModeEnum.None);
            door.CloseDoor();
            yield return 3 * PluginUpdates.PerSecond;
            Signal("hands-6");

            wait = Waiting("hands-7");
            while (wait.MoveNext())
                yield return wait.Current;
            var editor = InGameEditor.Of(block);
            _report.Check("hands run stops at the door", editor != null && editor.Stopped(), $"the run by hand of the client's construct: {editor?.LastRun() ?? "no editor on the server"}, Forced Door {door.Status}");
            Signal("hands-done");
        }

        /// <summary>Closes <paramref name="door" />, then presses <paramref name="button" /> and gives it three seconds.</summary>
        static IEnumerator<int> Pressed(IMyDoor door, MyButtonPanel button)
        {
            door.CloseDoor();
            yield return 4 * PluginUpdates.PerSecond;
            button.ActivateButton(0);
            yield return 3 * PluginUpdates.PerSecond;
        }

        /// <summary>Whether <paramref name="door" /> is opening or open.</summary>
        static bool Opening(IMyDoor door)
        {
            return door.Status == Sandbox.ModAPI.Ingame.DoorStatus.Opening || door.Status == Sandbox.ModAPI.Ingame.DoorStatus.Open;
        }

        /// <summary>The identity called <paramref name="name" />, or 0 where none is.</summary>
        static long Identity(string name)
        {
            var identities = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(identities, identity => identity.DisplayName == name);
            return identities.Select(identity => identity.IdentityId).FirstOrDefault();
        }

        /// <summary>Writes the file <paramref name="name" /> for the client to read.</summary>
        void Signal(string name)
        {
            File.WriteAllText(Path.Combine(_signals, name), name);
        }

        /// <summary>Waits for the client's file <paramref name="name" />, five minutes at most.</summary>
        IEnumerator<int> Waiting(string name)
        {
            for (var waited = 0; waited < 300 && !File.Exists(Path.Combine(_signals, name)); waited++)
                yield return PluginUpdates.PerSecond;
        }
    }
}
