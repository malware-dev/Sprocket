using System;

namespace Sprocket.ModSupport
{
    /// <summary>What a mod's registration is given to tell Sprocket about its blocks and types, once Sprocket has answered.</summary>
    public sealed class SprocketModBuilder
    {
        readonly SprocketRegistration _registration;

        /// <summary>The builder filling <paramref name="registration" />.</summary>
        public SprocketModBuilder(SprocketRegistration registration)
        {
            _registration = registration;
        }

        /// <summary>
        /// The blocks of the type <paramref name="blockType" />, of <paramref name="subtypes" /> where any are given: the TypeId
        /// the block's definition has, such as <c>UpgradeModule</c>, or its object builder type, <c>MyObjectBuilder_UpgradeModule</c>.
        /// </summary>
        public SprocketBlock Block(string blockType, params string[] subtypes)
        {
            if (string.IsNullOrEmpty(blockType))
                throw new ArgumentException("Block(...) needs the blocks' type, such as UpgradeModule.");

            const string builder = "MyObjectBuilder_";
            var full = blockType.StartsWith(builder, StringComparison.Ordinal) ? blockType : builder + blockType;
            return _registration.Scope(-1).OfType(full, subtypes ?? new string[0]);
        }

        /// <summary>Defines the host type <paramref name="id" />, holding <paramref name="words" /> as programs write them.</summary>
        public SprocketType DefineType(string id, params string[] words)
        {
            return _registration.Define(new SprocketType(_registration.Namespace, Checked(id), CheckedWords(words)));
        }

        /// <summary>Defines a host type from the enum <typeparamref name="T" />: its id from the enum's name, its words from the members' names.</summary>
        public SprocketEnumType<T> DefineType<T>() where T : struct
        {
            return DefineType<T>(SprocketIds.FromName(typeof(T).Name));
        }

        /// <summary>Defines the host type <paramref name="id" /> from the enum <typeparamref name="T" />, its words from the members' names.</summary>
        public SprocketEnumType<T> DefineType<T>(string id) where T : struct
        {
            var values = (T[])Enum.GetValues(typeof(T));
            var words = new string[values.Length];
            for (var i = 0; i < values.Length; i++)
                words[i] = SprocketIds.FromName(values[i].ToString());

            return DefineType(id, words, values);
        }

        /// <summary>Defines the host type <paramref name="id" /> from the enum <typeparamref name="T" />, its words written out, one for each member in order.</summary>
        public SprocketEnumType<T> DefineType<T>(string id, params string[] words) where T : struct
        {
            var values = (T[])Enum.GetValues(typeof(T));
            if (words.Length != values.Length)
                throw new ArgumentException($"Type {id} gives {words.Length} words for the {values.Length} members of {typeof(T).Name}. Give one word per member, in order.");

            return DefineType(id, words, values);
        }

        /// <summary>The host type named <paramref name="fullName" />, defined by Sprocket or another mod, such as <c>sprocket.door_status</c>.</summary>
        /// <remarks>Found once that type is registered: a member using a type that never is, is not offered.</remarks>
        public SprocketType Type(string fullName)
        {
            var dot = string.IsNullOrEmpty(fullName) ? -1 : fullName.LastIndexOf('.');
            if (dot <= 0 || fullName == null)
                throw new ArgumentException($"Type(\"{fullName}\") needs a full name, its namespace and its id, such as sprocket.door_status.");

            return new SprocketType(fullName.Substring(0, dot), fullName.Substring(dot + 1), new string[0]);
        }

        SprocketEnumType<T> DefineType<T>(string id, string[] words, T[] values) where T : struct
        {
            var type = new SprocketEnumType<T>(_registration.Namespace, Checked(id), CheckedWords(words), values);
            _registration.Define(type);
            return type;
        }

        static string Checked(string id)
        {
            SprocketIds.Check(id, "Type id");
            return id;
        }

        static string[] CheckedWords(string[] words)
        {
            if (words == null || words.Length == 0)
                throw new ArgumentException("A type needs at least one word.");

            foreach (var word in words)
                SprocketIds.Check(word, "Type word");
            return words;
        }
    }
}
