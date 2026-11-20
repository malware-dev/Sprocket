using System;
using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Versioning
{
    /// <summary>A release of the mod by its major and minor version, such as 1.1, which what's new is tagged with.</summary>
    public struct ModRelease : IComparable<ModRelease>, IEquatable<ModRelease>
    {
        /// <summary>The release <paramref name="major" />.<paramref name="minor" />.</summary>
        public ModRelease(int major, int minor)
        {
            Major = major;
            Minor = minor;
        }

        /// <summary>The major version.</summary>
        public int Major { get; }

        /// <summary>The minor version.</summary>
        public int Minor { get; }

        /// <summary>The release as it is written, such as <c>1.1</c>.</summary>
        /// <remarks>Persisted in the player's settings file.</remarks>
        public string Text => Major.ToString(CultureInfo.InvariantCulture) + "." + Minor.ToString(CultureInfo.InvariantCulture);

        /// <summary>Whether <paramref name="written" /> is a release, and which.</summary>
        /// <remarks>
        /// An integer alone is a release of major version 1, the form a file of release 1 holds, so
        /// <c>1</c> reads as 1.1.
        /// </remarks>
        public static bool TryRead(string written, out ModRelease release)
        {
            release = default(ModRelease);
            if (string.IsNullOrWhiteSpace(written))
                return false;

            var parts = written.Trim().Split('.');
            int major, minor;
            if (parts.Length == 1 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out minor))
            {
                release = new ModRelease(1, minor);
                return true;
            }

            if (parts.Length == 2 && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out major)
                && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minor))
            {
                release = new ModRelease(major, minor);
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        public int CompareTo(ModRelease other)
        {
            return Major != other.Major ? Major.CompareTo(other.Major) : Minor.CompareTo(other.Minor);
        }

        /// <summary>Whether <paramref name="a" /> came after <paramref name="b" />.</summary>
        public static bool operator >(ModRelease a, ModRelease b) => a.CompareTo(b) > 0;

        /// <summary>Whether <paramref name="a" /> came before <paramref name="b" />.</summary>
        public static bool operator <(ModRelease a, ModRelease b) => a.CompareTo(b) < 0;

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> are the same release.</summary>
        public static bool operator ==(ModRelease a, ModRelease b) => a.CompareTo(b) == 0;

        /// <summary>Whether <paramref name="a" /> and <paramref name="b" /> are different releases.</summary>
        public static bool operator !=(ModRelease a, ModRelease b) => a.CompareTo(b) != 0;

        /// <inheritdoc />
        public bool Equals(ModRelease other) => CompareTo(other) == 0;

        /// <inheritdoc />
        public override bool Equals(object obj) => obj is ModRelease && Equals((ModRelease)obj);

        /// <inheritdoc />
        public override int GetHashCode() => Major * 1000 + Minor;

        /// <inheritdoc />
        public override string ToString() => Text;
    }
}
