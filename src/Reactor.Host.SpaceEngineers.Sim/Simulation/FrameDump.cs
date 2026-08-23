using System.Collections.Generic;
using System.Text;
using Phosphor;
using Sandbox.ModAPI;
using VRageMath;

namespace Reactor.Sim.Simulation
{
    /// <summary>
    /// Builds one frame with no window and writes out what was drawn.
    /// <para>
    /// The half of this simulator that does not need eyes. A frame is a list, so it can be read, diffed
    /// against yesterday's, and eventually asserted on - which is what stops a layout regression from
    /// depending on somebody happening to look at the right corner of a window.
    /// </para>
    /// </summary>
    public static class FrameDump
    {
        public static string Render(float windowWidth, float windowHeight)
        {
            var device = new ScreenSpaceDevice();
            device.Fonts = new FallbackFonts(device, "Segoe UI");
            device.Install();

            float safeWidth = windowHeight * 4f / 3f;
            device.SafeGuiArea = new RectangleF((windowWidth - safeWidth) / 2f, 0f, safeWidth, windowHeight);
            device.ViewportSize = new Vector2(windowWidth, windowHeight);
            device.BeginFrame();

            var graphics = new ScreenSpaceGraphics(MyScreenSpaceLayer.AboveHud);
            graphics.Begin();

            new EditorScene().Draw(graphics);

            var text = new StringBuilder();
            text.AppendLine("window        " + windowWidth + " x " + windowHeight);
            text.AppendLine("safe gui area " + device.SafeGuiArea);
            text.AppendLine("UI size       " + graphics.Size);
            text.AppendLine("screen bounds " + graphics.ScreenBounds);
            text.AppendLine();

            IList<DrawCommand> commands = device.Commands;
            for (int i = 0; i < commands.Count; i++)
            {
                DrawCommand command = commands[i];
                text.Append(i.ToString("000")).Append("  ").Append(command.Kind.ToString().PadRight(9)).Append("  ");

                switch (command.Kind)
                {
                    case DrawKind.SetClip:
                        text.Append(command.Clip);
                        break;
                    case DrawKind.ClearClip:
                        break;
                    case DrawKind.Sprite:
                        text.Append(command.Material.String.PadRight(22)).Append(command.Destination);
                        break;
                    case DrawKind.Text:
                        text.Append("at ").Append(command.Position)
                            .Append(" scale ").Append(command.Scale.ToString("0.###"))
                            .Append("  \"").Append(command.Text).Append('"');
                        break;
                }

                text.AppendLine();
            }

            return text.ToString();
        }
    }
}
