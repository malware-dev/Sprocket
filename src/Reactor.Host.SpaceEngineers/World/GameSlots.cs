using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Collections;
using VRage.Game.ModAPI;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;

namespace Reactor.Host.SpaceEngineers.World
{
    /// <summary>
    /// The game's own toolbars.
    /// </summary>
    /// <remarks>
    /// <b>A block does not implement <see cref="IMyToolbarProvider" />, it carries one.</b> So
    /// <c>block as IMyToolbarProvider</c> compiles, returns null on every block in the game, and takes
    /// every toolbar read and write quietly with it - nothing throws, buttons simply never wire up.
    /// Reactor had that cast written out in three places and got it wrong in all three when the API
    /// moved the provider onto the component container.
    /// </remarks>
    public sealed class GameSlots : ISlots
    {
        public IMyToolbar Of(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            IMyToolbarProvider provider;
            if (!block.Components.TryGet(out provider) || provider.ToolbarCount == 0)
                return null;

            return provider.GetToolbar(0);
        }

        public void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into)
        {
            if (toolbar != null)
                toolbar.GetParameters(slot, into);
        }

        public bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters)
        {
            return toolbar != null && toolbar.BeginChanges().Set(slot, item, parameters).Apply();
        }

        public bool Exists(long entityId)
        {
            return MyAPIGateway.Entities != null && MyAPIGateway.Entities.EntityExists(entityId);
        }

        public bool Clear(IMyToolbar toolbar, int slot)
        {
            return toolbar != null && toolbar.BeginChanges().Clear(slot).Apply();
        }

        /// <summary>
        /// Whether a block has an action of the game's own by that name.
        /// </summary>
        /// <remarks>
        /// The block is asked rather than a list being kept, because which actions a block offers
        /// depends on its subtype and on what the game has done to it since - and a name that is
        /// wrong here would write a slot that looks wired and does nothing.
        /// </remarks>
        public bool Offers(IMyTerminalBlock block, string actionId)
        {
            return block != null && block.GetActionWithName(actionId) != null;
        }

        /// <summary>
        /// Every action the block declares, as the terminal would list them.
        /// </summary>
        /// <remarks>
        /// The list is the game's to fill and is handed back whole. A block with no actions at all
        /// is an ordinary answer - a conveyor tube has none - and so is a name the game leaves
        /// empty, which is why the id goes through as well and is what everything else holds.
        /// </remarks>
        public void Offered(IMyTerminalBlock block, List<Offer> into)
        {
            if (block == null)
                return;

            _actions.Clear();
            block.GetActions(_actions);

            for (var i = 0; i < _actions.Count; i++)
            {
                ITerminalAction action = _actions[i];
                into.Add(new Offer(action.Id, action.Name == null ? "" : action.Name.ToString(),
                    Wants(action)));
            }
        }

        readonly List<ITerminalAction> _actions = new List<ITerminalAction>();

        /// <summary>
        /// How many values an action has to be given.
        /// </summary>
        /// <remarks>
        /// <b>Not on the interface a block hands out.</b> The same object implements the one mods are
        /// given for declaring actions of their own, and that is where the parameter list is, so it
        /// is reached by asking for the other face of it.
        /// </remarks>
        static int Wants(ITerminalAction action)
        {
            var declared = action as IMyTerminalAction;
            if (declared == null)
                return 0;

            List<TerminalActionParameter> wanted = declared.ParameterDefinitions;
            return wanted == null ? 0 : wanted.Count;
        }

        public bool Perform(IMyTerminalBlock block, string actionId, IList<Literal> values)
        {
            if (block == null)
                return false;

            ITerminalAction action = block.GetActionWithName(actionId);
            if (action == null)
                return false;

            if (values == null || values.Count == 0)
            {
                action.Apply(block);
                return true;
            }

            _given.Clear();
            Fill(action, values, _given);
            action.Apply(block, new ListReader<TerminalActionParameter>(_given));
            return true;
        }

        readonly List<TerminalActionParameter> _given = new List<TerminalActionParameter>();

        /// <summary>
        /// Hands the action as many values as it asked for, as what somebody wrote.
        /// </summary>
        /// <remarks>
        /// As many as it declared, however many were written: a step left short hands on the
        /// declared default for the rest, which is what the game does when a player fills in part
        /// of a toolbar action's dialog.
        /// </remarks>
        static void Fill(ITerminalAction action, IList<Literal> values,
            List<TerminalActionParameter> into)
        {
            var declared = action as IMyTerminalAction;
            List<TerminalActionParameter> wanted =
                declared == null ? null : declared.ParameterDefinitions;

            var count = wanted == null ? values.Count : wanted.Count;
            for (var i = 0; i < count; i++)
            {
                if (i < values.Count)
                {
                    into.Add(Given(values[i]));
                    continue;
                }

                into.Add(wanted == null ? TerminalActionParameter.Empty : wanted[i]);
            }
        }

        /// <summary>
        /// One value, as the kind it was written as.
        /// </summary>
        /// <remarks>
        /// <b>Not converted to the type the declaration carries.</b> A declaration's default says
        /// how many values an action takes and nothing trustworthy about what they are: the game's
        /// own colour setter declares three <em>strings</em> and then refuses anything whose type is
        /// not an integer. Converted to what was declared, every value it was given was thrown away
        /// and the light never changed colour.
        /// <para>
        /// So what was written goes across as what it is, and the action decides. Somebody who
        /// wrote a number gets a number; somebody who wrote text in quotes gets text.
        /// </para>
        /// <para>
        /// A whole number goes as a 32-bit one where it fits, because that is what the game's own
        /// handlers cast to - and a cast of a boxed 64-bit value to <c>int</c> throws, however
        /// willing they say they are to accept one.
        /// </para>
        /// </remarks>
        internal static TerminalActionParameter Given(Literal given)
        {
            if (given.Kind == ValueKind.Flag)
                return TerminalActionParameter.Get(given.Flag);

            if (given.Kind == ValueKind.Real)
                return TerminalActionParameter.Get((float)given.Real);

            if (given.Kind == ValueKind.Integer)
            {
                return given.Whole >= int.MinValue && given.Whole <= int.MaxValue
                    ? TerminalActionParameter.Get((int)given.Whole)
                    : TerminalActionParameter.Get(given.Whole);
            }

            // Text, and a colour - which the game has no value of its own for, so it goes as the
            // way it is written and whatever reads it parses it.
            return TerminalActionParameter.Get(given.Said);
        }
    }
}
