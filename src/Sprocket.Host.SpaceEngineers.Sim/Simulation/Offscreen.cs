using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Sandbox.ModAPI;
using VRageMath;
using Sprocket.Host.SpaceEngineers.Drawing;
using Sprocket.Host.SpaceEngineers.Layout;
using Sprocket.Host.SpaceEngineers.Look;
using Sprocket.Sim.Content;

namespace Sprocket.Sim.Simulation
{
    /// <summary>
    /// The editor drawn with no window and nobody watching, and what it drew.
    /// </summary>
    /// <remarks>
    /// A frame is a list. Everything that wants a screen without eyes wants the same three things put
    /// together the same way - a substitute device, the graphics that write to it, and the scene - so
    /// they are put together here once. A dump, a suite entry and a test are then the same frame read
    /// three ways rather than three arrangements that can drift apart.
    /// </remarks>
    public sealed class Offscreen
    {
        private readonly ScreenSpaceDevice _device;
        private readonly ScreenSpaceGraphics _graphics;
        private readonly EditorScene _scene;
        private readonly float _width;
        private readonly float _height;

        public Offscreen(float windowWidth, float windowHeight)
        {
            _width = windowWidth;
            _height = windowHeight;

            _device = new ScreenSpaceDevice();

            // The letters are a stand-in, because the game's own widths need the game. The monospace
            // face is not: it is in the build output, so it is measured exactly and a frame holding a
            // listing reproduces on a machine that has never had Space Engineers. It matters more for
            // that face than for any other, because a fixed-width one measured with a proportional
            // stand-in reports columns lining up when they do not.
            //
            // <b>The symbol font is deliberately left on the stand-in, and that is a known
            // disagreement.</b> The window measures it exactly and this does not, so a symbol occupies
            // a different box in a recorded frame than it does in the game - the symbol font declares a
            // line of 53 where the stand-in answers 37 for everything. Putting it right moves every
            // recorded frame that holds a symbol and changes where the back arrow sits against a
            // heading, which is a question about the heading rather than about fonts.
            string maltek = Path.Combine(AppContext.BaseDirectory, "Content", "Fonts", "Maltek");

            _device.Fonts = new FontSet(new FallbackFonts(_device, "Segoe UI"))
                .And(Lettering.Mono, Ours(maltek, "MaltekMono"));

            _device.Install();

            float safeWidth = windowHeight * 4f / 3f;
            _device.SafeArea = new RectangleF((windowWidth - safeWidth) / 2f, 0f, safeWidth, windowHeight);
            _device.ViewportSize = new Vector2(windowWidth, windowHeight);

            _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud);
            _scene = EditorScene.Nowhere();
        }

        /// <summary>One of the faces this mod ships, or null where the build did not carry it.</summary>
        IFontMetrics Ours(string folder, string name)
        {
            return SeFontMetrics.LoadFrom(
                _device, Path.Combine(folder, name + ".xml"), "the mod's own content", true);
        }

        public EditorScene Scene => _scene;

        /// <summary>The last frame's calls. Beginning a frame empties it, so this is never a history.</summary>
        public IList<DrawCommand> Commands => _device.Commands;

        /// <summary>How many times anything has asked for the width of a string. See the device.</summary>
        public int Measured => _device.Measured;

        /// <summary>How tall a line of this font stands, in the units the screen lays out in.</summary>
        public float LineHeight(string font, float scale)
        {
            return _graphics.LineHeight(font, scale);
        }

        public void Draw()
        {
            _device.BeginFrame();
            _graphics.Begin();
            _scene.Draw(_graphics);
        }

        /// <summary>Draws, then runs the script, drawing after every step.</summary>
        public void Play(Script script)
        {
            Draw();

            if (script != null)
                script.Play(_scene, Draw);
        }

        /// <summary>The frame as text, in the shape a person or a diff can read.</summary>
        public string Describe()
        {
            var text = new StringBuilder();
            text.AppendLine("window        " + Shown.Say(_width) + " x " + Shown.Say(_height));
            text.AppendLine("safe gui area " + Shown.Say(_device.SafeArea));
            text.AppendLine("UI size       " + Shown.Say(_graphics.Size));
            text.AppendLine("screen bounds " + Shown.Say(_graphics.ScreenBounds));
            text.AppendLine("on screen     " + _scene.OnScreen);
            text.AppendLine();

            IList<DrawCommand> commands = _device.Commands;

            for (int i = 0; i < commands.Count; i++)
            {
                DrawCommand command = commands[i];
                text.Append(i.ToString("000")).Append("  ").Append(command.Kind.ToString().PadRight(9)).Append("  ");

                switch (command.Kind)
                {
                    case DrawKind.SetClip:
                        text.Append(Shown.Say(command.Clip));
                        break;
                    case DrawKind.ClearClip:
                        break;
                    case DrawKind.Sprite:
                        text.Append(command.Material.String.PadRight(22))
                            .Append(Shown.Say(command.Destination))
                            .Append("  ").Append(Ink(command.Color));
                        break;
                    case DrawKind.Text:
                        text.Append("at ").Append(Shown.Say(command.Position))
                            .Append(" scale ").Append(command.Scale.ToString("0.###"))
                            .Append("  ").Append(Ink(command.Color))
                            .Append("  \"").Append(command.Text).Append('"');
                        break;
                }

                text.AppendLine();
            }

            return text.ToString();
        }

        /// <summary>A colour as eight hex digits, so a tint that changed shows up in a diff.</summary>
        private static string Ink(Color color)
        {
            return "#"
                + color.R.ToString("X2") + color.G.ToString("X2")
                + color.B.ToString("X2") + color.A.ToString("X2");
        }
    }
}
