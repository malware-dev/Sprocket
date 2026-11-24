using System.Globalization;

namespace Sprocket.Host.SpaceEngineers.Framework.Versioning
{
    /// <summary>The version of the mod this build is, separate from the version of the program format it writes.</summary>
    /// <remarks>
    /// A stable release is <c>major.minor.patch</c>; a Bleeding Edge build is the next release in the works with its beta
    /// count, <c>1.1.0-beta.3</c>. Raised as part of each publish, in the contents approved for it.
    /// </remarks>
    public static class ModVersion
    {
        /// <summary>The major version.</summary>
        public const int Major = 1;

        /// <summary>The minor version.</summary>
        public const int Minor = 1;

        /// <summary>The patch.</summary>
        public const int Patch = 1;

        /// <summary>The Bleeding Edge build of this version, from 1; 0 for a stable release.</summary>
        public const int Beta = 0;

        /// <summary>Whether this build is a Bleeding Edge build rather than a stable release.</summary>
        /// <remarks>Read only, not const, so the text is built the same way whichever this build is.</remarks>
        public static readonly bool IsBeta = Beta > 0;

        /// <summary>The release this build belongs to, which what's new in it is tagged with.</summary>
        public static readonly ModRelease Release = new ModRelease(Major, Minor);

        /// <summary>The version as it is written, such as <c>1.1.0-beta.3</c> or <c>1.1.0</c>.</summary>
        public static readonly string Text = string.Format(CultureInfo.InvariantCulture, "{0}.{1}.{2}", Major, Minor, Patch)
                                             + (IsBeta ? "-beta." + Beta.ToString(CultureInfo.InvariantCulture) : "");
    }
}
