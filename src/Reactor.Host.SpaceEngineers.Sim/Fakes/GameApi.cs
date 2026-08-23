// The rest of the substitute set: the handful of game members Phosphor names outside of maths.
//
// It is worth seeing how short this is. The reason it stays short is architectural rather than lucky -
// Reactor's core has no game types in it at all, and everything the editor knows about blocks arrives
// through IWorld as strings and longs. So the only game surface that ever reaches a drawing layer is the
// drawing itself, and that was already held to what an IMyTextSurface can do.

using System.Collections.Generic;
using System.Text;
using Reactor.Sim.Simulation;
using VRageMath;

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

        public string String
        {
            get { return _id >= 0 && _id < Strings.Count ? Strings[_id] : string.Empty; }
        }

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

    /// <summary>The game's spelling, misspelling included.</summary>
    public enum MyGuiDrawAlignEnum
    {
        HORISONTAL_LEFT_AND_VERTICAL_TOP,
        HORISONTAL_CENTER_AND_VERTICAL_CENTER,
        HORISONTAL_RIGHT_AND_VERTICAL_TOP
    }
}

namespace VRage.Game
{
    /// <summary>The font names the game ships. Strings, in the game too.</summary>
    public static class MyFontEnum
    {
        public const string White = "White";
        public const string Red = "Red";
        public const string Green = "Green";
        public const string Blue = "Blue";
        public const string DarkBlue = "DarkBlue";
        public const string Debug = "Debug";
        public const string Monospace = "Monospace";
    }
}

namespace Sandbox.ModAPI
{
    /// <summary>
    /// Which of the game's own layers a draw belongs to. Ordering between layers is the game's business and
    /// the simulator honours it by drawing them in this order.
    /// </summary>
    public enum MyScreenSpaceLayer
    {
        BelowHud,
        AboveHud,
        AboveEverything
    }

    public static class MyScreenSpace
    {
        public static RectangleF GetSafeGuiArea()
        {
            return ScreenSpaceDevice.Current.SafeGuiArea;
        }

        public static Vector2 NormalizedSizeToPixels(Vector2 size)
        {
            return ScreenSpaceDevice.Current.NormalizedSizeToPixels(size);
        }
    }

    public static class MyScreenSpaceText
    {
        public static void DrawPixels(string font, string text, Vector2 position, float scale, Color color,
            VRage.Utils.MyGuiDrawAlignEnum align, float maxWidth, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.DrawText(font, text, position, scale, color, align, maxWidth, layer);
        }

        public static Vector2 MeasurePixels(string font, StringBuilder text, float scale)
        {
            return ScreenSpaceDevice.Current.MeasureText(font, text, scale);
        }

        public static float GetLineHeight(string font, float scale)
        {
            return ScreenSpaceDevice.Current.LineHeight(font, scale);
        }
    }

    public static class MyScreenSpaceSprite
    {
        public static void DrawPixels(VRage.Utils.MyStringId material, Rectangle destination, Rectangle? source,
            Color color, float rotation, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.DrawSprite(material, destination, source, color, rotation, layer);
        }

        /// <summary>
        /// Assumed, not observed. Keen has said a scissor is coming and this is the shape we are betting on:
        /// ordered with the draws rather than set as state, because the buffered layers replay their queue
        /// later, and in window pixels because that is what everything else on this API speaks.
        /// <para>
        /// If the real one differs, this file and the two call sites in ScreenSpaceGraphics are the entire
        /// cost of being wrong.
        /// </para>
        /// </summary>
        public static void SetClip(Rectangle rect, MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.SetClip(rect, layer);
        }

        /// <summary>Assumed, as above.</summary>
        public static void ClearClip(MyScreenSpaceLayer layer)
        {
            ScreenSpaceDevice.Current.ClearClip(layer);
        }
    }

    public interface IMyCamera
    {
        Vector2 ViewportSize { get; }
    }

    public interface IMySession
    {
        IMyCamera Camera { get; }
    }

    /// <summary>
    /// Two members deep, and that is the whole of it. The real gateway is enormous; what reaches Phosphor
    /// is the viewport size, once a frame.
    /// </summary>
    public static class MyAPIGateway
    {
        public static IMySession Session;
    }
}
