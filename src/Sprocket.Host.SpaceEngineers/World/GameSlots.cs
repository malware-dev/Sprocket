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

namespace Sprocket.Host.SpaceEngineers.World
{
    /// <summary>
    /// The game's own toolbars.
    /// </summary>
    /// <remarks>
    /// <b>A block does not implement <see cref="IMyToolbarProvider" />, it carries one.</b> So
    /// <c>block as IMyToolbarProvider</c> compiles, returns null on every block in the game, and takes
    /// every toolbar read and write quietly with it - nothing throws, buttons simply never wire up.
    /// Sprocket had that cast written out in three places and got it wrong in all three when the API
    /// moved the provider onto the component container.
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
        /// The list is the game's to fill. A block with no actions at all is an ordinary answer - a
        /// conveyor tube has none - and so is a name the game leaves empty, which is why the id goes
        /// through as well and is what everything else holds.
        /// <para>
        /// <b>What the list holds is not what the terminal shows.</b> The game registers an action on
        /// a type and then asks each block whether it is one the action is for: an advanced rotor and
        /// a hinge are one type, and each declares the other's head. <c>IsEnabled</c> is the question
        /// the terminal asks, and asking it here is what keeps Sprocket offering what the game does.
        /// </para>
        /// <para>
        /// Asked of the block a kind is built from, so an answer that turns on what the block is
        /// doing rather than on what it is would be fixed for the whole type at that moment.
        /// </para>
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
        /// <remarks>
        /// The other half of <see cref="Offered" />, and the half that describes itself. A block
        /// declares a handful of actions and carries far more values than that, and each value says
        /// what it is called and what kind of thing it holds without anybody writing it down.
        /// </remarks>
        public void Settable(IMyTerminalBlock block, List<Offer> into)
        {
            if (block == null)
                return;

            _properties.Clear();
            block.GetProperties(_properties);

            for (var i = 0; i < _properties.Count; i++)
            {
                ITerminalProperty property = _properties[i];

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
        /// <b>The property and the control are the same object again.</b> A value chosen from a list
        /// is a combo box on the terminal, and a combo box reports itself to a mod as a whole number
        /// - so the number is all the value surface has, and the words are on the other face of it.
        /// <para>
        /// Each one arrives as the number the block is told and an interned id for the words, which
        /// is the same shape a property's own title arrives in and is turned into a sentence the same
        /// way. Both are kept, because they are not the same thing: the words are what somebody
        /// reads and the number is what the program holds.
        /// </para>
        /// <para>
        /// The list handed to the block is this one, cleared and refilled, because this runs once per
        /// value per block kind while a world is being read. What comes out of it is copied into an
        /// array the offer keeps, since the block is free to fill this one again.
        /// </para>
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
                // Asked of a combo box that fills its list per block, this throws: the getter hands a
                // mod a wrapper around the list-filler whether or not there is one, so there is nothing
                // to test first.
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
        /// <remarks>
        /// The block's own limits, which is what the game's own dialog for setting one of these
        /// puts a slider between. Nobody writes them down and they are right for each block: a
        /// spotlight reaches further than a corner light and says so itself.
        /// <para>
        /// A property with no limits answers with the whole range its type can hold. Those are
        /// passed over, because "between -340282350000000000000000000000000000000 and" is not a
        /// thing to put beside a box.
        /// </para>
        /// </remarks>
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
        /// <remarks>
        /// Every functional block carries the whole screen apparatus whether or not it has a screen -
        /// what is shown, the font, its size and colour, the padding, the backgrounds - and a light
        /// would otherwise offer a dozen values of which a handful are the light. So a screen's values
        /// are the block's where it has a screen at all, which is a fact about the block rather than
        /// about the moment: its script's colours count while it is showing text, and its font while it
        /// is showing nothing.
        /// <para>
        /// The game's own actions on these values already follow the same test, so a block with no
        /// screen has neither.
        /// </para>
        /// </remarks>
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
        /// <remarks>
        /// The five type names the terminal reports are the five kinds the language has, which is
        /// not a coincidence worth relying on: anything else a future block reports is passed over
        /// rather than guessed at, because a value nobody can write is worse than one nobody is
        /// offered.
        /// </remarks>
        static bool Holds(ITerminalProperty property, out ValueKind kind)
        {
            kind = ValueKind.Text;

            string type = property == null ? null : property.TypeName;
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
        /// <b>The property and the control are the same object.</b> What a block hands out as a
        /// property is the control that draws it, so the title is reached by asking for the other
        /// face of it - the same trick <see cref="Wants" /> uses to reach an action's parameters.
        /// <para>
        /// The title arrives as an interned id rather than as words, and reading it as a string
        /// gives the key it was computed from. <c>MyTexts</c> is what turns that into the sentence
        /// the player already read on the terminal screen.
        /// </para>
        /// <para>
        /// Falls back to the id, which is not presentable - the interior light carries
        /// <c>Blink Interval</c> with a space and <c>Blink Lenght</c> with the game's own
        /// misspelling - but a name nobody can read beats a blank.
        /// </para>
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

        /// <summary>What type each of an action's values is declared as, or null where it takes none.</summary>
        static IList<Primitive> Declared(ITerminalAction action)
        {
            var declared = action as IMyTerminalAction;
            List<TerminalActionParameter> wanted = declared == null ? null : declared.ParameterDefinitions;
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
        /// <remarks>
        /// Written as the kind the property says it holds, which a property declares honestly where
        /// an action's declaration often says text and means a number.
        /// <para>
        /// A value written as the wrong kind is refused rather than guessed at. Turning a number
        /// into the text of that number, or text into a number, would be Sprocket deciding what
        /// somebody meant, and the answer would be silently wrong on a block nobody is watching.
        /// </para>
        /// </remarks>
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
            ITerminalProperty property = block == null ? null : block.GetProperty(propertyId);
            ValueKind kind;
            return property != null && Holds(property, out kind) ? new Property(block, property, kind) : null;
        }

        /// <inheritdoc />
        public Order Prepare(IMyTerminalBlock block, string id, Form form, IList<Literal> arguments,
            IList<Primitive> types)
        {
            if (block == null)
                return null;

            if (form == Form.Value)
                return arguments == null || arguments.Count == 0 ? null : Written(block, id, arguments[0]);

            ITerminalAction action = block.GetActionWithName(id);
            if (action == null)
                return null;

            if (arguments == null || arguments.Count == 0)
                return new Performed(block, action, null);

            var given = new List<TerminalActionParameter>();
            Fill(action, arguments, types, given);
            return new Performed(block, action, given);
        }

        /// <summary>A value made ready to be written as the type the property holds, or null where it is not that kind.</summary>
        static Order Written(IMyTerminalBlock block, string propertyId, Literal value)
        {
            ITerminalProperty property = block.GetProperty(propertyId);
            if (property == null)
                return null;

            switch (property.TypeName)
            {
                case "Boolean":
                    return value.Kind == ValueKind.Flag ? new Writes<bool>(block, property.AsBool(), value.Flag) : null;
                case "Single":
                    return value.Kind == ValueKind.Real
                        ? new Writes<float>(block, property.AsFloat(), (float)value.Real)
                        : null;
                case "Int64":
                    return value.Kind == ValueKind.Integer
                        ? new Writes<long>(block, property.As<long>(), value.Whole)
                        : null;
                case "Color":
                    return value.Kind == ValueKind.Color
                        ? new Writes<Color>(block, property.AsColor(),
                            new Color(value.Red, value.Green, value.Blue, value.Alpha))
                        : null;
                case "StringBuilder":
                    return value.Kind == ValueKind.Text
                        ? new Texts(block, property.As<StringBuilder>(), value.Text)
                        : null;
            }

            return null;
        }

        /// <summary>One of a block's own values, read through the property the game hands out for it.</summary>
        /// <remarks>
        /// The typed property is found once, when the test is compiled, and a look reads it. Text is compared where the block
        /// keeps it, so a <c>when</c> on a text looks without building a string.
        /// </remarks>
        sealed class Property : Gauge
        {
            readonly IMyTerminalBlock _block;
            readonly ValueKind _kind;
            readonly ITerminalProperty<bool> _flag;
            readonly ITerminalProperty<float> _real;
            readonly ITerminalProperty<long> _whole;
            readonly ITerminalProperty<Color> _color;
            readonly ITerminalProperty<StringBuilder> _text;

            public Property(IMyTerminalBlock block, ITerminalProperty property, ValueKind kind)
            {
                _block = block;
                _kind = kind;
                switch (kind)
                {
                    case ValueKind.Flag:
                        _flag = property.AsBool();
                        break;
                    case ValueKind.Real:
                        _real = property.AsFloat();
                        break;
                    case ValueKind.Integer:
                        _whole = property.As<long>();
                        break;
                    case ValueKind.Color:
                        _color = property.AsColor();
                        break;
                    default:
                        _text = property.As<StringBuilder>();
                        break;
                }
            }

            public override ValueKind Kind => _kind;

            public override Literal Read()
            {
                switch (_kind)
                {
                    case ValueKind.Flag:
                        return Literal.Of(_flag.GetValue(_block));
                    case ValueKind.Real:
                        return Literal.Of(_real.GetValue(_block));
                    case ValueKind.Integer:
                        return Literal.Of(_whole.GetValue(_block));
                    case ValueKind.Color:
                        var color = _color.GetValue(_block);
                        return Literal.Colour(color.R, color.G, color.B, color.A);
                }

                return Literal.Of(_text.GetValue(_block).ToString());
            }

            /// <remarks>A real is compared in the single the block keeps it as, so 0.1 written matches the 0.1 it holds.</remarks>
            public override bool Holds(Relation relation, Literal operand)
            {
                if (_kind == ValueKind.Real)
                    return Relations.Holds(Read(), relation, Literal.Of((float)operand.Number));

                if (_kind == ValueKind.Text && (relation == Relation.Is || relation == Relation.IsNot))
                    return Same(_text.GetValue(_block), operand.Text) == (relation == Relation.Is);

                return base.Holds(relation, operand);
            }

            static bool Same(StringBuilder held, string text)
            {
                if (held == null)
                    return text.Length == 0;

                if (held.Length != text.Length)
                    return false;

                for (var i = 0; i < text.Length; i++)
                {
                    if (held[i] != text[i])
                        return false;
                }

                return true;
            }
        }

        /// <summary>One of the block's actions, with its values already the types it takes.</summary>
        sealed class Performed : Order
        {
            readonly IMyTerminalBlock _block;
            readonly ITerminalAction _action;
            readonly List<TerminalActionParameter> _given;

            public Performed(IMyTerminalBlock block, ITerminalAction action, List<TerminalActionParameter> given)
            {
                _block = block;
                _action = action;
                _given = given;
            }

            /// <inheritdoc />
            public override void Give()
            {
                if (_given == null)
                    _action.Apply(_block);
                else
                    _action.Apply(_block, new ListReader<TerminalActionParameter>(_given));
            }
        }

        /// <summary>One of the block's values, already the type it holds.</summary>
        sealed class Writes<T> : Order
        {
            readonly IMyTerminalBlock _block;
            readonly ITerminalProperty<T> _property;
            readonly T _value;

            public Writes(IMyTerminalBlock block, ITerminalProperty<T> property, T value)
            {
                _block = block;
                _property = property;
                _value = value;
            }

            /// <inheritdoc />
            public override void Give()
            {
                _property.SetValue(_block, _value);
            }
        }

        /// <summary>
        /// One of the block's text values.
        /// </summary>
        /// <remarks>
        /// A fresh builder every time it is given, because the block is handed the builder itself and
        /// what it does with it afterwards is the block's business.
        /// </remarks>
        sealed class Texts : Order
        {
            readonly IMyTerminalBlock _block;
            readonly ITerminalProperty<StringBuilder> _property;
            readonly string _text;

            public Texts(IMyTerminalBlock block, ITerminalProperty<StringBuilder> property, string text)
            {
                _block = block;
                _property = property;
                _text = text;
            }

            /// <inheritdoc />
            public override void Give()
            {
                _property.SetValue(_block, new StringBuilder(_text));
            }
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
        /// <remarks>
        /// As many as it declared, however many were written: a step left short hands on the
        /// declared default for the rest, which is what the game does when a player fills in part
        /// of a toolbar action's dialog.
        /// </remarks>
        static void Fill(ITerminalAction action, IList<Literal> arguments, IList<Primitive> types,
            List<TerminalActionParameter> into)
        {
            var declared = action as IMyTerminalAction;
            List<TerminalActionParameter> wanted =
                declared == null ? null : declared.ParameterDefinitions;

            var count = wanted == null ? arguments.Count : wanted.Count;
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
        /// <remarks>
        /// The action decides what to make of it.
        /// <para>
        /// A whole number goes as a 32-bit one where it fits, because that is what the game's own
        /// handlers cast to - and a cast of a boxed 64-bit value to <c>int</c> throws, however
        /// willing they say they are to accept one.
        /// </para>
        /// </remarks>
        public static TerminalActionParameter Given(Literal given)
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
