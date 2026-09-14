namespace Reactor.Storage
{
    /// <summary>
    /// One value read out of a script.
    /// </summary>
    /// <remarks>
    /// <b>A word is a string here, not a segment, and that is measured.</b> The token a word came from is
    /// a segment, which is how a keyword is recognised without ever becoming text. But a value is stored,
    /// tens of thousands of them in a large program, and a segment is three times the size of a string
    /// reference while the model asks for every word as a string regardless. Keeping the segment made a
    /// load up to 9% slower and allocated 14% more, and saved nothing.
    /// <para>
    /// A list value does not own its items; it names where they are - see <see cref="Reading.Items" />.
    /// </para>
    /// </remarks>
    struct Value
    {
        public static readonly Value None = new Value(ValueForm.Identifier);

        public Value(ValueForm form, string word = null, long whole = 0, double real = 0,
            Keyword keyword = Keyword.None) : this()
        {
            Form = form;
            Keyword = keyword;
            Word = keyword != Keyword.None ? Keywords.Name(keyword) : word ?? "";
            Whole = whole;
            Real = real;
        }

        public ValueForm Form { get; private set; }

        /// <summary>The text of a word, however it was written. Empty for anything that is not one.</summary>
        public string Word { get; private set; }

        /// <summary>
        /// An integer, a colour's four bytes packed red-green-blue-alpha, or where a list's items are.
        /// </summary>
        public long Whole { get; private set; }

        public double Real { get; private set; }

        public Keyword Keyword { get; private set; }

        /// <summary>Whether this reads as a word, however it was written.</summary>
        public bool IsWord => Form == ValueForm.Identifier || Form == ValueForm.Keyword || Form == ValueForm.Text;

        /// <summary>Where a list's items start in <see cref="Reading.Items" />.</summary>
        public int From => (int)(Whole >> 32);

        /// <summary>How many items a list holds.</summary>
        public int Count => (int)(Whole & 0xFFFFFFFF);

        /// <summary>A list whose items are a run of <see cref="Reading.Items" />.</summary>
        public static Value Listed(int from, int count)
        {
            return new Value(ValueForm.List, null, ((long)from << 32) | (uint)count);
        }

        /// <summary>Whether this is the bare keyword given. A quoted word never is.</summary>
        public bool Says(Keyword keyword)
        {
            return Form == ValueForm.Keyword && Keyword == keyword;
        }

        /// <summary>A number, whichever way it was written, because a script may say 60 or 60.0.</summary>
        public double Number()
        {
            if (Form == ValueForm.Integer)
                return Whole;

            return Form == ValueForm.Real ? Real : 0d;
        }

        public void Colour(out byte r, out byte g, out byte b, out byte a)
        {
            r = (byte)((Whole >> 24) & 0xFF);
            g = (byte)((Whole >> 16) & 0xFF);
            b = (byte)((Whole >> 8) & 0xFF);
            a = (byte)(Whole & 0xFF);
        }
    }
}
