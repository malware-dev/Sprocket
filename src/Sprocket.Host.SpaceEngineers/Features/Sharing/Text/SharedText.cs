namespace Sprocket.Host.SpaceEngineers.Features.Sharing.Text
{
    /// <summary>One <c>[sprocket-blueprint]</c> section as read from text.</summary>
    public sealed class SharedText
    {
        /// <summary>The blueprint's identity, as the section's name gives it, or null for a plain blueprint, which carries its own.</summary>
        public System.Guid? Identity { get; set; }

        /// <summary>The name the section gives, or null where it gives none.</summary>
        public string Name { get; set; }

        /// <summary>The version of the packing the section says it was written in, or null where it says none.</summary>
        public string Version { get; set; }

        /// <summary>When the blueprint was last changed, in UTC, as the section says, or null where it says nothing.</summary>
        public System.DateTime? Changed { get; set; }

        /// <summary>The blueprint unpacked, or null where the section is of another version, has no data, or is damaged.</summary>
        public string Blueprint { get; set; }
    }
}
