using System;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Members
{
    /// <summary>
    /// What names a member, signal or host type in memory: the host's namespace it is in, <c>game</c> for the game's own, and
    /// its id.
    /// </summary>
    /// <remarks>
    /// Only a key a program holds has an empty namespace: a bare name as written, which means whichever one member of that id
    /// its blocks offer, in any namespace. Compared by value, ordinally, without allocating.
    /// </remarks>
    public struct MemberKey : IEquatable<MemberKey>
    {
        readonly string _namespace;
        readonly string _id;

        /// <summary>The member <paramref name="id" /> in the host's namespace <paramref name="space" />, or bare where it is empty.</summary>
        public MemberKey(string space, string id)
        {
            _namespace = space ?? "";
            _id = id ?? "";
        }

        /// <summary>The host's namespace, or empty for a bare name as written.</summary>
        public string Namespace => _namespace ?? "";

        /// <summary>The id within the namespace.</summary>
        public string Id => _id ?? "";

        /// <summary>Whether no namespace is given: a bare name as written.</summary>
        public bool IsBare => Namespace.Length == 0;

        /// <summary>Whether this names nothing at all.</summary>
        public bool IsEmpty => Id.Length == 0;

        /// <summary>The bare name <paramref name="id" />, with no namespace.</summary>
        public static MemberKey Bare(string id) => new MemberKey("", id);

        /// <summary>This key with the id <paramref name="id" /> in place of its own, in the same namespace.</summary>
        public MemberKey WithId(string id) => new MemberKey(Namespace, id);

        /// <summary>
        /// Whether this, as a program wrote it, names <paramref name="member" />, a member as its block has it: the same key, or a
        /// bare name of the same id.
        /// </summary>
        /// <remarks>A bare name matches a member of any namespace; whether it matches only one is the caller's to judge.</remarks>
        public bool Names(MemberKey member)
        {
            return IsBare ? Same(Id, member.Id) : Equals(member);
        }

        /// <inheritdoc />
        public bool Equals(MemberKey other)
        {
            return Same(Id, other.Id) && Same(Namespace, other.Namespace);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            return obj is MemberKey && Equals((MemberKey)obj);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return Namespace.GetHashCode() * 397 ^ Id.GetHashCode();
            }
        }

        /// <summary>Whether <paramref name="left" /> and <paramref name="right" /> name the same thing.</summary>
        public static bool operator ==(MemberKey left, MemberKey right) => left.Equals(right);

        /// <summary>Whether <paramref name="left" /> and <paramref name="right" /> name different things.</summary>
        public static bool operator !=(MemberKey left, MemberKey right) => !left.Equals(right);

        /// <summary>The key as a program spells it: qualified by its namespace, or bare.</summary>
        /// <remarks>Allocates; for messages and logs, never for comparing.</remarks>
        public override string ToString() => IsBare ? Id : Namespace + "." + Id;

        static bool Same(string left, string right)
        {
            return ReferenceEquals(left, right) || string.Equals(left, right, StringComparison.Ordinal);
        }
    }
}
