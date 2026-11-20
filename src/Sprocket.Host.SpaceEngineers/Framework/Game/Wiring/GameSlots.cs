using Sprocket.Host.SpaceEngineers.Framework.Game.Blocks;
using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Model;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Globalization;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRageMath;
using TerminalActionParameter = Sandbox.ModAPI.Ingame.TerminalActionParameter;
using IngameSurfaces = Sandbox.ModAPI.Ingame.IMyTextSurfaceProvider;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Wiring
{
    /// <summary>
    /// The game's own toolbars.
    /// </summary>
    /// <remarks>
    /// A block carries its <see cref="IMyToolbarProvider" /> as a component: <c>block as IMyToolbarProvider</c> compiles,
    /// and is null on every block in the game.
    /// </remarks>
    public sealed class GameSlots : ISlots
    {
        /// <inheritdoc />
        public IMyToolbar Of(IMyTerminalBlock block)
        {
            if (block == null)
                return null;

            IMyToolbarProvider provider;
            if (!block.Components.TryGet(out provider) || provider.ToolbarCount == 0)
                return null;

            return provider.GetToolbar(0);
        }

        /// <inheritdoc />
        public IMyToolbar Of(IMyTerminalBlock block, int index)
        {
            if (block == null || index < 0)
                return null;

            IMyToolbarProvider provider;
            if (!block.Components.TryGet(out provider) || index >= provider.ToolbarCount)
                return null;

            return provider.GetToolbar(index);
        }

        /// <inheritdoc />
        public void Parameters(IMyToolbar toolbar, int slot, List<TerminalActionParameter> into)
        {
            if (toolbar != null)
                toolbar.GetParameters(slot, into);
        }

        /// <inheritdoc />
        public bool Write(IMyToolbar toolbar, int slot, MyToolbarItemInfo item,
            List<TerminalActionParameter> parameters)
        {
            return toolbar != null && toolbar.BeginChanges().Set(slot, item, parameters).Apply();
        }

        /// <inheritdoc />
        public bool Exists(long entityId)
        {
            return MyAPIGateway.Entities != null && MyAPIGateway.Entities.EntityExists(entityId);
        }

        /// <inheritdoc />
        public bool Clear(IMyToolbar toolbar, int slot)
        {
            return toolbar != null && toolbar.BeginChanges().Clear(slot).Apply();
        }

        /// <inheritdoc />
        /// <remarks>A block that is not loaded here is said by its action alone, with no picture.</remarks>
        public string Described(MyToolbarItemInfo item, out string picture)
        {
            picture = "";
            switch (item.Kind)
            {
                case MyToolbarItemKind.BlockAction:
                {
                    var block = MyAPIGateway.Entities == null ? null : MyAPIGateway.Entities.GetEntityById(item.BlockEntityId) as IMyTerminalBlock;
                    if (block == null)
                        return item.ActionId ?? "";

                    picture = $"{block.BlockDefinition.TypeIdString}/{block.BlockDefinition.SubtypeName}";
                    var action = block.GetActionWithName(item.ActionId);
                    return $"{block.CustomName}: {(action == null ? item.ActionId : action.Name.ToString())}";
                }

                case MyToolbarItemKind.GroupAction:
                    return $"{item.GroupName}: {item.ActionId}";

                case MyToolbarItemKind.Definition:
                {
                    var definitions = Sandbox.Definitions.MyDefinitionManager.Static;
                    var definition = definitions?.GetDefinition(item.DefinitionId);
                    picture = item.DefinitionId.ToString();
                    return definition == null ? item.DefinitionId.SubtypeName : definition.DisplayNameText;
                }

                default:
                    return "";
            }
        }

        /// <summary>
        /// Whether a block has an action of the game's own by that name.
        /// </summary>
        public bool Offers(IMyTerminalBlock block, string actionId)
        {
            return block != null && block.GetActionWithName(actionId) != null;
        }

        /// <summary>
        /// Every action the block declares, as the terminal would list them.
        /// </summary>
        /// <remarks>
        /// The game registers an action on a type and the terminal asks each block <c>IsEnabled</c>: an advanced rotor and
        /// a hinge are one type, and each declares the other's head.
        /// </remarks>
        public void Offered(IMyTerminalBlock block, List<Offer> into)
        {
            if (block == null)
                return;

            _actions.Clear();
            block.GetActions(_actions);

            foreach (ITerminalAction action in _actions)
            {
                if (!action.IsEnabled(block))
                    continue;

                into.Add(new Offer(action.Id, action.Name == null ? "" : action.Name.ToString(),
                    Wants(action)).Declaring(Declared(action)));
            }
        }

        readonly List<ITerminalAction> _actions = new List<ITerminalAction>();

        /// <summary>
        /// Every value a block carries that can be told what to be.
        /// </summary>
        public void Settable(IMyTerminalBlock block, List<Offer> into)
        {
            if (block == null)
                return;

            _properties.Clear();
            block.GetProperties(_properties);

            foreach (ITerminalProperty property in _properties)
            {
                ValueKind kind;
                if (!Holds(property, out kind) || !Has(property, block))
                    continue;

                var made = new Offer(property.Id, Called(property), kind,
                    Least(block, property, kind), Most(block, property, kind));

                into.Add(Listed(property, made));
            }
        }

        readonly List<ITerminalProperty> _properties = new List<ITerminalProperty>();
        readonly List<MyTerminalControlComboBoxItem> _items = new List<MyTerminalControlComboBoxItem>();

        /// <summary>
        /// Everything a value says it may be, where it says so.
        /// </summary>
        /// <remarks>
        /// The game's combo box reports itself to a mod as an integer, with its words on the control. Runs once per value
        /// per block kind while a world is read, so the list handed to the block is reused.
        /// </remarks>
        Offer Listed(ITerminalProperty property, Offer made)
        {
            var listed = property as IMyTerminalControlCombobox;
            if (listed == null)
                return made;

            _items.Clear();
            try
            {
                listed.ComboBoxContent(_items);
            }
            catch (NullReferenceException)
            {
                // The game throws here for a combo box that fills its list per block, and offers nothing to test first.
                return made.Unread();
            }

            if (_items.Count == 0)
                return made;

            var choices = new Choice[_items.Count];
            for (var i = 0; i < _items.Count; i++)
            {
                choices[i] = new Choice(MyTexts.GetString(_items[i].Value),
                    _items[i].Key.ToString(CultureInfo.InvariantCulture));
            }

            return made.Between(choices);
        }

        /// <summary>
        /// The least a numeric value will accept, or empty where the block says nothing useful.
        /// </summary>
        /// <remarks>The game answers the whole range of a float for a property with no limits, which counts as saying nothing.</remarks>
        static string Least(IMyTerminalBlock block, ITerminalProperty property, ValueKind kind)
        {
            if (kind != ValueKind.Real)
                return "";

            float least = property.AsFloat().GetMinimum(block);
            return Bounded(least) ? Said(least) : "";
        }

        static string Most(IMyTerminalBlock block, ITerminalProperty property, ValueKind kind)
        {
            if (kind != ValueKind.Real)
                return "";

            float most = property.AsFloat().GetMaximum(block);
            return Bounded(most) ? Said(most) : "";
        }

        static bool Bounded(float edge)
        {
            return !float.IsNaN(edge) && !float.IsInfinity(edge)
                && edge > float.MinValue && edge < float.MaxValue;
        }

        static string Said(float edge)
        {
            return edge.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Whether the block has this value, which every value it declares is but a screen's own.
        /// </summary>
        /// <remarks>Every functional block in the game carries a screen's values, whether or not it has a screen.</remarks>
        static bool Has(ITerminalProperty property, IMyTerminalBlock block)
        {
            if (!Screen(property.Id))
                return true;

            var surfaces = block as IngameSurfaces;
            return surfaces != null && surfaces.SurfaceCount > 0;
        }

        /// <summary>Whether the value is one every functional block carries for a screen, from the game's own terminal data.</summary>
        static bool Screen(string id)
        {
            switch (id)
            {
                case "Content":
                case "ScriptForegroundColor":
                case "ScriptBackgroundColor":
                case "Font":
                case "FontSize":
                case "FontColor":
                case "alignment":
                case "TextPaddingSlider":
                case "BackgroundColor":
                case "ChangeIntervalSlider":
                case "PreserveAspectRatio":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// What kind of value a property holds, and whether it is one Sprocket has.
        /// </summary>
        static bool Holds(ITerminalProperty property, out ValueKind kind)
        {
            kind = ValueKind.Text;

            string type = property?.TypeName;
            if (string.IsNullOrEmpty(type))
                return false;

            if (type == "Boolean")
                kind = ValueKind.Flag;
            else if (type == "Int64")
                kind = ValueKind.Integer;
            else if (type == "Single")
                kind = ValueKind.Real;
            else if (type == "Color")
                kind = ValueKind.Color;
            else if (type != "StringBuilder")
                return false;

            return true;
        }

        /// <summary>
        /// What the terminal calls a value, in the player's own language.
        /// </summary>
        /// <remarks>
        /// In the game a property is the control that draws it, whose title is an interned id <c>MyTexts</c> turns into words.
        /// Falls back to the id.
        /// </remarks>
        static string Called(ITerminalProperty property)
        {
            var titled = property as IMyTerminalControlTitleTooltip;
            if (titled == null)
                return property.Id;

            string said = MyTexts.GetString(titled.Title);
            return string.IsNullOrEmpty(said) ? property.Id : said;
        }

        /// <summary>
        /// How many values an action has to be given.
        /// </summary>
        /// <remarks>The game keeps the parameter list on <c>IMyTerminalAction</c>, which the same object implements.</remarks>
        static int Wants(ITerminalAction action)
        {
            var declared = action as IMyTerminalAction;
            if (declared == null)
                return 0;

            List<TerminalActionParameter> wanted = declared.ParameterDefinitions;
            return wanted?.Count ?? 0;
        }

        /// <summary>What type each of an action's values is declared as, or null where it takes none.</summary>
        static IList<Primitive> Declared(ITerminalAction action)
        {
            var declared = action as IMyTerminalAction;
            List<TerminalActionParameter> wanted = declared?.ParameterDefinitions;
            if (wanted == null || wanted.Count == 0)
                return null;

            var types = new Primitive[wanted.Count];
            for (var i = 0; i < wanted.Count; i++)
                types[i] = Type(wanted[i].Value);

            return types;
        }

        /// <summary>The type an action's parameter is declared as, or none for one that is not a primitive.</summary>
        /// <remarks>Read off the default it was declared with, since the parameter's own type code is off the mod whitelist.</remarks>
        static Primitive Type(object declared)
        {
            if (declared is bool)
                return Primitive.Boolean;
            if (declared is byte)
                return Primitive.Byte;
            if (declared is sbyte)
                return Primitive.SByte;
            if (declared is short)
                return Primitive.Int16;
            if (declared is ushort)
                return Primitive.UInt16;
            if (declared is int)
                return Primitive.Int32;
            if (declared is uint)
                return Primitive.UInt32;
            if (declared is long)
                return Primitive.Int64;
            if (declared is ulong)
                return Primitive.UInt64;
            if (declared is float)
                return Primitive.Single;
            if (declared is double)
                return Primitive.Double;
            if (declared is char)
                return Primitive.Char;

            return declared is string ? Primitive.String : Primitive.None;
        }

        /// <summary>
        /// Tells one of a block's own values what to be.
        /// </summary>
        /// <remarks>A value of another kind than the property holds is refused, and answers false.</remarks>
        public bool Set(IMyTerminalBlock block, string propertyId, Literal value)
        {
            var order = block == null ? null : Written(block, propertyId, value);
            if (order == null)
                return false;

            order.Give();
            return true;
        }

        /// <inheritdoc />
        public Gauge Gauge(IMyTerminalBlock block, string propertyId)
        {
            ITerminalProperty property = block?.GetProperty(propertyId);
            ValueKind kind;
            return property != null && Holds(property, out kind) ? new GameProperty(block, property, kind) : null;
        }

        /// <inheritdoc />
        public BlockOrder Prepare(IMyTerminalBlock block, string id, ReachKind form, IList<Literal> arguments,
            IList<Primitive> types)
        {
            if (block == null)
                return null;

            if (form == ReachKind.Value)
                return arguments == null || arguments.Count == 0 ? null : Written(block, id, arguments[0]);

            ITerminalAction action = block.GetActionWithName(id);
            if (action == null)
                return null;

            if (arguments == null || arguments.Count == 0)
                return new GameActionOrder(block, action, null);

            var given = new List<TerminalActionParameter>();
            Fill(action, arguments, types, given);
            return new GameActionOrder(block, action, given);
        }

        /// <summary>A value made ready to be written as the type the property holds, or null where it is not that kind.</summary>
        static BlockOrder Written(IMyTerminalBlock block, string propertyId, Literal value)
        {
            ITerminalProperty property = block.GetProperty(propertyId);
            if (property == null)
                return null;

            switch (property.TypeName)
            {
                case "Boolean":
                    if (value.Kind != ValueKind.Flag)
                        return null;

                    return Flips(block, propertyId)
                        ? (BlockOrder)new GameSwitchOrder(block, property.AsBool(), value.Flag)
                        : new GameValueOrder<bool>(block, property.AsBool(), value.Flag);
                case "Single":
                    return value.Kind == ValueKind.Real
                        ? new GameValueOrder<float>(block, property.AsFloat(), (float)value.Real)
                        : null;
                case "Int64":
                    return value.Kind == ValueKind.Integer
                        ? new GameValueOrder<long>(block, property.As<long>(), value.Integer)
                        : null;
                case "Color":
                    return value.Kind == ValueKind.Color
                        ? new GameValueOrder<Color>(block, property.AsColor(),
                            new Color(value.Red, value.Green, value.Blue, value.Alpha))
                        : null;
                case "StringBuilder":
                    return value.Kind == ValueKind.Text
                        ? new GameTextOrder(block, property.As<StringBuilder>(), value.Text)
                        : null;
            }

            return null;
        }

        /// <summary>Whether the block's setter for the switch flips it whatever it is told, so it may be set only where it differs.</summary>
        /// <remarks>
        /// From the game's own terminal controls: a ship controller's handbrake and parking setters ask the grid to toggle, and
        /// on the server the grid toggles at once.
        /// </remarks>
        static bool Flips(IMyTerminalBlock block, string propertyId)
        {
            return block is IMyShipController && (propertyId == "HandBrake" || propertyId == "Park");
        }

        /// <inheritdoc />
        public bool Perform(IMyTerminalBlock block, string actionId, IList<Literal> arguments)
        {
            if (block == null)
                return false;

            ITerminalAction action = block.GetActionWithName(actionId);
            if (action == null)
                return false;

            if (arguments == null || arguments.Count == 0)
            {
                action.Apply(block);
                return true;
            }

            _given.Clear();
            Fill(action, arguments, null, _given);
            action.Apply(block, new ListReader<TerminalActionParameter>(_given));
            return true;
        }

        readonly List<TerminalActionParameter> _given = new List<TerminalActionParameter>();

        /// <summary>
        /// Hands the action as many values as it asked for, each as the type it takes where that is
        /// known.
        /// </summary>
        /// <remarks>A step left short hands on the declared default for the rest, as the game does for a partly filled toolbar dialog.</remarks>
        static void Fill(ITerminalAction action, IList<Literal> arguments, IList<Primitive> types,
            List<TerminalActionParameter> into)
        {
            var declared = action as IMyTerminalAction;
            List<TerminalActionParameter> wanted =
                declared?.ParameterDefinitions;

            var count = wanted?.Count ?? arguments.Count;
            for (var i = 0; i < count; i++)
            {
                if (i < arguments.Count)
                {
                    var type = types == null || i >= types.Count ? Primitive.None : types[i];
                    into.Add(type == Primitive.None
                        ? Given(arguments[i])
                        : TerminalActionParameter.Get(arguments[i].Native(type)));
                    continue;
                }

                into.Add(wanted == null ? TerminalActionParameter.Empty : wanted[i]);
            }
        }

        /// <summary>
        /// One value whose type nothing says, as the kind it was written as.
        /// </summary>
        /// <remarks>An integer goes as 32 bits where it fits: the game's own handlers cast to <c>int</c>, which throws on a boxed 64-bit value.</remarks>
        public static TerminalActionParameter Given(Literal given)
        {
            if (given.Kind == ValueKind.Flag)
                return TerminalActionParameter.Get(given.Flag);

            if (given.Kind == ValueKind.Real)
                return TerminalActionParameter.Get((float)given.Real);

            if (given.Kind == ValueKind.Integer)
            {
                return given.Integer >= int.MinValue && given.Integer <= int.MaxValue
                    ? TerminalActionParameter.Get((int)given.Integer)
                    : TerminalActionParameter.Get(given.Integer);
            }

            // The game has no parameter value for a color, so it goes as written.
            return TerminalActionParameter.Get(given.Said);
        }
    }
}
