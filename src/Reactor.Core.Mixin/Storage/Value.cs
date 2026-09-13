using System.Collections.Generic;

namespace Reactor.Storage
{
    /// <summary>
    /// One value read out of a script.
    /// </summary>
    /// <remarks>
    /// A vector holds its three numbers in <see cref="Items" /> rather than in fields of its own, so
    /// the struct stays the same size whatever the language grows.
    /// </remarks>
    struct Value
    {
        public static readonly Value None = new Value(ValueForm.Identifier, null);

        public Value(ValueForm form, string word, long whole = 0, double real = 0,
            List<Value> items = null) : this()
        {
            Form = form;
            Word = word ?? "";
            Whole = whole;
            Real = real;
            Items = items;
        }

        public ValueForm Form { get; private set; }

        /// <summary>An identifier or a string, and empty for everything else.</summary>
        public string Word { get; private set; }

        /// <summary>An integer, or a colour's four bytes packed red-green-blue-alpha.</summary>
        public long Whole { get; private set; }

        public double Real { get; private set; }

        /// <summary>A list's values, or a vector's three numbers. Null for anything else.</summary>
        public List<Value> Items { get; private set; }

        /// <summary>Whether this reads as a word, however it was written.</summary>
        public bool IsWord => Form == ValueForm.Identifier || Form == ValueForm.Text;

        /// <summary>Whether this is the bare word given, which is how a keyword is recognised.</summary>
        public bool Says(string keyword)
        {
            return Form == ValueForm.Identifier && Word == keyword;
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
