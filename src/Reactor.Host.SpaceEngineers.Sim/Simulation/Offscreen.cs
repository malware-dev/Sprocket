using System.Collections.Generic;
using System.Text;
using Sandbox.ModAPI;
using VRageMath;
using Reactor.Host.SpaceEngineers.Presentation.Graphics;

namespace Reactor.Sim.Simulation
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
            _device.Fonts = new FallbackFonts(_device, "Segoe UI");
            _device.Install();

            float safeWidth = windowHeight * 4f / 3f;
            _device.SafeArea = new RectangleF((windowWidth - safeWidth) / 2f, 0f, safeWidth, windowHeight);
            _device.ViewportSize = new Vector2(windowWidth, windowHeight);

            _graphics = new ScreenSpaceGraphics(_device, MyScreenSpaceLayer.AboveHud);
            _scene = EditorScene.Nowhere();
        }

        public EditorScene Scene => _scene;

        /// <summary>The last frame's calls. Beginning a frame empties it, so this is never a history.</summary>
        public IList<DrawCommand> Commands => _device.Commands;

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
