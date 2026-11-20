using Sprocket.Host.SpaceEngineers.Framework.Game.Members;
using Sprocket.Host.SpaceEngineers.Framework.Storage.Memory;
using System;
using System.Collections.Generic;
using Lumel.Controls;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Framework.Ui.Fields
{
    /// <summary>What a color field offers the player: their paint colors, the colors they last chose in a picker, and how they like to give one.</summary>
    /// <remarks>The colors last chosen and the way of giving one are the player's settings in this world, stored with the world.</remarks>
    public sealed class ColorOffer
    {
        /// <summary>How many colors chosen last it offers: one row of the panel's swatches.</summary>
        public const int RecentCount = 8;

        readonly PlayerMemory _player;
        readonly Action _changed;

        /// <summary>
        /// An offer of <paramref name="paint" />, keeping what the player chooses with <paramref name="player" />, their settings
        /// in this world, and telling <paramref name="changed" /> of each change, where it is not null.
        /// </summary>
        public ColorOffer(IList<Hsv> paint, PlayerMemory player, Action changed)
        {
            _player = player ?? new PlayerMemory();
            _changed = changed;

            var colors = new List<Color>();
            foreach (var each in paint)
                colors.Add(each.Color);
            Paint = colors;

            Recent = new ColorHistory(RecentCount);
            Recent.SetColors(Kept(_player.RecentColors));
            Recent.Changed += Remember;
        }

        /// <summary>The colors the player paints blocks with.</summary>
        public IReadOnlyList<Color> Paint { get; }

        /// <summary>The colors last chosen, the newest first, which every picker given this offer shares.</summary>
        public ColorHistory Recent { get; }

        /// <summary>Which way a color is given: the tab shown in the picker for it.</summary>
        public ColorEntry Entry
        {
            get { return (ColorEntry)_player.ColorTab; }
            set
            {
                if (_player.ColorTab == (int)value)
                    return;

                _player.ColorTab = (int)value;
                Changed();
            }
        }

        /// <summary>The colors <paramref name="codes" /> write, each once and no more than <see cref="RecentCount" />, skipping any that is not one.</summary>
        static List<Color> Kept(IList<string> codes)
        {
            var colors = new List<Color>();
            foreach (var code in codes)
            {
                Color color;
                byte alpha;
                if (colors.Count >= RecentCount || !ColorChannels.TryRead(code, out color, out alpha))
                    continue;

                color.A = alpha;
                if (!colors.Contains(color))
                    colors.Add(color);
            }

            return colors;
        }

        /// <summary>Writes the colors last chosen into the player's settings.</summary>
        void Remember()
        {
            var codes = _player.RecentColors;
            codes.Clear();
            foreach (var color in Recent.Colors)
                codes.Add(ColorChannels.Code(color));

            Changed();
        }

        void Changed()
        {
            if (_changed != null)
                _changed();
        }
    }
}
