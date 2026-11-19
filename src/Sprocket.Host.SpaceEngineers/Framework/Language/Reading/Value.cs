
namespace Sprocket.Host.SpaceEngineers.Framework.Language.Reading
{
    /// <summary>
    /// One value read out of a script.
    /// </summary>
    /// <remarks>
    /// A word is a string, measured: keeping the token's segment made a load up to 9% slower and allocate 14% more.
    /// </remarks>
    public struct Value
    {
        /// <summary>No value: what is answered where a position holds nothing.</summary>
        public static readonly Value None = new Value(ValueForm.Identifier);

        /// <summary>A value written as <paramref name="form" />, carrying what that form carries.</summary>
        public Value(ValueForm form, string word = null, long whole = 0, double real = 0,
            Keyword keyword = Keyword.None) : this()
        {
            Form = form;
            Keyword = keyword;
            Word = keyword != Keyword.None ? Keywords.Name(keyword) : word ?? "";
            Whole = whole;
            Real = real;
        }

        /// <summary>What it is written as.</summary>
        public ValueForm Form { get; private set; }

        /// <summary>The text of a word, however it was written, or empty for anything that is not one.</summary>
        public string Word { get; private set; }

        /// <summary>
        /// An integer, a color's four bytes packed red-green-blue-alpha, or where a list's items are.
        /// </summary>
        public long Whole { get; private set; }

        /// <summary>A real number, for one written with a point or an exponent.</summary>
        public double Real { get; private set; }

        /// <summary>Which of the format's words it is: for a keyword, and for a bare name spelled like a contextual keyword.</summary>
        /// <remarks>A quoted name has none.</remarks>
        public Keyword Keyword { get; private set; }

        /// <summary>Whether this is a word, bare or quoted, rather than text.</summary>
        public bool IsWord => Form == ValueForm.Identifier || Form == ValueForm.Keyword;

        /// <summary>Where a list's items, or a name's parts, start in the reading.</summary>
        public int From => (int)(Whole >> 32);

        /// <summary>How many items a list holds, or parts a name has.</summary>
        public int Count => (int)(Whole & 0xFFFFFFFF);

        /// <summary>A list whose items are a run of <see cref="ScriptValues.Items" />.</summary>
        public static Value Listed(int from, int count)
        {
            return new Value(ValueForm.List, null, Run(from, count));
        }

        /// <summary>A vector, its three numbers the run starting at <paramref name="from" />.</summary>
        public static Value Vectored(int from)
        {
            return new Value(ValueForm.Vector, null, Run(from, 3));
        }

        /// <summary>A name whose parts are a run of <see cref="ScriptValues.Parts" />.</summary>
        public static Value Qualified(int from, int count)
        {
            return new Value(ValueForm.Qualified, null, Run(from, count));
        }

        /// <summary>A member and its blocks, whose name's parts, arguments and names are a run of <see cref="ScriptValues.Parts" />.</summary>
        public static Value Referenced(int from, int count)
        {
            return new Value(ValueForm.Reference, null, Run(from, count));
        }

        /// <summary>A thing with its kind, <c>side(inside)</c>, the kind and the thing the two parts of <see cref="ScriptValues.Parts" /> from <paramref name="from" />.</summary>
        public static Value Kinded(int from)
        {
            return new Value(ValueForm.Kinded, null, Run(from, 2));
        }

        static long Run(int from, int count)
        {
            return ((long)from << 32) | (uint)count;
        }

        /// <summary>
        /// Whether this is the bare keyword given, which a quoted word never is: a reserved one, or a contextual one where the
        /// construct reading it expects it.
        /// </summary>
        public bool Says(Keyword keyword)
        {
            return keyword != Keyword.None && Keyword == keyword && (Form == ValueForm.Keyword || Form == ValueForm.Identifier);
        }

        /// <summary>A number, whether it was written 60 or 60.0.</summary>
        public double Number()
        {
            if (Form == ValueForm.Integer)
                return Whole;

            return Form == ValueForm.Real ? Real : 0d;
        }

        /// <summary>A color's red, green, blue and alpha.</summary>
        public void Color(out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)((Whole >> 24) & 0xFF);
            g = (byte)((Whole >> 16) & 0xFF);
            b = (byte)((Whole >> 8) & 0xFF);
            a = (byte)(Whole & 0xFF);
        }
    }
}
