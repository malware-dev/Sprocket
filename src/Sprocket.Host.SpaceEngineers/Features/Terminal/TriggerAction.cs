using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Collections;
using VRage.Game;
using VRage.Game.ModAPI;
using Sprocket.Host.SpaceEngineers.Localization;
using Sprocket.Host.SpaceEngineers.World;

// Ingame cannot be imported whole - IMyTerminalBlock and friends are declared in both namespaces and
// the mod-side ones are the ones with the events on them. Same reason as IBlockListener.
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Sprocket.Host.SpaceEngineers.Features.Terminal
{
    /// <summary>
    /// The action Sprocket puts into somebody else's block so that block can start a sequence.
    /// </summary>
    /// <remarks>
    /// The mechanism the whole trigger vocabulary rests on. A button panel raises nothing at all
    /// unless its slot holds an item, and before this API no mod could fill one - so without this,
    /// "when the airlock button is pressed" needs the player to have wired the button by hand in the
    /// game's own UI first.
    /// <para>
    /// <b>Registered on every machine, and that is not an oversight.</b> A slot stores this action's
    /// id as a string and each machine resolves it against its own terminal controls to draw the
    /// slot. Register only where Sprocket runs - the server - and the server fires it correctly while
    /// every client resolves nothing: the slot draws as unassigned and the panel's emissive shows the
    /// unassigned colour. Working, and looking broken, on a mod whose interface is a button panel.
    /// </para>
    /// </remarks>
    public static class TriggerAction
    {
        /// <summary>
        /// Stored inside somebody's saved game, so it can never change.
        /// </summary>
        public const string ActionId = "Sprocket_Trigger";

        /// <summary>
        /// Hidden from every list a player can assign from.
        /// </summary>
        /// <remarks>
        /// Sprocket places this itself, and every block in the game growing a Sprocket action would be
        /// noise in a list people already find long.
        /// <para>
        /// <b>Hidden through <c>InvalidToolbarTypes</c> and never through <c>Enabled</c>.</b>
        /// <c>GetActions</c> checks <c>IsEnabled</c> before it checks the toolbar type, so a false
        /// <c>Enabled</c> filters the firing path as well as the display one and the slot would
        /// neither draw nor fire. Writing is gated by <c>AllowedInToolbarType</c>, which never
        /// consults this, so a fully hidden action can still be placed.
        /// </para>
        /// </remarks>
        /// <remarks>
        /// <b>Assigned, never appended to.</b> The property starts null and the game reads it only
        /// if it is not - so <c>Add</c> on it is a null reference at registration, which takes the
        /// session down before the world loads. Documented in THDigi's terminal control example.
        /// </remarks>
        static List<MyToolbarType> EveryList()
        {
            // ButtonPanel is deliberately absent. Hiding it from that type also stops an assigned
            // slot resolving for display there, so the button never lights up - working, and looking
            // broken, on the one toolbar type Sprocket actually writes to.
            return new List<MyToolbarType>
            {
                MyToolbarType.Character, MyToolbarType.Ship,
                MyToolbarType.Seat, MyToolbarType.BuildCockpit, MyToolbarType.Spectator
            };
        }

        static bool _registered;

        /// <summary>What a slot points at: this block, and this trigger of it.</summary>
        public static MyToolbarItemInfo Slot(long sprocketBlockId)
        {
            return MyToolbarItemInfo.BlockAction(sprocketBlockId, ActionId);
        }

        public static bool IsOurs(MyToolbarItemInfo info)
        {
            return info.ActionId == ActionId;
        }

        /// <summary>The argument that says which sequence of which system, ready to write into a slot.</summary>
        /// <remarks>
        /// <b>One parameter, and its contents are opaque.</b> A parameter list's arity and types are
        /// frozen by every slot in every saved world and a slot is never rewritten, so the key can
        /// only grow inside the string.
        /// </remarks>
        public static List<TerminalActionParameter> Argument(string key)
        {
            var arguments = new List<TerminalActionParameter>();
            arguments.Add(TerminalActionParameter.Get(key));
            return arguments;
        }

        /// <summary>Makes the action once, in <paramref name="texts" />, which the game keeps for the session.</summary>
        public static void Register(Texts texts)
        {
            if (_registered)
                return;

            _registered = true;

            IMyTerminalAction action = MyAPIGateway.TerminalControls.CreateAction<IMyProgrammableBlock>(ActionId);
            action.Name = new StringBuilder(texts.TerminalAction);

            // Never rendered for a slot Sprocket labelled, and the fallback when it has not been.
            var slot = texts.TerminalOpen;
            action.Writer = delegate(IMyTerminalBlock _, StringBuilder text) { text.Append(slot); };

            // The value fixes the parameter's type, and is what the action receives if it is ever run
            // with none. A string, because what a slot carries is a key naming a sequence of a system
            // rather than an index into anything - and an empty one resolves to nothing, which is the
            // right answer to a slot nobody has configured.
            action.ParameterDefinitions.Add(TerminalActionParameter.Get(string.Empty));
            action.ActionWithParameters = Run;

            action.InvalidToolbarTypes = EveryList();

            MyAPIGateway.TerminalControls.AddAction<IMyProgrammableBlock>(action);
        }

        /// <summary>
        /// A block somewhere fired one of Sprocket's triggers.
        /// </summary>
        /// <remarks>
        /// Runs on the server, for free: the press is a server event and <c>Apply</c> never syncs, so
        /// this lands on the machine Sprocket already lives on with nothing forwarded.
        /// <para>
        /// Which unit needs no encoding. The slot stores the entity id of the block the action runs
        /// on, so a slot left behind by a replaced Sprocket block resolves to nothing and does not
        /// fire.
        /// </para>
        /// </remarks>
        static void Run(IMyTerminalBlock block, ListReader<TerminalActionParameter> parameters)
        {
            if (block == null || parameters.Count == 0)
                return;

            // The value is boxed, and its type is whatever was written rather than what was
            // declared - so a slot somebody edited by hand is a wrong type rather than a wrong key,
            // and silence is the right answer to it.
            var key = parameters[0].Value as string;
            if (string.IsNullOrEmpty(key))
                return;

            SprocketUnit unit = SprocketSession.UnitOf(block);
            if (unit != null)
                unit.Fire(key);
        }
    }
}
