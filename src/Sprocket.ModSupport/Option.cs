using System;
using VRage.Utils;
using VRageMath;

namespace Sprocket.ModSupport
{
    /// <summary>Makes the options a mod's actions and functions take, each named as players read it.</summary>
    public static partial class Option
    {
        static readonly ISprocketWiring Wiring = new Key();

        /// <summary>A real number called <paramref name="name" />.</summary>
        public static SprocketOption<double> Number(string name) => Number(SprocketText.Of(name));

        /// <summary>A real number called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<double> Number(MyStringId name) => Number(SprocketText.Of(name));

        /// <summary>A whole number called <paramref name="name" />.</summary>
        public static SprocketOption<long> Whole(string name) => Whole(SprocketText.Of(name));

        /// <summary>A whole number called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<long> Whole(MyStringId name) => Whole(SprocketText.Of(name));

        /// <summary>An on or off called <paramref name="name" />.</summary>
        public static SprocketOption<bool> Flag(string name) => Flag(SprocketText.Of(name));

        /// <summary>An on or off called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<bool> Flag(MyStringId name) => Flag(SprocketText.Of(name));

        /// <summary>A text called <paramref name="name" />.</summary>
        public static SprocketOption<string> Text(string name) => Text(SprocketText.Of(name));

        /// <summary>A text called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<string> Text(MyStringId name) => Text(SprocketText.Of(name));

        /// <summary>A color called <paramref name="name" />.</summary>
        public static SprocketOption<Color> Color(string name) => Color(SprocketText.Of(name));

        /// <summary>A color called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<Color> Color(MyStringId name) => Color(SprocketText.Of(name));

        /// <summary>A length of time called <paramref name="name" />, written <c>2s</c> in programs.</summary>
        public static SprocketOption<TimeSpan> Duration(string name) => Duration(SprocketText.Of(name));

        /// <summary>A length of time called by the text <paramref name="name" /> names in the mod's own texts.</summary>
        public static SprocketOption<TimeSpan> Duration(MyStringId name) => Duration(SprocketText.Of(name));

        /// <summary>One of the words of <paramref name="type" />, called <paramref name="name" />.</summary>
        public static SprocketOption<string> Choice(string name, SprocketType type) =>
            new SprocketOption<string>(Wiring, SprocketWire.TypeChoice, SprocketText.Of(name), given => given as string ?? "").Choosing(type);

        /// <summary>One of the words of the enum type <paramref name="type" />, called <paramref name="name" />, given to the mod as the enum.</summary>
        public static SprocketOption<T> Choice<T>(string name, SprocketEnumType<T> type) where T : struct =>
            new SprocketOption<T>(Wiring, SprocketWire.TypeChoice, SprocketText.Of(name), given => type.Value(given as string)).Choosing(type);

        static SprocketOption<double> Number(Func<string> name) =>
            new SprocketOption<double>(Wiring, SprocketWire.TypeNumber, name, given => given is double ? (double)given : 0.0);

        static SprocketOption<long> Whole(Func<string> name) =>
            new SprocketOption<long>(Wiring, SprocketWire.TypeWhole, name, given => given is long ? (long)given : 0L);

        static SprocketOption<bool> Flag(Func<string> name) =>
            new SprocketOption<bool>(Wiring, SprocketWire.TypeFlag, name, given => given is bool && (bool)given);

        static SprocketOption<string> Text(Func<string> name) =>
            new SprocketOption<string>(Wiring, SprocketWire.TypeText, name, given => given as string ?? "");

        static SprocketOption<Color> Color(Func<string> name) =>
            new SprocketOption<Color>(Wiring, SprocketWire.TypeColor, name, given => given is Color ? (Color)given : VRageMath.Color.White);

        static SprocketOption<TimeSpan> Duration(Func<string> name) =>
            new SprocketOption<TimeSpan>(Wiring, SprocketWire.TypeDuration, name, given => given is TimeSpan ? (TimeSpan)given : TimeSpan.Zero);
    }
}
