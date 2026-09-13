
namespace VRage.Utils
{
    /// <summary>
    /// An interned string, as the game's is. The simulator needs the text back out of it to find a
    /// material, which the real one also offers.
    /// </summary>
    public struct MyStringId
    {
        static readonly Dictionary<string, int> Ids = new Dictionary<string, int>();
        static readonly List<string> Strings = new List<string>();

        readonly int _id;

        MyStringId(int id)
        {
            _id = id;
        }

        public string String => _id >= 0 && _id < Strings.Count ? Strings[_id] : string.Empty;

        public static MyStringId GetOrCompute(string value)
        {
            if (value == null)
                value = string.Empty;

            int id;
            lock (Ids)
            {
                if (!Ids.TryGetValue(value, out id))
                {
                    id = Strings.Count;
                    Strings.Add(value);
                    Ids.Add(value, id);
                }
            }

            return new MyStringId(id);
        }

        public override bool Equals(object obj)
        {
            return obj is MyStringId && ((MyStringId)obj)._id == _id;
        }

        public override int GetHashCode()
        {
            return _id;
        }

        public override string ToString()
        {
            return String;
        }
    }
}
