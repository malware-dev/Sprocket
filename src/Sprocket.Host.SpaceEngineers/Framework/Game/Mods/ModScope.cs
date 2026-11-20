using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using Sprocket.ModSupport;

namespace Sprocket.Host.SpaceEngineers.Framework.Game.Mods
{
    /// <summary>A set of blocks a mod describes: of a type and subtypes, or any block a test holds of, narrowing a wider set where it has one.</summary>
    public sealed class ModScope
    {
        readonly string _blockType;
        readonly string[] _subtypes;

        /// <summary>The scope <paramref name="said" /> describes, narrowing <paramref name="wider" /> where that is not null.</summary>
        public ModScope(Dictionary<string, object> said, ModScope wider)
        {
            Wider = wider;
            _blockType = ModWire.Part<string>(said, SprocketWire.BlockType);
            _subtypes = ModWire.Part<string[]>(said, SprocketWire.Subtypes) ?? new string[0];
            Group = ModWire.Part<Func<string>>(said, SprocketWire.Group);
            SprocketsGroup = ModWire.Part<string>(said, SprocketWire.SprocketsGroup);
        }

        /// <summary>The wider scope this narrows, or null for a base.</summary>
        public ModScope Wider { get; }

        /// <summary>The mod's own group its members stand in unless they say otherwise, or null.</summary>
        public Func<string> Group { get; }

        /// <summary>The group of Sprocket's its members stand in unless they say otherwise, or null.</summary>
        public string SprocketsGroup { get; }

        /// <summary>How many scopes this narrows, 0 for a base.</summary>
        public int Depth => Wider == null ? 0 : Wider.Depth + 1;

        /// <summary>Whether <paramref name="block" /> is in the scope.</summary>
        /// <remarks>Asked as a block type is described, once per type and subtype: a scope names block types and subtypes, never single blocks.</remarks>
        public bool Claims(IMyTerminalBlock block)
        {
            if (Wider != null && !Wider.Claims(block))
                return false;

            // A base scope names its block type; one that names none, sent without the wrapper, takes no block.
            if (Wider == null && _blockType == null)
                return false;

            if (_blockType != null && block.BlockDefinition.TypeIdString != _blockType)
                return false;

            return _subtypes.Length == 0 || Array.IndexOf(_subtypes, block.BlockDefinition.SubtypeName) >= 0;
        }
    }
}
