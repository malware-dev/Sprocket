using System;
using System.Collections.Generic;
using Sprocket.Host.SpaceEngineers.Memory;
using Sprocket.Host.SpaceEngineers.World;
using VRageMath;

namespace Sprocket.Host.SpaceEngineers.Features.Fields
{
    /// <summary>What a color field offers the player: their paint colors, the colors they last chose in a picker, and how they like to give one.</summary>
    /// <remarks>The colors last chosen and the way of giving one are the player's settings in this world, kept with the world.</remarks>
    public sealed class ColorOffer
    {
        readonly PlayerMemory _player;
        readonly Action _changed;

        /// <summary>
        /// An offer of <paramref name="paint" />, keeping what the player chooses with <paramref name="player" />, their settings
        /// in this world, and telling <paramref name="changed" /> of each change, where it is not null.
        /// </summary>
        public ColorOffer(IList<Hsv> paint, PlayerMemory player, Action changed)
        {
            Paint = paint;
            _player = player ?? new PlayerMemory();
            _changed = changed;
        }

        /// <summary>The colors the player paints blocks with.</summary>
        public IList<Hsv> Paint { get; }

        /// <summary>Which way a color is given, as the place of the picker's tab for it, counted from nought.</summary>
        public int Tab
        {
            get { return _player.ColorTab; }
            set
            {
                if (_player.ColorTab == value)
                    return;

                _player.ColorTab = value;
                Changed();
            }
        }

        /// <summary>The colors last chosen, the newest first, no more than <paramref name="count" />.</summary>
        public void Recent(List<Color> into, int count)
        {
            into.Clear();
            foreach (var code in _player.RecentColors)
            {
                Color color;
                byte alpha;
                if (into.Count < count && ColorChannels.Colour(code, out color, out alpha))
                    into.Add(color);
            }
        }

        /// <summary>Puts the color written <paramref name="code" /> first among the colors last chosen, once, keeping no more than <paramref name="count" />.</summary>
        public void Chose(string code, int count)
        {
            var recent = _player.RecentColors;
            var at = recent.FindIndex(each => string.Equals(each, code, StringComparison.OrdinalIgnoreCase));
            if (at == 0)
                return;

            if (at > 0)
                recent.RemoveAt(at);

            recent.Insert(0, code);
            if (recent.Count > count)
                recent.RemoveRange(count, recent.Count - count);

            Changed();
        }

        void Changed()
        {
            if (_changed != null)
                _changed();
        }
    }
}
